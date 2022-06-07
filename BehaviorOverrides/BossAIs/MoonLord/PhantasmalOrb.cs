using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
	public class PhantasmalOrb : ModProjectile
	{
		public bool CanSplit
		{
			get => projectile.ai[1] == 0f;
			set => projectile.ai[1] = 1 - value.ToInt();
		}
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Phantasmal Orb");
		}

		public override void SetDefaults()
		{
			projectile.width = projectile.height = 70;
			projectile.hostile = true;
			projectile.ignoreWater = true;
			projectile.tileCollide = false;
			projectile.alpha = 255;
			projectile.penetrate = -1;
			projectile.timeLeft = 45;
			cooldownSlot = 1;
		}

		public override void AI()
		{
			projectile.velocity *= 0.965f;
			projectile.Opacity = MathHelper.Lerp(projectile.Opacity, 1f, 0.56f);

			if (projectile.timeLeft != 2)
				return;

			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				float shootSpeed = 2.75f;
				Vector2 orthogonalVelocity = projectile.velocity.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.UnitY) * shootSpeed;
				Projectile.NewProjectile(projectile.Center, orthogonalVelocity, ProjectileID.PhantasmalBolt, projectile.damage, 0f);
				Projectile.NewProjectile(projectile.Center, -orthogonalVelocity, ProjectileID.PhantasmalBolt, projectile.damage, 0f);
			}
		}


		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			float lerpMult = (1f + 0.22f * (float)Math.Cos(Main.GlobalTime % 30f * MathHelper.TwoPi * 3f + projectile.identity % 10f)) * 0.8f;

			Texture2D texture = Main.projectileTexture[projectile.type];
			Vector2 drawPos = projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY);
			Color baseColor = new Color(39, 255, 151, 192);
			baseColor *= projectile.Opacity * 0.6f;
			baseColor.A = 0;
			Color colorA = baseColor;
			Color colorB = baseColor * 0.5f;
			colorA *= lerpMult;
			colorB *= lerpMult;
			Vector2 origin = texture.Size() / 2f;
			Vector2 scale = new Vector2(projectile.scale * projectile.Opacity * lerpMult);

			SpriteEffects spriteEffects = SpriteEffects.None;
			if (projectile.spriteDirection == -1)
				spriteEffects = SpriteEffects.FlipHorizontally;

			spriteBatch.Draw(texture, drawPos, null, colorA, MathHelper.PiOver2, origin, scale, spriteEffects, 0);
			spriteBatch.Draw(texture, drawPos, null, colorA, 0f, origin, scale, spriteEffects, 0);
			spriteBatch.Draw(texture, drawPos, null, colorB, MathHelper.PiOver2, origin, scale * 0.8f, spriteEffects, 0);
			spriteBatch.Draw(texture, drawPos, null, colorB, 0f, origin, scale * 0.8f, spriteEffects, 0);

			spriteBatch.Draw(texture, drawPos, null, colorA, MathHelper.PiOver2 + Main.GlobalTime * 0.35f, origin, scale, spriteEffects, 0);
			spriteBatch.Draw(texture, drawPos, null, colorA, Main.GlobalTime * 0.35f, origin, scale, spriteEffects, 0);
			spriteBatch.Draw(texture, drawPos, null, colorB, MathHelper.PiOver2 + Main.GlobalTime * 0.625f, origin, scale * 0.8f, spriteEffects, 0);
			spriteBatch.Draw(texture, drawPos, null, colorB, Main.GlobalTime * 0.625f, origin, scale * 0.8f, spriteEffects, 0);

			spriteBatch.Draw(texture, drawPos, null, colorA, MathHelper.PiOver4, origin, scale * 0.6f, spriteEffects, 0);
			spriteBatch.Draw(texture, drawPos, null, colorA, MathHelper.PiOver4 * 3f, origin, scale * 0.6f, spriteEffects, 0);
			spriteBatch.Draw(texture, drawPos, null, colorB, MathHelper.PiOver4, origin, scale * 0.4f, spriteEffects, 0);
			spriteBatch.Draw(texture, drawPos, null, colorB, MathHelper.PiOver4 * 3f, origin, scale * 0.4f, spriteEffects, 0);

			spriteBatch.Draw(texture, drawPos, null, colorA, MathHelper.PiOver4 + Main.GlobalTime * 1.1f * 0.75f, origin, scale * 0.6f, spriteEffects, 0);
			spriteBatch.Draw(texture, drawPos, null, colorA, MathHelper.PiOver4 * 3f + Main.GlobalTime * 1.1f * 0.75f, origin, scale * 0.6f, spriteEffects, 0);
			spriteBatch.Draw(texture, drawPos, null, colorB, MathHelper.PiOver4 + Main.GlobalTime * 1.1f, origin, scale * 0.4f, spriteEffects, 0);
			spriteBatch.Draw(texture, drawPos, null, colorB, MathHelper.PiOver4 * 3f + Main.GlobalTime * 1.1f, origin, scale * 0.4f, spriteEffects, 0);

			return false;
		}

		public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
		{
			target.Calamity().lastProjectileHit = projectile;
		}
	}
}
