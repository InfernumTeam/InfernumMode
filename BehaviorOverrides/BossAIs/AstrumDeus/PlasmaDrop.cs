using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumDeus
{
	public class PlasmaDrop : ModProjectile
	{
		public override void SetStaticDefaults() => DisplayName.SetDefault("Astral Plasma Droplet");

		public override void SetDefaults()
		{
			projectile.width = projectile.height = 18;
			projectile.hostile = true;
			projectile.ignoreWater = true;
			projectile.tileCollide = true;
			projectile.timeLeft = 360;
		}

		public override void AI()
		{
			projectile.velocity.Y = MathHelper.Clamp(projectile.velocity.Y + 0.27f, -20f, 7f);
			projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;
			projectile.scale = Utils.InverseLerp(-5f, 45f, projectile.timeLeft, true);
		}

		public override bool CanHitPlayer(Player target) => projectile.Opacity > 0.7f;

		public override Color? GetAlpha(Color lightColor)
		{
			Color color = projectile.identity % 2 == 0 ? new Color(234, 119, 93) : new Color(109, 242, 196);
			color = Color.Lerp(color, Color.White, 0.55f);
			color.A = 0;
			return color * projectile.Opacity * 0.4f;
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			Texture2D plasmaTexture = Main.projectileTexture[projectile.type];
			for (int i = 0; i < 6; i++)
			{
				Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 2.5f;
				Vector2 drawPosition = projectile.Center - Main.screenPosition + drawOffset;

				spriteBatch.Draw(plasmaTexture, drawPosition, null, projectile.GetAlpha(Color.White), projectile.rotation, plasmaTexture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);
			}
			return false;
		}

		public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 180);
	}
}
