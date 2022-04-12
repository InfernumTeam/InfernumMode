using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Thanatos
{
    public class ExolaserSpark : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Exolaser Spark");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 18;
            Projectile.scale = 1.2f;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 240;
            Projectile.Opacity = 0f;
            Projectile.hide = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            if (Projectile.timeLeft < 30)
            {
                Projectile.Opacity = Projectile.timeLeft / 30f;
                Projectile.damage = 0;
                return;
            }

            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + 0.1f, 0f, 1f);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Projectile.velocity.Length() < 5f)
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

            Color frontAfterimageColor = Projectile.GetAlpha(lightColor) * 0.45f;
            frontAfterimageColor.A = 120;
            for (int i = 0; i < 7; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 7f + Projectile.rotation - MathHelper.PiOver2).ToRotationVector2() * Projectile.scale * 4f;
                Vector2 afterimageDrawPosition = Projectile.Center + drawOffset - Main.screenPosition;
                Main.spriteBatch.Draw(texture, afterimageDrawPosition, null, frontAfterimageColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }

            for (int i = 0; i < 12; i++)
            {
                Vector2 drawOffset = -Projectile.velocity.SafeNormalize(Vector2.Zero) * i * Projectile.scale * 4f;
                Vector2 afterimageDrawPosition = Projectile.Center + drawOffset - Main.screenPosition;
                Color backAfterimageColor = Projectile.GetAlpha(lightColor) * ((12f - i) / 12f);
                backAfterimageColor.A = 0;
                Main.spriteBatch.Draw(texture, afterimageDrawPosition, null, backAfterimageColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }
            return false;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindProjectiles.Add(index);
        }
    }
}
