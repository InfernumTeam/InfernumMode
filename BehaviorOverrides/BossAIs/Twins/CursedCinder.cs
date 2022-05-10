using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Twins
{
    public class CursedCinder : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Cursed Cinder");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 12;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 240;
            projectile.Opacity = 0f;
            cooldownSlot = 1;
        }
        public override void AI()
        {
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.1f, 0f, 1f);

            if (projectile.velocity.Length() < 26f)
                projectile.velocity *= 1.02f;
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255, 56) * projectile.Opacity;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 origin = texture.Size() * 0.5f;

            for (int i = 0; i < 7; i++)
            {
                Vector2 drawOffset = -projectile.velocity.SafeNormalize(Vector2.Zero) * i * 7f;
                Vector2 afterimageDrawPosition = projectile.Center + drawOffset - Main.screenPosition;
                Color backAfterimageColor = projectile.GetAlpha(lightColor) * ((7f - i) / 7f);
                spriteBatch.Draw(texture, afterimageDrawPosition, null, backAfterimageColor, projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
            }

            Color frontAfterimageColor = projectile.GetAlpha(lightColor) * 0.15f;
            for (int i = 0; i < 8; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 8f + projectile.rotation - MathHelper.PiOver2).ToRotationVector2() * 4f;
                Vector2 afterimageDrawPosition = projectile.Center + drawOffset - Main.screenPosition;
                spriteBatch.Draw(texture, afterimageDrawPosition, null, frontAfterimageColor, projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
            }
            return false;
        }
    }
}
