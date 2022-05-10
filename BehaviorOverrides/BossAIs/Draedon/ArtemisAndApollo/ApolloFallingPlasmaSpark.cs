using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo
{
    public class ApolloFallingPlasmaSpark : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Plasma Spark");
            Main.projFrames[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 16;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.hostile = true;
            projectile.timeLeft = 360;
            projectile.Opacity = 0f;
            projectile.hide = true;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            projectile.frameCounter++;
            if (projectile.frameCounter > 12)
            {
                projectile.frame++;
                projectile.frameCounter = 0;
            }
            if (projectile.frame >= Main.projFrames[projectile.type])
                projectile.frame = 0;

            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.1f, 0f, 1f);
            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;

            if (projectile.velocity.Y < -1f)
                projectile.velocity.Y *= 0.96f;
            else
            {
                projectile.velocity.Y += 0.3f;
                if (projectile.velocity.Y > 16f)
                    projectile.velocity.Y = 16f;
            }

            projectile.velocity.X *= 0.995f;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (projectile.Opacity != 1f)
                return;

            target.AddBuff(BuffID.CursedInferno, 120);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255, 48) * projectile.Opacity;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];
            Rectangle frame = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;

            for (int i = 0; i < 4; i++)
            {
                Vector2 drawOffset = -projectile.velocity.SafeNormalize(Vector2.Zero) * i * 12f;
                Vector2 afterimageDrawPosition = projectile.Center + drawOffset - Main.screenPosition;
                Color backAfterimageColor = projectile.GetAlpha(lightColor) * ((4f - i) / 4f);
                Main.spriteBatch.Draw(texture, afterimageDrawPosition, frame, backAfterimageColor, projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
            }

            Color frontAfterimageColor = projectile.GetAlpha(lightColor) * 0.2f;
            for (int i = 0; i < 9; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 9f + projectile.rotation - MathHelper.PiOver2).ToRotationVector2() * 2f;
                Vector2 afterimageDrawPosition = projectile.Center + drawOffset - Main.screenPosition;
                Main.spriteBatch.Draw(texture, afterimageDrawPosition, frame, frontAfterimageColor, projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
            }
            return false;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers)
        {
            behindProjectiles.Add(index);
        }
    }
}
