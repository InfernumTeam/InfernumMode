using InfernumMode.OverridingSystem;
using Terraria;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.BossAIs.Golem
{
    public class GolemFistLeftBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.GolemFistLeft;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc) => DoClawAI(npc, true);

        public static bool DoClawAI(NPC npc, bool leftClaw)
        {
            return false;
        }
    }
}
