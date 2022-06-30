using CalamityMod.Events;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.BossAIs.Deerclops
{
    public class DeerclopsBehaviorOverride : NPCBehaviorOverride
    {
        public enum DeerclopsAttackState
        {
            SpawnAnimation
        }

        public override int NPCOverrideType => NPCID.Deerclops;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            // Select a target as necessary.
            npc.TargetClosestIfTargetIsInvalid();

            Player target = Main.player[npc.target];
            ref float attackTimer = ref npc.ai[1];

            switch ((DeerclopsAttackState)npc.ai[0])
            {
                case DeerclopsAttackState.SpawnAnimation:
                    DoBehavior_SpawnAnimation(npc, target, ref attackTimer);
                    break;
            }
            attackTimer++;
            return false;
        }

        public static void DoBehavior_SpawnAnimation(NPC npc, Player target, ref float attackTimer)
        {
            
        }
    }
}
