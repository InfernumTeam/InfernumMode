using InfernumMode.Achievements.InfernumAchievements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using Terraria.ModLoader.IO;

namespace InfernumMode.Achievements
{
    public class AchievementManager : ModSystem
    {
        #region Statics
        private static List<Achievement> AchievementInstances;

        internal static Dictionary<Achievement, int> SortedAchievements;

        internal static Achievement[] achievements;

        // This needs to happen after InfernumMode.Instance is loaded.
        public static void LoadAchievements()
        {
            AchievementInstances = new List<Achievement>();
            SortedAchievements = new Dictionary<Achievement, int>();
            LoadAchievementInstances(InfernumMode.Instance);

            foreach(var achievement in AchievementInstances)
            {
                achievement.Initialize();
                SortedAchievements.Add(achievement, achievement.PositionInMainList);
                achievements[achievement.PositionInMainList] = achievement;
            }
        }
        public static void UnloadAchievements()
        {
            AchievementInstances = null;
            SortedAchievements = null;
            achievements = null;
        }
        internal static void LoadAchievementInstances(Mod mod)
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
        /// <summary>
        /// Called every frame.
        /// </summary>
        internal static void UpdateAchievements()
        {
            foreach (var achievement in AchievementInstances)
            {
                if (achievement.IsCompleted && !achievement.DoneCompletionEffects)
                {
                    achievement.DoneCompletionEffects = true;
                    achievement.OnCompletion();
                }
                else if(!achievement.IsCompleted)
                    achievement.Update();
            }
        }
        /// <summary>
        /// Call this in specific places for your specific achievements, creating the appropriate UpdateContext context.
        /// </summary>
        /// <param name="context"></param>
        internal static void ExtraUpdateAchievements(UpdateContext context)
        {
            foreach (var achievement in AchievementInstances)
            {
                if(context.NPCType != -1)
                {
                    if (achievement.GetType() == typeof(KillAllBossesAchievement) && !achievement.IsCompleted)
                        achievement.ExtraUpdateNPC(context.NPCType);
                    if (achievement.GetType() == typeof(KillAllMinibossesAchievement) && !achievement.IsCompleted)
                        achievement.ExtraUpdateNPC(context.NPCType);
                    if(achievement.GetType() == typeof(MechaMayhemAchievement) && !achievement.IsCompleted)
                        achievement.ExtraUpdateNPC(context.NPCType);  
                    if (achievement.GetType() == typeof(BereftVassalAchievement) && !achievement.IsCompleted)
                        achievement.ExtraUpdateNPC(context.NPCType);
                    if (achievement.GetType() == typeof(ExoPathAchievement) && !achievement.IsCompleted)
                        achievement.ExtraUpdateNPC(context.NPCType);
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
            return achievements.ToList();
        }
        public static int GetIconIndex(Achievement achievement)
        {
            if (SortedAchievements.ContainsKey(achievement))
            {
                return SortedAchievements.GetValueOrDefault(achievement);
            }
            return 0;
        }
        #endregion

        #region Overrides
        public override void LoadWorldData(TagCompound tag)
        {
            foreach (var achievement in AchievementInstances)
            {
                achievement.LoadProgress(tag);
            }
        }

        public override void SaveWorldData(TagCompound tag)
        {
            foreach (var achievement in AchievementInstances)
            {
                achievement.SaveProgress(tag);
            }
        }

        public override void OnWorldLoad()
        {
            foreach (var achievement in AchievementInstances)
            {
                achievement.CurrentCompletion = 0;
                achievement.DoneCompletionEffects = false;
            }
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
        public int NPCType;

        public int ItemType;

        public SpecificUpdateContexts SpecificContext;

        public UpdateContext(int nPCType = -1, int itemType = -1, SpecificUpdateContexts specificContext = SpecificUpdateContexts.None)
        {
            NPCType = nPCType;
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
