using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.CalClone;
using CalamityMod.NPCs.Cryogen;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.NPCs.Leviathan;
using CalamityMod.NPCs.Polterghast;
using CalamityMod.NPCs.Signus;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.NPCs.Yharon;
using InfernumMode.Content.BehaviorOverrides.BossAIs.DoG;
using InfernumMode.Content.BehaviorOverrides.BossAIs.MoonLord;
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
    public class GlobalNPCDrawEffects : GlobalNPC
    {
        public override bool InstancePerEntity => true;

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
                npc.Infernum().ExtraAI[20] = MathHelper.Clamp(npc.Infernum().ExtraAI[20] + dealsNoContactDamage.ToDirectionInt() * 0.025f, 0f, 1f);
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

            bool isDoG = npc.type == ModContent.NPCType<DevourerofGodsHead>() || npc.type == ModContent.NPCType<DevourerofGodsBody>() || npc.type == ModContent.NPCType<DevourerofGodsTail>();
            if (isDoG)
            {
                if (npc.Opacity <= 0.02f)
                {
                    index = -1;
                    return;
                }

                bool inPhase2 = DoGPhase2HeadBehaviorOverride.InPhase2;
                if (npc.type == ModContent.NPCType<DevourerofGodsHead>())
                    index = inPhase2 ? DevourerofGodsHead.phase2IconIndex : DevourerofGodsHead.phase1IconIndex;
                else if (npc.type == ModContent.NPCType<DevourerofGodsBody>())
                    index = inPhase2 ? DevourerofGodsBody.phase2IconIndex : -1;
                else if (npc.type == ModContent.NPCType<DevourerofGodsTail>())
                    index = inPhase2 ? DevourerofGodsTail.phase2IconIndex : DevourerofGodsTail.phase1IconIndex;
            }

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
                index = ModContent.GetModBossHeadSlot("InfernumMode/Content/BehaviorOverrides/BossAIs/Cryogen/CryogenMapIcon");

            // Have Dreadnautilus use a custom map icon.
            if (npc.type == NPCID.BloodNautilus)
                index = ModContent.GetModBossHeadSlot("InfernumMode/Content/BehaviorOverrides/BossAIs/Dreadnautilus/DreadnautilusMapIcon");

            // Have CalClone and her brothers use a custom map icon.
            if (npc.type == ModContent.NPCType<CalamitasClone>())
                index = ModContent.GetModBossHeadSlot("InfernumMode/Content/BehaviorOverrides/BossAIs/CalamitasClone/CalCloneMapIcon");
            if (npc.type == ModContent.NPCType<Cataclysm>())
                index = ModContent.GetModBossHeadSlot("InfernumMode/Content/BehaviorOverrides/BossAIs/CalamitasClone/CataclysmMapIcon");
            if (npc.type == ModContent.NPCType<Catastrophe>())
                index = ModContent.GetModBossHeadSlot("InfernumMode/Content/BehaviorOverrides/BossAIs/CalamitasClone/CatastropheMapIcon");

            // Have Sepulcher use a custom map icon.
            if (npc.type == ModContent.NPCType<SepulcherHead>())
                index = ModContent.GetModBossHeadSlot("InfernumMode/Content/BehaviorOverrides/BossAIs/SupremeCalamitas/SepulcherMapIcon");
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

                if (OverridingListManager.InfernumPreDrawOverrideList.TryGetValue(npc.type, out OverridingListManager.NPCPreDrawDelegate value))
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
                    return value.Invoke(npc, Main.spriteBatch, drawColor);
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

        #region Frame Manipulation
        public override void FindFrame(NPC npc, int frameHeight)
        {
            if (OverridingListManager.InfernumFrameOverrideList.TryGetValue(npc.type, out OverridingListManager.NPCFindFrameDelegate value) && InfernumMode.CanUseCustomAIs && !npc.IsABestiaryIconDummy)
                value.Invoke(npc, frameHeight);
        }
        #endregion
    }
}