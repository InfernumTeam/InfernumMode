using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class StormLightning : ModProjectile
    {
        internal PrimitiveTrailCopy LightningDrawer;

        public static int Lifetime => 65;
        public ref float InitialVelocityAngle => ref projectile.ai[0];

        // Technically not a ratio, and more of a seed, but it is used in a 0-2pi squash
        // later in the code to get an arbitrary unit vector (which is then checked).
        public ref float BaseTurnAngleRatio => ref projectile.ai[1];
        public ref float AccumulatedXMovementSpeeds => ref projectile.localAI[0];
        public ref float BranchingIteration => ref projectile.localAI[1];

        public virtual float LightningTurnRandomnessFactor { get; } = 2f;
        public override string Texture => "CalamityMod/Projectiles/LightningProj";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Teratesla Lightning Torrent");
            ProjectileID.Sets.TrailingMode[projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 150;
        }

        public override void SetDefaults()
        {
            projectile.width = 14;
            projectile.height = 14;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.hostile = true;
            projectile.extraUpdates = 32;
            projectile.timeLeft = projectile.MaxUpdates * Lifetime;
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
            projectile.frameCounter++;
            projectile.oldPos[1] = projectile.oldPos[0];

            projectile.scale = (float)Math.Sin(MathHelper.Pi * projectile.timeLeft / (Lifetime * (projectile.MaxUpdates - 1))) * 4f;
            if (projectile.scale > 1f)
                projectile.scale = 1f;

            Lighting.AddLight(projectile.Center, Color.White.ToVector3());
            if (projectile.frameCounter >= projectile.extraUpdates * 2)
            {
                projectile.frameCounter = 0;

                float originalSpeed = MathHelper.Min(6f, projectile.velocity.Length());
                UnifiedRandom unifiedRandom = new UnifiedRandom((int)BaseTurnAngleRatio);
                int turnTries = 0;
                Vector2 newBaseDirection = -Vector2.UnitY;
                Vector2 potentialBaseDirection;

                do
                {
                    BaseTurnAngleRatio = unifiedRandom.Next() % 100;
                    potentialBaseDirection = (BaseTurnAngleRatio / 100f * MathHelper.TwoPi).ToRotationVector2();

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
                    if (Math.Abs(potentialBaseDirection.X * (projectile.extraUpdates + 1) * 2f * originalSpeed + AccumulatedXMovementSpeeds) > projectile.MaxUpdates * LightningTurnRandomnessFactor)
                    {
                        canChangeLightningDirection = false;
                    }

                    // If the above checks were all passed, redefine the base direction of the lightning.
                    if (canChangeLightningDirection)
                        newBaseDirection = potentialBaseDirection;

                    turnTries++;
                }
                while (turnTries < 100);

                if (projectile.velocity != Vector2.Zero)
                {
                    AccumulatedXMovementSpeeds += newBaseDirection.X * (projectile.extraUpdates + 1) * 2f * originalSpeed;
                    projectile.velocity = newBaseDirection.RotatedBy(InitialVelocityAngle + MathHelper.PiOver2) * originalSpeed;
                    projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
                }
            }
        }

        public float PrimitiveWidthFunction(float completionRatio) => Utils.InverseLerp(0f, 0.3f, completionRatio, true) * Utils.InverseLerp(0.93f, 0.7f, completionRatio, true) * projectile.scale * 12f;

        public Color PrimitiveColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.Cyan, Color.Blue, projectile.identity % 5f / 7f);
            return color;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            List<Vector2> checkPoints = projectile.oldPos.Where(oldPos => oldPos != Vector2.Zero).ToList();
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

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (LightningDrawer is null)
                LightningDrawer = new PrimitiveTrailCopy(PrimitiveWidthFunction, PrimitiveColorFunction, null, false, GameShaders.Misc["Infernum:AresLightningArc"]);

            GameShaders.Misc["Infernum:AresLightningArc"].UseImage("Images/Misc/Perlin");
            GameShaders.Misc["Infernum:AresLightningArc"].Apply();

            LightningDrawer.Draw(projectile.oldPos, projectile.Size * 0.5f - Main.screenPosition, 150);
            return false;
        }
    }
}
