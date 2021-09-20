using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Crabulon
{
    public class HomingSpore : ModProjectile
    {
		public float HomePower => projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Spore");
            Main.projFrames[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 6;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.penetrate = 1;
            projectile.timeLeft = 150;
        }

        public override void AI()
        {
			// Idly release spore dust.
			Dust fungi = Dust.NewDustPerfect(projectile.Center, 56);
			fungi.velocity = projectile.velocity + Main.rand.NextVector2Circular(0.2f, 0.2f);
			fungi.scale = 1.1f;
			fungi.color = Color.Cyan;
			fungi.noGravity = true;

			HomeInOnTarget();

			Lighting.AddLight(projectile.Center, Color.CornflowerBlue.ToVector3() * projectile.Opacity * 0.5f);
        }

		public void HomeInOnTarget()
		{
			float homeSpeed = MathHelper.Lerp(3.5f, 6.75f, HomePower);
			Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
			if (projectile.timeLeft <= 105 && !projectile.WithinRange(target.Center, 55f))
				projectile.velocity = (projectile.velocity * 15f + projectile.SafeDirectionTo(target.Center) * homeSpeed) / 16f;
		}

		public override bool CanDamage() => projectile.alpha < 20;
    }
}
