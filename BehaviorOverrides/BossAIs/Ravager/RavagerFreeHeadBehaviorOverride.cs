using CalamityMod.NPCs.Ravager;
using InfernumMode.OverridingSystem;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Ravager
{
	public class RavagerFreeHeadBehaviorOverride : NPCBehaviorOverride
	{
		public override int NPCOverrideType => ModContent.NPCType<RavagerHead2>();

		public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

		// No.
		public override bool PreAI(NPC npc)
		{
			npc.active = false;
			return false;
		}
	}
}
