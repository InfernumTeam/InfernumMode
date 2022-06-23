using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Prime
{
    public class NukeSmoke : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Smoke");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 40;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(45f, 38f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 16f, Projectile.timeLeft, true) * 0.6f;
            Projectile.rotation = Projectile.identity % 9f / 9f * MathHelper.TwoPi;
            Projectile.rotation += MathHelper.Lerp(0.009f, 0.018f, Projectile.identity % 9f / 9f) * (Projectile.velocity.X > 0f).ToDirectionInt() * (40f - Projectile.timeLeft);
            Projectile.velocity *= 0.985f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Color particle = Color.Lerp(Color.White, Color.Yellow, Projectile.identity % 6f / 6f * 0.6f) * Projectile.Opacity;
            float scaleFactor = MathHelper.Lerp(0.4f, 0.75f, Projectile.identity % 9f / 9f) * 0.11f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.Draw(texture, drawPosition, null, particle, Projectile.rotation, texture.Size() * 0.5f, scaleFactor, SpriteEffects.None, 0f);

            Main.spriteBatch.ResetBlendState();
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Utilities.CircularCollision(Projectile.Center, targetHitbox, Projectile.scale * 295f);
        }
    }
}
