using CalamityMod.NPCs.DesertScourge;
using InfernumMode.Core.OverridingSystem;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.DesertScourge
{
    public class DesertScourgeHeadSmallBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<DesertNuisanceHead>();

        public override bool PreAI(NPC npc)
        {
            // Bye lmao!
            npc.active = false;
            return false;
        }
    }
}
