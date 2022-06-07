using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumDeus
{
	public class StellarEnergy : ModProjectile
	{
		public ref float Time => ref projectile.ai[0];
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Stellar Energy");
			ProjectileID.Sets.TrailCacheLength[projectile.type] = 16;
			ProjectileID.Sets.TrailingMode[projectile.type] = 2;
		}

		public override void SetDefaults()
		{
			projectile.width = projectile.height = 54;
			projectile.hostile = true;
			projectile.ignoreWater = true;
			projectile.tileCollide = false;
			projectile.alpha = 255;
			projectile.penetrate = 1;
			projectile.timeLeft = 420;
		}

		public override void AI()
		{
			if (projectile.localAI[0] == 0f)
			{
				Main.PlaySound(SoundID.Item9, projectile.Center);
				projectile.localAI[0] = 1f;
			}

			projectile.Opacity = Utils.InverseLerp(0f, 20f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 20f, Time, true);

			List<Projectile> stars = Utilities.AllProjectilesByID(ModContent.ProjectileType<GiantAstralStar>()).ToList();
			if (stars.Count == 0 || stars.First().scale > 7f)
			{
				projectile.Kill();
				return;
			}

			if (projectile.WithinRange(stars.First().Center, (stars.First().modProjectile as GiantAstralStar).Radius * 0.925f))
			{
				stars.First().scale += 0.085f;
				stars.First().netUpdate = true;
				projectile.Kill();
			}

			projectile.velocity = projectile.velocity.RotateTowards(projectile.AngleTo(stars.First().Center), 0.085f) * 1.02f;
			projectile.rotation = projectile.rotation.AngleLerp(projectile.velocity.ToRotation() + MathHelper.PiOver2, 0.2f);

			Time++;
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			Vector2 drawPosition = projectile.Center - Main.screenPosition;
			Texture2D starTexture = Main.projectileTexture[projectile.type];
			Vector2 largeScale = new Vector2(0.8f, 4f) * projectile.Opacity * 0.5f;
			Vector2 smallScale = new Vector2(0.8f, 1.25f) * projectile.Opacity * 0.5f;
			spriteBatch.Draw(starTexture, drawPosition, null, projectile.GetAlpha(lightColor), MathHelper.PiOver2, starTexture.Size() * 0.5f, largeScale, SpriteEffects.None, 0);
			spriteBatch.Draw(starTexture, drawPosition, null, projectile.GetAlpha(lightColor), 0f, starTexture.Size() * 0.5f, smallScale, SpriteEffects.None, 0);
			spriteBatch.Draw(starTexture, drawPosition, null, projectile.GetAlpha(lightColor), MathHelper.PiOver2, starTexture.Size() * 0.5f, largeScale * 0.6f, SpriteEffects.None, 0);
			spriteBatch.Draw(starTexture, drawPosition, null, projectile.GetAlpha(lightColor), 0f, starTexture.Size() * 0.5f, smallScale * 0.6f, SpriteEffects.None, 0);

			for (int i = 0; i < projectile.oldPos.Length - 1; ++i)
			{
				float afterimageRot = projectile.oldRot[i];
				SpriteEffects sfxForThisAfterimage = projectile.oldSpriteDirection[i] == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

				Vector2 drawPos = projectile.oldPos[i] + projectile.Size * 0.5f - Main.screenPosition + new Vector2(0f, projectile.gfxOffY);
				Color color = projectile.GetAlpha(lightColor) * ((float)(projectile.oldPos.Length - i) / projectile.oldPos.Length);
				Main.spriteBatch.Draw(starTexture, drawPos, null, color, afterimageRot, starTexture.Size() * 0.5f, projectile.scale, sfxForThisAfterimage, 0f);

				drawPos += (projectile.oldPos[i + 1] - projectile.oldPos[i]) * 0.5f;
				Main.spriteBatch.Draw(starTexture, drawPos, null, color, afterimageRot, starTexture.Size() * 0.5f, projectile.scale, sfxForThisAfterimage, 0f);
			}

			return false;
		}

		public override Color? GetAlpha(Color lightColor)
		{
			Color color = projectile.identity % 2 == 0 ? new Color(234, 119, 93) : new Color(109, 242, 196);
			color.A = 0;
			return color * projectile.Opacity;
		}
	}
}
