using CalamityMod;
using CalamityMod.DataStructures;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AquaticScourge
{
    public class AcidBubble : ModProjectile, IPixelPrimitiveDrawer, IAdditiveDrawer
    {
        public PrimitiveTrailCopy WaterDrawer;

        public ref float Time => ref Projectile.ai[0];

        public static int Lifetime => 240;

        public static float Radius => 60f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Acid Bubble");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = (int)Radius;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            Projectile.Opacity = CalamityUtils.Convert01To010(Projectile.timeLeft / (float)Lifetime) * 3.6f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;
            Projectile.scale = Projectile.Opacity * MathHelper.Lerp(0.6f, 1f, Projectile.identity * MathHelper.Pi % 1f);

            // Randomly emit bubbles.
            Vector2 bubbleSpawnPosition = Projectile.Center + Main.rand.NextVector2Circular(120f, 120f) * Projectile.scale;
            bubbleSpawnPosition += Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloatDirection() * 14f;
            if (Main.rand.NextBool(3))
            {
                for (int i = 0; i < 4; i++)
                {
                    Gore bubble = Gore.NewGorePerfect(Projectile.GetSource_FromAI(), bubbleSpawnPosition, Projectile.velocity * 0.2f + Main.rand.NextVector2Circular(1f, 1f) * 0.75f, 411);
                    bubble.timeLeft = Main.rand.Next(8, 14);
                    bubble.scale = Main.rand.NextFloat(0.5f, 0.5f);
                    bubble.type = Main.rand.NextBool(3) ? 422 : 421;
                }
            }

            Time++;
        }

        public float WidthFunction(float completionRatio) => Radius * Projectile.scale * CalamityUtils.Convert01To010(completionRatio);

        public Color ColorFunction(float completionRatio)
        {
            float colorInterpolant = (float)Math.Pow(Math.Abs(Math.Sin(completionRatio * MathHelper.Pi + Main.GlobalTimeWrappedHourly)), 3D) * 0.5f;
            return Color.Lerp(new Color(140, 234, 87), new Color(144, 114, 166), colorInterpolant) * Projectile.Opacity * 0.3f;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(InfernumSoundRegistry.BubblePop, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            WaterDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.DukeTornadoVertexShader);

            InfernumEffectsRegistry.DukeTornadoVertexShader.UseImage1("Images/Misc/Perlin");
            List<Vector2> drawPoints = new();

            for (float offsetAngle = -MathHelper.PiOver2; offsetAngle <= MathHelper.PiOver2; offsetAngle += MathHelper.Pi / 6f)
            {
                drawPoints.Clear();

                float adjustedAngle = offsetAngle + Main.GlobalTimeWrappedHourly * 2.2f;
                Vector2 offsetDirection = adjustedAngle.ToRotationVector2();
                Vector2 radius = Vector2.One * Radius;
                radius.Y *= MathHelper.Lerp(1f, 2f, (float)Math.Abs(Math.Cos(Main.GlobalTimeWrappedHourly * 1.9f)));

                for (int i = 0; i <= 8; i++)
                    drawPoints.Add(Vector2.Lerp(Projectile.Center - offsetDirection * radius * 0.8f, Projectile.Center + offsetDirection * radius * 0.8f, i / 8f));

                WaterDrawer.DrawPixelated(drawPoints, -Main.screenPosition, 30, adjustedAngle);
            }
        }

        // Draw an additive bubble overlay over the prims.
        public void AdditiveDraw(SpriteBatch spriteBatch)
        {
            Texture2D bubble = InfernumTextureRegistry.Bubble.Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color bubbleColor = Projectile.GetAlpha(Color.Lerp(Color.YellowGreen, Color.Wheat, 0.75f)) * 0.7f;
            Vector2 bubbleScale = Vector2.One * (Projectile.scale * 0.3f + (float)Math.Cos(Main.GlobalTimeWrappedHourly * 1.1f + Projectile.identity) * 0.025f);

            // Make the bubble scale squish a bit in one of the four cardinal directions for more a fluid aesthetic.
            Vector2 scalingDirection = -Vector2.UnitY.RotatedBy(Projectile.identity % 4 / 4f * MathHelper.TwoPi);
            bubbleScale += scalingDirection * (float)(Math.Cos(Main.GlobalTimeWrappedHourly * 3.1f + Projectile.identity) * 0.5f + 0.5f) * 0.07f;

            Main.EntitySpriteDraw(bubble, drawPosition, null, bubbleColor, Projectile.rotation, bubble.Size() * 0.5f, bubbleScale, 0, 0);
        }
    }
}
