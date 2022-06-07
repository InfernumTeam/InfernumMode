using CalamityMod.NPCs;
using CalamityMod.NPCs.ProfanedGuardians;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.ProfanedGuardians
{
	public class DefenderGuardianBehaviorOverride : NPCBehaviorOverride
	{
		public override int NPCOverrideType => ModContent.NPCType<ProfanedGuardianBoss2>();

		public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

		public override bool PreAI(NPC npc)
		{
			if (!Main.npc.IndexInRange(CalamityGlobalNPC.doughnutBoss) || !Main.npc[CalamityGlobalNPC.doughnutBoss].active)
			{
				npc.active = false;
				npc.netUpdate = true;
				return false;
			}

			NPC thingToDefend = Main.npc[CalamityGlobalNPC.doughnutBoss];
			float thingToDefendLifeRatio = thingToDefend.life / (float)thingToDefend.lifeMax;
			ref float attackTimer = ref npc.Infernum().ExtraAI[0];

			// Defend the crystal guardian if it has a lower life ratio than the main boss.
			int healerIndex = NPC.FindFirstNPC(ModContent.NPCType<ProfanedGuardianBoss3>());
			if (Main.npc.IndexInRange(healerIndex) && Main.npc[healerIndex].life / (float)Main.npc[healerIndex].lifeMax < thingToDefendLifeRatio)
				thingToDefend = Main.npc[healerIndex];

			npc.target = thingToDefend.target;
			npc.damage = 0;
			npc.spriteDirection = thingToDefend.spriteDirection;
			npc.alpha = 128;

			// Cast rocks from time to time.
			float wrappedAttackTimer = attackTimer % 360f;
			if (wrappedAttackTimer > 330f && wrappedAttackTimer % 5f == 4f && !npc.WithinRange(Main.player[npc.target].Center, 250f))
			{
				Main.PlaySound(SoundID.Item89, npc.Center);
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					Utilities.NewProjectileBetter(npc.Center, -Vector2.UnitY * 6f, ModContent.ProjectileType<MagicProfanedRock>(), 235, 0f);
					npc.netUpdate = true;
				}
			}

			// Move around.
			Vector2 hoverDestination = thingToDefend.Center + (attackTimer / 75f).ToRotationVector2() * 200f;
			if (npc.velocity.Length() < 2f)
				npc.velocity = Vector2.UnitY * -2.4f;

			float flySpeed = MathHelper.Lerp(9f, 23f, Utils.InverseLerp(50f, 270f, npc.Distance(hoverDestination), true));
			flySpeed *= Utils.InverseLerp(0f, 50f, npc.Distance(hoverDestination), true);
			npc.velocity = npc.velocity * 0.85f + npc.SafeDirectionTo(hoverDestination) * flySpeed * 0.15f;
			npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(hoverDestination) * flySpeed, 4f);

			attackTimer++;
			return false;
		}
	}
}