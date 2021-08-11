using CalamityMod.NPCs;
using CalamityMod.NPCs.Ravager;
using CalamityMod.NPCs.SlimeGod;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Ravager
{
    public class RavagerBodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<RavagerBody>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        #region Enumerations
        public enum RavagerAttackType
        {

        }
        #endregion

        #region AI

        public override bool PreAI(NPC npc)
        {
            return true;

            // Do targeting.
            npc.TargetClosest();
            Player target = Main.player[npc.target];



            return false;
        }
        #endregion AI
    }
}
