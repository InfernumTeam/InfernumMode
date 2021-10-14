using InfernumMode.OverridingSystem;
using Terraria;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.BossAIs.Golem
{
    public class GolemFistLeftBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.GolemFistLeft;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc) => DoFistAI(npc, true);

        public static bool DoFistAI(NPC npc, bool leftFist)
        {
            return false;
        }
    }
}
