using InfernumMode.Content.Achievements;
using InfernumMode.Content.Achievements.UI;
using InfernumMode.Content.BossIntroScreens;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class UIRenderingSystem : ModSystem
    {
        internal static AchievementUIManager achievementUIManager = new();

        internal static WishesUIManager wishesUIManager = new();

        internal static UIState CurrentAchievementUI = achievementUIManager;

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
            if (mouseIndex != -1)
            {
                layers.Insert(mouseIndex, new LegacyGameInterfaceLayer("Boss Introduction Screens", () =>
                {
                    IntroScreenManager.Draw();
                    return true;
                }, InterfaceScaleType.None));
                layers.Insert(mouseIndex, new LegacyGameInterfaceLayer("Achievment Completion Animation", () =>
                {
                    AchievementsNotificationTracker.DrawInGame(Main.spriteBatch);
                    return true;
                }, InterfaceScaleType.UI));
            }
        }
    }
}