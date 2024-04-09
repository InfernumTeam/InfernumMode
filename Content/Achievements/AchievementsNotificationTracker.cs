using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI;

namespace InfernumMode.Content.Achievements
{
    public class AchievementsNotificationTracker
    {
        private static readonly List<IInGameNotification> Notifications = [];

        public static void AddAchievementAsUpdated(Achievement achievement)
        {
            if (Main.netMode != NetmodeID.Server)
                Notifications.Add(new AchievementUpdatePopup(achievement));
        }

        public static void AddAchievementAsCompleted(Achievement achievement)
        {
            if (Main.netMode != NetmodeID.Server)
                Notifications.Add(new AchievementCompletionPopup(achievement));
        }

        public static void Update()
        {
            for (int i = 0; i < Notifications.Count; i++)
            {
                Notifications[i].Update();
                if (Notifications[i].ShouldBeRemoved)
                {
                    Notifications.Remove(Notifications[i]);
                    i--;
                }
            }
        }

        public static void DrawInGame(SpriteBatch spriteBatch)
        {
            float yPosition = Main.screenHeight - 40;
            if (PlayerInput.UsingGamepad)
                yPosition -= 25f;

            Vector2 positionAnchorBottom = new(Main.screenWidth / 2, yPosition);
            foreach (IInGameNotification notification in Notifications)
            {
                notification.DrawInGame(spriteBatch, positionAnchorBottom);
                notification.PushAnchor(ref positionAnchorBottom);
                if (positionAnchorBottom.Y < -100f)
                    break;
            }
        }

        public static void Clear() => Notifications.Clear();
    }
}
