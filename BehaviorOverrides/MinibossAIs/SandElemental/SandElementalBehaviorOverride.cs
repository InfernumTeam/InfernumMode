using CalamityMod.NPCs.NormalNPCs;
using InfernumMode.OverridingSystem;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.SandElemental
{
    public class SandElementalBehaviorOverride : NPCBehaviorOverride
    {
        public enum SandElementalAttackState
		{
            HoverMovement,
            TornadoSummoning
		}

        public override int NPCOverrideType => ModContent.NPCType<ThiccWaifu>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame;

        public override bool PreAI(NPC npc)
        {
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float currentFrame = ref npc.localAI[0];

            switch ((SandElementalAttackState)(int)attackState)
            {
                case SandElementalAttackState.HoverMovement:
                    break;
            }

            return false;
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frame.Y = (int)(frameHeight * npc.localAI[0]);
        }
	}
}
