using CalamityMod;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.BoC
{
    public class IchorSpit : ModProjectile
    {
		public ref float Time => ref projectile.ai[0];
		public bool CanHomeIn => projectile.ai[1] == 1f;
        public override void SetStaticDefaults() => DisplayName.SetDefault("Ichor");

		public override void SetDefaults()
        {
            projectile.width = projectile.height = 10;
			projectile.tileCollide = false;
			projectile.light = 0.6f;
			projectile.hostile = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 300;
        }

		public override void AI()
		{
			// Make a blood-like sound on the first frame of this projectile's existence.
			if (projectile.localAI[0] == 0f)
			{
				Main.PlaySound(SoundID.Item17, projectile.position);
				projectile.localAI[0] = 1f;
			}

			if (CanHomeIn)
			{
				// Projectiles of this kind that can home in should have a lower life time.
				if (projectile.timeLeft > 220)
					projectile.timeLeft = 220;

				// If this projectile is not close to death, home in.
				if (Time > 55f)
				{
					Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
					if (!projectile.WithinRange(target.Center, 50f))
						projectile.velocity = (projectile.velocity * 56f + projectile.SafeDirectionTo(target.Center) * 7f) / 57f;
				}
			}

			// Release ichor dust idly.
			Dust ichor = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 170, projectile.velocity.X * 0.1f, projectile.velocity.Y * 0.1f, 100, default, 1.5f);
			ichor.velocity = projectile.velocity;
			ichor.scale *= 0.6f;
			ichor.noGravity = true;

			projectile.rotation += projectile.direction * 0.3f;
			Time++;
		}

		public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;

		public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(BuffID.Ichor, 45);

		public override void Kill(int timeLeft)
		{
			// Make a sound and release some ichor dust on death.
			Main.PlaySound(SoundID.Item10, projectile.position);
			for (int i = 0; i < 6; i++)
			{
				Dust ichor = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 170, -projectile.velocity.X * 0.2f, -projectile.velocity.Y * 0.2f, 100, default, 2.5f);
				ichor.noGravity = true;
				ichor.velocity *= 2f;

				ichor = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 170, -projectile.velocity.X * 0.2f, -projectile.velocity.Y * 0.2f, 100, default, 1.2f);
				ichor.velocity *= 2f;
			}
		}
	}
}
