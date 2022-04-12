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
            Projectile.width = Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(300f, 285f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Projectile.velocity.Length() < 18f)
                Projectile.velocity *= 1.02f;

            Lighting.AddLight(Projectile.Center, Color.Red.ToVector3());
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Utilities.ProjTexture(Projectile.type);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Color afterimageColor = new(0.3f, 0f, 0f, 0f);

            for (int i = 0; i < 6; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 4f;
                Main.spriteBatch.Draw(texture, drawPosition + drawOffset, null, Projectile.GetAlpha(afterimageColor), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }

            Main.spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override bool? CanDamage() => Projectile.alpha < 20 ? null : false;
    }
}
