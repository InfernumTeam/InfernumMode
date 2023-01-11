using InfernumMode.Core.OverridingSystem;
using Terraria;
using Terraria.ID;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Golem
{
    public class GolemFistRightBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.GolemFistRight;

        public override bool PreAI(NPC npc) => GolemFistLeftBehaviorOverride.DoFistAI(npc, false);
    }
}
