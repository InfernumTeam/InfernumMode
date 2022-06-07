using CalamityMod;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.CeaselessVoid
{
	public class MysteriousMatter : ModProjectile
	{
		public ref float Time => ref projectile.ai[0];
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Mysterious Matter");
			ProjectileID.Sets.TrailCacheLength[projectile.type] = 2;
			ProjectileID.Sets.TrailingMode[projectile.type] = 0;
		}

		public override void SetDefaults()
		{
			projectile.width = projectile.height = 26;
			projectile.hostile = true;
			projectile.ignoreWater = true;
			projectile.tileCollide = false;
			projectile.penetrate = -1;
			projectile.timeLeft = 60;
		}

		public override void AI()
		{
			projectile.Opacity = Utils.InverseLerp(0f, 22f, Time, true) * Utils.InverseLerp(0f, 22f, projectile.timeLeft, true);

			// Fire a bunch of ceasless energy at the nearest target once at the apex of the projectile's lifetime.
			if (Time == 30f)
			{
				Player closestTarget = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
				Main.PlaySound(SoundID.Item28, closestTarget.Center);

				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					int damage = CalamityGlobalNPC.DoGHead >= 0 ? 425 : 250;
					for (int i = 0; i < 3; i++)
					{
						float offsetAngle = MathHelper.Lerp(-0.63f, 0.63f, i / 2f);
						Vector2 shootVelocity = projectile.SafeDirectionTo(closestTarget.Center).RotatedByRandom(offsetAngle) * 4f;
						Utilities.NewProjectileBetter(projectile.Center, shootVelocity, ModContent.ProjectileType<CeaselessEnergy>(), damage, 0f);
					}
				}
			}

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
			Texture2D texture = Main.projectileTexture[projectile.type];
			Vector2 drawPosition = projectile.Center - Main.screenPosition;
			Vector2 origin = texture.Size() * 0.5f;
			float scale = projectile.scale;
			spriteBatch.Draw(texture, drawPosition, null, projectile.GetAlpha(lightColor), projectile.rotation, origin, scale, SpriteEffects.None, 0f);
			return false;
		}

		public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
		{
			target.Calamity().lastProjectileHit = projectile;
		}
	}
}
