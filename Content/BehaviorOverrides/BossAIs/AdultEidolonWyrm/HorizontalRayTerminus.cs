using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using CalamityMod.NPCs.AdultEidolonWyrm;
using InfernumMode.Common.Graphics;
using System;
using CalamityMod.Particles;
using InfernumMode.Core.GlobalInstances.Systems;
using Terraria.Audio;
using CalamityMod.Items.Tools;
using InfernumMode.Assets.Sounds;
using InfernumMode.Content.WorldGeneration;
using CalamityMod.World;
using static CalamityMod.CalamityUtils;
using Terraria.ID;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class HorizontalRayTerminus : ModProjectile, IAboveWaterProjectileDrawer
    {
        public enum WingMotionState
        {
            RiseUpward,
            Flap
        }

        public struct XerocWing
        {
            public float WingRotation
            {
                get;
                set;
            }

            public float PreviousWingRotation
            {
                get;
                set;
            }

            public float WingRotationDifferenceMovingAverage
            {
                get;
                set;
            }

            public void Update(WingMotionState motionState, float animationCompletion, float instanceRatio)
            {
                PreviousWingRotation = WingRotation;

                switch (motionState)
                {
                    case WingMotionState.RiseUpward:
                        WingRotation = (-0.6f).AngleLerp(0.36f - instanceRatio * 0.15f, animationCompletion);
                        break;
                    case WingMotionState.Flap:
                        WingRotation = PiecewiseAnimation((animationCompletion + MathHelper.Lerp(instanceRatio, 0f, 0.5f)) % 1f, Anticipation, Flap, Rest, Recovery);
                        break;
                }

                WingRotationDifferenceMovingAverage = MathHelper.Lerp(WingRotationDifferenceMovingAverage, WingRotation - PreviousWingRotation, 0.15f);
            }
        }

        public Primitive3DStrip RuneStripDrawer
        {
            get;
            set;
        }

        public Player Target => Main.player[Player.FindClosest(Projectile.Center, 1, 1)];

        public XerocWing[] Wings
        {
            get;
            set;
        } = new XerocWing[1];

        public ref float RuneFadeInInterpolant => ref Projectile.localAI[0];

        public ref float WingsFadeInInterpolant => ref Projectile.localAI[1];

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

        // Piecewise function variables for determining the angular offset of wings when flapping.
        // Positive rotations = upward flaps.
        // Negative rotations = downward flaps.
        public static CurveSegment Anticipation => new(EasingType.PolyOut, 0f, -0.4f, 0.65f, 3);

        public static CurveSegment Flap => new(EasingType.PolyIn, 0.5f, Anticipation.EndingHeight(), -1.88f, 4);

        public static CurveSegment Rest => new(EasingType.PolyIn, 0.71f, Flap.EndingHeight(), 0.59f, 3);

        public static CurveSegment Recovery => new(EasingType.PolyIn, 0.9f, Rest.EndingHeight(), -0.4f - Rest.EndingHeight(), 2);

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

        public override string Texture => "CalamityMod/Items/SummonItems/Terminus";

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.aiStyle = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 9600;
            Projectile.penetrate = -1;
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

            if (Time >= WingGrowTime + RedirectTime + AttackTime)
            {
                Projectile.timeLeft = 9600;
            }

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
                Projectile.position.X = MathHelper.Clamp(Projectile.position.X, 720f, AbyssWallWidth);
            else
                Projectile.position.X = MathHelper.Clamp(Projectile.position.X, Main.maxTilesX - AbyssWallWidth, Main.maxTilesX - 720f);
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
            int boltReleaseRate = 30;
            int boltCountPerBurst = 15;
            float time = Time - WingGrowTime - RedirectTime;

            // Make the runes fade in.
            RuneFadeInInterpolant = MathHelper.Clamp(RuneFadeInInterpolant + 0.03f, 0f, 1f);

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
                SoundEngine.PlaySound(InfernumSoundRegistry.WyrmChargeSound, Target.Center);

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
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Vector2.UnitX * direction * 14f, 0.06f);

                // Periodically release bolts.
                if (time % boltReleaseRate == boltReleaseRate - 1f)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.AEWIceBurst, Projectile.Center);
                    if (Main.myPlayer == Projectile.owner)
                    {
                        int telegraphID = ModContent.ProjectileType<AEWTelegraphLine>();
                        int boltID = ModContent.ProjectileType<DivineLightBolt>();
                        for (int i = 0; i < boltCountPerBurst; i++)
                        {
                            Vector2 shootVelocity = (MathHelper.TwoPi * i / boltCountPerBurst).ToRotationVector2() * 2f;

                            int telegraph = Utilities.NewProjectileBetter(Projectile.Center, shootVelocity, telegraphID, 0, 0f, -1, 0f, 30f);
                            if (Main.projectile.IndexInRange(telegraph))
                                Main.projectile[telegraph].localAI[1] = 1f;

                            Utilities.NewProjectileBetter(Projectile.Center, shootVelocity, boltID, AEWHeadBehaviorOverride.NormalShotDamage, 0f);
                        }
                    }
                }
            }

            // Flap wings.
            float animationCompletion = time / wingFlapRate % 1f;
            UpdateWings(WingMotionState.Flap, animationCompletion);
            Projectile.position.Y = MathHelper.Lerp(Projectile.position.Y, Target.Center.Y - 250f, 0.16f);
        }

        public void UpdateWings(WingMotionState motionState, float animationCompletion)
        {
            for (int i = 0; i < Wings.Length; i++)
            {
                float instanceRatio = i / (float)Wings.Length;
                if (Wings.Length <= 1)
                    instanceRatio = 0f;

                Wings[i].Update(motionState, animationCompletion, instanceRatio);
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawAboveWater(SpriteBatch spriteBatch)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Color baseColor = Color.White * Projectile.Opacity;
            SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            // Draw wings behind the Terminus.
            for (int i = 0; i < Wings.Length; i++)
                DrawWings(drawPosition, Wings[i].WingRotation, Wings[i].WingRotationDifferenceMovingAverage, Projectile.rotation, WingsFadeInInterpolant);

            spriteBatch.Draw(texture, drawPosition, null, baseColor, Projectile.rotation, origin, Projectile.scale, direction, 0.4f);

            spriteBatch.SetBlendState(BlendState.NonPremultiplied);
            DrawRunes();
            spriteBatch.ExitShaderRegion();
        }

        public float RuneHeightFunction(float _) => RuneFadeInInterpolant * 20f + 0.01f;

        public Color RuneColorFunction(float _) => Color.Lerp(Color.Pink, Color.Red, 0.4f) * RuneFadeInInterpolant;

        public void DrawRunes()
        {
            if (RuneFadeInInterpolant <= 0f)
                return;

            Vector2 left = Projectile.Center - Vector2.UnitX * 60f - Main.screenPosition;
            Vector2 right = Projectile.Center + Vector2.UnitX * 60f - Main.screenPosition;
            RuneStripDrawer ??= new(RuneHeightFunction, RuneColorFunction);

            Main.instance.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            RuneStripDrawer.Draw(left, right, 0.3f, 4f, Main.GlobalTimeWrappedHourly * 2f);
        }

        public static void DrawWings(Vector2 drawPosition, float wingRotation, float rotationDifferenceMovingAverage, float generalRotation, float fadeInterpolant)
        {
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Texture2D wingsTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/AdultEidolonWyrm/TerminusWing").Value;
            Vector2 leftWingOrigin = wingsTexture.Size() * new Vector2(1f, 0.86f);
            Vector2 rightWingOrigin = leftWingOrigin;
            rightWingOrigin.X = wingsTexture.Width - rightWingOrigin.X;
            Color wingsDrawColor = Color.Lerp(Color.Transparent, Color.Wheat, fadeInterpolant);
            Color wingsDrawColorWeak = Color.Lerp(Color.Transparent, Color.Red * 0.4f, fadeInterpolant);

            float squishOffset = MathHelper.Min(0.7f, Math.Abs(rotationDifferenceMovingAverage) * 3.5f);
            Vector2 scale = new Vector2(1f, 1f - squishOffset) * fadeInterpolant;
            for (int i = 4; i >= 0; i--)
            {
                Color wingColor = Color.Lerp(wingsDrawColor, wingsDrawColorWeak, i / 4f) * Utils.Remap(rotationDifferenceMovingAverage, 0f, 0.04f, 0.66f, 0.75f);
                float rotationOffset = i * MathHelper.Min(rotationDifferenceMovingAverage, 0.16f) * (1f - squishOffset) * 0.5f;
                float currentWingRotation = wingRotation + rotationOffset;

                Main.spriteBatch.Draw(wingsTexture, drawPosition, null, wingColor, generalRotation + currentWingRotation, leftWingOrigin, scale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(wingsTexture, drawPosition, null, wingColor, generalRotation - currentWingRotation, rightWingOrigin, scale, SpriteEffects.FlipHorizontally, 0f);
            }

            Main.spriteBatch.ResetBlendState();
        }


        public override void Kill(int timeLeft)
        {
        }
    }
}
