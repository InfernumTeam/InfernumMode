using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.UI;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ID;
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

        internal static Dictionary<int, BossPhaseInfo> ModCallPhaseInfos;

        internal static Dictionary<int, Texture2D> ModCallBossIcons;

        internal static List<int> ModCallNPCsThatCanHaveAHPBar;

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

        public static Texture2D InvincibilityOverlay { get; private set; }

        public static Texture2D BaseIcon { get; private set; }
        #endregion

        #region Overrides

        public override void Load()
        {
            ActiveBossBars = new();
            ModCallPhaseInfos = new();
            ModCallBossIcons = new();
            ModCallNPCsThatCanHaveAHPBar = new();

            if (Main.netMode != NetmodeID.Server)
            {
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
                InvincibilityOverlay = ModContent.Request<Texture2D>("InfernumMode/Content/BossBars/Textures/InvincibilityOverlay", AssetRequestMode.ImmediateLoad).Value;
                BaseIcon = ModContent.Request<Texture2D>("InfernumMode/Content/BossBars/Textures/DefaultIcon", AssetRequestMode.ImmediateLoad).Value;
            }
        }

        public override void Unload()
        {
            ActiveBossBars = null;
            PhaseInfos = null;
            ModCallPhaseInfos = null;
            ModCallBossIcons = null;
            ModCallNPCsThatCanHaveAHPBar = null;
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
            InvincibilityOverlay = null;
            BaseIcon = null;
        }

        public override string DisplayName => "Infernum Mod";

        public override bool PreventDraw => true;

        public override void Update(IBigProgressBar currentBar, ref BigProgressBarInfo info)
        {
            for (int j = 0; j < Main.maxNPCs; j++)
            {
                if (Main.npc[j].active && !BossHealthBarManager.BossExclusionList.Contains(Main.npc[j].type))
                {
                    bool isEoWSegment = Main.npc[j].type is NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail;

                    bool canHaveHpBar = false;
                    if (Main.npc[j].TryGetGlobalNPC<CalamityGlobalNPC>(out var result))
                        canHaveHpBar = result.CanHaveBossHealthBar;

                    if ((Main.npc[j].IsABoss() && !isEoWSegment) || BossHealthBarManager.MinibossHPBarList.Contains(Main.npc[j].type) || canHaveHpBar || ModCallNPCsThatCanHaveAHPBar.Contains(Main.npc[j].type))
                        AddBar(j);
                }
            }
            for (int i = 0; i < ActiveBossBars.Count; i++)
            {
                BaseBossBar bossBar = ActiveBossBars[i];
                bossBar.Update();
                if (bossBar.CloseAnimationTimer >= 30)
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

            // Shove the bar to the left a bit if anything is taking up the default position.
            if (Main.playerInventory || Main.invasionType > 0 || Main.pumpkinMoon || Main.snowMoon || DD2Event.Ongoing || AcidRainEvent.AcidRainEventIsOngoing)
                x -= 250;

            foreach (BaseBossBar bar in ActiveBossBars)
            {
                bar.Draw(spriteBatch, x, y);
                y -= 110;
            }
        }
        #endregion

        #region Methods
        // This is seperate to the provided Load hook due to requiring the NPCBehaviorOverrides to be loaded beforehand.
        internal static void LoadPhaseInfo()
        {
            PhaseInfos = new();
            // Load every phase info.
            foreach (var behaviorOverridePair in NPCBehaviorOverride.BehaviorOverrides)
            {
                NPCBehaviorOverride behaviorOverride = behaviorOverridePair.Value;
                List<float> phaseThresholds = behaviorOverride.PhaseLifeRatioThresholds.ToList();
                // Add 1 (100%) to the start, as none of them include that.
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

            // Not ideal. The Exo Twins have a singular HP bar, and having to hardcode check that here is annoying.
            // TODO: Is this needed? Was taken from Calamity's bar without checking, but it seems weird that they are the only instance
            // where this is required.
            if (npc.type == ModContent.NPCType<Artemis>() || !canAddBar)
                return;

            if (canAddBar)
                ActiveBossBars.Add(new BaseBossBar(npcIndex));
        }
        #endregion
    }
}
