using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Crabulon
{
    public class HomingSpore : ModProjectile
    {
        public float HomePower => projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Spore");
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 8;
            projectile.scale = 0.8f;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.penetrate = 1;
            projectile.timeLeft = 150;
            projectile.Opacity = 0f;
        }

        public override void AI()
        {
            HomeInOnTarget();

            Lighting.AddLight(projectile.Center, Color.CornflowerBlue.ToVector3() * projectile.Opacity * 0.5f);
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver4;

            if (projectile.timeLeft > 10f)
                projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.1f, 0f, 1f);
            else
                projectile.Opacity = MathHelper.Clamp(projectile.Opacity - 0.1f, 0f, 1f);
        }

        public void HomeInOnTarget()
        {
            float homeSpeed = MathHelper.Lerp(3.5f, 6.75f, HomePower);
            if (BossRushEvent.BossRushActive)
                homeSpeed *= 3f;

            Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
            if (projectile.timeLeft <= 105 && !projectile.WithinRange(target.Center, 55f))
                projectile.velocity = (projectile.velocity * 15f + projectile.SafeDirectionTo(target.Center) * homeSpeed) / 16f;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * projectile.Opacity;

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type]);
            return false;
        }

        public override bool CanDamage() => projectile.alpha < 20;
    }
}
