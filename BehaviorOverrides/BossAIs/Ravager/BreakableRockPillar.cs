using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Ravager
{
	public class BreakableRockPillar : ModNPC
	{
		public Player Target => Main.player[npc.target];
		public ref float AttackTimer => ref npc.ai[0];
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Rock Pillar");
		}

		public override void SetDefaults()
		{
			npc.damage = 180;
			npc.width = 60;
			npc.height = 60;
			npc.defense = 50;
			npc.DR_NERD(0.3f);
			npc.chaseable = false;
			npc.noTileCollide = true;
			npc.noGravity = true;
			npc.canGhostHeal = false;
			npc.lifeMax = CalamityWorld.downedProvidence ? 5600 : 1300;
			npc.alpha = 255;
			npc.aiStyle = -1;
			aiType = -1;
			npc.knockBackResist = 0f;
			npc.HitSound = SoundID.NPCHit41;
			npc.DeathSound = SoundID.NPCDeath14;
			npc.Calamity().canBreakPlayerDefense = true;
		}

		public override void AI()
		{
			// Handle despawn stuff.
			if (CalamityGlobalNPC.scavenger == -1)
			{
				npc.active = false;
				return;
			}

			if (npc.timeLeft < 3600)
				npc.timeLeft = 3600;

			// Inherit Ravager's target.
			npc.target = Main.npc[CalamityGlobalNPC.scavenger].target;

			// Rise up at first and spin.
			if (AttackTimer < 180f)
			{
				npc.damage = 0;
				npc.rotation += MathHelper.Lerp(0f, 0.3f, Utils.InverseLerp(0f, 24f, AttackTimer, true) * Utils.InverseLerp(180f, 30f, AttackTimer, true));
				npc.Opacity = Utils.InverseLerp(0f, 12f, AttackTimer, true);
				npc.velocity = Vector2.UnitY * MathHelper.Lerp(-40f, 0f, Utils.InverseLerp(0f, 30f, AttackTimer, true));

				// Stop if enough time has passed and the ideal direction is being aimed at.
				Vector2 idealDirection = npc.SafeDirectionTo(Target.Center);
				idealDirection = new Vector2(idealDirection.X, idealDirection.Y * 0.15f).SafeNormalize(Vector2.Zero);

				// Lunge in the ideal direction if enough time has passed or aiming in the direction of the ideal velocity.
				bool canLunge = npc.rotation.ToRotationVector2().AngleBetween(idealDirection) < 0.39f || AttackTimer > 175f;
				if (AttackTimer > 35f && canLunge)
				{
					npc.velocity = idealDirection * 19f;
					AttackTimer = 180f;
					npc.netUpdate = true;
				}
			}

			// Release rocks downward after being launched.
			else if (AttackTimer % 12f == 11f)
			{
				Main.PlaySound(SoundID.Item51, npc.Center);
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					int rockDamage = CalamityWorld.downedProvidence && !BossRushEvent.BossRushActive ? 340 : 205;
					Vector2 rockSpawnPosition = npc.Center + npc.rotation.ToRotationVector2() * Main.rand.NextFloatDirection() * 120f;
					Utilities.NewProjectileBetter(rockSpawnPosition, Vector2.UnitY * 5f, ModContent.ProjectileType<RockPiece>(), rockDamage, 0f);
				}
			}

			if (AttackTimer > 180f)
			{
				npc.damage = npc.defDamage;
				npc.rotation = npc.velocity.ToRotation();
			}

			// Die naturally after enough time has passed.
			if (AttackTimer > 330f)
			{
				npc.life = 0;
				npc.HitEffect();
				npc.checkDead();
			}

			AttackTimer++;
		}

		public override bool PreNPCLoot() => false;
	}
}
