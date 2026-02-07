using CalamityMod.NPCs.HiveMind;
using InfernumMode.Core.OverridingSystem;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.HiveMind
{
    public class HiveBlobBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<HiveBlob>();

        public override bool PreAI(NPC npc)
        {
            if (npc.ai[2] > 0)
                npc.active = false;
            return false;
        }
    }
}
