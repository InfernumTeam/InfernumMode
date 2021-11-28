using CalamityMod;
using CalamityMod.NPCs.Abyss;
using InfernumMode.OverridingSystem;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AquaticScourge
{
	public class AEWHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<EidolonWyrmHeadHuge>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            return false;
        }
    }
}
