using CalamityMod;
using CalamityMod.NPCs.Cryogen;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.NPCs.Leviathan;
using CalamityMod.NPCs.Signus;
using CalamityMod.NPCs.Yharon;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.GlobalInstances
{
    public class GlobalNPCDrawEffects : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        #region Map Icon Manipulation
        public override void BossHeadSlot(NPC npc, ref int index)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return;

            // Make Anahita completely invisible on the map when sufficiently faded out.
            if (npc.type == ModContent.NPCType<Anahita>() && npc.Opacity < 0.1f)
                index = -1;

            // Make Signus completely invisible on the map.
            if (npc.type == ModContent.NPCType<Signus>())
                index = -1;

            // Prevent Yharon from showing himself amongst his illusions in Subphase 10.
            if (npc.type == ModContent.NPCType<Yharon>())
            {
                if (npc.life / (float)npc.lifeMax <= 0.05f && npc.Infernum().ExtraAI[2] == 1f)
                    index = -1;
            }

            // Have Cryogen use a custom map icon.
            if (npc.type == ModContent.NPCType<Cryogen>())
                index = ModContent.GetModBossHeadSlot("InfernumMode/BehaviorOverrides/BossAIs/Cryogen/CryogenMapIcon");
        }

        #endregion

        #region Manual Drawing
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (InfernumMode.CanUseCustomAIs && !npc.IsABestiaryIconDummy)
            {
                bool isDoG = npc.type == ModContent.NPCType<DevourerofGodsHead>() || npc.type == ModContent.NPCType<DevourerofGodsBody>() || npc.type == ModContent.NPCType<DevourerofGodsTail>();
                if (isDoG && npc.alpha >= 252)
                    return false;

                if (OverridingListManager.InfernumPreDrawOverrideList.ContainsKey(npc.type))
                {
                    if (Main.LocalPlayer.Calamity().trippy)
                    {
                        SpriteEffects direction = SpriteEffects.None;
                        if (npc.spriteDirection == 1)
                            direction = SpriteEffects.FlipHorizontally;

                        Vector2 origin = npc.frame.Size() * 0.5f;
                        Color shroomColor = npc.GetAlpha(new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB, 0));
                        float colorFadeFactor = 0.99f;
                        shroomColor.R = (byte)(shroomColor.R * colorFadeFactor);
                        shroomColor.G = (byte)(shroomColor.G * colorFadeFactor);
                        shroomColor.B = (byte)(shroomColor.B * colorFadeFactor);
                        shroomColor.A = (byte)(shroomColor.A * colorFadeFactor);
                        for (int i = 0; i < 4; i++)
                        {
                            Vector2 drawPosition = npc.Center;
                            float horizontalOffset = Math.Abs(npc.Center.X - Main.LocalPlayer.Center.X);
                            float verticalOffset = Math.Abs(npc.Center.Y - Main.LocalPlayer.Center.Y);

                            if (i is 0 or 2)
                                drawPosition.X = Main.LocalPlayer.Center.X + horizontalOffset;
                            else
                                drawPosition.X = Main.LocalPlayer.Center.X - horizontalOffset;

                            if (i is 0 or 1)
                                drawPosition.Y = Main.LocalPlayer.Center.Y + verticalOffset;
                            else
                                drawPosition.Y = Main.LocalPlayer.Center.Y - verticalOffset;
                            drawPosition.Y += npc.gfxOffY;
                            drawPosition -= Main.screenPosition;

                            Main.spriteBatch.Draw(TextureAssets.Npc[npc.type].Value, drawPosition, npc.frame, shroomColor, npc.rotation, origin, npc.scale, direction, 0f);
                        }
                    }
                    return OverridingListManager.InfernumPreDrawOverrideList[npc.type].Invoke(npc, Main.spriteBatch, drawColor);
                }
            }
            return base.PreDraw(npc, Main.spriteBatch, screenPos, drawColor);
        }
        #endregion

        #region Healthbar Manipulation
        public override bool? DrawHealthBar(NPC npc, byte hbPosition, ref float scale, ref Vector2 position)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return base.DrawHealthBar(npc, hbPosition, ref scale, ref position);

            if (npc.type is NPCID.CultistBoss or NPCID.CultistBossClone)
                scale = 1f;

            bool isDoG = npc.type == ModContent.NPCType<DevourerofGodsHead>() || npc.type == ModContent.NPCType<DevourerofGodsBody>() || npc.type == ModContent.NPCType<DevourerofGodsTail>();
            if (isDoG && npc.alpha >= 252)
                return false;

            if (npc.type == NPCID.EaterofWorldsBody)
                return false;

            return base.DrawHealthBar(npc, hbPosition, ref scale, ref position);
        }

        #endregion

        #region Frame Manipulation
        public override void FindFrame(NPC npc, int frameHeight)
        {
            if (OverridingListManager.InfernumFrameOverrideList.ContainsKey(npc.type) && InfernumMode.CanUseCustomAIs)
                OverridingListManager.InfernumFrameOverrideList[npc.type].DynamicInvoke(npc, frameHeight);
        }
        #endregion
    }
}