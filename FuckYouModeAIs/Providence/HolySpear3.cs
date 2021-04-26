using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Providence
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
			Texture2D value = Main.projectileTexture[projectile.type];
			int green = 125;
			int blue = 125;
			Color baseColor = new Color(255, green, blue, 255);

			Color color33 = baseColor * 0.5f;
			color33.A = 0;
			Vector2 vector28 = projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY);
			Color color34 = color33;
			Vector2 origin5 = value.Size() / 2f;
			Color color35 = color33 * 0.5f;
			float num162 = Utils.InverseLerp(15f, 30f, projectile.timeLeft, clamped: true) * Utils.InverseLerp(360f, 340f, projectile.timeLeft, clamped: true) * (1f + 0.2f * (float)Math.Cos(Main.GlobalTime % 30f / 0.5f * ((float)Math.PI * 2f) * 3f)) * 0.8f;
			Vector2 vector29 = new Vector2(1f, 1.5f) * num162;
			Vector2 vector30 = new Vector2(0.5f, 1f) * num162;
			color34 *= num162;
			color35 *= num162;

			SpriteEffects spriteEffects = SpriteEffects.None;
			if (projectile.spriteDirection == -1)
				spriteEffects = SpriteEffects.FlipHorizontally;

			if (CalamityConfig.Instance.Afterimages)
			{
				for (int i = 0; i < projectile.oldPos.Length; i++)
				{
					Vector2 drawPos = projectile.oldPos[i] + vector28;
					Color color = projectile.GetAlpha(color34) * ((projectile.oldPos.Length - i) / projectile.oldPos.Length);
					Main.spriteBatch.Draw(value, drawPos, null, color, projectile.rotation, origin5, vector29, spriteEffects, 0f);
					Main.spriteBatch.Draw(value, drawPos, null, color, projectile.rotation, origin5, vector30, spriteEffects, 0f);

					color = projectile.GetAlpha(color35) * ((projectile.oldPos.Length - i) / projectile.oldPos.Length);
					Main.spriteBatch.Draw(value, drawPos, null, color, projectile.rotation, origin5, vector29 * 0.6f, spriteEffects, 0f);
					Main.spriteBatch.Draw(value, drawPos, null, color, projectile.rotation, origin5, vector30 * 0.6f, spriteEffects, 0f);
				}
			}

			spriteBatch.Draw(value, vector28, null, color34, projectile.rotation, origin5, vector29, spriteEffects, 0);
			spriteBatch.Draw(value, vector28, null, color34, projectile.rotation, origin5, vector30, spriteEffects, 0);
			spriteBatch.Draw(value, vector28, null, color35, projectile.rotation, origin5, vector29 * 0.6f, spriteEffects, 0);
			spriteBatch.Draw(value, vector28, null, color35, projectile.rotation, origin5, vector30 * 0.6f, spriteEffects, 0);

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
