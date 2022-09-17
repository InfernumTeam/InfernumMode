using CalamityMod;
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
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }
        public float ColorSpectrumHue
        {
            get => Projectile.ai[1];
            set => Projectile.ai[1] = value;
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
            Projectile.width = 72;
            Projectile.height = 72;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.scale = 0.001f;
        }

        public override void AI()
        {
            if (Time == 1f)
            {
                Projectile.scale = Main.rand.NextFloat(0.45f, 0.6f);
                Projectile.ExpandHitboxBy((int)(72 * Projectile.scale));
                ColorSpectrumHue = Main.rand.NextFloat(0f, 0.9999f);
                Projectile.netUpdate = true;
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            }
            Time++;

            ColorSpectrumHue = (ColorSpectrumHue + 1f / Lifetime) % 0.999f;
            Projectile.Opacity = Utils.GetLerpValue(0f, FadeinTime, Time, true) * Utils.GetLerpValue(Lifetime, Lifetime - FadeoutTime, Time, true);
            Projectile.scale *= 0.95f;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D sparkleTexture = ModContent.Request<Texture2D>(Texture).Value;

            Color sparkleColor = Main.hslToRgb(ColorSpectrumHue % 1f, 1f, 0.64f) * Projectile.Opacity * 0.7f;
            sparkleColor *= MathHelper.Lerp(1f, 1.6f, Utils.GetLerpValue(Lifetime * 0.5f - 15f, Lifetime * 0.5f + 15f, Time, true));
            sparkleColor.A = 0;

            Color orthogonalsparkleColor = Color.Lerp(sparkleColor, Color.White, 0.5f) * 0.5f;

            Vector2 origin = sparkleTexture.Size() / 2f;

            Vector2 sparkleScale = new Vector2(0.3f, 1f) * Projectile.Opacity * Projectile.scale;
            Vector2 orthogonalsparkleScale = new Vector2(0.3f, 2f) * Projectile.Opacity * Projectile.scale;

            Main.spriteBatch.Draw(sparkleTexture,
                             Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY,
                             null,
                             sparkleColor,
                             MathHelper.PiOver2 + Projectile.rotation,
                             origin,
                             orthogonalsparkleScale,
                             SpriteEffects.None,
                             0f);
            Main.spriteBatch.Draw(sparkleTexture,
                             Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY,
                             null,
                             sparkleColor,
                             Projectile.rotation,
                             origin,
                             sparkleScale,
                             SpriteEffects.None,
                             0f);
            Main.spriteBatch.Draw(sparkleTexture,
                             Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY,
                             null,
                             orthogonalsparkleColor,
                             MathHelper.PiOver2 + Projectile.rotation,
                             origin,
                             orthogonalsparkleScale * 0.6f,
                             SpriteEffects.None,
                             0f);
            Main.spriteBatch.Draw(sparkleTexture,
                             Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY,
                             null,
                             orthogonalsparkleColor,
                             Projectile.rotation,
                             origin,
                             sparkleScale * 0.6f,
                             SpriteEffects.None,
                             0f);
            return false;
        }
    }
}
