using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace InfernumMode.BehaviorOverrides.BossAIs
{
    public class FuckYouModeDrawEffects : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        #region Get Alpha
        public override Color? GetAlpha(NPC npc, Color drawColor)
        {
            // Give a dark tint to the moon lord.
            if (npc.type == NPCID.MoonLordHand ||
                npc.type == NPCID.MoonLordHead ||
                npc.type == NPCID.MoonLordCore)
            {
                if (PoDWorld.InfernumMode)
                    return new Color(7, 81, 81);
            }
            return base.GetAlpha(npc, drawColor);
        }
        #endregion

        #region Map Icon Manipulation
        public override void BossHeadSlot(NPC npc, ref int index)
        {
            if ((npc.type == ModContent.NPCType<CalamityMod.NPCs.DevourerofGods.DevourerofGodsHead>() ||
                npc.type == ModContent.NPCType<CalamityMod.NPCs.DevourerofGods.DevourerofGodsBody>() ||
                npc.type == ModContent.NPCType<CalamityMod.NPCs.DevourerofGods.DevourerofGodsTail>() ||
                npc.type == ModContent.NPCType<CalamityMod.NPCs.DevourerofGods.DevourerofGodsHeadS>() ||
                npc.type == ModContent.NPCType<CalamityMod.NPCs.DevourerofGods.DevourerofGodsBodyS>() ||
                npc.type == ModContent.NPCType<CalamityMod.NPCs.DevourerofGods.DevourerofGodsTailS>()) &&
                PoDWorld.InfernumMode && npc.alpha >= 252)
            {
                index = -1;
            }
        }
        #endregion

        #region Manual Drawing
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color drawColor)
        {
            if (PoDWorld.InfernumMode)
            {
                // DoG alpha effects
                if (npc.type == ModContent.NPCType<CalamityMod.NPCs.DevourerofGods.DevourerofGodsHead>() ||
                    npc.type == ModContent.NPCType<CalamityMod.NPCs.DevourerofGods.DevourerofGodsBody>() ||
                    npc.type == ModContent.NPCType<CalamityMod.NPCs.DevourerofGods.DevourerofGodsTail>() ||
                    npc.type == ModContent.NPCType<CalamityMod.NPCs.DevourerofGods.DevourerofGodsHeadS>() ||
                    npc.type == ModContent.NPCType<CalamityMod.NPCs.DevourerofGods.DevourerofGodsBodyS>() ||
                    npc.type == ModContent.NPCType<CalamityMod.NPCs.DevourerofGods.DevourerofGodsTailS>())
                {
                    if (npc.alpha >= 252)
                        return false;
                }

                if (OverridingListManager.InfernumPreDrawOverrideList.ContainsKey(npc.type))
                    return (bool)OverridingListManager.InfernumPreDrawOverrideList[npc.type].DynamicInvoke(npc, spriteBatch, drawColor);
            }
            return base.PreDraw(npc, spriteBatch, drawColor);
        }
        #endregion

        #region Healthbar Manipulation
        public override bool? DrawHealthBar(NPC npc, byte hbPosition, ref float scale, ref Vector2 position)
        {
            if (!PoDWorld.InfernumMode)
                return base.DrawHealthBar(npc, hbPosition, ref scale, ref position);

            if ((npc.type == ModContent.NPCType<CalamityMod.NPCs.DevourerofGods.DevourerofGodsHead>() ||
                npc.type == ModContent.NPCType<CalamityMod.NPCs.DevourerofGods.DevourerofGodsBody>() ||
                npc.type == ModContent.NPCType<CalamityMod.NPCs.DevourerofGods.DevourerofGodsTail>() ||
                npc.type == ModContent.NPCType<CalamityMod.NPCs.DevourerofGods.DevourerofGodsHeadS>() ||
                npc.type == ModContent.NPCType<CalamityMod.NPCs.DevourerofGods.DevourerofGodsBodyS>() ||
                npc.type == ModContent.NPCType<CalamityMod.NPCs.DevourerofGods.DevourerofGodsTailS>()) && npc.alpha >= 252)
            {
                return false;
            }

            if (npc.type == NPCID.EaterofWorldsBody)
                return false;

            return base.DrawHealthBar(npc, hbPosition, ref scale, ref position);
        }

        #endregion

        #region Frame Manipulation
        public override void FindFrame(NPC npc, int frameHeight)
        {
            if (OverridingListManager.InfernumFrameOverrideList.ContainsKey(npc.type) && PoDWorld.InfernumMode)
                OverridingListManager.InfernumFrameOverrideList[npc.type].DynamicInvoke(npc, frameHeight);
        }
        #endregion
    }
}