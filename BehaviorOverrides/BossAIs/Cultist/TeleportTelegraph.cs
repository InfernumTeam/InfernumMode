using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Cultist
{
	public class TeleportTelegraph : ModProjectile
	{
		public bool CanCreateDust => projectile.ai[0] == 0f;
		public override void SetStaticDefaults() => DisplayName.SetDefault("Telegraph");

		public override void SetDefaults()
		{
			projectile.width = projectile.height = 2;
			projectile.tileCollide = false;
			projectile.ignoreWater = true;
			projectile.hide = true;
			projectile.timeLeft = 45;
			projectile.penetrate = -1;
		}

		public override void AI()
		{
			// Play a teleport sound.
			if (projectile.localAI[0] == 0f)
			{
				projectile.localAI[0] = 1f;
				Main.PlaySound(SoundID.Item105, projectile.Center);
			}

			projectile.Opacity = Utils.InverseLerp(0f, 12f, projectile.timeLeft);
			projectile.scale = Utils.InverseLerp(45f, 5f, projectile.timeLeft);
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			Texture2D telegraphTexture = Main.projectileTexture[projectile.type];
			Color telegraphColor = Color.White * projectile.Opacity * 0.2f;
			telegraphColor.A = 0;

			for (int i = 0; i < 35; i++)
			{
				Vector2 drawPosition = projectile.Center + (MathHelper.TwoPi * i / 5f + Main.GlobalTime * 3f).ToRotationVector2() * 2f;
				drawPosition -= Main.screenPosition;

				Vector2 scale = new Vector2(0.58f, 1f) * projectile.scale;
				scale *= MathHelper.Lerp(0.015f, 1f, i / 35f);

				spriteBatch.Draw(telegraphTexture, drawPosition, null, telegraphColor, 0f, telegraphTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
			}
			return false;
		}

		public override void Kill(int timeLeft)
		{
			if (!CanCreateDust)
				return;

			for (int i = 0; i < 20; i++)
			{
				Dust magic = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(30f, 30f), 267);
				magic.color = Color.SkyBlue;
				magic.scale = 1.1f;
				magic.fadeIn = 0.6f;
				magic.velocity = Main.rand.NextVector2Circular(2f, 2f);
				magic.velocity = Vector2.Lerp(magic.velocity, -Vector2.UnitY * magic.velocity.Length(), Main.rand.NextFloat(0.65f, 1f));
				magic.noGravity = true;
			}
		}

		public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI)
		{
			drawCacheProjsBehindNPCsAndTiles.Add(index);
		}

		public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
		{
			target.Calamity().lastProjectileHit = projectile;
		}
	}
}
