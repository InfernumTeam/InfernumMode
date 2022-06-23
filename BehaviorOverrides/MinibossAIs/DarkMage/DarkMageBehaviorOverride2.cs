using InfernumMode.OverridingSystem;
using System;
using Terraria;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.DarkMage
{
    public class DarkMageBehaviorOverride2 : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.DD2DarkMageT3;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame;

        public override bool PreAI(NPC npc) => DarkMageBehaviorOverride.DoAI(npc);

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frame.Width = 80;
            npc.frame.Height = 80;
            npc.frame.Y = (int)Math.Round(npc.localAI[0]);
        }
    }
}
