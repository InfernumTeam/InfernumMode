using CalamityMod.Particles;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Common.Graphics.ScreenEffects;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasShadow
{
    public class BrimstoneLightning : ModProjectile
    {
        public bool HasReachedDestination
        {
            get;
            set;
        }

        public Vector2 Destination
        {
            get;
            set;
        }

        public const int Lifetime = 45;

        public ref float InitialVelocityAngle => ref Projectile.ai[0];

        // Technically not a ratio, and more of a seed, but it is used in a 0-2pi squash
        // later in the code to get an arbitrary unit vector (which is then checked).
        public ref float BaseTurnAngleRatio => ref Projectile.ai[1];
        public ref float AccumulatedXMovementSpeeds => ref Projectile.localAI[0];
        public ref float BranchingIteration => ref Projectile.localAI[1];

        public virtual float LightningTurnRandomnessFactor { get; } = 2f;
        public override string Texture => "CalamityMod/Projectiles/LightningProj";
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Brimstone Lightning Bolt");
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 50;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.MaxUpdates = 6;
            Projectile.timeLeft = Projectile.MaxUpdates * Lifetime;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.timeLeft);
            writer.Write(HasReachedDestination);
            writer.Write(AccumulatedXMovementSpeeds);
            writer.Write(BranchingIteration);
            writer.WriteVector2(Destination);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.timeLeft = reader.ReadInt32();
            HasReachedDestination = reader.ReadBoolean();
            AccumulatedXMovementSpeeds = reader.ReadSingle();
            BranchingIteration = reader.ReadSingle();
            Destination = reader.ReadVector2();
        }

        public override void AI()
        {
            // FrameCounter in this context is really just an arbitrary timer
            // which allows random turning to occur.
            Projectile.frameCounter++;
            Projectile.oldPos[1] = Projectile.oldPos[0];

            // Create impact effects upon reaching the destination.
            if (!HasReachedDestination && Projectile.WithinRange(Destination, 12f))
            {
                // Play a zap sound.
                SoundEngine.PlaySound(SoundID.DD2_LightningBugZap, Destination);
                for (int i = 0; i < 8; i++)
                {
                    Vector2 sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f, 11f);
                    Color sparkColor = Color.Lerp(Color.Orange, Color.IndianRed, Main.rand.NextFloat(0.4f, 1f));
                    GeneralParticleHandler.SpawnParticle(new SparkParticle(Destination, sparkVelocity, false, 60, 2f, sparkColor));

                    sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 10f);
                    Color arcColor = Color.Lerp(Color.Orange, Color.Red, Main.rand.NextFloat(0.3f, 1f));
                    GeneralParticleHandler.SpawnParticle(new ElectricArc(Destination, sparkVelocity, arcColor, 0.84f, 30));
                }

                // Do funny screen stuff.
                Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 12f;
                ScreenEffectSystem.SetBlurEffect(Destination, 0.8f, 45);

                Projectile.velocity = Vector2.Zero;
                HasReachedDestination = true;
                Projectile.timeLeft = ProjectileID.Sets.TrailCacheLength[Type] - 1;
                Projectile.netUpdate = true;
            }

            // Adjust opacity and scale.
            float adjustedTimeLife = Projectile.timeLeft / Projectile.MaxUpdates;
            Projectile.Opacity = Utils.GetLerpValue(0f, 9f, adjustedTimeLife, true) * Utils.GetLerpValue(Lifetime, Lifetime - 3f, adjustedTimeLife, true);
            Projectile.scale = Projectile.Opacity;

            Lighting.AddLight(Projectile.Center, Color.White.ToVector3());
            if (Projectile.frameCounter >= Projectile.extraUpdates * 2)
            {
                Projectile.frameCounter = 0;

                float originalSpeed = MathF.Min(15f, Projectile.velocity.Length());
                UnifiedRandom unifiedRandom = new((int)BaseTurnAngleRatio);
                int turnTries = 0;
                Vector2 newBaseDirection = -Vector2.UnitY;
                Vector2 potentialBaseDirection;

                do
                {
                    BaseTurnAngleRatio = unifiedRandom.Next() % 100;
                    potentialBaseDirection = (BaseTurnAngleRatio / 100f * TwoPi).ToRotationVector2();

                    // Ensure that the new potential direction base is always moving upwards (this is supposed to be somewhat similar to a -UnitY + RotatedBy).
                    potentialBaseDirection.Y = -Math.Abs(potentialBaseDirection.Y);

                    bool canChangeLightningDirection = true;

                    // Potential directions with very little Y speed should not be considered, because this
                    // consequentially means that the X speed would be quite large.
                    if (potentialBaseDirection.Y > -0.02f)
                        canChangeLightningDirection = false;

                    // This mess of math basically encourages movement at the ends of an extraUpdate cycle,
                    // discourages super frequenent randomness as the accumulated X speed changes get larger,
                    // or if the original speed is quite large.
                    if (Math.Abs(potentialBaseDirection.X * (Projectile.extraUpdates + 1) * 2f * originalSpeed + AccumulatedXMovementSpeeds) > Projectile.MaxUpdates * LightningTurnRandomnessFactor)
                        canChangeLightningDirection = false;

                    // If the above checks were all passed, redefine the base direction of the lightning.
                    if (canChangeLightningDirection)
                        newBaseDirection = potentialBaseDirection;

                    turnTries++;
                }
                while (turnTries < 100);

                if (Projectile.velocity != Vector2.Zero)
                {
                    AccumulatedXMovementSpeeds += newBaseDirection.X * (Projectile.extraUpdates + 1) * 2f * originalSpeed;
                    Projectile.velocity = newBaseDirection.RotatedBy(InitialVelocityAngle + PiOver2) * originalSpeed;
                    Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;
                }
            }
        }

        public float PrimitiveWidthFunction(float completionRatio) => LumUtils.Convert01To010(completionRatio) * Projectile.scale * Projectile.width;

        public Color PrimitiveColorFunction(float completionRatio)
        {
            float colorInterpolant = Sin(Projectile.identity / 3f + completionRatio * 20f + Main.GlobalTimeWrappedHourly * 1.1f) * 0.5f + 0.5f;
            Color color = LumUtils.MulticolorLerp(colorInterpolant, Color.Red, Color.Yellow, Color.Pink);
            return color;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            List<Vector2> checkPoints = Projectile.oldPos.Where(oldPos => oldPos != Vector2.Zero).ToList();
            if (checkPoints.Count <= 2)
                return false;

            for (int i = 0; i < checkPoints.Count - 1; i++)
            {
                float _ = 0f;
                float width = PrimitiveWidthFunction(i / (float)checkPoints.Count);
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), checkPoints[i], checkPoints[i + 1], width * 0.8f, ref _))
                    return true;
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            GameShaders.Misc["CalamityMod:HeavenlyGaleLightningArc"].UseImage1("Images/Misc/Perlin");
            GameShaders.Misc["CalamityMod:HeavenlyGaleLightningArc"].Apply();

            PrimitiveRenderer.RenderTrail(Projectile.oldPos, new(PrimitiveWidthFunction, PrimitiveColorFunction, _ => Projectile.Size * 0.5f, false, Shader: GameShaders.Misc["CalamityMod:HeavenlyGaleLightningArc"]), 18);
            return false;
        }
    }
}
