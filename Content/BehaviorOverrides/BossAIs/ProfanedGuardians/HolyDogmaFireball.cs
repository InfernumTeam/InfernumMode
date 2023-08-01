using CalamityMod;
using CalamityMod.NPCs;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Providence;
using InfernumMode.Content.Projectiles.Wayfinder;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class HolyDogmaFireball : ModProjectile
    {
        public enum StateType
        {
            Growing,
            InitialFiring,
            Flinging,
            SlowdownMovement,
            FinalFiring
        }

        public ref float Timer => ref Projectile.ai[0];

        public StateType CurrentState
        {
            get
            {
                return (StateType)Projectile.ai[1];
            }
            set
            {
                Projectile.ai[1] = (float)value;
            }
        }

        public static NPC Commander
        {
            get
            {
                if (Main.npc.IndexInRange(CalamityGlobalNPC.doughnutBoss))
                {
                    if (Main.npc[CalamityGlobalNPC.doughnutBoss].type == GuardianComboAttackManager.CommanderType)
                        return Main.npc[CalamityGlobalNPC.doughnutBoss];
                }
                return null;
            }
        }

        public static Player Target
        {
            get
            {
                if (Main.player.IndexInRange(Commander.target))
                    return Main.player[Commander.target];
                return null;
            }
        }

        public float GrowTime = 30f;

        public int InitialLaserTelegraphTime = 60;

        public int InitialLaserShootTime = 35;

        public float FlingSpeed = 15f;

        public float BeamAmount => CurrentState is StateType.InitialFiring ? 12f : 8f;

        public override string Texture => InfernumTextureRegistry.InvisPath;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Large Fireball");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 400;
            Projectile.hostile = true;
            Projectile.Opacity = 0;
            Projectile.scale = 0;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 7000;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Commander is null)
            {
                Projectile.Kill();
                return;
            }

            float telegraphMaxAngularVelocity = ToRadians(1.2f);

            // Handle setting the telegraph opacities.

            switch (CurrentState)
            {
                case StateType.Growing:
                    Projectile.Opacity = Clamp(Projectile.Opacity + 0.1f, 0f, 1f);
                    Projectile.scale = Clamp(Projectile.scale += 1f / GrowTime, 0f, 1f);
                    if (Timer >= GrowTime)
                    {
                        CurrentState++;
                        Timer = 0;
                        return;
                    }
                    break;

                case StateType.InitialFiring:
                    if (Timer == 0)
                    {
                        SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceBurnSound with { Pitch = 0.8f, PitchVariance = 0.3f }, Projectile.Center);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 0; i < BeamAmount; i++)
                            {
                                float angularVelocity = Main.rand.NextFloat(0.65f, 1f) * Main.rand.NextFromList(-1f, 1f) * telegraphMaxAngularVelocity;
                                Vector2 laserDirection = (TwoPi * i / BeamAmount + Main.rand.NextFloatDirection() * 0.16f).ToRotationVector2();

                                ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(laser =>
                                {
                                    laser.ModProjectile<HolyMagicLaserbeam>().LaserTelegraphTime = InitialLaserTelegraphTime;
                                    laser.ModProjectile<HolyMagicLaserbeam>().LaserShootTime = InitialLaserShootTime;
                                    laser.ModProjectile<HolyMagicLaserbeam>().FromGuardians = true;
                                });
                                Utilities.NewProjectileBetter(Projectile.Center, laserDirection, ModContent.ProjectileType<HolyMagicLaserbeam>(), ProvidenceBehaviorOverride.MagicLaserbeamDamage, 0f, -1, angularVelocity);
                            }
                        }
                    }
                    else if (Timer >= InitialLaserTelegraphTime + InitialLaserShootTime)
                    {
                        CurrentState++;
                        Timer = 0;
                        return;
                    }
                    break;

                case StateType.Flinging:
                    float flingSpeedScalar = 1f;
                    float distanceToTarget = Projectile.Distance(Target.Center);
                    float minDistance = 500f;
                    float maxDistance = 1000f;
                    if (distanceToTarget > minDistance)
                    {
                        flingSpeedScalar += Clamp(Utils.GetLerpValue(500f, maxDistance, distanceToTarget, false), 0f, 2.5f);
                    }
                    Projectile.velocity = Projectile.SafeDirectionTo(Target.Center) * (FlingSpeed * flingSpeedScalar);
                    CurrentState++;
                    Timer = 0;
                    return;

                case StateType.SlowdownMovement:
                    Projectile.velocity *= 0.98f;
                    if (Projectile.velocity.Length() <= 10)
                    {
                        // Inform the commander that this has been launched.
                        Commander.Infernum().ExtraAI[GuardianComboAttackManager.CommanderDogmaFireballHasBeenYeetedIndex] = 1f;
                        CurrentState++;
                        Timer = 0;
                        return;
                    }
                    break;


                case StateType.FinalFiring:
                    Projectile.velocity *= 0.98f;
                    if (Timer == 0)
                    {

                        SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceBurnSound with { Pitch = 1.3f, PitchVariance = 0.3f }, Projectile.Center);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 1; i <= BeamAmount; i++)
                            {
                                float angularVelocity = Main.rand.NextFloat(0f, TwoPi) * Main.rand.NextFromList(-1f, 1f) * telegraphMaxAngularVelocity;
                                float setAngle = TwoPi * i / BeamAmount;
                                Vector2 laserDirection = (setAngle + Main.rand.NextFloat(0f, Pi) * Main.rand.NextFromList(-1f, 1f)).ToRotationVector2();

                                ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(laser =>
                                {
                                    laser.ModProjectile<HolyMagicLaserbeam>().LaserTelegraphTime = 90;
                                    laser.ModProjectile<HolyMagicLaserbeam>().LaserShootTime = InitialLaserShootTime;
                                    laser.ModProjectile<HolyMagicLaserbeam>().FromGuardians = true;
                                    laser.ModProjectile<HolyMagicLaserbeam>().SetAngleToMoveTo = TwoPi * i / BeamAmount;
                                });
                                Utilities.NewProjectileBetter(Projectile.Center, laserDirection, ModContent.ProjectileType<HolyMagicLaserbeam>(), ProvidenceBehaviorOverride.MagicLaserbeamDamage, 0f, -1, angularVelocity);
                            }
                        }
                    }
                    int minimumTime = InitialLaserTelegraphTime + InitialLaserShootTime;

                    if (Timer >= minimumTime && Timer <= minimumTime + GrowTime)
                        Projectile.scale = Clamp(Projectile.scale -= 1f / GrowTime, 0f, 1f);
                    else if (Projectile.scale == 0 || Timer >= minimumTime + GrowTime + 2)
                    {
                        Projectile.Kill();
                        return;
                    }
                    break;
            }

            Timer++;
        }

        // This is manually updated when needed.
        public override bool ShouldUpdatePosition() => CurrentState is StateType.Flinging or StateType.SlowdownMovement or StateType.FinalFiring;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Utils.CenteredRectangle(Projectile.Center, new Vector2(300f) * Projectile.scale).Intersects(targetHitbox);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D invis = InfernumTextureRegistry.Invisible.Value;
            Texture2D noise = InfernumTextureRegistry.HarshNoise.Value;
            Effect fireball = InfernumEffectsRegistry.FireballShader.GetShader().Shader;

            fireball.Parameters["sampleTexture2"].SetValue(noise);
            fireball.Parameters["mainColor"].SetValue(Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], 0.3f).ToVector3());
            fireball.Parameters["resolution"].SetValue(new Vector2(250f, 250f));
            fireball.Parameters["speed"].SetValue(0.76f);
            fireball.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            fireball.Parameters["zoom"].SetValue(0.0004f);
            fireball.Parameters["dist"].SetValue(60f);
            fireball.Parameters["opacity"].SetValue(Projectile.Opacity);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, fireball, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(invis, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, invis.Size() * 0.5f, 400f * Projectile.scale, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
