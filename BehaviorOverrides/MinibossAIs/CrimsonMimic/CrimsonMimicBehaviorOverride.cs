using InfernumMode.OverridingSystem;
using System;
using Terraria;
using Terraria.ID;

using static InfernumMode.BehaviorOverrides.MinibossAIs.CorruptionMimic.CorruptionMimicBehaviorOverride;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.CrimsonMimic
{
    public class CrimsonMimicBehaviorOverride : NPCBehaviorOverride
    {
        public enum CorruptionMimicAttackState
        {
            Inactive,
            RapidJumps,
            GroundPound,
        }

        public override int NPCOverrideType => NPCID.BigMimicCrimson;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame;

        public override bool PreAI(NPC npc)
        {
            // Pick a target.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Reset things.
            npc.defense = 10;
            npc.npcSlots = 16f;
            npc.knockBackResist = 0f;
            npc.noTileCollide = false;
            npc.noGravity = false;

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float isHostile = ref npc.ai[2];
            ref float currentFrame = ref npc.localAI[0];
            
            if ((npc.justHit || target.WithinRange(npc.Center, 200f)) && isHostile == 0f)
            {
                isHostile = 1f;
                attackTimer = 0f;
                npc.netUpdate = true;
            }

            switch ((CorruptionMimicAttackState)(int)attackState)
            {
                case CorruptionMimicAttackState.Inactive:
                    if (DoBehavior_Inactive(npc, target, isHostile == 1f, ref attackTimer, ref currentFrame))
                        SelectNextAttack(npc);
                    break;
                case CorruptionMimicAttackState.RapidJumps:
                    if (DoBehavior_RapidJumps(npc, target, false, ref attackTimer, ref currentFrame))
                        SelectNextAttack(npc);
                    break;
                case CorruptionMimicAttackState.GroundPound:
                    if (DoBehavior_GroundPound(npc, target, ref attackTimer, ref currentFrame))
                        SelectNextAttack(npc);
                    break;
            }
            attackTimer++;

            return false;
        }

        public static void SelectNextAttack(NPC npc)
        {
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            switch ((CorruptionMimicAttackState)npc.ai[0])
            {
                case CorruptionMimicAttackState.Inactive:
                    npc.ai[0] = (int)CorruptionMimicAttackState.RapidJumps;
                    break;
                case CorruptionMimicAttackState.RapidJumps:
                    npc.ai[0] = (int)CorruptionMimicAttackState.GroundPound;
                    break;
                case CorruptionMimicAttackState.GroundPound:
                    npc.ai[0] = (int)CorruptionMimicAttackState.RapidJumps;
                    break;
            }

            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frameCounter++;
            npc.frame.Y = (int)(frameHeight * Math.Round(npc.localAI[0]));
        }
    }
}
