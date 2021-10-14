using InfernumMode.OverridingSystem;
using Terraria;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.BossAIs.Golem
{
    public class GolemHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.GolemHead;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            return false;
        }
    }
}
