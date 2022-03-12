using CalamityMod.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class EmpressSparkle : ModProjectile
    {
        public float Time
        {
            get => projectile.ai[0];
            set => projectile.ai[0] = value;
        }
        public float ColorSpectrumHue
        {
            get => projectile.ai[1];
            set => projectile.ai[1] = value;
        }
        public const int Lifetime = 90;
        public const int FadeinTime = 18;
        public const int FadeoutTime = 18;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Majestic Sparkle");
        }

        public override void SetDefaults()
        {
            projectile.width = 72;
            projectile.height = 72;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = Lifetime;
            projectile.scale = 0.001f;
        }

        public override void AI()
        {
            if (Time == 1f)
            {
                projectile.scale = Main.rand.NextFloat(0.45f, 0.6f);
                CalamityGlobalProjectile.ExpandHitboxBy(projectile, (int)(72 * projectile.scale));
                ColorSpectrumHue = Main.rand.NextFloat(0f, 0.9999f);
                projectile.netUpdate = true;
                projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            }
            Time++;

            ColorSpectrumHue = (ColorSpectrumHue + 1f / Lifetime) % 0.999f;
            projectile.Opacity = Utils.InverseLerp(0f, FadeinTime, Time, true) * Utils.InverseLerp(Lifetime, Lifetime - FadeoutTime, Time, true);
            projectile.scale *= 0.95f;
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D sparkleTexture = ModContent.GetTexture(Texture);

            Color sparkleColor = Main.hslToRgb(ColorSpectrumHue % 1f, 1f, 0.64f) * projectile.Opacity * 0.7f;
            sparkleColor *= MathHelper.Lerp(1f, 1.6f, Utils.InverseLerp(Lifetime * 0.5f - 15f, Lifetime * 0.5f + 15f, Time, true));
            sparkleColor.A = 0;

            Color orthogonalsparkleColor = Color.Lerp(sparkleColor, Color.White, 0.5f) * 0.5f;

            Vector2 origin = sparkleTexture.Size() / 2f;

            Vector2 sparkleScale = new Vector2(0.3f, 1f) * projectile.Opacity * projectile.scale;
            Vector2 orthogonalsparkleScale = new Vector2(0.3f, 2f) * projectile.Opacity * projectile.scale;

            spriteBatch.Draw(sparkleTexture,
                             projectile.Center - Main.screenPosition + Vector2.UnitY * projectile.gfxOffY,
                             null,
                             sparkleColor,
                             MathHelper.PiOver2 + projectile.rotation,
                             origin,
                             orthogonalsparkleScale,
                             SpriteEffects.None,
                             0f);
            spriteBatch.Draw(sparkleTexture,
                             projectile.Center - Main.screenPosition + Vector2.UnitY * projectile.gfxOffY,
                             null,
                             sparkleColor,
                             projectile.rotation,
                             origin,
                             sparkleScale,
                             SpriteEffects.None,
                             0f);
            spriteBatch.Draw(sparkleTexture,
                             projectile.Center - Main.screenPosition + Vector2.UnitY * projectile.gfxOffY,
                             null,
                             orthogonalsparkleColor,
                             MathHelper.PiOver2 + projectile.rotation,
                             origin,
                             orthogonalsparkleScale * 0.6f,
                             SpriteEffects.None,
                             0f);
            spriteBatch.Draw(sparkleTexture,
                             projectile.Center - Main.screenPosition + Vector2.UnitY * projectile.gfxOffY,
                             null,
                             orthogonalsparkleColor,
                             projectile.rotation,
                             origin,
                             sparkleScale * 0.6f,
                             SpriteEffects.None,
                             0f);
            return false;
        }
    }
}
