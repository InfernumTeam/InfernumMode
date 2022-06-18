using InfernumMode.OverridingSystem;
using Terraria;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.BossAIs.Golem
{
    public class GolemFistRightBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.GolemFistRight;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI; // | NPCOverrideContext.NPCPreDraw;

        public override bool PreAI(NPC npc) => GolemFistLeftBehaviorOverride.DoFistAI(npc, false);

        // public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor) => GolemFistLeftBehaviorOverride.DrawFist(npc, spriteBatch, lightColor, false);
    }
}
