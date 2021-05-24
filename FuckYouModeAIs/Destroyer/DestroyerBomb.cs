using CalamityMod;
using CalamityMod.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Destroyer
{
	public class DestroyerBomb : ModProjectile
    {
		public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Explosion");
			Main.projFrames[projectile.type] = 3;
		}

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 20;
            projectile.hostile = true;
			projectile.ignoreWater = true;
            projectile.tileCollide = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 660;
        }

        public override void AI()
        {
            projectile.frameCounter++;

            // Flick with red right before death as a telegraph.
            if (projectile.frameCounter % 4 == 3)
                projectile.frame = projectile.frame == 0 ? (projectile.timeLeft < 60 ? 2 : 1) : 0;

            if (projectile.velocity.Y < 17f)
                projectile.velocity.Y += 0.2f;

			projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;

            projectile.tileCollide = projectile.timeLeft < 520;

            Tile tileAtPosition = CalamityUtils.ParanoidTileRetrieval((int)projectile.Center.X / 16, (int)projectile.Center.Y / 16);
            if (TileID.Sets.Platforms[tileAtPosition.type] && tileAtPosition.active() && projectile.tileCollide)
                projectile.Kill();

            Lighting.AddLight(projectile.Center, Vector3.One * 0.85f);
        }

		// Explode on death.
        public override void Kill(int timeLeft)
        {
            projectile.damage = 25;
            projectile.Damage();

			Main.PlaySound(SoundID.Item14, projectile.position);
            CalamityGlobalProjectile.ExpandHitboxBy(projectile, 54);

            Utils.PoofOfSmoke(projectile.Center);
		}

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;
    }
}
