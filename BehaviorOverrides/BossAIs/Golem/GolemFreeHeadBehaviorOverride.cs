using InfernumMode.OverridingSystem;
using Terraria;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.BossAIs.Golem
{
    public class GolemFreeHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.GolemHeadFree;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            return false;
        }
    }
}
