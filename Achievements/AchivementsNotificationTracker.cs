using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI;

namespace InfernumMode.Achievements
{
    public class AchivementsNotificationTracker
    {
        private static List<IInGameNotification> Notifications = new();

        public static void AddAchievementAsCompleted(Achievement achievement)
        {
            if (Main.netMode != NetmodeID.Server)
                Notifications.Add(new AchivementCompletionPopup(achievement));
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
		public static void DrawInGame(SpriteBatch sb)
		{
			float num = Main.screenHeight - 40;
			if (PlayerInput.UsingGamepad)
			{
				num -= 25f;
			}
			Vector2 positionAnchorBottom = new(Main.screenWidth / 2, num);
			foreach (IInGameNotification notification in Notifications)
			{
				notification.DrawInGame(sb, positionAnchorBottom);
				notification.PushAnchor(ref positionAnchorBottom);
				if (positionAnchorBottom.Y < -100f)
				{
					break;
				}
			}
		}
		public static void DrawInIngameOptions(SpriteBatch spriteBatch, Rectangle area, ref int gamepadPointIdLocalIndexToUse)
		{
			int padding = 4;
			int height = area.Height / 5 - padding;
			Rectangle area2 = new(area.X, area.Y, area.Width - 6, height);
			int i = 0;
			foreach (IInGameNotification notification in Notifications)
			{
				notification.DrawInNotificationsArea(spriteBatch, area2, ref gamepadPointIdLocalIndexToUse);
				area2.Y += height + padding;

				i++;
				if (i >= 5)
					break;
			}
		}
		public static void Clear()
		{
			Notifications.Clear();
		}
	}
}
