using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.BossAIs.Golem
{
    public class GolemFreeHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.GolemHeadFree;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            // If not doing anything don't do anything
            if (npc.dontTakeDamage)
                return false;


            return false;
        }
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            GolemHeadBehaviorOverride.DoEyeDrawing(npc);
            return false;
        }
    }
}
