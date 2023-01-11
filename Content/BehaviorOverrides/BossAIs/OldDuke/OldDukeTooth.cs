using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.OldDuke
{
    public class OldDukeTooth : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Rotten Tooth");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 300;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            if (Time < 105f)
            {
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                Projectile.velocity = (Projectile.velocity * 69f + Projectile.SafeDirectionTo(target.Center) * 22f) / 70f;
            }

            Projectile.Opacity = Utils.GetLerpValue(0f, 5f, Time, true) * Utils.GetLerpValue(0f, 16f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.position + Projectile.Size * 0.5f - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Color backAfterimageColor = Projectile.GetAlpha(new Color(255, 255, 255, 0) * 0.5f);
            for (int i = 0; i < 8; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 4f;
                Main.spriteBatch.Draw(texture, drawPosition + drawOffset, null, backAfterimageColor, Projectile.rotation, origin, Projectile.scale, 0, 0f);
            }
            Main.spriteBatch.Draw(texture, drawPosition, null, Color.White * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, 0, 0f);

            return false;
        }
    }
}
