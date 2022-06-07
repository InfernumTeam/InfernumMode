using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Events;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Signus
{
	public class CosmicMine : ModProjectile
	{
		public ref float Time => ref projectile.ai[0];
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Cosmic Mine");
			ProjectileID.Sets.TrailingMode[projectile.type] = 0;
			ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
		}

		public override void SetDefaults()
		{
			projectile.width = projectile.height = 34;
			projectile.hostile = true;
			projectile.tileCollide = false;
			projectile.ignoreWater = true;
			projectile.timeLeft = 60;
			projectile.penetrate = -1;
			projectile.extraUpdates = BossRushEvent.BossRushActive ? 1 : 0;
			cooldownSlot = 1;
		}
		public override void AI()
		{
			if (CalamityGlobalNPC.signus == -1)
			{
				projectile.active = false;
				return;
			}

			projectile.scale = Utils.InverseLerp(30f, 60f, Time, true) + 1f;
			Time++;
		}

		public override Color? GetAlpha(Color lightColor) => new Color(255, 255, 255, projectile.alpha);

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1);
			return false;
		}

		public override void OnHitPlayer(Player target, int damage, bool crit)
		{
			target.AddBuff(ModContent.BuffType<WhisperingDeath>(), 300);
		}

		public override void Kill(int timeLeft)
		{
			Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ProvidenceHolyBlastImpact"), projectile.Center);

			for (int i = 0; i < 50; i++)
			{
				Vector2 shootVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(18f, 55f);
				Utilities.NewProjectileBetter(projectile.Center + shootVelocity * 3f, shootVelocity, ModContent.ProjectileType<CosmicKunai>(), 250, 0f);
			}
		}

		public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
		{
			target.Calamity().lastProjectileHit = projectile;
		}
	}
}
