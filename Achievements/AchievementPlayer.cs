using InfernumMode.Achievements.InfernumAchievements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using Terraria.ModLoader.IO;

namespace InfernumMode.Achievements
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
        /// Call this in specific places for your specific achievements, creating the appropriate UpdateContext context.
        /// </summary>
        /// <param name="context"></param>
        internal static void ExtraUpdateAchievements(Player player, UpdateContext context)
        {
            if (!player.active)
                return;

            foreach (var achievement in player.GetModPlayer<AchievementPlayer>().AchievementInstances)
            {
                if (context.NPCIndex != -1)
                {
                    if (achievement.GetType() == typeof(KillAllBossesAchievement) && !achievement.IsCompleted)
                        achievement.ExtraUpdateNPC(context.NPCIndex);
                    if (achievement.GetType() == typeof(KillAllMinibossesAchievement) && !achievement.IsCompleted)
                        achievement.ExtraUpdateNPC(context.NPCIndex);
                    if (achievement.GetType() == typeof(MechaMayhemAchievement) && !achievement.IsCompleted)
                        achievement.ExtraUpdateNPC(context.NPCIndex);  
                    if (achievement.GetType() == typeof(BereftVassalAchievement) && !achievement.IsCompleted)
                        achievement.ExtraUpdateNPC(context.NPCIndex);
                    if (achievement.GetType() == typeof(ExoPathAchievement) && !achievement.IsCompleted)
                        achievement.ExtraUpdateNPC(context.NPCIndex);
                }

                if (context.ItemType != -1)
                {
                    if (achievement.GetType() == typeof(InfernalChaliceAchievement) && !achievement.IsCompleted)
                        achievement.ExtraUpdateItem(context.ItemType);
                }

                if (context.SpecificContext != SpecificUpdateContexts.None)
                {
                    switch (context.SpecificContext)
                    {
                        case SpecificUpdateContexts.PlayerDeath:
                            if (achievement.GetType() == typeof(BabysFirstAchievement) && !achievement.IsCompleted)
                                achievement.ExtraUpdate();
                            break;
                    }
                }
            }
        }

        internal static List<Achievement> GetAchievementsList()
        {
            return Main.LocalPlayer.GetModPlayer<AchievementPlayer>().achievements.ToList();
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

        public override void OnEnterWorld(Player player)
        {
            AchivementsNotificationTracker.Clear();
        }

        public override void PostUpdate()
        {
            foreach (var achievement in AchievementInstances)
            {
                if (achievement.IsCompleted && !achievement.DoneCompletionEffects)
                {
                    achievement.DoneCompletionEffects = true;
                    achievement.OnCompletion();
                }
                else if (!achievement.IsCompleted)
                    achievement.Update();
            }
            AchivementsNotificationTracker.Update();
        }
        #endregion

        #region Helper Fields
        internal static bool DoGDefeated = false;
        internal static bool ProviDefeated = false;
        internal static bool DraedonDefeated = false;
        internal static bool ShouldUpdateSavedMechOrder = false;
        #endregion
    }
    
    // These two arent *entirely* needed as of right now, but allows for future proofing for if/when we add
    // more achievements.
    public struct UpdateContext
    {
        public int NPCIndex;

        public int ItemType;

        public SpecificUpdateContexts SpecificContext;

        public UpdateContext(int npcIndex = -1, int itemType = -1, SpecificUpdateContexts specificContext = SpecificUpdateContexts.None)
        {
            NPCIndex = npcIndex;
            ItemType = itemType;
            SpecificContext = specificContext;
        }
    }
    
    public enum SpecificUpdateContexts
    {
        None,
        PlayerDeath
    }
}
