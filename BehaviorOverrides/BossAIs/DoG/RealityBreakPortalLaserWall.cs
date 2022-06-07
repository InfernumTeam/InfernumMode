using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
	public class RealityBreakPortalLaserWall : ModProjectile
	{
		public ref float Time => ref projectile.ai[0];
		public override void SetStaticDefaults() => DisplayName.SetDefault("Portal");

		public override void SetDefaults()
		{
			projectile.width = 90;
			projectile.height = 90;
			projectile.ignoreWater = true;
			projectile.tileCollide = false;
			projectile.alpha = 255;
			projectile.penetrate = -1;
			projectile.timeLeft = 100;
		}

		public override void AI()
		{
			projectile.rotation += 0.325f;

			Time++;

			// Release the laser burst a second after spawning.
			if (Time == 60f)
			{
				Main.PlaySound(SoundID.Item12, projectile.Center);

				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
					float shootInterpolant = Utils.InverseLerp(600f, 1450f, projectile.Distance(target.Center), true);

					int laserCount = (int)MathHelper.Lerp(5f, 12f, shootInterpolant);
					float shootSpeed = MathHelper.Lerp(15f, 25f, shootInterpolant);
					for (int i = 0; i < laserCount; i++)
					{
						Vector2 shootVelocity = projectile.SafeDirectionTo(target.Center).RotatedBy(MathHelper.Lerp(-0.6f, 0.6f, i / (float)(laserCount - 1f))) * shootSpeed;
						Projectile.NewProjectile(projectile.Center, shootVelocity, InfernumMode.CalamityMod.ProjectileType("DoGDeath"), 85, 0f, projectile.owner);
					}
				}
			}

			projectile.Opacity = Utils.InverseLerp(0f, 50f, Time, true) * Utils.InverseLerp(0f, 30f, projectile.timeLeft, true);
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			spriteBatch.SetBlendState(BlendState.Additive);

			Texture2D portalTexture = Main.projectileTexture[projectile.type];
			Vector2 drawPosition = projectile.Center - Main.screenPosition;
			Vector2 origin = portalTexture.Size() * 0.5f;
			Color baseColor = Color.White;

			// Black portal.
			Color color = Color.Lerp(baseColor, Color.Black, 0.55f) * projectile.Opacity * 1.8f;
			spriteBatch.Draw(portalTexture, drawPosition, null, color, projectile.rotation, origin, projectile.scale * 1.2f, SpriteEffects.None, 0f);
			spriteBatch.Draw(portalTexture, drawPosition, null, color, -projectile.rotation, origin, projectile.scale * 1.2f, SpriteEffects.None, 0f);

			// Cyan portal.
			color = Color.Lerp(baseColor, Color.Cyan, 0.55f) * projectile.Opacity * 1.6f;
			spriteBatch.Draw(portalTexture, drawPosition, null, color, projectile.rotation * 0.6f, origin, projectile.scale * 1.2f, SpriteEffects.None, 0f);

			// Magenta portal.
			color = Color.Lerp(baseColor, Color.Fuchsia, 0.55f) * projectile.Opacity * 1.6f;
			spriteBatch.Draw(portalTexture, drawPosition, null, color, projectile.rotation * -0.6f, origin, projectile.scale * 1.2f, SpriteEffects.None, 0f);

			spriteBatch.ResetBlendState();
			return false;
		}
	}
}
