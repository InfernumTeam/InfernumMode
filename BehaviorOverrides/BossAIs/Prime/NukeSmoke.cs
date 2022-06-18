using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Prime
{
    public class NukeSmoke : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Smoke");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 14;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 40;
        }

        public override void AI()
        {
            projectile.Opacity = Utils.InverseLerp(45f, 38f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 16f, projectile.timeLeft, true) * 0.6f;
            projectile.rotation = projectile.identity % 9f / 9f * MathHelper.TwoPi;
            projectile.rotation += MathHelper.Lerp(0.009f, 0.018f, projectile.identity % 9f / 9f) * (projectile.velocity.X > 0f).ToDirectionInt() * (40f - projectile.timeLeft);
            projectile.velocity *= 0.985f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.SetBlendState(BlendState.Additive);
            Texture2D texture = Main.projectileTexture[projectile.type];
            Color particle = Color.Lerp(Color.White, Color.Yellow, projectile.identity % 6f / 6f * 0.6f) * projectile.Opacity;
            float scaleFactor = MathHelper.Lerp(0.4f, 0.75f, projectile.identity % 9f / 9f) * 0.11f;
            Vector2 drawPosition = projectile.Center - Main.screenPosition;

            spriteBatch.Draw(texture, drawPosition, null, particle, projectile.rotation, texture.Size() * 0.5f, scaleFactor, SpriteEffects.None, 0f);

            spriteBatch.ResetBlendState();
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Utilities.CircularCollision(projectile.Center, targetHitbox, projectile.scale * 295f);
        }
    }
}
