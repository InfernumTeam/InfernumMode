using CalamityMod;
using CalamityMod.NPCs.AquaticScourge;
using InfernumMode.Core.OverridingSystem;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AquaticScourge
{
    public class AquaticScourgeTailBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<AquaticScourgeTail>();

        public override void SetDefaults(NPC npc)
        {
            // Set defaults that, if were to be changed by Calamity, would cause significant issues to the fight.
            npc.width = 32;
            npc.height = 32;
            npc.scale = 1f;
            npc.Opacity = 1f;
            npc.defense = 25;
            npc.DR_NERD(0.15f);
            npc.alpha = 255;
            npc.chaseable = false;
        }

        public override bool PreAI(NPC npc)
        {
            AquaticScourgeBodyBehaviorOverride.DoAI(npc);
            return false;
        }
    }
}
