using CalamityMod.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.EoW
{
	public class CursedBullet : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cursed Shot");
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 7;
        }

        public override void SetDefaults()
        {
            projectile.scale = 0.8f;
            projectile.width = projectile.height = (int)(projectile.scale * 16f);
            projectile.hostile = true;
			projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 300;
		}

        public override void AI()
        {
            projectile.rotation = projectile.velocity.ToRotation();
            if (projectile.velocity.Length() < 12f)
                projectile.velocity *= 1.04f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            CalamityGlobalProjectile.DrawCenteredAndAfterimage(projectile, Color.White, ProjectileID.Sets.TrailingMode[projectile.type], 3);
            return false;
        }
    }
}
