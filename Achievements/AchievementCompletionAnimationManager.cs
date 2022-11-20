using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfernumMode.Achievements
{
    public class AchievementCompletionAnimationManager
    {
        #region Statics
        public static List<Achievement> AchievementsToShow = new();

        private static Achievement CurrentAchievement = null;

        private static int Timer = 0;

        public static void Update()
        {
            // If the current achievement is null, and there are any in the list.
            if (CurrentAchievement is null && AchievementsToShow.Count > 0)
            {
                // Set it as the current achievement.
                CurrentAchievement = AchievementsToShow[0];
                // Clear it from the list.
                AchievementsToShow.RemoveAt(0);
                Timer = 0;
                return;
            }
            Timer++;
        }
        public static void Draw()
        {

        }
        #endregion
    }
}
