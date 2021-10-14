using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Providence
{
	public class HolySpear3 : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Holy Spear");
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = 30;
            projectile.height = 30;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 360;
            cooldownSlot = 1;
        }

        public override void AI()
		{
			projectile.ai[1] += 1f;

			float slowGateValue = 90f;
			float fastGateValue = 30f;
			float minVelocity = 3f;
			float maxVelocity = 12f;
			float deceleration = 0.95f;
			float acceleration = 1.2f;

			if (projectile.ai[1] <= slowGateValue)
			{
				if (projectile.velocity.Length() > minVelocity)
					projectile.velocity *= deceleration;
			}
			else if (projectile.ai[1] < slowGateValue + fastGateValue)
			{
				if (projectile.velocity.Length() < maxVelocity)
					projectile.velocity *= acceleration;
			}
			else
				projectile.ai[1] = 0f;

			projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
			Texture2D spearTexture = Main.projectileTexture[projectile.type];
			int green = 125;
			int blue = 125;
			Color baseColor = new Color(255, green, blue, 255);

			float fadeFactor = Utils.InverseLerp(15f, 30f, projectile.timeLeft, true) * Utils.InverseLerp(360f, 340f, projectile.timeLeft, true) * (1f + 0.2f * (float)Math.Cos(Main.GlobalTime % 30f / 0.5f * MathHelper.TwoPi * 3f)) * 0.8f;
			Color fadedBrightColor = baseColor * 0.5f;
			fadedBrightColor.A = 0;
			Vector2 drawPosition = projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY);
			Vector2 origin5 = spearTexture.Size() / 2f;
			Color brightColor = fadedBrightColor * fadeFactor;
			Color dimColor = fadedBrightColor * fadeFactor * 0.5f;
			Vector2 largeScale = new Vector2(1f, 1.5f) * fadeFactor;
			Vector2 smallScale = new Vector2(0.5f, 1f) * fadeFactor;

			SpriteEffects spriteEffects = SpriteEffects.None;
			if (projectile.spriteDirection == -1)
				spriteEffects = SpriteEffects.FlipHorizontally;

			if (CalamityConfig.Instance.Afterimages)
			{
				for (int i = 0; i < projectile.oldPos.Length; i++)
				{
					Vector2 drawPos = projectile.oldPos[i] + drawPosition;
					Color color = projectile.GetAlpha(brightColor) * ((projectile.oldPos.Length - i) / projectile.oldPos.Length);
					spriteBatch.Draw(spearTexture, drawPos, null, color, projectile.rotation, origin5, largeScale, spriteEffects, 0f);
					spriteBatch.Draw(spearTexture, drawPos, null, color, projectile.rotation, origin5, smallScale, spriteEffects, 0f);

					color = projectile.GetAlpha(dimColor) * ((projectile.oldPos.Length - i) / projectile.oldPos.Length);
					spriteBatch.Draw(spearTexture, drawPos, null, color, projectile.rotation, origin5, largeScale * 0.6f, spriteEffects, 0f);
					spriteBatch.Draw(spearTexture, drawPos, null, color, projectile.rotation, origin5, smallScale * 0.6f, spriteEffects, 0f);
				}
			}

			spriteBatch.Draw(spearTexture, drawPosition, null, brightColor, projectile.rotation, origin5, largeScale, spriteEffects, 0);
			spriteBatch.Draw(spearTexture, drawPosition, null, brightColor, projectile.rotation, origin5, smallScale, spriteEffects, 0);
			spriteBatch.Draw(spearTexture, drawPosition, null, dimColor, projectile.rotation, origin5, largeScale * 0.6f, spriteEffects, 0);
			spriteBatch.Draw(spearTexture, drawPosition, null, dimColor, projectile.rotation, origin5, smallScale * 0.6f, spriteEffects, 0);

			return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
			int buffType = Main.dayTime ? ModContent.BuffType<HolyFlames>() : ModContent.BuffType<Nightwither>();
			target.AddBuff(buffType, 120);
		}

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)	
        {
			target.Calamity().lastProjectileHit = projectile;
		}
    }
}
