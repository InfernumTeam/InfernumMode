using CalamityMod;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.NPCs.Providence;
using CalamityMod.NPCs.Yharon;
using CalamityMod.Particles;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Fonts;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Yharon;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using static CalamityMod.UI.BossHealthBarManager;
using static InfernumMode.Content.BossBars.BossBarManager;

namespace InfernumMode.Content.BossBars
{
    // A lot of this is yoinked from BossHPUI in base Calamity, considering they function pretty much the same internally.
    public class BaseBossBar
    {
        #region Fields/Properties
        public FireParticleSet EnrageParticleSet = new(-1, int.MaxValue, Color.Yellow, Color.Red * 1.2f, 10f, 0.65f);

        public List<PhaseNotch> PhaseNotches;

        public int NPCIndex = -1;

        public int EnrageTimer;

        public int IncreasingDefenseOrDRTimer;

        public int InvincibilityTimer;

        public int OpenAnimationTimer;

        public int CloseAnimationTimer;

        public long InitialMaxLife;

        public long PreviousLife;

        public float PreviousRatio;

        /// <summary>
        /// The type of the NPC this bar is indended for.
        /// </summary>
        public int IntendedNPCType;

        public Texture2D BossIcon
        {
            get;
            private set;
        }

        public NPC AssociatedNPC
        {
            get
            {
                if (!Main.npc.IndexInRange(NPCIndex))
                    return null;
                return Main.npc[NPCIndex];
            }
        }

        public int NPCType => AssociatedNPC?.type ?? (-1);

        public long CombinedNPCLife
        {
            get
            {
                if (AssociatedNPC == null || !AssociatedNPC.active)
                    return 0L;

                long life = AssociatedNPC.life;
                foreach (KeyValuePair<NPCSpecialHPGetRequirement, NPCSpecialHPGetFunction> requirement in SpecialHPRequirements)
                    if (requirement.Key(AssociatedNPC))
                        return requirement.Value(AssociatedNPC, checkingForMaxLife: false);
                if (!OneToMany.ContainsKey(NPCType))
                    return life;

                for (int i = 0; i < Main.maxNPCs; i++)
                    if (Main.npc[i].active && Main.npc[i].life > 0 && OneToMany[NPCType].Contains(Main.npc[i].type))
                        life += Main.npc[i].life;

                return life;
            }
        }

        public long CombinedNPCMaxLife
        {
            get
            {
                if (AssociatedNPC == null || !AssociatedNPC.active)
                    return 0L;

                long maxLife = AssociatedNPC.lifeMax;

                foreach (KeyValuePair<NPCSpecialHPGetRequirement, NPCSpecialHPGetFunction> requirement in SpecialHPRequirements)
                    if (requirement.Key(AssociatedNPC))
                        return requirement.Value(AssociatedNPC, checkingForMaxLife: true);

                if (!OneToMany.ContainsKey(NPCType))
                    return maxLife;

                for (int i = 0; i < Main.maxNPCs; i++)
                    if (Main.npc[i].active && Main.npc[i].life > 0 && OneToMany[NPCType].Contains(Main.npc[i].type))
                        maxLife += Main.npc[i].lifeMax;

                return maxLife;
            }
        }

        public bool NPCIsEnraged
        {
            get
            {
                if (AssociatedNPC == null || !AssociatedNPC.active)
                    return false;
                if (AssociatedNPC.Calamity().CurrentlyEnraged)
                    return true;
                if (!OneToMany.ContainsKey(NPCType))
                    return false;
                for (int i = 0; i < Main.maxNPCs; i++)
                    if (Main.npc[i].active && Main.npc[i].life > 0 && OneToMany[NPCType].Contains(Main.npc[i].type) && Main.npc[i].Calamity().CurrentlyEnraged)
                        return true;
                return false;
            }
        }

        public bool NPCIsIncreasingDefenseOrDR
        {
            get
            {
                if (AssociatedNPC == null || !AssociatedNPC.active)
                {
                    return false;
                }
                if (AssociatedNPC.Calamity().CurrentlyIncreasingDefenseOrDR)
                {
                    return true;
                }
                if (!OneToMany.ContainsKey(NPCType))
                {
                    return false;
                }
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].life > 0 && OneToMany[NPCType].Contains(Main.npc[i].type) && (Main.npc[i].Calamity().CurrentlyIncreasingDefenseOrDR || Main.npc[i].Calamity().DR > 0.98f))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool NPCIsInvincible
        {
            get
            {
                if (AssociatedNPC == null || !AssociatedNPC.active)
                {
                    return false;
                }
                if (AssociatedNPC.dontTakeDamage)
                {
                    return true;
                }
                if (!OneToMany.ContainsKey(NPCType))
                {
                    return false;
                }
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].life > 0 && OneToMany[NPCType].Contains(Main.npc[i].type) && Main.npc[i].dontTakeDamage)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        #endregion

        #region Method
        public BaseBossBar(int index)
        {
            NPCIndex = index;
            if (AssociatedNPC != null && AssociatedNPC.active)
            {
                IntendedNPCType = AssociatedNPC.type;
                PreviousLife = CombinedNPCLife;
            }

            PhaseNotches = new();

            int length = GetTotalPhaseIndicators();
            for (int i = 0; i < length; i++)
                PhaseNotches.Add(new());

            BossIcon = BaseIcon;
        }

        public void Update()
        {
            PreviousLife = CombinedNPCLife;
            if (AssociatedNPC == null || !AssociatedNPC.active || NPCType != IntendedNPCType || AssociatedNPC.Calamity().ShouldCloseHPBar)
            {
                EnrageTimer = Utils.Clamp(EnrageTimer - 4, 0, 120);
                IncreasingDefenseOrDRTimer = Utils.Clamp(IncreasingDefenseOrDRTimer - 4, 0, 120);
                CloseAnimationTimer = Utils.Clamp(CloseAnimationTimer + 1, 0, 30);
                return;
            }

            // Update timers.
            OpenAnimationTimer = Utils.Clamp(OpenAnimationTimer + 1, 0, 25);
            EnrageTimer = Utils.Clamp(EnrageTimer + NPCIsEnraged.ToDirectionInt(), 0, 45);
            IncreasingDefenseOrDRTimer = Utils.Clamp(IncreasingDefenseOrDRTimer + NPCIsIncreasingDefenseOrDR.ToDirectionInt(), 0, 20);
            InvincibilityTimer = Utils.Clamp(InvincibilityTimer + NPCIsInvincible.ToDirectionInt(), 0, 20);

            // Update the enrage fire particles.
            if (EnrageTimer > 0)
                EnrageParticleSet.ParticleSpawnRate = (int)Lerp(600f, 4f, Utils.GetLerpValue(0f, 45f, EnrageTimer, true));
            else
                EnrageParticleSet.ParticleSpawnRate = int.MaxValue;
            EnrageParticleSet.Update();

            if (CombinedNPCMaxLife != 0L && (InitialMaxLife == 0L || InitialMaxLife < CombinedNPCMaxLife))
                InitialMaxLife = CombinedNPCMaxLife;

            // Attempt to acquire a head icon.
            int headIndex = AssociatedNPC.GetBossHeadTextureIndex();
            if (TextureAssets.NpcHeadBoss.IndexInRange(headIndex))
                BossIcon = TextureAssets.NpcHeadBoss[headIndex].Value;
            if (AssociatedNPC.type == ModContent.NPCType<Draedon>())
                BossIcon = ModContent.Request<Texture2D>("CalamityMod/Items/Armor/Vanity/DraedonMask").Value;
            else if (AssociatedNPC.type == NPCID.MoonLordCore)
                BossIcon = TextureAssets.NpcHeadBoss[8].Value;
            else if (AssociatedNPC.type == NPCID.Golem)
                BossIcon = TextureAssets.NpcHeadBoss[5].Value;

            // Swap to the mod call icon if it exists.
            if (ModCallBossIcons.TryGetValue(NPCType, out var icon))
                BossIcon = icon;

            // Ensure there is the correct amount of indicators.
            int indicatorAmount = GetTotalPhaseIndicators();
            if (PhaseNotches.Count > indicatorAmount)
            {
                do
                {
                    PhaseNotches.RemoveAt(PhaseNotches.Count - 1);
                }
                while (PhaseNotches.Count > indicatorAmount);
            }
            else if (PhaseNotches.Count < indicatorAmount)
            {
                do
                {
                    PhaseNotches.Add(new());
                }
                while (PhaseNotches.Count < indicatorAmount);
            }
        }

        private float GetCurrentRatio(out int currentPhase)
        {
            float baseRatio = (float)CombinedNPCLife / CombinedNPCMaxLife;
            currentPhase = 1;
            if ((PhaseInfos.TryGetValue(NPCType, out BossPhaseInfo phaseInfo) && InfernumMode.CanUseCustomAIs) || ModCallPhaseInfos.TryGetValue(NPCType, out phaseInfo))
            {
                float startingHealthPercentForPhase = 1f;
                float endingHealthPercentForPhase = 0f;

                int phaseCount = phaseInfo.PhaseCount;
                List<float> phaseThresholds = phaseInfo.PhaseThresholds;

                if (NPCType == ModContent.NPCType<Yharon>())
                {
                    // This is already hardcoded so I dont care.
                    phaseCount = 4;
                    if (!YharonBehaviorOverride.InSecondPhase)
                        phaseThresholds = new List<float> { phaseThresholds[0], phaseThresholds[1], phaseThresholds[2], phaseThresholds[3] };
                    else
                        phaseThresholds = new List<float> { 1f, phaseThresholds[4], phaseThresholds[5], phaseThresholds[6], phaseThresholds[7] };
                }

                for (int i = 0; i < phaseCount; i++)
                {
                    float currentRatioToCheck = phaseThresholds[i];
                    int index = i;

                    if (baseRatio <= currentRatioToCheck)
                    {
                        if (i != phaseCount - 1)
                            continue;

                        currentRatioToCheck = 0f;
                        index++;
                    }
                    endingHealthPercentForPhase = currentRatioToCheck;

                    if (i > 0)
                    {
                        currentPhase = index;
                        startingHealthPercentForPhase = phaseThresholds[index - 1];
                    }
                    break;
                }

                if (AssociatedNPC.Calamity().ShouldCloseHPBar)
                    currentPhase = GetTotalPhaseIndicators() + 1;

                if (endingHealthPercentForPhase == startingHealthPercentForPhase)
                    return 0f;

                // Calculate the relative life ratio for the phase. If the phase starts at 100% and ends at 75%, it will return 0 for 100% and 1 for 75%.
                float phaseHealthInterpolant = Utils.GetLerpValue(endingHealthPercentForPhase, startingHealthPercentForPhase, baseRatio, true);
                return phaseHealthInterpolant;
            }

            // Just return this as a failsafe.
            if (float.IsNaN(baseRatio))
                return 0f;
            return baseRatio;
        }

        private int GetTotalPhaseIndicators()
        {
            // Isn't this AMAZING!? Deciding to make yharon (and any boss for that matter) regen HP was a bad idea for things like this.
            if ((PhaseInfos.TryGetValue(NPCType, out BossPhaseInfo phaseInfo) && InfernumMode.CanUseCustomAIs) || ModCallPhaseInfos.TryGetValue(NPCType, out phaseInfo))
            {
                if (phaseInfo.NPCType == ModContent.NPCType<Yharon>())
                    return phaseInfo.PhaseCount / 2;
                return phaseInfo.PhaseCount;
            }
            return 1;
        }

        public void Draw(SpriteBatch spriteBatch, int x, int y)
        {
            float currentRatio = GetCurrentRatio(out int currentPhase);
            Vector2 barCenter = new(x, y);
            float mainOpacity = Utils.GetLerpValue(0f, 25f, OpenAnimationTimer, true) * Utils.GetLerpValue(30f, 0f, CloseAnimationTimer, true);
            Color drawColor = Color.White * mainOpacity;

            // Draw the frame.
            spriteBatch.Draw(BarFrame, barCenter, null, drawColor, 0f, BarFrame.Size() * 0.5f, 1f, SpriteEffects.None, 0f);

            Vector2 leftBarTipPos = barCenter + new Vector2(-147f, 0f);
            Vector2 rightBarTipPos = barCenter + new Vector2(84f, 0f);
            Vector2 mainBarTipPos = Vector2.Lerp(rightBarTipPos, leftBarTipPos, Clamp(currentRatio, 0f, 1f));

            // Draw the HP bar.
            Vector2 hpBarLeftPos = leftBarTipPos + new Vector2(10f, 0f);
            Vector2 hpBarRightPos = rightBarTipPos + new Vector2(14f, 0f);
            float hpBarWidthMax = hpBarLeftPos.Distance(hpBarRightPos);
            Vector2 hpScale = new(hpBarWidthMax * currentRatio, 33f);
            Vector2 hpOrigin = new(1f, 0.5f);

            Color mainBarColor = new(153, 24, 51);
            Color bloomColor = new(208, 47, 63);

            spriteBatch.End();
            PlayerInput.SetZoom_UI();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, null, null, null, null, null, Main.UIScaleMatrix);

            Effect barShader = InfernumEffectsRegistry.BossBarShader.GetShader().Shader;
            barShader.Parameters["pixelationAmount"].SetValue(4f);
            barShader.Parameters["lifeRatio"].SetValue(currentRatio);
            barShader.Parameters["opacity"].SetValue(mainOpacity);
            barShader.Parameters["mainColor"].SetValue(mainBarColor.ToVector3());
            barShader.Parameters["bloomColor"].SetValue(bloomColor.ToVector3());
            barShader.CurrentTechnique.Passes[0].Apply();

            spriteBatch.Draw(InfernumTextureRegistry.Pixel.Value, hpBarRightPos, null, drawColor, 0f, hpOrigin, hpScale, SpriteEffects.None, 0f);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);

            // Draw the invincibility overlay.
            if ((InvincibilityTimer > 0 || IncreasingDefenseOrDRTimer > 0) && AssociatedNPC.type != NPCID.MoonLordCore)
            {
                float invincibilityTimerInterpolant = InvincibilityTimer / 20f;
                float drTimerInterpolant = IncreasingDefenseOrDRTimer / 20f;
                float activeInterpolant = drTimerInterpolant;
                if (InvincibilityTimer > 0)
                    activeInterpolant = invincibilityTimerInterpolant;
                Vector2 invincibilityBarStart = mainBarTipPos + new Vector2(4f + (1f - currentRatio) * 10f, 0f);
                Rectangle barCutoff = new((int)((1f - currentRatio) * InvincibilityOverlay.Width), 0, (int)(currentRatio * InvincibilityOverlay.Width), InvincibilityOverlay.Height);
                Color color;
                if (invincibilityTimerInterpolant > 0)
                    color = Color.White;
                else
                    color = new(70, 70, 90);
                spriteBatch.Draw(InvincibilityOverlay, invincibilityBarStart, barCutoff, color * mainOpacity * activeInterpolant, 0f, InvincibilityOverlay.Size() * new Vector2(0f, 0.5f), 1f, SpriteEffects.None, 0f);
            }

            // Draw the icon frame.
            spriteBatch.Draw(IconFrame, barCenter, null, drawColor, 0f, IconFrame.Size() * 0.5f, 1f, SpriteEffects.None, 0f);

            // Draw the icon.
            float idealIconSize = 40f;
            float actualIconSize = MathF.Max(BossIcon.Width, BossIcon.Height);
            float iconScaleNeeded = idealIconSize / actualIconSize;

            // Provi is very wide.
            if (NPCType == ModContent.NPCType<Providence>())
                iconScaleNeeded *= 1.55f;

            Vector2 iconDrawPos = barCenter + new Vector2(135f, 0f);
            Color afterimageColor = Color.White;

            if (NPCIsEnraged || EnrageTimer > 0)
            {
                EnrageParticleSet.DrawSet(iconDrawPos + Vector2.UnitY * 4f + Main.screenPosition);
                EnrageParticleSet.SpawnAreaCompactness = 18f;
                EnrageParticleSet.RelativePower = 0.4f;
                afterimageColor = Color.Lerp(Color.White, Color.OrangeRed, Utils.GetLerpValue(0f, 45f, EnrageTimer, true));
            }

            for (int i = 0; i < 12; i++)
            {
                Vector2 backglowOffset = (TwoPi * i / 12f).ToRotationVector2() * 3f;
                spriteBatch.Draw(BossIcon, iconDrawPos + backglowOffset, null, afterimageColor with { A = 0 } * 0.5f * Pow(mainOpacity, 2f), 0f, BossIcon.Size() * 0.5f, iconScaleNeeded, SpriteEffects.None, 0f);
            }
            spriteBatch.Draw(BossIcon, iconDrawPos, null, Color.Lerp(drawColor, afterimageColor * mainOpacity, 0.75f), 0f, BossIcon.Size() * 0.5f, iconScaleNeeded, SpriteEffects.None, 0f);

            // Draw the tip.
            spriteBatch.Draw(MainBarTip, mainBarTipPos, null, drawColor, 0f, MainBarTip.Size() * 0.5f, 1f, SpriteEffects.None, 0f);

            // Draw the phase indicators.
            Vector2 rightPhaseIndicatorShellDrawPos = barCenter + new Vector2(114f, -38f);

            // Draw the middle connectors.
            float phaseShellXPos = rightPhaseIndicatorShellDrawPos.X - PhaseIndicatorEnd.Width * 0.5f - PhaseIndicatorMiddle.Width * 0.5f + 4f;
            float phaseNotchXPos = phaseShellXPos + 6f;
            for (int i = 0; i < PhaseNotches.Count; i++)
            {
                bool shouldBePopped = ((PhaseNotches.Count - i) < currentPhase);
                Vector2 phaseNotchDrawPos = new(phaseNotchXPos, rightPhaseIndicatorShellDrawPos.Y + 3f);
                PhaseNotches[i].Draw(spriteBatch, phaseNotchDrawPos, drawColor, shouldBePopped, CloseAnimationTimer > 0 || AssociatedNPC.type != ModContent.NPCType<Yharon>());
                phaseNotchXPos -= 15f;

                if (i < PhaseNotches.Count - 1)
                {
                    Vector2 middleDrawPos = new(phaseShellXPos, rightPhaseIndicatorShellDrawPos.Y - 9f);
                    spriteBatch.Draw(PhaseIndicatorMiddle, middleDrawPos, null, drawColor, 0f, PhaseIndicatorMiddle.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
                    phaseShellXPos -= 15f;
                }
            }

            // Draw the rightmost piece.
            spriteBatch.Draw(PhaseIndicatorEnd, rightPhaseIndicatorShellDrawPos, null, drawColor, 0f, PhaseIndicatorEnd.Size() * 0.5f, 1f, SpriteEffects.None, 0f);

            // Draw the leftmost piece.
            Vector2 leftPhaseShellPos = new(phaseShellXPos - 6f, rightPhaseIndicatorShellDrawPos.Y);
            spriteBatch.Draw(PhaseIndicatorStart, leftPhaseShellPos, null, drawColor, 0f, PhaseIndicatorStart.Size() * 0.5f, 1f, SpriteEffects.None, 0f);

            // Draw the percentage box.
            Vector2 percentBaseDrawPos = leftPhaseShellPos + new Vector2(-30f, -3f);
            spriteBatch.Draw(PercentageFrame, percentBaseDrawPos, null, drawColor, 0f, PercentageFrame.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            float totalRatio = (float)CombinedNPCLife / CombinedNPCMaxLife;
            float formattedRatio = Truncate(totalRatio * 10000f) / 100;
            if (float.IsNaN(formattedRatio))
                formattedRatio = 0f;
            string percentText = formattedRatio.ToString() + "%";
            Vector2 textDrawPos = percentBaseDrawPos + new Vector2(16f, 6.5f);
            Color shadowColor = new(210, 158, 68);
            Vector2 size = InfernumFontRegistry.HPBarFont.MeasureString(percentText);
            Vector2 origin = new(size.X, size.Y * 0.5f);
            ChatManager.DrawColorCodedString(spriteBatch, InfernumFontRegistry.HPBarFont, percentText, textDrawPos, shadowColor * mainOpacity, 0f, origin, Vector2.One * 0.62f);
        }
        #endregion
    }
}
