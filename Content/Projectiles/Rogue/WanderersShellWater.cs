using CalamityMod;
using CalamityMod.Particles;
using InfernumMode.Assets.Effects;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Rogue
{
    public class WanderersShellWater : ModProjectile
    {
        internal PrimitiveTrailCopy BeamDrawer;

        public ref float Time => ref Projectile.ai[0];

        public const int Lifetime = 48;

        public const float LaserLength = 2400f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Water Torrent");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
            Projectile.alpha = 255;
            Projectile.DamageType = RogueDamageClass.Instance;
        }

        public override void AI()
        {
            // Fade in.
            Projectile.alpha = Utils.Clamp(Projectile.alpha - 25, 0, 255);

            Projectile.scale = LumUtils.Convert01To010(Projectile.timeLeft / (float)Lifetime) * 3f;
            if (Projectile.scale > 1f)
                Projectile.scale = 1f;

            // Release bubbles on the first frame.
            if (Projectile.localAI[1] == 0f)
            {
                Color pulseColor = Main.rand.NextBool() ? Main.rand.NextBool() ? Color.SkyBlue : Color.LightSkyBlue : Main.rand.NextBool() ? Color.LightBlue : Color.DeepSkyBlue;
                var pulse = new DirectionalPulseRing(Projectile.Center, Vector2.Zero, pulseColor, Vector2.One * 1.35f, Projectile.velocity.ToRotation(), 0.05f, 0.42f, 30);
                GeneralParticleHandler.SpawnParticle(pulse);

                int numDust = 18;
                for (int i = 0; i < numDust; i++)
                {
                    Vector2 ringVelocity = (TwoPi * i / numDust).ToRotationVector2().RotatedBy(Projectile.velocity.ToRotation() + PiOver2) * 5f;
                    Dust ringDust = Dust.NewDustPerfect(Projectile.position, 211, ringVelocity, 100, default, 1.25f);
                    ringDust.noGravity = true;
                }
                Projectile.localAI[1] = 1f;
            }

            // Randomly emit bubbles.
            Vector2 bubbleSpawnPosition = Projectile.Center + Projectile.velocity * Main.rand.NextFloat(800f);
            bubbleSpawnPosition += Projectile.velocity.RotatedBy(PiOver2) * Main.rand.NextFloatDirection() * 14f;
            if (Main.rand.NextBool(3))
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int bubble = Utilities.NewProjectileBetter(bubbleSpawnPosition, Projectile.velocity * Main.rand.NextFloat(6f), ProjectileID.Bubble, 0, 0f);
                    if (Main.projectile.IndexInRange(bubble))
                    {
                        Main.projectile[bubble].MaxUpdates = 3;
                        Main.projectile[bubble].tileCollide = false;
                    }
                }
            }
            if (!Main.rand.NextBool(5))
            {
                for (int i = 0; i < 4; i++)
                {
                    Gore bubble = Gore.NewGorePerfect(Projectile.GetSource_FromAI(), bubbleSpawnPosition, Projectile.velocity * 0.2f + Main.rand.NextVector2Circular(1f, 1f) * 0.75f, 411);
                    bubble.timeLeft = Main.rand.Next(8, 14);
                    bubble.scale = Main.rand.NextFloat(0.6f, 1f) * 1.2f;
                    bubble.type = Main.rand.NextBool(3) ? 412 : 411;
                }
            }

            // Create bright light.
            DelegateMethods.v3_1 = ColorFunction(0f).ToVector3();
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, 16f, DelegateMethods.CastLight);

            Time++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            float width = Projectile.width * 0.8f;
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity * (LaserLength - 80f) * 0.65f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, width, ref _);
        }

        public float WidthFunction(float completionRatio)
        {
            float squeezeInterpolant = Utils.GetLerpValue(0f, 0.05f, completionRatio, true) * Utils.GetLerpValue(1f, 0.95f, completionRatio, true);
            float baseWidth = SmoothStep(2f, Projectile.width, squeezeInterpolant) * Clamp(Projectile.scale, 0.01f, 1f);
            return baseWidth * Lerp(1f, 2.3f, Projectile.localAI[0]);
        }

        // Prevent natural movement.
        public override bool ShouldUpdatePosition() => false;

        public Color ColorFunction(float completionRatio)
        {
            float opacty = Utils.GetLerpValue(0.92f, 0.6f, completionRatio, true) * Lerp(1f, 0.45f, Projectile.localAI[0]) * Projectile.Opacity;
            Color color = Color.Lerp(Color.DeepSkyBlue, Color.Turquoise, Math.Abs(Sin(completionRatio * Pi + Main.GlobalTimeWrappedHourly)) * 0.5f);
            return color * opacty;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            BeamDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.DukeTornadoVertexShader);

            InfernumEffectsRegistry.DukeTornadoVertexShader.SetShaderTexture(ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin"));

            List<Vector2> points = [];
            for (int i = 0; i <= 8; i++)
                points.Add(Vector2.Lerp(Projectile.Center - Projectile.velocity * 300f, Projectile.Center + Projectile.velocity * LaserLength, i / 8f));

            for (int i = 0; i < 2; i++)
            {
                BeamDrawer.Draw(points, Projectile.Size * 0.5f - Main.screenPosition, 60);
                Projectile.localAI[0] = i;
            }

            return false;
        }
    }
}
