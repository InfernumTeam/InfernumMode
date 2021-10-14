using InfernumMode.OverridingSystem;
using Terraria;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.BossAIs.Golem
{
    public class GolemFistRightBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.GolemFistRight;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc) => GolemFistLeftBehaviorOverride.DoFistAI(npc, false);
    }
}
