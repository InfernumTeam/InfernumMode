using CalamityMod;
using CalamityMod.Items.Weapons.Ranged;
using InfernumMode.Assets.Effects;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

namespace InfernumMode.Content.Projectiles.Rogue
{
    public class StormMaidensLightning : ModProjectile
    {
        public bool HasPlayedSound;

        public const int Lifetime = 30;
        public ref float InitialVelocityAngle => ref Projectile.ai[0];

        // Technically not a ratio, and more of a seed, but it is used in a 0-2pi squash
        // later in the code to get an arbitrary unit vector (which is then checked).
        public ref float BaseTurnAngleRatio => ref Projectile.ai[1];
        public ref float AccumulatedXMovementSpeeds => ref Projectile.localAI[0];
        public ref float BranchingIteration => ref Projectile.localAI[1];

        public override string Texture => "CalamityMod/Projectiles/LightningProj";
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Storm Maiden Lightning Bolt");
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 50;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.MaxUpdates = 7;
            Projectile.DamageType = RogueDamageClass.Instance;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = Projectile.MaxUpdates * 10;
            Projectile.timeLeft = Projectile.MaxUpdates * Lifetime;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(AccumulatedXMovementSpeeds);
            writer.Write(BranchingIteration);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            AccumulatedXMovementSpeeds = reader.ReadSingle();
            BranchingIteration = reader.ReadSingle();
        }

        public override void AI()
        {
            // FrameCounter in this context is really just an arbitrary timer
            // which allows random turning to occur.
            Projectile.frameCounter++;
            Projectile.oldPos[1] = Projectile.oldPos[0];

            // Adjust opacity and scale.
            float adjustedTimeLife = Projectile.timeLeft / Projectile.MaxUpdates;
            Projectile.Opacity = Utils.GetLerpValue(0f, 9f, adjustedTimeLife, true) * Utils.GetLerpValue(Lifetime, Lifetime - 3f, adjustedTimeLife, true);
            Projectile.scale = Projectile.Opacity;

            // Play a strike sound on the first frame.
            if (!HasPlayedSound)
            {
                SoundEngine.PlaySound(HeavenlyGale.LightningStrikeSound with { Volume = 0.3f }, Main.player[Projectile.owner].Center);
                HasPlayedSound = true;
            }

            Lighting.AddLight(Projectile.Center, Color.White.ToVector3());
            if (Projectile.frameCounter >= Projectile.extraUpdates * 2)
            {
                Projectile.frameCounter = 0;

                int turnTries = 0;
                float originalSpeed = MathF.Min(15f, Projectile.velocity.Length());
                float lightningTurnRandomnessFactor = 3.2f;
                Vector2 newBaseDirection = -Vector2.UnitY;
                Vector2 potentialBaseDirection;
                UnifiedRandom unifiedRandom = new((int)BaseTurnAngleRatio);

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
                    if (Math.Abs(potentialBaseDirection.X * (Projectile.extraUpdates + 1) * 2f * originalSpeed + AccumulatedXMovementSpeeds) > Projectile.MaxUpdates * lightningTurnRandomnessFactor)
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

        public float PrimitiveWidthFunction(float completionRatio) => CalamityUtils.Convert01To010(completionRatio) * Projectile.scale * Projectile.width;

        public Color PrimitiveColorFunction(float completionRatio)
        {
            float colorInterpolant = Sin(Projectile.identity / 3f + completionRatio * 20f + Main.GlobalTimeWrappedHourly * 3.1f) * 0.5f + 0.5f;
            Color color = CalamityUtils.MulticolorLerp(colorInterpolant, Color.Red, Color.Orange, Color.Pink, Color.IndianRed);
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
            var lightning = InfernumEffectsRegistry.GaleLightningShader;
            Main.instance.GraphicsDevice.Textures[1] = ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin").Value;
            PrimitiveRenderer.RenderTrail(Projectile.oldPos, new(PrimitiveWidthFunction, PrimitiveColorFunction, _ => Projectile.Size * 0.5f, false, Shader: lightning), 25);
            return false;
        }
    }
}
