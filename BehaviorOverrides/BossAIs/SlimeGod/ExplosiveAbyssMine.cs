using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SlimeGod
{
	public class ExplosiveAbyssMine : ModProjectile
	{
		public override string Texture => "CalamityMod/Projectiles/Boss/AbyssBallVolley";

		public override void SetStaticDefaults() => DisplayName.SetDefault("Abyss Mine");

		public override void SetDefaults()
		{
			projectile.width = projectile.height = 26;
			projectile.hostile = true;
			projectile.alpha = 60;
			projectile.penetrate = -1;
			projectile.tileCollide = false;
			projectile.timeLeft = 640;
		}

		public override void AI()
		{
			projectile.velocity *= 0.96f;
			if (projectile.ai[1] == 0f)
			{
				projectile.ai[1] = 1f;
				Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 33);
			}

			if (Main.rand.NextBool(4))
				Dust.NewDust(projectile.position + projectile.velocity, projectile.width, projectile.height, (int)CalamityDusts.PurpleCosmilite, 0f, 0f);
		}

		public override void Kill(int timeLeft)
		{
			Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 14);
			CalamityGlobalProjectile.ExpandHitboxBy(projectile, 50);

			for (int i = 0; i < 30; i++)
			{
				Dust purpleSlime = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, (int)CalamityDusts.PurpleCosmilite, 0f, 0f, 100, default, 1.2f);
				purpleSlime.velocity *= 3f;
				if (Main.rand.NextBool(2))
				{
					purpleSlime.scale = 0.5f;
					purpleSlime.fadeIn = Main.rand.NextFloat(1f, 2f);
				}
			}
			for (int i = 0; i < 60; i++)
			{
				Dust purpleSlime = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, (int)CalamityDusts.PurpleCosmilite, 0f, 0f, 100, default, 1.7f);
				purpleSlime.velocity *= 5f;

				purpleSlime = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, (int)CalamityDusts.PurpleCosmilite, 0f, 0f, 100, default, 1f);
				purpleSlime.velocity *= 2f;
			}
		}

		public override void OnHitPlayer(Player target, int damage, bool crit)
		{
			target.AddBuff(ModContent.BuffType<Shadowflame>(), 180);
			projectile.Kill();
		}
	}
}
