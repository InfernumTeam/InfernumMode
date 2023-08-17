using CalamityMod;
using CalamityMod.NPCs.Ravager;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Ravager
{
    public class RavagerClawRightBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<RavagerClawRight>();

        public override void SetDefaults(NPC npc)
        {
            // Set defaults that, if were to be changed by Calamity, would cause significant issues to the fight.
            npc.width = 80;
            npc.height = 40;
            npc.scale = 1f;
            npc.defense = 40;
            npc.DR_NERD(0.15f);
            npc.alpha = 255;
        }

        public override bool PreAI(NPC npc) => RavagerClawLeftBehaviorOverride.DoClawAI(npc, false);

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor) => RavagerClawLeftBehaviorOverride.DrawClaw(npc, Main.spriteBatch, lightColor, false);
    }
}
