using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Ravager;
using CalamityMod.NPCs.SlimeGod;
using CalamityMod.Projectiles.Boss;
using CalamityMod.World;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.World.Generation;

namespace InfernumMode.FuckYouModeAIs.Ravager
{
    public class QSRefactor : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.None;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

		public static Color AI_121_QueenSlime_GetDustColor()
		{
			Color arg_53_0 = new Color(0, 160, 255);
			Color value = new Color(255, 80, 255);
			Color value2 = Color.Lerp(new Color(200, 200, 200), value, Main.rand.NextFloat());
			return Color.Lerp(arg_53_0, value2, Main.rand.NextFloat());
		}
		private void AI_121_QueenSlime_FlyMovement(NPC npc)
		{
			npc.noTileCollide = true;
			npc.noGravity = true;
			float flySpeed = 14f;
			float moveSpeed = 0.1f;
			float num2 = 250f;
			npc.TargetClosest(true);
			Vector2 idealVelocity = npc.Center + new Vector2(500f * npc.direction, -num2) - npc.Center;

			float distanceFromDestination = idealVelocity.Length();
			if (Math.Abs(idealVelocity.X) < 40f)
				idealVelocity.X = npc.velocity.X;

			if (distanceFromDestination > 100f && ((npc.velocity.X < -12f && idealVelocity.X > 0f) || (npc.velocity.X > 12f && idealVelocity.X < 0f)))
			{
				moveSpeed = 0.2f;
			}

			if (distanceFromDestination < 40f)
				idealVelocity = npc.velocity;

			else if (distanceFromDestination < 80f)
			{
				idealVelocity.Normalize();
				idealVelocity *= flySpeed * 0.65f;
			}
			else
			{
				idealVelocity.Normalize();
				idealVelocity *= flySpeed;
			}

			npc.SimpleFlyMovement(idealVelocity, moveSpeed);
			npc.rotation = MathHelper.Clamp(npc.velocity.X * 0.1f, -0.5f, 0.5f);
		}
		public override bool PreAI(NPC npc)
		{
			int num = 30;
			int num2 = 40;
			float num3 = 1f;
			bool flag = false;
			bool inPhase2 = npc.life <= npc.lifeMax / 2;
			if (npc.localAI[0] == 0f)
			{
				npc.ai[1] = -100f;
				npc.localAI[0] = npc.lifeMax;
				npc.TargetClosest(true);
				npc.netUpdate = true;
			}
			Lighting.AddLight(npc.Center, 1f, 0.7f, 0.9f);
			int num4 = 500;
			if (Main.player[npc.target].dead || Math.Abs(npc.Center.X - Main.player[npc.target].Center.X) / 16f > num4)
			{
				npc.TargetClosest(true);
				if (Main.player[npc.target].dead || Math.Abs(npc.Center.X - Main.player[npc.target].Center.X) / 16f > num4)
				{
					if (npc.timeLeft > 10)
						npc.timeLeft = 10;

					if (Main.player[npc.target].Center.X < npc.Center.X)
					{
						npc.direction = 1;
					}
					else
					{
						npc.direction = -1;
					}
				}
			}
			if (!Main.player[npc.target].dead && npc.timeLeft > 10 && !inPhase2 && npc.ai[3] >= 300f && npc.ai[0] == 0f && npc.velocity.Y == 0f)
			{
				npc.ai[0] = 2f;
				npc.ai[1] = 0f;
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					npc.netUpdate = true;
					npc.TargetClosest(false);
					Point point = npc.Center.ToTileCoordinates();
					Point point2 = Main.player[npc.target].Center.ToTileCoordinates();
					Vector2 vector = Main.player[npc.target].Center - npc.Center;
					int num5 = 10;
					int num6 = 0;
					int num7 = 7;
					int num8 = 0;
					bool flag3 = false;
					if (npc.ai[3] >= 360f || vector.Length() > 2000f)
					{
						if (npc.ai[3] > 360f)
						{
							npc.ai[3] = 360f;
						}
						flag3 = true;
						num8 = 100;
					}
					while (!flag3 && num8 < 100)
					{
						num8++;
						int num9 = Main.rand.Next(point2.X - num5, point2.X + num5 + 1);
						int num10 = Main.rand.Next(point2.Y - num5, point2.Y + 1);
						if ((num10 < point2.Y - num7 || num10 > point2.Y + num7 || num9 < point2.X - num7 || num9 > point2.X + num7) && (num10 < point.Y - num6 || num10 > point.Y + num6 || num9 < point.X - num6 || num9 > point.X + num6) && !Main.tile[num9, num10].nactive())
						{
							int num11 = num10;
							int num12 = 0;
							if (Main.tile[num9, num11].nactive() && Main.tileSolid[Main.tile[num9, num11].type] && !Main.tileSolidTop[Main.tile[num9, num11].type])
							{
								num12 = 1;
							}
							else
							{
								while (num12 < 150 && num11 + num12 < Main.maxTilesY)
								{
									int num13 = num11 + num12;
									if (Main.tile[num9, num13].nactive() && Main.tileSolid[Main.tile[num9, num13].type] && !Main.tileSolidTop[Main.tile[num9, num13].type])
									{
										num12--;
										break;
									}
									num12++;
								}
							}
							num10 += num12;
							bool flag4 = true;
							if (flag4 && Main.tile[num9, num10].lava())
							{
								flag4 = false;
							}
							if (flag4 && !Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0))
							{
								flag4 = false;
							}
							if (flag4)
							{
								npc.localAI[1] = num9 * 16 + 8;
								npc.localAI[2] = num10 * 16 + 16;
								break;
							}
						}
					}
					if (num8 >= 100)
					{
						Vector2 bottom = Main.player[Player.FindClosest(npc.position, npc.width, npc.height)].Bottom;
						npc.localAI[1] = bottom.X;
						npc.localAI[2] = bottom.Y;
						npc.ai[3] = 0f;
					}
				}
			}
			if (!inPhase2 && (!Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0) || Math.Abs(npc.Top.Y - Main.player[npc.target].Bottom.Y) > 320f))
			{
				npc.ai[3] += 1.5f;
			}
			else
			{
				float num14 = npc.ai[3];
				npc.ai[3] -= 1f;
				if (npc.ai[3] < 0f)
				{
					if (Main.netMode != NetmodeID.MultiplayerClient && num14 > 0f)
					{
						npc.netUpdate = true;
					}
					npc.ai[3] = 0f;
				}
			}
			if (npc.timeLeft <= 10 && ((inPhase2 && npc.ai[0] != 0f) || (!inPhase2 && npc.ai[0] != 3f)))
			{
				if (inPhase2)
				{
					npc.ai[0] = 0f;
				}
				else
				{
					npc.ai[0] = 3f;
				}
				npc.ai[1] = 0f;
				npc.ai[2] = 0f;
				npc.ai[3] = 0f;
				npc.netUpdate = true;
			}
			npc.noTileCollide = false;
			npc.noGravity = false;
			if (inPhase2)
			{
				npc.localAI[3] += 1f;
				if (npc.localAI[3] >= 24f)
				{
					npc.localAI[3] = 0f;
				}
				if (npc.ai[0] == 4f && npc.ai[2] == 1f)
				{
					npc.localAI[3] = 6f;
				}
				if (npc.ai[0] == 5f && npc.ai[2] != 1f)
				{
					npc.localAI[3] = 7f;
				}
			}
			switch ((int)npc.ai[0])
			{
				case 0:
					if (inPhase2)
					{
						AI_121_QueenSlime_FlyMovement(npc);
					}
					else
					{
						npc.noTileCollide = false;
						npc.noGravity = false;
						if (npc.velocity.Y == 0f)
						{
							npc.velocity.X *= 0.8f;
							if (npc.velocity.X > -0.1 && npc.velocity.X < 0.1)
							{
								npc.velocity.X = 0f;
							}
						}
					}
					if (npc.timeLeft > 10 && (inPhase2 || npc.velocity.Y == 0f))
					{
						npc.ai[1] += 1f;
						int num15 = 60;
						if (inPhase2)
						{
							num15 = 120;
						}
						if (npc.ai[1] > num15)
						{
							npc.ai[1] = 0f;
							if (inPhase2)
							{
								Player player = Main.player[npc.target];
								if (Main.rand.Next(2) != 1)
								{
									npc.ai[0] = 4f;
								}
								else
								{
									npc.ai[0] = 5f;
								}
								if (npc.ai[0] == 4f)
								{
									npc.ai[2] = 1f;
									if (player != null && player.active && !player.dead && (player.Bottom.Y < npc.Bottom.Y || Math.Abs(player.Center.X - npc.Center.X) > 250f))
									{
										npc.ai[0] = 5f;
										npc.ai[2] = 0f;
									}
								}
							}
							else
								npc.ai[0] = Utils.SelectRandom(Main.rand, 3f, 4f, 5f);

							npc.netUpdate = true;
						}
					}
					break;
				case 1:
					{
						npc.rotation = 0f;
						npc.ai[1] += 1f;
						num3 = MathHelper.Clamp(npc.ai[1] / 30f, 0f, 1f);
						num3 = 0.5f + num3 * 0.5f;
						if (npc.ai[1] >= 30f && Main.netMode != NetmodeID.MultiplayerClient)
						{
							npc.ai[0] = 0f;
							npc.ai[1] = 0f;
							npc.netUpdate = true;
							npc.TargetClosest(true);
						}
						if (Main.netMode == NetmodeID.MultiplayerClient && npc.ai[1] >= 60f)
						{
							npc.ai[0] = 0f;
							npc.ai[1] = 0f;
							npc.TargetClosest(true);
						}
						Color newColor = AI_121_QueenSlime_GetDustColor();
						newColor.A = 150;
						for (int i = 0; i < 10; i++)
						{
							int num17 = Dust.NewDust(npc.position + Vector2.UnitX * -20f, npc.width + 40, npc.height, 4, npc.velocity.X, npc.velocity.Y, 50, newColor, 1.5f);
							Main.dust[num17].noGravity = true;
							Main.dust[num17].velocity *= 2f;
						}
						break;
					}
				case 2:
					npc.rotation = 0f;
					npc.ai[1] += 1f;
					num3 = MathHelper.Clamp((60f - npc.ai[1]) / 60f, 0f, 1f);
					num3 = 0.5f + num3 * 0.5f;
					if (npc.ai[1] >= 60f)
					{
						flag = true;
					}
					if (npc.ai[1] == 60f)
					{
						Gore.NewGore(npc.Center + new Vector2(-40f, (float)(-(float)npc.height / 2)), npc.velocity, 1258, 1f);
					}
					if (npc.ai[1] >= 60f && Main.netMode != NetmodeID.MultiplayerClient)
					{
						npc.Bottom = new Vector2(npc.localAI[1], npc.localAI[2]);
						npc.ai[0] = 1f;
						npc.ai[1] = 0f;
						npc.netUpdate = true;
					}
					if (Main.netMode == NetmodeID.MultiplayerClient && npc.ai[1] >= 120f)
					{
						npc.ai[0] = 1f;
						npc.ai[1] = 0f;
					}
					if (!flag)
					{
						Color newColor2 = AI_121_QueenSlime_GetDustColor();
						newColor2.A = 150;
						for (int j = 0; j < 10; j++)
						{
							int num18 = Dust.NewDust(npc.position + Vector2.UnitX * -20f, npc.width + 40, npc.height, 4, npc.velocity.X, npc.velocity.Y, 50, newColor2, 1.5f);
							Main.dust[num18].noGravity = true;
							Main.dust[num18].velocity *= 0.5f;
						}
					}
					break;
				case 3:
					npc.rotation = 0f;
					if (npc.velocity.Y == 0f)
					{
						npc.velocity.X *= 0.8f;
						if (npc.velocity.X > -0.1 && npc.velocity.X < 0.1)
						{
							npc.velocity.X = 0f;
						}
						npc.ai[1] += 4f;
						if (npc.life < npc.lifeMax * 0.66)
						{
							npc.ai[1] += 4f;
						}
						if (npc.life < npc.lifeMax * 0.33)
						{
							npc.ai[1] += 4f;
						}
						if (npc.ai[1] >= 0f)
						{
							npc.netUpdate = true;
							npc.TargetClosest(true);
							if (npc.ai[2] == 3f)
							{
								npc.velocity.Y = -13f;
								npc.velocity.X += 3.5f * npc.direction;
								npc.ai[1] = 0f;
								npc.ai[2] = 0f;
								if (npc.timeLeft > 10)
								{
									npc.ai[0] = 0f;
								}
								else
								{
									npc.ai[1] = -60f;
								}
							}
							else
							{
								if (npc.ai[2] == 2f)
								{
									npc.velocity.Y = -6f;
									npc.velocity.X += 4.5f * npc.direction;
									npc.ai[1] = -40f;
									npc.ai[2] += 1f;
								}
								else
								{
									npc.velocity.Y = -8f;
									npc.velocity.X += 4f * npc.direction;
									npc.ai[1] = -40f;
									npc.ai[2] += 1f;
								}
							}
						}
					}
					else
					{
						if (npc.target < 255)
						{
							float num19 = 3f;
							if ((npc.direction == 1 && npc.velocity.X < num19) || (npc.direction == -1 && npc.velocity.X > -num19))
							{
								if ((npc.direction == -1 && npc.velocity.X < 0.1) || (npc.direction == 1 && npc.velocity.X > -0.1))
								{
									npc.velocity.X += 0.2f * npc.direction;
								}
								else
								{
									npc.velocity.X *= 0.93f;
								}
							}
						}
					}
					break;
				case 4:
					npc.rotation *= 0.9f;
					npc.noTileCollide = true;
					npc.noGravity = true;
					if (npc.ai[2] == 1f)
					{
						npc.noTileCollide = false;
						npc.noGravity = false;
						int num20 = 30;
						if (inPhase2)
						{
							num20 = 10;
						}
						if (npc.velocity.Y == 0f)
						{
							npc.ai[0] = 0f;
							npc.ai[1] = 0f;
							npc.ai[2] = 0f;
							npc.netUpdate = true;
							if (Main.netMode != NetmodeID.MultiplayerClient)
							{
								Projectile.NewProjectile(npc.Bottom, Vector2.Zero, 922, num2, 0f, Main.myPlayer, 0f, 0f);
							}
							for (int k = 0; k < 20; k++)
							{
								int num21 = Dust.NewDust(npc.Bottom - new Vector2(npc.width / 2, 30f), npc.width, 30, 31, npc.velocity.X, npc.velocity.Y, 40, AI_121_QueenSlime_GetDustColor(), 1f);
								Main.dust[num21].noGravity = true;
								Main.dust[num21].velocity.Y = -5f + Main.rand.NextFloat() * -3f;
								Dust expr_114E_cp_0_cp_0 = Main.dust[num21];
								expr_114E_cp_0_cp_0.velocity.X *= 7f;
							}
						}
						else
						{
							if (npc.ai[1] >= num20)
							{
								for (int l = 0; l < 4; l++)
								{
									Vector2 position = npc.Bottom - new Vector2(Main.rand.NextFloatDirection() * 16f, Main.rand.Next(8));
									int num22 = Dust.NewDust(position, 2, 2, 31, npc.velocity.X, npc.velocity.Y, 40, AI_121_QueenSlime_GetDustColor(), 1.4f);
									Main.dust[num22].position = position;
									Main.dust[num22].noGravity = true;
									Main.dust[num22].velocity.Y = npc.velocity.Y * 0.9f;
									Main.dust[num22].velocity.X = ((Main.rand.Next(2) == 0) ? -10f : 10f) + Main.rand.NextFloatDirection() * 3f;
								}
							}
						}
						npc.velocity.X *= 0.8f;
						float num23 = npc.ai[1];
						npc.ai[1] += 1f;
						if (npc.ai[1] >= num20)
						{
							if (num23 < num20)
							{
								npc.netUpdate = true;
							}
							if (inPhase2 && npc.ai[1] > num20 + 120)
							{
								npc.ai[0] = 0f;
								npc.ai[1] = 0f;
								npc.ai[2] = 0f;
								npc.velocity.Y *= 0.8f;
								npc.netUpdate = true;
							}
							else
							{
								npc.velocity.Y++;
								float num24 = 14f;
								if (npc.velocity.Y == 0f)
								{
									npc.velocity.Y = 0.01f;
								}
								if (npc.velocity.Y >= num24)
								{
									npc.velocity.Y = num24;
								}
							}
						}
						else
						{
							npc.velocity.Y *= 0.8f;
						}
					}
					else
					{
						if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[1] == 0f)
						{
							npc.TargetClosest(true);
							npc.netUpdate = true;
						}
						npc.ai[1] += 1f;
						if (npc.ai[1] >= 30f)
						{
							if (npc.ai[1] >= 60f)
							{
								npc.ai[1] = 60f;
								if (Main.netMode != NetmodeID.MultiplayerClient)
								{
									npc.ai[1] = 0f;
									npc.ai[2] = 1f;
									npc.velocity.Y = -3f;
									npc.netUpdate = true;
								}
							}
							Player player3 = Main.player[npc.target];
							Vector2 center = npc.Center;
							if (!player3.dead && player3.active && Math.Abs(npc.Center.X - player3.Center.X) / 16f <= num4)
							{
								center = player3.Center;
							}
							center.Y -= 384f;
							if (npc.velocity.Y == 0f)
							{
								npc.velocity = center - npc.Center;
								npc.velocity = npc.velocity.SafeNormalize(Vector2.Zero);
								npc.velocity *= 20f;
							}
							else
							{
								npc.velocity.Y *= 0.95f;
							}
						}
					}
					break;
				case 5:
					npc.rotation *= 0.9f;
					npc.noTileCollide = true;
					npc.noGravity = true;
					if (inPhase2)
					{
						npc.ai[3] = 0f;
					}
					if (npc.ai[2] == 1f)
					{
						npc.ai[1] += 1f;
						if (npc.ai[1] >= 10f)
						{
							if (Main.netMode != NetmodeID.MultiplayerClient)
							{
								int num25 = 10;
								int num26 = num25;
								if (!inPhase2)
								{
									num26 = 6;
								}
								for (int m = 0; m < num26; m++)
								{
									Vector2 vector2 = new Vector2(9f, 0f);
									vector2 = vector2.RotatedBy(-m * MathHelper.TwoPi / num25, Vector2.Zero);
									Projectile.NewProjectile(npc.Center.X, npc.Center.Y, vector2.X, vector2.Y, 926, num, 0f, Main.myPlayer, 0f, 0f);
								}
							}
							npc.ai[0] = 0f;
							npc.ai[1] = 0f;
							npc.ai[2] = 0f;
							npc.netUpdate = true;
						}
					}
					else
					{
						if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[1] == 0f)
						{
							npc.TargetClosest(true);
							npc.netUpdate = true;
						}
						npc.ai[1] += 1f;
						if (npc.ai[1] >= 50f)
						{
							npc.ai[1] = 50f;
							if (Main.netMode != NetmodeID.MultiplayerClient)
							{
								npc.ai[1] = 0f;
								npc.ai[2] = 1f;
								npc.netUpdate = true;
							}
						}
						float num27 = 100f;
						for (int n = 0; n < 4; n++)
						{
							Vector2 vector3 = npc.Center + Main.rand.NextVector2CircularEdge(num27, num27);
							if (!inPhase2)
							{
								vector3 += new Vector2(0f, 20f);
							}
							Vector2 vector4 = vector3 - npc.Center;
							vector4 = vector4.SafeNormalize(Vector2.Zero) * -8f;
							int num28 = Dust.NewDust(vector3, 2, 2, 31, vector4.X, vector4.Y, 40, AI_121_QueenSlime_GetDustColor(), 1.8f);
							Main.dust[num28].position = vector3;
							Main.dust[num28].noGravity = true;
							Main.dust[num28].alpha = 250;
							Main.dust[num28].velocity = vector4;
							Main.dust[num28].customData = this;
						}
						if (inPhase2)
						{
							AI_121_QueenSlime_FlyMovement(npc);
						}
					}
					break;
			}
			npc.dontTakeDamage = npc.hide = flag;
			if (num3 != npc.scale)
			{
				npc.position.X += npc.width / 2;
				npc.position.Y += npc.height;
				npc.scale = num3;
				npc.width = (int)(114f * npc.scale);
				npc.height = (int)(100f * npc.scale);
				npc.position.X -= npc.width / 2;
				npc.position.Y -= npc.height;
			}
			return false;
		}
	}
}
