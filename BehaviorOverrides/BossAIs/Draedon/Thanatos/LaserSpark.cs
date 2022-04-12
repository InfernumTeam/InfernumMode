using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Thanatos
{
    public class LaserSpark : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Laser Spark");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 18;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 360;
            Projectile.Opacity = 0f;
            Projectile.hide = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.1f, 0f, 1f);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Projectile.velocity.Length() < 14f)
                Projectile.velocity *= 1.0225f;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255, 32) * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Utilities.ProjTexture(Projectile.type);
            Vector2 origin = texture.Size() * 0.5f;

            for (int i = 0; i < 4; i++)
            {
                Vector2 drawOffset = -Projectile.velocity.SafeNormalize(Vector2.Zero) * i * 12f;
                Vector2 afterimageDrawPosition = Projectile.Center + drawOffset - Main.screenPosition;
                Color backAfterimageColor = Projectile.GetAlpha(lightColor) * ((4f - i) / 4f);
                Main.spriteBatch.Draw(texture, afterimageDrawPosition, null, backAfterimageColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }

            Color frontAfterimageColor = Projectile.GetAlpha(lightColor) * 0.27f;
            for (int i = 0; i < 9; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 9f + Projectile.rotation - MathHelper.PiOver2).ToRotationVector2() * 3f;
                Vector2 afterimageDrawPosition = Projectile.Center + drawOffset - Main.screenPosition;
                Main.spriteBatch.Draw(texture, afterimageDrawPosition, null, frontAfterimageColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }
            return false;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindProjectiles.Add(index);
        }
    }
}
