using InfernumMode.Achievements;
using InfernumMode.Achievements.UI;
using InfernumMode.BossIntroScreens;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace InfernumMode.Systems
{
    public class UIRenderingSystem : ModSystem
    {
        internal static AchievementUIManager achievementUIManager = new();
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
                    AchivementsNotificationTracker.DrawInGame(Main.spriteBatch);
                    //AchivementsNotificationTracker.DrawInIngameOptions(Main.spriteBatch);
                    return true;
                }, InterfaceScaleType.UI));
            }
        }
    }
}