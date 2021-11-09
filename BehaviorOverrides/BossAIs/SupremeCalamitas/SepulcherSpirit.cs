using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SepulcherSpirit : ModProjectile
    {
        public ref float TimeCountdown => ref projectile.ai[0];
        public ref float SpiritHue => ref projectile.localAI[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sepulcher Spirit");
            Main.projFrames[projectile.type] = 3;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 16;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 480;
        }

        public override void AI()
        {
            projectile.frameCounter++;
            if (projectile.frameCounter % 5 == 4)
                projectile.frame = (projectile.frame + 1) % Main.projFrames[projectile.type];
            projectile.Opacity = Utils.InverseLerp(0f, 24f, TimeCountdown, true);
            TimeCountdown--;

            // Attempt to hover above the target.
            Vector2 hoverDestination = Main.player[projectile.owner].Center + SupremeCalamitasBehaviorOverride.SepulcherSpawnOffset;
            Vector2 idealVelocity = projectile.SafeDirectionTo(hoverDestination) * 25f;
            if (!projectile.WithinRange(hoverDestination, 100f))
                projectile.velocity = (projectile.velocity * 19f + idealVelocity) / 20f;
            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;

            if (TimeCountdown <= 0f)
                projectile.Kill();
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            lightColor = Main.hslToRgb(SpiritHue, 1f, 0.5f);
            Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1, Main.projectileTexture[projectile.type], false);
            return false;
        }
    }
}
