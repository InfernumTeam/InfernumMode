using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Golem
{
    public class GolemFreeHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.GolemHeadFree;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public override bool PreAI(NPC npc)
        {
            npc.chaseable = !npc.dontTakeDamage;
            npc.Opacity = npc.dontTakeDamage ? 0f : 1f;
            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Main.NewText("B");
            if (npc.dontTakeDamage)
                return false;

            Texture2D texture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Golem/FreeHead");
            Rectangle rect = new Rectangle(0, 0, texture.Width, texture.Height);
            Main.spriteBatch.Draw(texture, npc.Center - Main.screenPosition, rect, lightColor, npc.rotation, npc.Center, 1f, SpriteEffects.None, 0f);
            GolemHeadBehaviorOverride.DoEyeDrawing(npc);
            return false;
        }
    }
}
