using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Golem
{
    public class GroundFireCrystal : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Fire Crystal");

        public override void SetDefaults()
        {
            projectile.width = 20;
            projectile.height = 20;
            projectile.ignoreWater = true;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.Opacity = 0f;
            projectile.timeLeft = 270;
        }

        public override void AI()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && projectile.localAI[0] == 0f)
            {
                Utilities.NewProjectileBetter(projectile.Center, projectile.velocity.SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<FistBulletTelegraph>(), 0, 0f);
                projectile.localAI[0] = 1f;
            }

            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.03f, 0f, 1f);

            if (projectile.Opacity >= 1f)
                projectile.velocity = (projectile.velocity * 1.05f).ClampMagnitude(5f, 36f);
            projectile.rotation = projectile.velocity.ToRotation();
        }

        public override bool CanDamage() => projectile.Opacity >= 1f;

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Rectangle rectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Vector2 origin = rectangle.Size() * .5f;
            Color drawColor = projectile.GetAlpha(lightColor);
            drawColor = Color.Lerp(drawColor, Color.Yellow, 0.5f);
            drawColor.A /= 7;

            spriteBatch.Draw(texture, projectile.Center - Main.screenPosition, rectangle, drawColor, projectile.rotation, origin, projectile.scale, 0, 0f);
            for (int i = 0; i < 3; i++)
            {
                Vector2 drawOffset = projectile.velocity * -i * 0.6f;
                Color afterimageColor = drawColor * (1f - i / 3f);
                spriteBatch.Draw(texture, projectile.Center + drawOffset - Main.screenPosition, rectangle, afterimageColor, projectile.rotation, origin, projectile.scale, 0, 0f);
            }
            return false;
        }
    }
}
