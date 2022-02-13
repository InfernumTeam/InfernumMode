using CalamityMod.NPCs.NormalNPCs;
using InfernumMode.OverridingSystem;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.EarthElemental
{
    public class EarthElementalBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<Horse>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame;

        // AI stuff here.
        // I suggest looking at existing boss AIs as references with this.
        // Certain tricks make things easier in the long run.
        public override bool PreAI(NPC npc)
        {
            return false;
        }

        // Frame stuff here.
        public override void FindFrame(NPC npc, int frameHeight)
        {

        }
    }
}
