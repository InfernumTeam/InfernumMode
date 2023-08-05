using CalamityMod.NPCs.NormalNPCs;
using InfernumMode.Core.OverridingSystem;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Plantera
{
    public class PlanteraFreeTentacleBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<PlanterasFreeTentacle>();

        public override bool PreAI(NPC npc)
        {
            // Nope.
            npc.active = false;
            return false;
        }
    }
}
