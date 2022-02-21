using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class AEWBodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => InfernumMode.CalamityMod.NPCType("EidolonWyrmBodyHuge");

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            if (Main.npc.IndexInRange(npc.realLife) && Main.npc[npc.realLife].active)
                npc.Opacity = Main.npc[npc.realLife].Opacity;
            return true;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            AEWHeadBehaviorOverride.DrawSegment(spriteBatch, lightColor, npc);
            return false;
        }
    }

    public class AEWBody2BehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => InfernumMode.CalamityMod.NPCType("EidolonWyrmBodyAltHuge");

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            if (Main.npc.IndexInRange(npc.realLife) && Main.npc[npc.realLife].active)
                npc.Opacity = Main.npc[npc.realLife].Opacity;
            return true;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            AEWHeadBehaviorOverride.DrawSegment(spriteBatch, lightColor, npc);
            return false;
        }
    }

    public class AEWTailBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => InfernumMode.CalamityMod.NPCType("EidolonWyrmTailHuge");

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            if (Main.npc.IndexInRange(npc.realLife) && Main.npc[npc.realLife].active)
                npc.Opacity = Main.npc[npc.realLife].Opacity;
            return true;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            AEWHeadBehaviorOverride.DrawSegment(spriteBatch, lightColor, npc);
            return false;
        }
    }
}
