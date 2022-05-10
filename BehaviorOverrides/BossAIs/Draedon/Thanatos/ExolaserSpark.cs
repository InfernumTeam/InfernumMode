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
            projectile.width = projectile.height = 18;
            projectile.scale = 1.2f;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.hostile = true;
            projectile.timeLeft = 240;
            projectile.Opacity = 0f;
            projectile.hide = true;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            if (projectile.timeLeft < 30)
            {
                projectile.Opacity = projectile.timeLeft / 30f;
                projectile.damage = 0;
                return;
            }

            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.1f, 0f, 1f);
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (projectile.velocity.Length() < 5f)
                projectile.velocity *= 1.0225f;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255, 32) * projectile.Opacity;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 origin = texture.Size() * 0.5f;

            Color frontAfterimageColor = projectile.GetAlpha(lightColor) * 0.45f;
            frontAfterimageColor.A = 120;
            for (int i = 0; i < 7; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 7f + projectile.rotation - MathHelper.PiOver2).ToRotationVector2() * projectile.scale * 4f;
                Vector2 afterimageDrawPosition = projectile.Center + drawOffset - Main.screenPosition;
                Main.spriteBatch.Draw(texture, afterimageDrawPosition, null, frontAfterimageColor, projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
            }

            for (int i = 0; i < 12; i++)
            {
                Vector2 drawOffset = -projectile.velocity.SafeNormalize(Vector2.Zero) * i * projectile.scale * 4f;
                Vector2 afterimageDrawPosition = projectile.Center + drawOffset - Main.screenPosition;
                Color backAfterimageColor = projectile.GetAlpha(lightColor) * ((12f - i) / 12f);
                backAfterimageColor.A = 0;
                Main.spriteBatch.Draw(texture, afterimageDrawPosition, null, backAfterimageColor, projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
            }
            return false;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers)
        {
            behindProjectiles.Add(index);
        }
    }
}
