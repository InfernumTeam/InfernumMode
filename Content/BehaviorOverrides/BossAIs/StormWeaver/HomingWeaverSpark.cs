using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.StormWeaver
{
    public class HomingWeaverSpark : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults() => DisplayName.SetDefault("Spark");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 18;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 300;
            Projectile.Opacity = 0f;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.1f, 0f, 1f);

            if (Time < 45f)
            {
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                Projectile.velocity = (Projectile.velocity * 19f + Projectile.SafeDirectionTo(target.Center) * 18.5f) / 20f;
            }
            else if (Projectile.velocity.Length() < 33f)
                Projectile.velocity *= 1.026f;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Time++;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255, 56) * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() * 0.5f;

            for (int i = 0; i < 7; i++)
            {
                Vector2 drawOffset = -Projectile.velocity.SafeNormalize(Vector2.Zero) * i * 7f;
                Vector2 afterimageDrawPosition = Projectile.Center + drawOffset - Main.screenPosition;
                Color backAfterimageColor = Projectile.GetAlpha(lightColor) * ((7f - i) / 7f);
                Main.spriteBatch.Draw(texture, afterimageDrawPosition, null, backAfterimageColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }

            Color frontAfterimageColor = Projectile.GetAlpha(lightColor) * 0.15f;
            for (int i = 0; i < 8; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 8f + Projectile.rotation - MathHelper.PiOver2).ToRotationVector2() * 4f;
                Vector2 afterimageDrawPosition = Projectile.Center + drawOffset - Main.screenPosition;
                Main.spriteBatch.Draw(texture, afterimageDrawPosition, null, frontAfterimageColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }
            return false;
        }
    }
}
