using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace InfernumMode.FuckYouModeAIs.DoG
{
    public class RealityBreakPortalBeam : ModProjectile
    {
        public Vector2 AimDestination;
        public override void SetStaticDefaults() => DisplayName.SetDefault("Portal");

        public override void SetDefaults()
        {
            projectile.width = 90;
            projectile.height = 90;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.alpha = 60;
            projectile.penetrate = -1;
            projectile.timeLeft = 150;
        }

        public override void AI()
        {
            projectile.ai[0] += 1f;
            if (Main.netMode != NetmodeID.MultiplayerClient && projectile.ai[0] == 60f)
            {
                Vector2 rayDirection = projectile.DirectionTo(AimDestination);
                Utilities.NewProjectileBetter(projectile.Center, rayDirection, ModContent.ProjectileType<DoGDeathray>(), 440, 0f, Main.myPlayer, 0f, projectile.whoAmI);
            }
            else if (projectile.ai[0] <= 45f)
            {
                Player closest = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
                AimDestination = closest.Center;
            }

            projectile.rotation -= MathHelper.TwoPi / 100f;
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if (projectile.ai[0] <= 60f)
                spriteBatch.DrawLineBetter(projectile.Center, projectile.Center + projectile.AngleTo(AimDestination).ToRotationVector2() * 5000f, Color.Cyan, 3f);

            spriteBatch.SetBlendState(BlendState.Additive);

            Texture2D portalTexture = Main.projectileTexture[projectile.type];
            Vector2 drawPosition = projectile.Center - Main.screenPosition;
            Vector2 origin = portalTexture.Size() * 0.5f;
            Color baseColor = Color.White;

            // Black portal.
            Color color = Color.Lerp(baseColor, Color.Black, 0.55f) * projectile.Opacity * 1.8f;
            spriteBatch.Draw(portalTexture, drawPosition, null, color, projectile.rotation, origin, projectile.scale * 1.2f, SpriteEffects.None, 0f);
            spriteBatch.Draw(portalTexture, drawPosition, null, color, -projectile.rotation, origin, projectile.scale * 1.2f, SpriteEffects.None, 0f);

            // Cyan portal.
            color = Color.Lerp(baseColor, Color.Cyan, 0.55f) * projectile.Opacity * 1.6f;
            spriteBatch.Draw(portalTexture, drawPosition, null, color, projectile.rotation * 0.6f, origin, projectile.scale * 1.2f, SpriteEffects.None, 0f);

            // Magenta portal.
            color = Color.Lerp(baseColor, Color.Fuchsia, 0.55f) * projectile.Opacity * 1.6f;
            spriteBatch.Draw(portalTexture, drawPosition, null, color, projectile.rotation * -0.6f, origin, projectile.scale * 1.2f, SpriteEffects.None, 0f);

            spriteBatch.ResetBlendState();
            return false;
        }
    }
}
