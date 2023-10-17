using CalamityMod.Events;
using InfernumMode.Content.Achievements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using Terraria.ModLoader.IO;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class AchievementPlayer : ModPlayer
    {
        #region Statics
        internal List<Achievement> AchievementInstances;

        internal Dictionary<Achievement, int> SortedAchievements;

        internal Achievement[] achievements;

        internal void LoadAchievementInstances(Mod mod)
        {
            Type baseAchievementType = typeof(Achievement);
            Type[] loadableTypes = AssemblyManager.GetLoadableTypes(mod.Code);
            int i = 0;
            foreach (Type type in loadableTypes)
            {
                if (type.IsSubclassOf(baseAchievementType) && !type.IsAbstract && type != baseAchievementType)
                {
                    Achievement instance = (Achievement)FormatterServices.GetUninitializedObject(type);
                    AchievementInstances.Add(instance);
                    i++;
                }
            }
            achievements = new Achievement[i];
        }

        internal void InitializeIfNecessary()
        {
            if (AchievementInstances is not null)
                return;

            AchievementInstances = new List<Achievement>();
            SortedAchievements = new Dictionary<Achievement, int>();
            LoadAchievementInstances(InfernumMode.Instance);

            foreach (var achievement in AchievementInstances)
            {
                achievement.Initialize();
                SortedAchievements.Add(achievement, achievement.PositionInMainList);
                achievements[achievement.PositionInMainList] = achievement;
            }
        }

        /// <summary>
        /// Call this where an achivement with the provided update check should call its extra update.
        /// </summary>
        /// <param name="player">The player running the extra update</param>
        /// <param name="updateCheck">The type of check that should be ran</param>
        /// <param name="extraInfo">Optional extra information, what this is used for is per achievement</param>
        internal static void ExtraUpdateHandler(Player player, AchievementUpdateCheck updateCheck, int extraInfo = -1)
        {
            foreach (Achievement achievement in player.GetModPlayer<AchievementPlayer>().AchievementInstances)
            {
                bool shouldComplete = true;

                // If boss rush is active, and the achivement isnt completable during it, dont complete it.
                if (BossRushEvent.BossRushActive && !achievement.ObtainableDuringBossRush)
                    shouldComplete = false;

                if (shouldComplete && achievement.UpdateCheck == updateCheck)
                    achievement.ExtraUpdate(player, extraInfo);
            }
        }

        internal static List<Achievement> GetAchievementsList() => Main.LocalPlayer.GetModPlayer<AchievementPlayer>().achievements.ToList();

        public static bool UnlockedAchievement<T>(Player player) where T : Achievement
        {
            var achievements = player.GetModPlayer<AchievementPlayer>().AchievementInstances;
            if (!achievements.Any(a => a is T))
                return false;

            return achievements.First(a => a is T).IsCompleted;
        }

        public static int GetIconIndex(Achievement achievement)
        {
            var sortedAchievements = Main.LocalPlayer.GetModPlayer<AchievementPlayer>().SortedAchievements;
            if (sortedAchievements.ContainsKey(achievement))
                return sortedAchievements.GetValueOrDefault(achievement);

            return 0;
        }
        #endregion

        #region Overrides
        public override void SaveData(TagCompound tag)
        {
            foreach (var achievement in AchievementInstances)
                achievement.SaveProgress(tag);
        }

        public override void LoadData(TagCompound tag)
        {
            InitializeIfNecessary();
            foreach (var achievement in AchievementInstances)
                achievement.LoadProgress(tag);
        }

        public override void Initialize() => InitializeIfNecessary();

        public override void OnEnterWorld() => AchievementsNotificationTracker.Clear();

        public override void PostUpdate()
        {
            if (Main.myPlayer != Player.whoAmI)
                return;

            foreach (var achievement in AchievementInstances)
            {
                if (achievement.IsCompleted && !achievement.DoneCompletionEffects)
                {
                    achievement.DoneCompletionEffects = true;
                    achievement.OnCompletion(Player);
                }
                // If it isnt completed, or boss rush is not active or the achivement doesnt care about br, update it.
                else if (!achievement.IsCompleted && (!BossRushEvent.BossRushActive || achievement.ObtainableDuringBossRush))
                    achievement.Update();
            }
            AchievementsNotificationTracker.Update();
        }

        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource) => ExtraUpdateHandler(Player, AchievementUpdateCheck.PlayerDeath);
        #endregion

        #region Helper Fields
        internal static bool DukeFishronDefeated;
        internal static bool DoGDefeated;
        internal static bool ProviDefeated;
        internal static bool NightProviDefeated;
        internal static bool DraedonDefeated;
        #endregion
    }
}
