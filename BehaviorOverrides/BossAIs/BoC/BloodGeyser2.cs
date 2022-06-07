using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.BoC
{
	public class BloodGeyser2 : ModProjectile
	{
		internal const float Gravity = 0.25f;
		public override void SetStaticDefaults() => DisplayName.SetDefault("Ichor Geyser");

		public override void SetDefaults()
		{
			projectile.width = projectile.height = 12;
			projectile.hostile = true;
			projectile.tileCollide = false;
			projectile.timeLeft = 420;
			projectile.penetrate = 1;
		}

		public override void AI()
		{
			projectile.tileCollide = projectile.timeLeft < 220;
			projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;

			// Release blood idly.
			Dust blood = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, DustID.Blood, 0f, 0f, 100, default, 0.5f);
			blood.velocity = Vector2.Zero;
			blood.noGravity = true;

			projectile.velocity.Y += Gravity;
		}

		public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;

		public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<BurningBlood>(), 120);

		public override Color? GetAlpha(Color lightColor) => new Color(246, 195, 80, projectile.alpha);
	}
}
