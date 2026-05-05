
using CalamityMod;
using CalamityMod.NPCs.Perforator;
using InfernumMode.Core.OverridingSystem;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Perforators
{
    public class SmallPerforatorBodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<PerforatorBodySmall>();

        public override void SetDefaults(NPC npc)
        {
            npc.Calamity().canBreakPlayerDefense = true;
        }
    }
}
