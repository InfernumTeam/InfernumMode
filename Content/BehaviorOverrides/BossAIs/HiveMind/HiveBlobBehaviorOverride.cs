using CalamityMod.NPCs.HiveMind;
using InfernumMode.Core.OverridingSystem;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.HiveMind
{
    public class HiveBlobBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<HiveBlob2>();

        public override bool PreAI(NPC npc)
        {
            // How about no?
            npc.active = false;
            return false;
        }
    }
}
