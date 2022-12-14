using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.UI;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace InfernumMode.BossBars
{
    public class BossBarManager : ModBossBarStyle
    {
        #region Fields/Properties
        public const int MaxBars = 4;
        #endregion

        #region Statics
        internal static List<BaseBossBar> ActiveBossBars;


        public static Texture2D MainBarTexture { get; private set; }

        public static Texture2D MainBorderTexture { get; private set; }

        public static Texture2D EdgeBorderTexture { get; private set; }

        // For convenience, use the Calamity list.
        internal static Dictionary<int, int[]> Bosses => BossHealthBarManager.OneToMany;

        internal static void Load(Mod mod)
        {
            ActiveBossBars = new();
            MainBarTexture = ModContent.Request<Texture2D>("InfernumMode/BossBars/Textures/BaseBarTexture", AssetRequestMode.ImmediateLoad).Value;
            MainBorderTexture =  ModContent.Request<Texture2D>("InfernumMode/BossBars/Textures/BarBorderMain", AssetRequestMode.ImmediateLoad).Value;
            EdgeBorderTexture = ModContent.Request<Texture2D>("InfernumMode/BossBars/Textures/BarBorderEdge", AssetRequestMode.ImmediateLoad).Value;
        }

        #endregion

        #region Overrides
        public override void Unload()
        {
            ActiveBossBars = null;
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
            int x = Main.screenWidth - 420;
            int y = Main.screenHeight - startHeight;
            if (Main.playerInventory || Main.invasionType > 0 || Main.pumpkinMoon || Main.snowMoon || DD2Event.Ongoing || AcidRainEvent.AcidRainEventIsOngoing)
            {
                x -= 250;
            }
            foreach (BaseBossBar bar in ActiveBossBars)
            {
                bar.Draw(spriteBatch, x, y);
                y -= 70;
            }
        }
        #endregion

        #region Methods
        private void AddBar(int npcIndex)
        {
            if (ActiveBossBars.Count >= MaxBars)
                return;

            NPC npc = Main.npc[npcIndex];
            bool canAddBar = npc.active && npc.life > 0 && ActiveBossBars.All((BaseBossBar b) => b.NPCIndex != npcIndex) && !npc.Calamity().ShouldCloseHPBar;

            if (npc.type == ModContent.NPCType<Artemis>() || !canAddBar)
                return;

            string overridingName = null;
            if (npc.type == ModContent.NPCType<Apollo>())
                overridingName = "XS-01 Artemis and XS-03 Apollo";

            if (canAddBar)
            {
                ActiveBossBars.Add(new BaseBossBar(npcIndex, overridingName));
            }
        }
        #endregion
    }
}
