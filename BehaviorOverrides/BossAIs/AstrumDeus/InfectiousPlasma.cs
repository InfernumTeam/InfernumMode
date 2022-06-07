using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumDeus
{
	public class InfectiousPlasma : ModProjectile
	{
		public override void SetStaticDefaults() => DisplayName.SetDefault("Astral Plasma");

		public override void SetDefaults()
		{
			projectile.width = projectile.height = 44;
			projectile.hostile = true;
			projectile.ignoreWater = true;
			projectile.tileCollide = false;
			projectile.timeLeft = 720;
		}

		public override void AI()
		{
			projectile.velocity *= 0.9875f;
			if (Main.netMode != NetmodeID.MultiplayerClient && projectile.timeLeft < 540 && projectile.timeLeft % 80f == 79f)
			{
				Vector2 plasmaVelocity = -Vector2.UnitY.RotatedByRandom(0.56f) * Main.rand.NextFloat(7f, 16f);
				Utilities.NewProjectileBetter(projectile.Center, plasmaVelocity, ModContent.ProjectileType<PlasmaDrop>(), 160, 0f);
			}

			projectile.Opacity = Utils.InverseLerp(720f, 700f, projectile.timeLeft, true) * Utils.InverseLerp(5f, 30f, projectile.timeLeft, true);
			projectile.scale = MathHelper.Lerp(0.65f, 0.25f, Utils.InverseLerp(325f, 30f, projectile.timeLeft, true));
		}

		public override bool CanHitPlayer(Player target) => projectile.Opacity > 0.7f;

		public override Color? GetAlpha(Color lightColor)
		{
			Color color = projectile.identity % 2 == 0 ? new Color(234, 119, 93) : new Color(109, 242, 196);
			color = Color.Lerp(color, Color.White, 0.35f);
			color.A = 0;
			return color * projectile.Opacity * 0.45f;
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			Texture2D plasmaTexture = Main.projectileTexture[projectile.type];
			for (int i = 0; i < 5; i++)
			{
				Matrix drawOffsetEncoding = Matrix.CreateRotationX(Main.GlobalTime * 2.32f + i * 1.37f);
				drawOffsetEncoding *= Matrix.CreateRotationZ(Main.GlobalTime * 1.77f - i * 1.83f);
				Vector3 vectorizedOffset = Vector3.Transform(Vector3.Forward, drawOffsetEncoding) * 0.5f + new Vector3(0.5f);
				Vector2 drawOffset = new Vector2(vectorizedOffset.X, vectorizedOffset.Y) * MathHelper.Lerp(1f, 16f, vectorizedOffset.Z);
				Vector2 drawPosition = projectile.Center - Main.screenPosition + drawOffset;

				spriteBatch.Draw(plasmaTexture, drawPosition, null, projectile.GetAlpha(Color.White), drawOffset.ToRotation(), plasmaTexture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);
			}
			return false;
		}

		public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 240);
	}
}
