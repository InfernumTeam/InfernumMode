using CalamityMod.Items.Tools;
using CalamityMod.NPCs.AdultEidolonWyrm;
using CalamityMod.Particles;
using CalamityMod.Sounds;
using CalamityMod.World;
using InfernumMode.Common.Graphics;
using InfernumMode.Content.WorldGeneration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class ScreenCleaveTerminus : BaseAttackingTerminusProjectile, IAboveWaterProjectileDrawer
    {
        public Player Target => Main.player[Player.FindClosest(Projectile.Center, 1, 1)];

        public override int WingCount => 1;

        public Vector2 AimCenterPoint
        {
            get;
            set;
        }

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

        public static int AttackTime => 150;

        public static int ReturnToWyrmTime => 90;

        public static int Lifetime => WingGrowTime + AttackTime + ReturnToWyrmTime;

        public static NPC EidolonWyrm
        {
            get
            {
                int aewIndex = NPC.FindFirstNPC(ModContent.NPCType<AdultEidolonWyrmHead>());
                if (aewIndex == -1)
                    return null;

                return Main.npc[aewIndex];
            }
        }

        public override void AI()
        {
            // Disappear if the AEW is not present.
            /*
            if (EidolonWyrm is null)
            {
                Projectile.Kill();
                return;
            }
            */

            Time++;
            if (Time >= Lifetime - ReturnToWyrmTime)
                Time = Lifetime - AttackTime - ReturnToWyrmTime;

            if (Time < WingGrowTime)
                DoBehavior_RiseAndGrowWings();
            else if (Time < WingGrowTime + AttackTime)
                DoBehavior_AttackTarget();
            else
            {
                // Fly away.
                float time = Time - WingGrowTime - AttackTime;
                float animationCompletion = time / 35f % 1f;
                UpdateWings(WingMotionState.Flap, animationCompletion);

                Projectile.velocity.Y -= Math.Abs(Projectile.velocity.Y) * 0.06f + 0.25f;
                if (Projectile.velocity.Y < -30f)
                    Projectile.velocity.Y = -30f;
            }

            // Stay within the world.
            /*
            if (Abyss.AtLeftSideOfWorld)
                Projectile.position.X = MathHelper.Clamp(Projectile.position.X, 720f, AbyssWallWidth);
            else
                Projectile.position.X = MathHelper.Clamp(Projectile.position.X, Main.maxTilesX - AbyssWallWidth, Main.maxTilesX - 720f);
            */
            Projectile.rotation = (Projectile.position.X - Projectile.oldPosition.X) * 0.012f;
        }

        public void DoBehavior_RiseAndGrowWings()
        {
            WingsFadeInInterpolant = Utils.GetLerpValue(0f, WingGrowTime - 15f, Time, true);
            float animationCompletion = (float)Math.Pow(WingsFadeInInterpolant, 1.7);
            UpdateWings(WingMotionState.RiseUpward, animationCompletion);

            // Rise upward.
            Projectile.velocity = -Vector2.UnitY * (1f - WingsFadeInInterpolant) * 6f;
        }

        public void DoBehavior_AttackTarget()
        {
            int energyChargeTime = 50;
            int wingFlapRate = 50;
            int telegraphReleaseRate = 12;
            float time = Time - WingGrowTime;
            float hoverOffset = 1300f;

            // Make the runes fade in.
            RuneFadeInInterpolant = MathHelper.Clamp(RuneFadeInInterpolant + 0.03f, 0f, 1f);

            // Slow down and charge energy.
            if (time < energyChargeTime)
            {
                if (time == 1f)
                {
                    SoundEngine.PlaySound(CrystylCrusher.ChargeSound, Target.Center);
                    AimCenterPoint = Target.Center;
                }

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

            // Move around the target.
            if (time >= energyChargeTime)
            {
                Vector2 hoverDestination = AimCenterPoint + (MathHelper.TwoPi * (time - energyChargeTime) / (AttackTime - energyChargeTime)).ToRotationVector2() * hoverOffset;
                Projectile.Center = Vector2.Lerp(Projectile.Center, hoverDestination, 0.18f).MoveTowards(hoverDestination, 120f);

                // Release telegraphs inward.
                if (time % telegraphReleaseRate == telegraphReleaseRate - 1f)
                {
                    SoundEngine.PlaySound(CommonCalamitySounds.SwiftSliceSound, Target.Center);
                    if (Main.myPlayer == Projectile.owner)
                    {
                        float remainingTime = AttackTime - time + 16f;
                        Utilities.NewProjectileBetter(Projectile.Center, Projectile.SafeDirectionTo(AimCenterPoint), ModContent.ProjectileType<LightCleaveTelegraph>(), 0, 0f, -1, hoverOffset, remainingTime);
                    }
                }
            }

            // Flap wings.
            float animationCompletion = time / wingFlapRate % 1f;
            UpdateWings(WingMotionState.Flap, animationCompletion);
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawAboveWater(SpriteBatch spriteBatch) => DrawSelf(spriteBatch);
    }
}
