using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Perforators
{
	public class BloodGlob : ModProjectile
	{
		public override void SetStaticDefaults() => DisplayName.SetDefault("Blood Glob");

		public override void SetDefaults()
		{
			projectile.width = projectile.height = 28;
			projectile.hostile = true;
			projectile.tileCollide = false;
			projectile.ignoreWater = true;
			projectile.timeLeft = 420;
			projectile.penetrate = -1;
		}

		public override void AI()
		{
			projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;
			projectile.velocity.Y = MathHelper.Clamp(projectile.velocity.Y - 0.25f, -20f, 20f);
		}

		public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;

		public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<BurningBlood>(), 240);

		public override Color? GetAlpha(Color lightColor) => Color.White * projectile.Opacity;
	}
}
