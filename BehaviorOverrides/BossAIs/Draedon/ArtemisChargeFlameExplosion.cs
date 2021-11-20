using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
	public class ArtemisChargeFlameExplosion : ModProjectile
	{
		public ref float Identity => ref projectile.ai[0];
		public PrimitiveTrail LightningDrawer;
		public PrimitiveTrail LightningBackgroundDrawer;

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Exofire Explosion");
			Main.projFrames[projectile.type] = 5;
		}

		public override void SetDefaults()
		{
			projectile.width = 44;
			projectile.height = 44;
			projectile.hostile = true;
			projectile.ignoreWater = true;
			projectile.tileCollide = false;
			projectile.penetrate = -1;
			projectile.timeLeft = 300;
			cooldownSlot = 1;
		}

		public override void AI()
		{
			// Emit light.
			Lighting.AddLight(projectile.Center, 0.3f * projectile.Opacity, 0.3f * projectile.Opacity, 0.3f * projectile.Opacity);

			// Handle frames.
			projectile.frameCounter++;
			projectile.frame = projectile.frameCounter / 5;

			// Die once the final frame is passed.
			if (projectile.frame >= Main.projFrames[projectile.type])
				projectile.Kill();
		}

		public override bool CanHitPlayer(Player target) => projectile.Opacity == 1f;

		public override void OnHitPlayer(Player target, int damage, bool crit)
		{
			if (projectile.Opacity != 1f)
				return;

			target.AddBuff(BuffID.OnFire, 240);
		}

		public override void Kill(int timeLeft)
		{
			Main.PlaySound(SoundID.Item93, projectile.Center);

			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			// Explode into plasma sparks on death.
			for (int i = 0; i < 3; i++)
			{
				Vector2 sparkVelocity = Main.rand.NextVector2CircularEdge(10f, 10f);
				Utilities.NewProjectileBetter(projectile.Center, sparkVelocity, ModContent.ProjectileType<ExofireSpark>(), 550, 0f);
			}
		}

		public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
		{
			target.Calamity().lastProjectileHit = projectile;
		}
	}
}
