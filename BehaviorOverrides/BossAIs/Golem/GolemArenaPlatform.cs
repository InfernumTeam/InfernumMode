using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Golem
{
	public class GolemArenaPlatform : ModNPC
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault(string.Empty);
			NPCID.Sets.TrailingMode[npc.type] = 0;
			NPCID.Sets.TrailCacheLength[npc.type] = 7;
		}

		public override void SetDefaults()
		{
			npc.damage = 0;
			npc.lifeMax = 500;
			npc.immortal = true;
			npc.dontTakeDamage = true;
			npc.noGravity = true;
			npc.noTileCollide = true;
			npc.dontCountMe = true;
			npc.width = 100;
			npc.height = 24;
			npc.aiStyle = -1;
			npc.knockBackResist = 0;
			npc.Opacity = 0f;
			npc.netAlways = true;
		}

		public override void AI()
		{
			// Die if Golem is not present.
			if (!Main.npc.IndexInRange(NPC.golemBoss))
			{
				npc.active = false;
				return;
			}

			// Die if the platform has left Golem's arena.
			if (!Main.npc[NPC.golemBoss].Infernum().arenaRectangle.Intersects(npc.Hitbox))
			{
				npc.active = false;
				return;
			}

			// Fade in.
			npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.1f, 0f, 1f);

			npc.gfxOffY = -12;

			float offsetFromPreviousPosition = npc.position.Y - npc.oldPosition.Y;
			foreach (Player player in Main.player)
			{
				if (!player.active || player.dead || player.GoingDownWithGrapple || Collision.SolidCollision(player.position, player.width, player.height) || player.controlDown)
					continue;

				Rectangle playerRect = new Rectangle((int)player.position.X, (int)player.position.Y + (player.height), player.width, 1);

				int effectiveNPCHitboxHeight = Math.Min((int)player.velocity.Y, 0) + (int)Math.Abs(offsetFromPreviousPosition) + 14;
				if (playerRect.Intersects(new Rectangle((int)npc.position.X, (int)npc.position.Y, npc.width, effectiveNPCHitboxHeight)) && player.position.Y <= npc.position.Y)
				{
					if (!player.justJumped && player.velocity.Y >= 0 && !Collision.SolidCollision(player.position + player.velocity, player.width, player.height))
					{
						player.velocity.Y = 0;
						player.position.Y = npc.position.Y - player.height + 4;
						player.position += npc.velocity;

						if (Math.Abs(player.velocity.X) < 0.01f)
						{
							player.legFrame.Y = 0;
							player.legFrameCounter = 0;
						}
						player.wingFrame = 0;
						player.wingFrameCounter = 0;
						player.bodyFrame.Y = 0;
						player.bodyFrameCounter = 0;
					}
				}
			}
		}

		// Ensure that platforms are fullbright, for visual clarity.
		public override Color? GetAlpha(Color drawColor) => Color.White * npc.Opacity;

		public override bool CheckActive() => false;

		public override bool? CanBeHitByItem(Player player, Item item) => false;

		public override bool? CanBeHitByProjectile(Projectile projectile) => false;
	}
}
