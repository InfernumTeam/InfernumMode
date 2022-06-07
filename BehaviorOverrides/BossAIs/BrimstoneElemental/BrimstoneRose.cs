using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Events;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.BrimstoneElemental
{
	public class BrimstoneRose : ModProjectile
	{
		public Vector2 StartingVelocity;
		public ref float Time => ref projectile.ai[0];
		public bool SpawnedWhileAngry => projectile.ai[1] == 1f;
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Brimstone Rose");
			ProjectileID.Sets.TrailCacheLength[projectile.type] = 2;
			ProjectileID.Sets.TrailingMode[projectile.type] = 0;
		}

		public override void SetDefaults()
		{
			projectile.width = projectile.height = 28;
			projectile.hostile = true;
			projectile.ignoreWater = true;
			projectile.tileCollide = false;
			projectile.penetrate = -1;
			projectile.timeLeft = 60;
			cooldownSlot = 1;
		}

		public override void AI()
		{
			projectile.scale = Utils.InverseLerp(0f, 25f, Time, true);
			projectile.Opacity = (float)Math.Sqrt(projectile.scale) * Utils.InverseLerp(0f, 18f, projectile.timeLeft, true);

			// Initialize rotation.
			if (projectile.rotation == 0f)
				projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);

			Lighting.AddLight(projectile.Center, projectile.Opacity * 0.9f, 0f, 0f);

			Time++;
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			lightColor.R = (byte)(255 * projectile.Opacity);
			Utilities.DrawAfterimagesCentered(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1);
			return false;
		}

		public override void OnHitPlayer(Player target, int damage, bool crit)
		{
			if ((CalamityWorld.downedProvidence && BrimstoneElementalBehaviorOverride.ReadyToUseBuffedAI) || BossRushEvent.BossRushActive)
				target.AddBuff(ModContent.BuffType<AbyssalFlames>(), 180);
			else
				target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 120);
		}

		public override void Kill(int timeLeft)
		{
			Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
			Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 20);
			for (int dust = 0; dust < 5; dust++)
				Dust.NewDust(projectile.position + projectile.velocity, projectile.width, projectile.height, (int)CalamityDusts.Brimstone, 0f, 0f);

			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				int petalCount = CalamityWorld.downedProvidence && BrimstoneElementalBehaviorOverride.ReadyToUseBuffedAI ? 3 : 2;
				int petalDamage = CalamityWorld.downedProvidence && BrimstoneElementalBehaviorOverride.ReadyToUseBuffedAI ? 325 : 145;
				float petalShootSpeed = CalamityWorld.downedProvidence && BrimstoneElementalBehaviorOverride.ReadyToUseBuffedAI ? 13.5f : 10f;
				if (BossRushEvent.BossRushActive)
				{
					petalCount = 3;
					petalShootSpeed = 14f;
				}

				if (SpawnedWhileAngry)
				{
					petalShootSpeed *= 1.6f;
					petalCount = 5;
				}
				for (int i = 0; i < petalCount; i++)
				{
					Vector2 shootVelocity = projectile.SafeDirectionTo(target.Center).RotatedBy(MathHelper.Lerp(-0.68f, 0.68f, i / (float)petalCount)) * petalShootSpeed;
					Utilities.NewProjectileBetter(projectile.Center, shootVelocity, ModContent.ProjectileType<BrimstonePetal>(), petalDamage, 0f);
				}
			}
		}

		public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
		{
			target.Calamity().lastProjectileHit = projectile;
		}
	}
}
