using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Yharon
{
    public class ProvSummonFlameExplosion : ModProjectile
    {
        public override string Texture => "InfernumMode/BehaviorOverrides/BossAIs/Yharon/YharonFlameExplosion";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Hyperthermal Explosion");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = Projectile.MaxUpdates * 120;
            Projectile.scale = 0.1f;
        }

        public override void AI()
        {
            Projectile.scale += 0.06f;
            Projectile.Opacity = Utils.GetLerpValue(Projectile.MaxUpdates * 150f, Projectile.MaxUpdates * 132f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, Projectile.MaxUpdates * 50f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Projectile.velocity.Length() < 18f)
                Projectile.velocity *= 1.02f;

            Lighting.AddLight(Projectile.Center, Color.Orange.ToVector3());
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Texture2D texture = Utilities.ProjTexture(Projectile.type);
            Color explosionColor = Color.Lerp(Color.Orange, Color.Yellow, 0.5f);
            explosionColor = Color.Lerp(explosionColor, Color.White, Projectile.Opacity * 0.2f);
            explosionColor *= Projectile.Opacity * 0.7f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            for (int i = 0; i < (int)MathHelper.Lerp(3f, 6f, Projectile.Opacity); i++)
                Main.spriteBatch.Draw(texture, drawPosition, null, explosionColor, 0f, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);

            Main.spriteBatch.ResetBlendState();
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Utilities.CircularCollision(Projectile.Center, targetHitbox, Projectile.scale * 135f);
        }

        public override bool? CanDamage() => Projectile.Opacity > 0.45f ? null : false;
    }
}
