using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.StormWeaver
{
	public class StormWeaveCloud : ModProjectile
	{
		public ref float Time => ref projectile.ai[0];
		public ref float Variant => ref projectile.ai[1];
		public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
		public override void SetStaticDefaults() => DisplayName.SetDefault("Cloud");

		public override void SetDefaults()
		{
			projectile.width = 72;
			projectile.height = 72;
			projectile.penetrate = -1;
			projectile.tileCollide = false;
			projectile.timeLeft = 300;
			projectile.Opacity = 0f;
			projectile.scale = 0.01f;
		}

		public override void AI()
		{
			projectile.Opacity = Utils.InverseLerp(300f, 285f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 35f, projectile.timeLeft, true);
			projectile.scale = MathHelper.Clamp(projectile.Opacity + 0.065f, 0f, 1f);

			if (Variant == 0f)
			{
				Variant = Main.rand.Next(4) + 1f;
				switch ((int)Variant)
				{
					case 1:
						projectile.Size = new Vector2(530f, 218f);
						break;
					case 2:
						projectile.Size = new Vector2(372f, 132f);
						break;
					case 3:
						projectile.Size = new Vector2(296f, 116f);
						break;
					case 4:
						projectile.Size = new Vector2(226f, 68f);
						break;
				}

				projectile.netUpdate = true;
			}

			projectile.velocity = projectile.velocity.MoveTowards(Vector2.Zero, 0.04f) * 0.985f;

			if (Time > 60f)
			{
				for (int i = 0; i < projectile.width / 105f; i++)
				{
					if (!Main.rand.NextBool(92))
						continue;

					Main.PlaySound(SoundID.DD2_LightningAuraZap, projectile.Center);

					if (Main.netMode != NetmodeID.MultiplayerClient)
					{
						Vector2 sparkVelocity = Vector2.UnitY * Main.rand.NextFloat(12f, 16f);
						Vector2 sparkSpawnPosition = projectile.Bottom + new Vector2(Main.rand.NextFloatDirection() * projectile.width * 0.45f, Main.rand.NextFloat(-8f, 0f));
						Utilities.NewProjectileBetter(sparkSpawnPosition, sparkVelocity, ModContent.ProjectileType<WeaverSpark2>(), 255, 0f);
						Utilities.NewProjectileBetter(sparkSpawnPosition, -sparkVelocity, ModContent.ProjectileType<WeaverSpark2>(), 255, 0f);
					}
				}
			}

			Time++;
		}

		public override Color? GetAlpha(Color lightColor)
		{
			return Color.Lerp(lightColor, Color.Black, Utils.InverseLerp(0f, 25f, Time, true) * 0.45f) * projectile.Opacity;
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			if (Variant <= 0f || Variant > 4f)
				return false;

			Texture2D texture = ModContent.GetTexture($"InfernumMode/BehaviorOverrides/BossAIs/StormWeaver/StormWeaveCloud{(int)Variant}");
			Vector2 origin = texture.Size() * 0.5f;

			Vector2 drawPosition = projectile.Center - Main.screenPosition;
			Color frontAfterimageColor = projectile.GetAlpha(Color.Lerp(lightColor, Color.Cyan, 0.8f)) * 0.25f;
			for (int i = 0; i < 8; i++)
			{
				Vector2 drawOffset = (MathHelper.TwoPi * i / 8f).ToRotationVector2();
				drawOffset *= MathHelper.Lerp(-1f, 8f, (float)Math.Sin(Main.GlobalTime * 1.3f) * 0.5f + 0.5f);

				Vector2 afterimageDrawPosition = drawPosition + drawOffset;
				spriteBatch.Draw(texture, afterimageDrawPosition, null, frontAfterimageColor, projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
			}
			spriteBatch.Draw(texture, drawPosition, null, projectile.GetAlpha(lightColor), projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);
			return false;
		}
	}
}
