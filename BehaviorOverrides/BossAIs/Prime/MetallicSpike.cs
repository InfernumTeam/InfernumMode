using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Prime
{
    public class MetallicSpike : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Metal Spike");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 14;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 300;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(300f, 285f, projectile.timeLeft, true);
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (projectile.velocity.Length() < 18f)
                projectile.velocity *= 1.02f;

            Lighting.AddLight(projectile.Center, Color.Red.ToVector3());
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Color afterimageColor = new Color(0.3f, 0f, 0f, 0f);

            for (int i = 0; i < 6; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 4f;
                spriteBatch.Draw(texture, drawPosition + drawOffset, null, projectile.GetAlpha(afterimageColor), projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
            }

            spriteBatch.Draw(texture, drawPosition, null, projectile.GetAlpha(lightColor), projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override bool CanDamage() => projectile.alpha < 20;
    }
}
