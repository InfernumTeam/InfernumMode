using CalamityMod.NPCs.Ravager;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Ravager
{
    public class RavagerClawRightBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<RavagerClawRight>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public override bool PreAI(NPC npc) => RavagerClawLeftBehaviorOverride.DoClawAI(npc, false);

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor) => RavagerClawLeftBehaviorOverride.DrawClaw(npc, spriteBatch, lightColor, false);
    }
}
