using CalamityMod;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Signus
{
	public class UnworldlyEntity : ModNPC
	{
		public Player Target => Main.player[npc.target];
		public ref float Timer => ref npc.ai[0];
		public ref float DeathCountdown => ref npc.ai[1];
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Unworldly Entity");
			Main.npcFrameCount[npc.type] = 5;
		}

		public override void SetDefaults()
		{
			npc.damage = 180;
			npc.npcSlots = 0f;
			npc.width = npc.height = 62;
			npc.defense = 15;
			npc.lifeMax = 5666;
			if (BossRushEvent.BossRushActive)
				npc.lifeMax = 26666;

			npc.aiStyle = aiType = -1;
			npc.knockBackResist = 0f;
			npc.noGravity = true;
			npc.noTileCollide = true;
			npc.canGhostHeal = false;
			npc.HitSound = SoundID.NPCHit41;
			npc.Calamity().canBreakPlayerDefense = true;
		}

		public override void AI()
		{
			// Fade away on death.
			if (DeathCountdown > 0f)
			{
				DeathCountdown--;

				npc.velocity *= 0.925f;
				npc.rotation = npc.rotation.AngleLerp(npc.velocity.X * 0.032f, 0.15f);
				npc.Opacity = Utils.InverseLerp(1f, 60f, DeathCountdown, true);

				if (DeathCountdown <= 1f)
					npc.active = false;

				return;
			}

			if (Timer >= Main.rand.NextFloat(400f, 500f))
			{
				DeathCountdown = 60f;
				npc.netUpdate = true;
			}

			// Handle despawn stuff.
			if (!Target.active || Target.dead)
			{
				npc.TargetClosest(false);
				if (!Target.active || Target.dead)
				{
					if (npc.timeLeft > 10)
						npc.timeLeft = 10;
					return;
				}
			}
			else if (npc.timeLeft > 600)
				npc.timeLeft = 600;

			// Fade in.
			npc.Opacity = Utils.InverseLerp(0f, 30f, Timer, true);

			// Fly upwards before charging at the target.
			if (Timer < 40f)
			{
				if (npc.velocity == Vector2.Zero)
				{
					npc.velocity = -Vector2.UnitY.RotatedByRandom(0.26f) * Main.rand.NextFloat(1f, 3f);
					npc.netUpdate = true;
				}
				npc.velocity *= 0.97f;
				npc.rotation = -npc.velocity.X * 0.02f;
			}

			// Charge after this.
			else
			{
				Vector2 idealVelocity = npc.SafeDirectionTo(Target.Center) * 17f;
				npc.velocity = (npc.velocity * 29f + idealVelocity) / 30f;
				npc.velocity = npc.velocity.MoveTowards(idealVelocity, 0.15f);
				npc.rotation = npc.rotation.AngleLerp(npc.velocity.X * 0.02f, 0.15f);
			}

			npc.spriteDirection = (npc.velocity.X < 0f).ToDirectionInt();
			Timer++;

			// Explode into strange tentacles if close to the target or enough time has passed.
			if (npc.WithinRange(Target.Center, 40f))
			{
				Main.PlaySound(SoundID.DD2_KoboldExplosion, npc.Center);
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					for (int i = 0; i < 3; i++)
					{
						// Randomize tentacle behavior variables.
						float ai0 = Main.rand.NextFloat(0.01f, 0.08f) * Main.rand.NextBool().ToDirectionInt();
						float ai1 = Main.rand.NextFloat(0.01f, 0.08f) * Main.rand.NextBool().ToDirectionInt();

						int tentacle = Projectile.NewProjectile(Target.Center, Main.rand.NextVector2Circular(4f, 4f), ModContent.ProjectileType<VoidTentacle>(), 250, 0f);
						if (Main.projectile.IndexInRange(tentacle))
						{
							Main.projectile[tentacle].ai[0] = ai0;
							Main.projectile[tentacle].ai[1] = ai1;
						}
					}
				}

				npc.life = 0;
				npc.checkDead();
				npc.active = false;
			}
		}

		public override bool PreNPCLoot() => false;

		public override bool CheckDead()
		{
			DeathCountdown = 60f;

			npc.life = npc.lifeMax;
			npc.dontTakeDamage = true;
			npc.active = true;
			npc.netUpdate = true;
			return false;
		}

		public override void FindFrame(int frameHeight)
		{
			npc.frameCounter++;

			if (npc.frameCounter >= 6D)
			{
				npc.frame.Y += 92;
				if (npc.frame.Y >= frameHeight * Main.npcFrameCount[npc.type])
					npc.frame.Y = 0;

				npc.frameCounter = 0D;
			}
		}
	}
}
