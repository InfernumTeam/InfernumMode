using CalamityMod.NPCs.CalClone;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

using CalCloneNPC = CalamityMod.NPCs.CalClone.CalamitasClone;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasClone
{
    public class CatastropheBehaviorOverride : NPCBehaviorOverride
    {
        public override int? NPCIDToDeferToForTips => ModContent.NPCType<CalCloneNPC>();

        public override int NPCOverrideType => ModContent.NPCType<Catastrophe>();

        public override bool PreAI(NPC npc)
        {
            CataclysmBehaviorOverride.DoAI(npc);
            return false;
        }

        #region Frames and Drawcode
        public override void FindFrame(NPC npc, int frameHeight)
        {
            int xFrame = 0;
            int yFrame = 0;
            npc.frame.Width = 82;
            npc.frame.Height = 172;
            npc.frame.X = xFrame * npc.frame.Width;
            npc.frame.Y = yFrame * npc.frame.Height;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor) => CataclysmBehaviorOverride.DrawBrother(npc, spriteBatch, lightColor);
        #endregion Frames and Drawcode
    }
}