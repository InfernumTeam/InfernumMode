using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class AEWBodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => InfernumMode.CalamityMod.Find<ModNPC>("EidolonWyrmBodyHuge").Type;

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
        public override int NPCOverrideType => InfernumMode.CalamityMod.Find<ModNPC>("EidolonWyrmBodyAltHuge").Type;

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
        public override int NPCOverrideType => InfernumMode.CalamityMod.Find<ModNPC>("EidolonWyrmTailHuge").Type;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            if (Main.npc.IndexInRange(npc.realLife) && Main.npc[npc.realLife].active)
                npc.Opacity = Main.npc[npc.realLife].Opacity;
            npc.dontTakeDamage = Main.npc[npc.realLife].ai[0] != (int)AEWHeadBehaviorOverride.AEWAttackType.ImpactTail;

            // Become invincible again if the shield was not destroyed in time.
            if (!npc.dontTakeDamage && Main.npc[npc.realLife].ai[1] >= Main.npc[npc.realLife].Infernum().ExtraAI[2])
                npc.dontTakeDamage = true;

            npc.chaseable = !npc.dontTakeDamage;

            return true;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            AEWHeadBehaviorOverride.DrawSegment(spriteBatch, lightColor, npc);
            return false;
        }
    }
}
