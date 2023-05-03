using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.UI;
using InfernumMode.Content.BehaviorOverrides.BossAIs.GreatSandShark;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossBars
{
    public class BossBarManager : ModBossBarStyle
    {
        #region Fields/Properties
        public const int MaxBars = 4;

        public const int IconWidthHeight = 50;
        #endregion

        #region Statics
        internal static List<BaseBossBar> ActiveBossBars;

        // Store phase information for every boss, by type.
        internal static Dictionary<int, BossPhaseInfo> PhaseInfos;

        public static DynamicSpriteFont BarFont { get; private set; }

        public static Texture2D BarFrame { get; private set; }

        public static Texture2D IconFrame { get; private set; }

        public static Texture2D MainBarTip { get; private set; }

        public static Texture2D MinionBarTip { get; private set; }

        public static Texture2D MinionFrame { get; private set; }

        public static Texture2D PercentageFrame { get; private set; }

        public static Texture2D PhaseIndicatorEnd { get; private set; }

        public static Texture2D PhaseIndicatorMiddle { get; private set; }

        public static Texture2D PhaseIndicatorNotch { get; private set; }

        public static Texture2D PhaseIndicatorStart { get; private set; }

        public static Texture2D PhaseIndicatorPlate { get; private set; }
        #endregion

        #region Overrides

        public override void Load()
        {
            ActiveBossBars = new();

            if (!Main.dedServ)
            {
                // This was crashing on Linux and such in Calamity, I am unable to check if it will here so I am playing it safe and only allowing custom fonts to work on Windows.
                if ((int)Environment.OSVersion.Platform == 2)
                    BarFont = ModContent.Request<DynamicSpriteFont>("InfernumMode/Assets/Fonts/BarFont", AssetRequestMode.ImmediateLoad).Value;
                else
                    // If not the correct OS, we need to make it the default Terraria Font, Andy.
                    BarFont = FontAssets.MouseText.Value;
            }

            BarFrame = ModContent.Request<Texture2D>("InfernumMode/Content/BossBars/Textures/Frame", AssetRequestMode.ImmediateLoad).Value;
            IconFrame = ModContent.Request<Texture2D>("InfernumMode/Content/BossBars/Textures/IconBase", AssetRequestMode.ImmediateLoad).Value;
            MainBarTip = ModContent.Request<Texture2D>("InfernumMode/Content/BossBars/Textures/MainBarTip", AssetRequestMode.ImmediateLoad).Value;
            MinionBarTip = ModContent.Request<Texture2D>("InfernumMode/Content/BossBars/Textures/MinionBarTip", AssetRequestMode.ImmediateLoad).Value;
            MinionFrame = ModContent.Request<Texture2D>("InfernumMode/Content/BossBars/Textures/MinionFrame", AssetRequestMode.ImmediateLoad).Value;
            PercentageFrame = ModContent.Request<Texture2D>("InfernumMode/Content/BossBars/Textures/PercentageFrame", AssetRequestMode.ImmediateLoad).Value;
            PhaseIndicatorEnd = ModContent.Request<Texture2D>("InfernumMode/Content/BossBars/Textures/PhaseIndicatorEnd", AssetRequestMode.ImmediateLoad).Value;
            PhaseIndicatorMiddle = ModContent.Request<Texture2D>("InfernumMode/Content/BossBars/Textures/PhaseIndicatorMiddle", AssetRequestMode.ImmediateLoad).Value;
            PhaseIndicatorNotch = ModContent.Request<Texture2D>("InfernumMode/Content/BossBars/Textures/PhaseIndicatorNotch", AssetRequestMode.ImmediateLoad).Value;
            PhaseIndicatorStart = ModContent.Request<Texture2D>("InfernumMode/Content/BossBars/Textures/PhaseIndicatorStart", AssetRequestMode.ImmediateLoad).Value;
            PhaseIndicatorPlate = ModContent.Request<Texture2D>("InfernumMode/Content/BossBars/Textures/PhasePlate", AssetRequestMode.ImmediateLoad).Value;
        }

        public override void Unload()
        {
            ActiveBossBars = null;
            PhaseInfos = null;
            BarFont = null;
            BarFrame = null;
            MainBarTip = null;
            MinionBarTip = null;
            PercentageFrame = null;
            PhaseIndicatorEnd = null;
            PhaseIndicatorMiddle = null;
            PhaseIndicatorNotch = null;
            PhaseIndicatorStart = null;
            PhaseIndicatorStart = null;
            PhaseIndicatorPlate = null;
        }

        public override string DisplayName => "Infernum Mod";

        public override bool PreventDraw => true;

        public override void Update(IBigProgressBar currentBar, ref BigProgressBarInfo info)
        {
            // Yoinked from Calamity's.
            for (int j = 0; j < 200; j++)
            {
                if (Main.npc[j].active && !BossHealthBarManager.BossExclusionList.Contains(Main.npc[j].type))
                {
                    bool isEoWSegment = Main.npc[j].type is 14 or 15;
                    if ((Main.npc[j].IsABoss() && !isEoWSegment) || BossHealthBarManager.MinibossHPBarList.Contains(Main.npc[j].type) || Main.npc[j].Calamity().CanHaveBossHealthBar)
                        AddBar(j);
                }
            }
            for (int i = 0; i < ActiveBossBars.Count; i++)
            {
                BaseBossBar bossBar = ActiveBossBars[i];
                bossBar.Update();
                if (bossBar.CloseAnimationTimer >= 120)
                {
                    ActiveBossBars.RemoveAt(i);
                    i--;
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch, IBigProgressBar currentBar, BigProgressBarInfo info)
        {
            int startHeight = 100;
            int x = Main.screenWidth - 220;
            int y = Main.screenHeight - startHeight;
            if (Main.playerInventory || Main.invasionType > 0 || Main.pumpkinMoon || Main.snowMoon || DD2Event.Ongoing || AcidRainEvent.AcidRainEventIsOngoing)
            {
                x -= 250;
            }
            foreach (BaseBossBar bar in ActiveBossBars)
            {
                bar.Draw(spriteBatch, x, y);
                y -= 110;
            }
        }
        #endregion

        #region Methods
        internal static void LoadPhaseInfo()
        {
            PhaseInfos = new();
            // Load every phase info.
            foreach (var behaviorOverridePair in NPCBehaviorOverride.BehaviorOverrides)
            {
                NPCBehaviorOverride behaviorOverride = behaviorOverridePair.Value;
                List<float> phaseThresholds = behaviorOverride.PhaseLifeRatioThresholds.ToList();
                // Add 1, or 100% to the start.
                phaseThresholds.Insert(0, 1f);
                PhaseInfos.Add(behaviorOverride.NPCOverrideType, new(behaviorOverride.NPCOverrideType, phaseThresholds));
            }
        }

        private static void AddBar(int npcIndex)
        {
            if (ActiveBossBars.Count >= MaxBars)
                return;

            NPC npc = Main.npc[npcIndex];
            bool canAddBar = npc.active && npc.life > 0 && ActiveBossBars.All((BaseBossBar b) => b.NPCIndex != npcIndex) && !npc.Calamity().ShouldCloseHPBar;

            if (npc.type == ModContent.NPCType<Artemis>() || !canAddBar)
                return;


            if (canAddBar)
                ActiveBossBars.Add(new BaseBossBar(npcIndex));
        }
        #endregion
    }
}
