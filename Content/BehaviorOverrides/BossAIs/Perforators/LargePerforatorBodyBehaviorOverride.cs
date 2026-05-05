
using CalamityMod;
using CalamityMod.NPCs.Perforator;
using InfernumMode.Core.OverridingSystem;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Perforators
{
    public class LargePerforatorBodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<PerforatorBodyLarge>();

        public override void SetDefaults(NPC npc)
        {
            npc.Calamity().canBreakPlayerDefense = true;
        }
    }
}
