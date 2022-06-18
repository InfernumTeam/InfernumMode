using CalamityMod;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Cultist
{
	public class IceMass : ModProjectile
	{
		public ref float Time => ref projectile.ai[0];

		public const int ShardBurstCount = 9;

		public override void SetStaticDefaults() => DisplayName.SetDefault("Ice Mass");

		public override void SetDefaults()
		{
			projectile.width = 92;
			projectile.height = 102;
			projectile.tileCollide = false;
			projectile.ignoreWater = true;
			projectile.netImportant = true;
			projectile.hostile = true;
			projectile.timeLeft = 180;
			projectile.Opacity = 0f;
			projectile.extraUpdates = 1;
			projectile.penetrate = -1;
		}

		public override void AI()
		{
			projectile.Opacity = Utils.InverseLerp(0f, 40f, Time, true);
			projectile.rotation += MathHelper.Pi / 30f;

			if (Time >= 110)
				projectile.velocity *= 0.975f;
			Time++;
		}

		public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			// Draw telegraph lines.
			// The amount of these will create a somewhat geometric pattern.
			if (Time > 60f && Time < 170f)
			{
				float lineWidth = Utils.InverseLerp(60f, 90f, Time, true) * Utils.InverseLerp(170f, 140f, Time, true) * 2.5f + 0.2f;

				if (lineWidth > 1f)
					lineWidth += (float)Math.Sin(Main.GlobalTime * 5f) * 0.15f;

				for (int i = 0; i < ShardBurstCount; i++)
				{
					Vector2 lineDirection = (MathHelper.TwoPi * (i + 0.5f) / ShardBurstCount).ToRotationVector2();
					spriteBatch.DrawLineBetter(projectile.Center, projectile.Center + lineDirection * 5980f, Color.SkyBlue, lineWidth);
				}
			}
			return true;
		}

		public override void Kill(int timeLeft)
		{
			Main.PlaySound(SoundID.Item92, projectile.Center);
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			// Release ice shards based on the telegraphs.
			for (int i = 0; i < ShardBurstCount; i++)
			{
				for (float speed = 6f; speed <= 21f; speed += 3.3f)
				{
					Vector2 iceVelocity = (MathHelper.TwoPi * (i + 0.5f) / ShardBurstCount).ToRotationVector2() * speed * (BossRushEvent.BossRushActive ? 1.6f : 1f);
					Utilities.NewProjectileBetter(projectile.Center, iceVelocity, ModContent.ProjectileType<IceShard>(), 185, 0f);
				}
			}
		}
	}
}
