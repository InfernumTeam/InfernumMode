using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.DesertScourge
{
    public class SandstormBlast2 : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sand Blast");
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = 10;
            projectile.height = 10;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.timeLeft = 360;
            projectile.alpha = 255;
        }

        public override void AI()
        {
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.125f, 0f, 1f);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;

            for (int i = 0; i < 6; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 3f;
                spriteBatch.Draw(texture, drawPosition + drawOffset, null, projectile.GetAlpha(new Color(1f, 1f, 1f, 0f)) * 0.65f, projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
            }
            spriteBatch.Draw(texture, drawPosition, null, projectile.GetAlpha(lightColor), projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
