using CalamityMod.Items.Tools;
using CalamityMod.NPCs.PrimordialWyrm;
using CalamityMod.Particles;
using CalamityMod.World;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Content.WorldGeneration;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using static CalamityMod.CalamityUtils;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class HorizontalRayTerminus : BaseAttackingTerminusProjectile, IAboveWaterProjectileDrawer
    {
        public Player Target => Main.player[Player.FindClosest(Projectile.Center, 1, 1)];

        public override int WingCount => 1;

        public ref float Time => ref Projectile.ai[0];

        public static float AbyssWallWidth => (CustomAbyss.MaxAbyssWidth - CustomAbyss.WallThickness - 36f) * 16f;

        public static float AbyssCenterX
        {
            get
            {
                float abyssCenter = AbyssWallWidth * 0.5f;
                if (!Abyss.AtLeftSideOfWorld)
                    abyssCenter = Main.maxTilesX * 16f - abyssCenter;
                return abyssCenter;
            }
        }

        public static int WingGrowTime => 90;

        public static int RedirectTime => 60;

        public static int AttackTime => 420;

        public static int ReturnToWyrmTime => 90;

        public static int Lifetime => WingGrowTime + RedirectTime + AttackTime + ReturnToWyrmTime;

        public static NPC EidolonWyrm
        {
            get
            {
                int aewIndex = NPC.FindFirstNPC(ModContent.NPCType<PrimordialWyrmHead>());
                if (aewIndex == -1)
                    return null;

                return Main.npc[aewIndex];
            }
        }

        public override void AI()
        {
            // Disappear if the AEW is not present.
            if (EidolonWyrm is null)
            {
                Projectile.Kill();
                return;
            }

            Time++;
            if (Time >= Lifetime)
                Projectile.Kill();

            if (Time < WingGrowTime)
                DoBehavior_RiseAndGrowWings();
            else if (Time < WingGrowTime + RedirectTime)
                DoBehavior_HoverToSideOfTarget();
            else if (Time < WingGrowTime + RedirectTime + AttackTime)
                DoBehavior_AttackTarget();
            else
            {
                // Fly away.
                float time = Time - WingGrowTime - RedirectTime - AttackTime;
                float animationCompletion = time / 35f % 1f;
                UpdateWings(WingMotionState.Flap, animationCompletion);

                Projectile.velocity.Y -= Math.Abs(Projectile.velocity.Y) * 0.06f + 0.25f;
                if (Projectile.velocity.Y < -30f)
                    Projectile.velocity.Y = -30f;
            }

            // Stay within the world.
            if (Abyss.AtLeftSideOfWorld)
                Projectile.position.X = Clamp(Projectile.position.X, 720f, AbyssWallWidth);
            else
                Projectile.position.X = Clamp(Projectile.position.X, Main.maxTilesX - AbyssWallWidth, Main.maxTilesX - 720f);
            Projectile.rotation = (Projectile.position.X - Projectile.oldPosition.X) * 0.012f;
        }

        public void DoBehavior_RiseAndGrowWings()
        {
            WingsFadeInInterpolant = Utils.GetLerpValue(0f, WingGrowTime - 15f, Time, true);
            float animationCompletion = Pow(WingsFadeInInterpolant, 1.7f);
            UpdateWings(WingMotionState.RiseUpward, animationCompletion);

            // Rise upward.
            Projectile.velocity = -Vector2.UnitY * (1f - WingsFadeInInterpolant) * 6f;
        }

        public void DoBehavior_HoverToSideOfTarget()
        {
            float time = Time - WingGrowTime;
            float animationCompletion = time / RedirectTime % 1f;
            UpdateWings(WingMotionState.Flap, animationCompletion);

            // Hover to the side of the target that's closest to an abyss wall.
            Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X > AbyssCenterX).ToDirectionInt() * 450f, -300f);
            Projectile.velocity = Vector2.UnitY * animationCompletion * 2.5f;
            Projectile.Center = Vector2.Lerp(Projectile.Center, hoverDestination, 0.12f);
        }

        public void DoBehavior_AttackTarget()
        {
            int energyChargeTime = 106;
            int wingFlapRate = 50;
            int attackCycleTime = 75;
            float time = Time - WingGrowTime - RedirectTime;

            // Make the runes fade in.
            RuneFadeInInterpolant = Clamp(RuneFadeInInterpolant + 0.03f, 0f, 1f);

            // Slow down and charge energy.
            if (time < energyChargeTime)
            {
                if (time == 1f)
                    SoundEngine.PlaySound(CrystylCrusher.ChargeSound, Target.Center);

                wingFlapRate = energyChargeTime / 2;
                Projectile.velocity *= 0.94f;

                // Spawn energy particles.
                Vector2 energySpawnPosition = Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(50f, 228f);
                Color energyColor = Color.Lerp(Color.Pink, Color.Yellow, Main.rand.NextFloat(0.8f));
                SquishyLightParticle energy = new(energySpawnPosition, (Projectile.Center - energySpawnPosition) * 0.07f, 1.18f, energyColor, 48, 1f, 1.2f, 3f, 0.003f);
                GeneralParticleHandler.SpawnParticle(energy);

                // Jitter in place.
                Projectile.Center += Main.rand.NextVector2CircularEdge(1.8f, 1.8f);
            }

            // Fire the funny laser.
            if (time == energyChargeTime)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.TerminusLaserbeamSound, Target.Center);

                if (Main.myPlayer == Projectile.owner)
                {
                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(deathray =>
                    {
                        deathray.ModProjectile<TerminusDeathray>().OwnerIndex = Projectile.identity;
                    });
                    Utilities.NewProjectileBetter(Projectile.Bottom, Vector2.UnitY, ModContent.ProjectileType<TerminusDeathray>(), AEWHeadBehaviorOverride.PowerfulShotDamage, 0f, Projectile.owner, 0f, AttackTime - time);
                    Projectile.netUpdate = true;
                }
            }

            // Move towards the target.
            if (time >= energyChargeTime)
            {
                wingFlapRate = 36;
                float direction = Math.Abs(Projectile.velocity.X) >= 8f ? Math.Sign(Projectile.velocity.X) : (Target.Center.X > Projectile.Center.X).ToDirectionInt();
                float wrappedAttackTimer = (time - energyChargeTime) % attackCycleTime;

                // Move horizontally.
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Vector2.UnitX * direction * 14f, 0.06f);

                // Prepare the bolt barrage.
                if (wrappedAttackTimer == attackCycleTime - 15f)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.AEWIceBurst, Projectile.Center);
                    Projectile.ai[1] = Projectile.AngleTo(Target.Center);
                    Projectile.netUpdate = true;
                }

                // Release barrages of bolts in the general direction of the target that they must evade while trying to not get eaten by the AEW.
                bool targetIsInFrontOfMe = (Target.Center.X > Projectile.Center.X).ToDirectionInt() == Math.Sign(Projectile.velocity.X);
                if (Main.myPlayer == Projectile.owner && wrappedAttackTimer >= attackCycleTime - 15f && wrappedAttackTimer % 2f == 1f && targetIsInFrontOfMe)
                {
                    int telegraphID = ModContent.ProjectileType<AEWTelegraphLine>();
                    int boltID = ModContent.ProjectileType<DivineLightBolt>();
                    float offsetAngle = Utils.Remap(wrappedAttackTimer, attackCycleTime - 15f, attackCycleTime - 1f, -0.95f, 0.95f);
                    Vector2 shootVelocity = (Projectile.ai[1] + offsetAngle).ToRotationVector2() * 3.6f;

                    int telegraph = Utilities.NewProjectileBetter(Projectile.Center, shootVelocity, telegraphID, 0, 0f, -1, 0f, 45f);
                    if (Main.projectile.IndexInRange(telegraph))
                        Main.projectile[telegraph].localAI[1] = 1f;

                    Utilities.NewProjectileBetter(Projectile.Center, shootVelocity, boltID, AEWHeadBehaviorOverride.NormalShotDamage, 0f);
                }
            }

            // Flap wings.
            float animationCompletion = time / wingFlapRate % 1f;
            UpdateWings(WingMotionState.Flap, animationCompletion);

            // Stay above the target to prevent just flying away from the laser.
            Projectile.position.Y = Lerp(Projectile.position.Y, Target.Center.Y - 250f, 0.16f);
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawAboveWater(SpriteBatch spriteBatch) => DrawSelf(spriteBatch);
    }
}
