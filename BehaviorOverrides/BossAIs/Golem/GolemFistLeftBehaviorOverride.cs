using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
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
            Main.NewText(leftFist);
            npc.dontTakeDamage = true;
            npc.chaseable = false;
            npc.Opacity = 1f;
            return false;
        }
    }
}
