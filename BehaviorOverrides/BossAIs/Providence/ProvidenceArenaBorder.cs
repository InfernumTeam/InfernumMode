using CalamityMod;
using InfernumMode.ILEditingStuff;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Providence
{
	public class ProvidenceArenaBorder : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Border");
		}

		public override void SetDefaults()
		{
			projectile.width = projectile.height = 2;
			projectile.tileCollide = false;
			projectile.ignoreWater = true;
			projectile.penetrate = -1;
			projectile.timeLeft = int.MaxValue;
			projectile.alpha = 255;
			projectile.hide = true;
		}

		public override void AI()
		{
			if (InfernumMode.ProvidenceArenaTimer <= 0)
				projectile.Kill();
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			Texture2D texture = ModContent.GetTexture(Texture);

			float arenaFallCompletion = MathHelper.Clamp(InfernumMode.ProvidenceArenaTimer / 120f, 0f, 1f);
			Vector2 top = PoDWorld.ProvidenceArena.TopLeft() * 16f + new Vector2(8f, 32f);
			Vector2 bottom = PoDWorld.ProvidenceArena.TopLeft() + Vector2.UnitY * 2f;
			for (int i = 0; i < 200; i++)
			{
				if (CalamityUtils.ParanoidTileRetrieval((int)bottom.X, (int)bottom.Y).active())
					break;
				bottom.Y++;
			}

			bottom = bottom * 16f + new Vector2(8f, 52f);
			float distanceToBottom = MathHelper.Distance(top.Y, bottom.Y);
			float distancePerSegment = MathHelper.Max(texture.Height, 8f) * projectile.scale;
			for (float y = 0f; y < distanceToBottom; y += distancePerSegment)
			{
				Rectangle frame = texture.Frame();
				if (y + frame.Height >= distanceToBottom)
					frame.Height = (int)(distanceToBottom - y);

				Vector2 drawPosition = new Vector2(top.X, top.Y + y + (1f - arenaFallCompletion) * distanceToBottom);
				Color color = Lighting.GetColor((int)(drawPosition.X / 16), (int)(drawPosition.Y / 16));
				color = Color.Lerp(color, Color.White, 0.6f);
				drawPosition -= Main.screenPosition;
				Main.spriteBatch.Draw(texture, drawPosition, frame, color, 0f, Vector2.Zero, projectile.scale, 0, 0f);
			}

			return false;
		}

		public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers)
		{
			DrawBlackEffectHook.DrawCacheProjsOverSignusBlackening.Add(index);
		}

		public override bool ShouldUpdatePosition() => false;
	}
}
