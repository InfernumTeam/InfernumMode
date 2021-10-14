using CalamityMod.NPCs;
using CalamityMod.NPCs.Ravager;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Ravager
{
    public class RavagerLegLeftBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<RavagerLegLeft>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            // Fuck off if the main boss is gone.
            if (CalamityGlobalNPC.scavenger < 0 || !Main.npc[CalamityGlobalNPC.scavenger].active)
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            NPC ravager = Main.npc[CalamityGlobalNPC.scavenger];

            if (npc.timeLeft < 1800)
                npc.timeLeft = 1800;

            npc.Center = ravager.Center + new Vector2(-70f, 88f);
            npc.Opacity = ravager.Opacity;

            return false;
        }
    }
}
