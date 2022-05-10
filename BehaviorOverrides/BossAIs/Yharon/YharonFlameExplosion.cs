using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Yharon
{
    public class YharonFlameExplosion : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Hyperthermal Explosion");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 14;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.extraUpdates = 1;
            projectile.timeLeft = projectile.MaxUpdates * 210;
            projectile.scale = 0.15f;
        }

        public override void AI()
        {
            projectile.scale += 0.16f;
            projectile.Opacity = Utils.InverseLerp(projectile.MaxUpdates * 300f, projectile.MaxUpdates * 265f, projectile.timeLeft, true) * Utils.InverseLerp(0f, projectile.MaxUpdates * 50f, projectile.timeLeft, true);
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (projectile.velocity.Length() < 18f)
                projectile.velocity *= 1.02f;

            Lighting.AddLight(projectile.Center, Color.Orange.ToVector3());
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.SetBlendState(BlendState.Additive);

            Texture2D texture = Main.projectileTexture[projectile.type];
            Color explosionColor = Color.Lerp(Color.Orange, Color.Yellow, 0.5f);
            explosionColor = Color.Lerp(explosionColor, Color.White, projectile.Opacity * 0.2f);
            explosionColor *= projectile.Opacity * 0.5f;
            Vector2 drawPosition = projectile.Center - Main.screenPosition;

            for (int i = 0; i < (int)MathHelper.Lerp(3f, 6f, projectile.Opacity); i++)
                spriteBatch.Draw(texture, drawPosition, null, explosionColor, 0f, texture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);

            spriteBatch.ResetBlendState();
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Utilities.CircularCollision(projectile.Center, targetHitbox, projectile.scale * 135f);
        }

        public override bool CanDamage() => projectile.Opacity > 0.45f;
    }
}
