using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.QueenBee
{
    public class TinyBee : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults()
		{
            DisplayName.SetDefault("Hive");
            Main.projFrames[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 32;
            projectile.ignoreWater = true;
            projectile.timeLeft = 240;
            projectile.scale = 1f;
            projectile.alpha = 255;
            projectile.tileCollide = false;
            projectile.friendly = false;
            projectile.hostile = true;
        }

        public override void AI()
		{
            projectile.alpha = Utils.Clamp(projectile.alpha - 50, 0, 255);
            projectile.rotation = projectile.velocity.X * 0.15f;

            if (Time % 120f > 90f)
                projectile.velocity *= 0.95f;
            else if (Time % 120f < 30f)
			{
                Player closestPlayer = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
                projectile.velocity = (projectile.velocity * 22f + projectile.SafeDirectionTo(closestPlayer.Center) * 7f) / 23f;
			}

            projectile.frame = projectile.timeLeft / 4 % Main.projFrames[projectile.type];

            Time++;
        }

		public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(BuffID.Poisoned, 90);

        public override void Kill(int timeLeft)
		{
			Main.PlaySound(SoundID.NPCDeath1, projectile.Center);
			for (int i = 0; i < 3; i++)
			{
				Dust honey = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 147, 0f, 0f, 0, default, 1f);
				if (Main.rand.NextBool(2))
                    honey.scale *= 1.4f;
			}
        }
    }
}
