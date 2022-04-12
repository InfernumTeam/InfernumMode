using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class EmpressAurora : ModProjectile
    {
        public const int Lifetime = 210;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Aurora");
        }

        public override void SetDefaults()
        {
            projectile.width = 30;
            projectile.height = 30;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.friendly = false;
            projectile.timeLeft = Lifetime;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.netImportant = true;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 origin = texture.Size() / 2f;
            float timeInterpolant = Main.GlobalTime % 8f / 8f;
            Vector2 baseDrawPosition = projectile.Center - Main.screenPosition;

            int auroraCount = 25;
            float[] drawOffsetsX = new float[auroraCount];
            float[] drawOffsetsY = new float[auroraCount];
            float[] scales = new float[auroraCount];
            float[] hues = new float[auroraCount];

            float fadeOpacity = Utils.GetLerpValue(0f, 60f, projectile.timeLeft, true) * Utils.GetLerpValue(Lifetime, Lifetime - 60f, projectile.timeLeft, true);
            float dissipateOpacity = Utils.GetLerpValue(0f, 60f, projectile.timeLeft, true) * Utils.GetLerpValue(Lifetime, 90f, projectile.timeLeft, true);
            dissipateOpacity = MathHelper.Lerp(0.3f, 0.64f, dissipateOpacity);
            float widthFactorMax = 1200f / texture.Width;
            float widthFactorMin = 640f / texture.Width;
            Vector2 scaleFactor = new Vector2(3f, 6f);
            for (int i = 0; i < auroraCount; i++)
            {
                float timePulse = (float)Math.Sin(timeInterpolant * MathHelper.TwoPi + MathHelper.PiOver2 + i / 2f);
                drawOffsetsX[i] = timePulse * (300f - i * 3f);
                drawOffsetsY[i] = (float)Math.Sin(timeInterpolant * MathHelper.TwoPi * 2f + MathHelper.Pi / 3f + i) * 30f;
                drawOffsetsY[i] -= i * 3f;
                scales[i] = widthFactorMin + (i + 1) * (widthFactorMax - widthFactorMin) / auroraCount;
                scales[i] *= 0.3f;
                hues[i] = (timePulse * 0.5f + 0.5f) * 0.7f + timeInterpolant;

                Color color = Main.hslToRgb(hues[i] % 1f, 1f, 0.5f);

                if (Main.dayTime)
                    color = Main.OurFavoriteColor;

                color *= fadeOpacity * dissipateOpacity;
                color.A /= 8;

                float rotation = MathHelper.PiOver2 + timePulse * MathHelper.Pi / -20f + MathHelper.Pi * i;

                Vector2 drawPosition = baseDrawPosition + new Vector2(drawOffsetsX[i], drawOffsetsY[i]);
                spriteBatch.Draw(texture, drawPosition, null, color, rotation, origin, new Vector2(scales[i], scales[i]) * scaleFactor, 0, 0);
            }
            return false;
        }
    }
}
