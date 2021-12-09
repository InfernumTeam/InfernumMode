using CalamityMod.NPCs.Abyss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class AEWBodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<EidolonWyrmBodyHuge>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCPreDraw;

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            AEWHeadBehaviorOverride.DrawSegment(spriteBatch, lightColor, npc);
            return false;
        }
    }

    public class AEWBody2BehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<EidolonWyrmBodyAltHuge>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCPreDraw;

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            AEWHeadBehaviorOverride.DrawSegment(spriteBatch, lightColor, npc);
            return false;
        }
    }

    public class AEWTailBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<EidolonWyrmTailHuge>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCPreDraw;

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            AEWHeadBehaviorOverride.DrawSegment(spriteBatch, lightColor, npc);
            return false;
        }
    }
}
