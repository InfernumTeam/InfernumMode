using CalamityMod.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Polterghast
{
	public class GhostlyRock : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Rock");
            ProjectileID.Sets.TrailingMode[projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 32;
            projectile.scale = 1.25f;
            projectile.hostile = true;
            projectile.friendly = false;
            projectile.tileCollide = false;
            projectile.timeLeft = 300;
		}

        public override void AI()
        {
            if (projectile.velocity.Length() < 15f)
                projectile.velocity *= 1.045f;

            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;
            projectile.Opacity = Utils.InverseLerp(300f, 275f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 25f, projectile.timeLeft, true);
            Lighting.AddLight(projectile.Center, Color.White.ToVector3());
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            CalamityGlobalProjectile.DrawCenteredAndAfterimage(projectile, Color.White, ProjectileID.Sets.TrailingMode[projectile.type], 2);
            return false;
        }

        public override bool CanDamage() => projectile.Opacity >= 1f;

        public override void Kill(int timeLeft)
        {
            projectile.position = projectile.Center;
            projectile.width = projectile.height = 64;
            projectile.position.X = projectile.position.X - projectile.width / 2;
            projectile.position.Y = projectile.position.Y - projectile.height / 2;
            projectile.maxPenetrate = -1;
            projectile.Damage();
        }
    }
}
