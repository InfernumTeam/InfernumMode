using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.CalClone;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.NPCs.Polterghast;
using InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasShadow;
using InfernumMode.Content.BehaviorOverrides.BossAIs.DoG;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares;
using InfernumMode.Content.BehaviorOverrides.BossAIs.MoonLord;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances
{
    public partial class GlobalNPCOverrides : GlobalNPC
    {
        #region Get Alpha
        public override Color? GetAlpha(NPC npc, Color drawColor)
        {
            // Give a dark tint to the moon lord.
            if (npc.type is NPCID.MoonLordHand or NPCID.MoonLordHead or NPCID.MoonLordCore)
            {
                if (InfernumMode.CanUseCustomAIs)
                    return Color.Lerp(drawColor, MoonLordCoreBehaviorOverride.OverallTint with { A = 187 } * 1.35f, 0.75f) * npc.Opacity;
            }

            if (npc.type == ModContent.NPCType<ThanatosHead>() ||
                npc.type == ModContent.NPCType<ThanatosBody1>() ||
                npc.type == ModContent.NPCType<ThanatosBody2>() ||
                npc.type == ModContent.NPCType<ThanatosTail>())
            {
                bool dealsNoContactDamage = npc.damage == 0;
                npc.Infernum().ExtraAI[20] = Clamp(npc.Infernum().ExtraAI[20] + dealsNoContactDamage.ToDirectionInt() * 0.025f, 0f, 1f);
                return Color.Lerp(drawColor * npc.Opacity, new Color(102, 74, 232, 0) * npc.Opacity * 0.6f, npc.Infernum().ExtraAI[20]);
            }

            return base.GetAlpha(npc, drawColor);
        }
        #endregion

        #region Map Icon Manipulation
        public override void BossHeadSlot(NPC npc, ref int index)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return;

            BossHeadSlotEvent?.Invoke(npc, ref index);
        }

        public override void BossHeadRotation(NPC npc, ref float rotation)
        {
            bool isDoG = npc.type == ModContent.NPCType<DevourerofGodsHead>() || npc.type == ModContent.NPCType<DevourerofGodsBody>() || npc.type == ModContent.NPCType<DevourerofGodsTail>();
            if (isDoG)
            {
                if (DoGPhase2HeadBehaviorOverride.InPhase2)
                    rotation = npc.rotation;
            }

            if (npc.type == ModContent.NPCType<Polterghast>())
                rotation = npc.rotation;
        }

        public override void BossHeadSpriteEffects(NPC npc, ref SpriteEffects spriteEffects)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return;

            if (npc.type == ModContent.NPCType<CalamitasClone>() || npc.type == ModContent.NPCType<Cataclysm>() || npc.type == ModContent.NPCType<Catastrophe>())
                spriteEffects = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
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

                if (NPCBehaviorOverride.BehaviorOverrides.TryGetValue(npc.type, out var value))
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
                    return value.PreDraw(npc, Main.spriteBatch, drawColor);
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

            // Don't draw HP bars if Ares is in the background.
            if (npc.realLife == CalamityGlobalNPC.draedonExoMechPrime && CalamityGlobalNPC.draedonExoMechPrime >= 0 && Math.Abs(Main.npc[CalamityGlobalNPC.draedonExoMechPrime].ai[2]) >= 0.25f)
                return false;

            if (npc.type == NPCID.EaterofWorldsBody)
                return false;

            return base.DrawHealthBar(npc, hbPosition, ref scale, ref position);
        }

        #endregion

        #region Layering Manipulation
        public override void DrawBehind(NPC npc, int index)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return;

            bool isAres = npc.whoAmI == CalamityGlobalNPC.draedonExoMechPrime || npc.realLife == CalamityGlobalNPC.draedonExoMechPrime;
            if (isAres && CalamityGlobalNPC.draedonExoMechPrime >= 0 && AresBodyBehaviorOverride.ShouldDrawBehindTiles && npc.hide)
            {
                Main.instance.DrawCacheNPCProjectiles.Remove(index);
                ScreenOverlaysSystem.DrawCacheBeforeBlack.Add(index);
            }
        }
        #endregion Layering Manipulation

        #region Frame Manipulation
        public override void FindFrame(NPC npc, int frameHeight)
        {
            if (InfernumMode.CanUseCustomAIs && NPCBehaviorOverride.BehaviorOverrides.TryGetValue(npc.type, out var value) && !npc.IsABestiaryIconDummy)
                value.FindFrame(npc, frameHeight);
        }
        #endregion

        #region Name Manipulation
        public override void ModifyTypeName(NPC npc, ref string typeName)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return;

            if (npc.type == ModContent.NPCType<CalamitasClone>())
                typeName = Utilities.GetLocalization("NameOverrides.CalamitasShadowClone.EntryName").Format(CalamitasShadowBehaviorOverride.CustomName);
            if (npc.type == ModContent.NPCType<Cataclysm>())
                typeName = CalamitasShadowBehaviorOverride.CustomNameCataclysm.Value;
            if (npc.type == ModContent.NPCType<Catastrophe>())
                typeName = CalamitasShadowBehaviorOverride.CustomNameCatastrophe.Value;
        }
        #endregion Name Manipulation
    }
}
