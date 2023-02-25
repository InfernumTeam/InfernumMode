using CalamityMod.NPCs.AquaticScourge;
using InfernumMode.Core.OverridingSystem;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AquaticScourge
{
    public class AquaticScourgeTailBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<AquaticScourgeTail>();

        public override bool PreAI(NPC npc)
        {
            AquaticScourgeBodyBehaviorOverride.DoAI(npc);
            return false;
        }
    }
}
