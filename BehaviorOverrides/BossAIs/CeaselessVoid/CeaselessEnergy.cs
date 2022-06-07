using CalamityMod;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.CeaselessVoid
{
	public class CeaselessEnergy : ModProjectile
	{
		public ref float Time => ref projectile.ai[0];
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Ceaseless Energy");
			ProjectileID.Sets.TrailCacheLength[projectile.type] = 2;
			ProjectileID.Sets.TrailingMode[projectile.type] = 0;
		}

		public override void SetDefaults()
		{
			projectile.width = projectile.height = 16;
			projectile.hostile = true;
			projectile.ignoreWater = true;
			projectile.tileCollide = false;
			projectile.penetrate = -1;
			projectile.timeLeft = 300;
			cooldownSlot = 1;
		}

		public override void AI()
		{
			projectile.Opacity = Utils.InverseLerp(0f, 8f, Time, true) * Utils.InverseLerp(0f, 24f, projectile.timeLeft, true);
			projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;

			// Accelerate.
			if (projectile.velocity.Length() < 23f)
				projectile.velocity *= BossRushEvent.BossRushActive ? 1.03f : 1.023f;

			Time++;
		}

		public override Color? GetAlpha(Color lightColor)
		{
			float alpha = Utils.InverseLerp(0f, 30f, Time, true);
			return new Color(1f, 1f, 1f, alpha) * projectile.Opacity * MathHelper.Lerp(0.6f, 1f, alpha);
		}

		public override bool CanDamage() => projectile.Opacity >= 1f;

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			lightColor.A = 0;
			Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1);
			return false;
		}

		public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
		{
			target.Calamity().lastProjectileHit = projectile;
		}
	}
}
