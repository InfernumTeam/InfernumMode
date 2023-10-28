using CalamityMod.NPCs.AcidRain;
using CalamityMod.NPCs.NormalNPCs;
using CalamityMod.NPCs.SunkenSea;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Content.Achievements.InfernumAchievements
{
    public class KillAllMinibossesAchievement : Achievement
    {
        #region Fields
        private Dictionary<int, bool> MinibossesCompleted;

        // Should have ordered them previously, seeing as the above dict is based on the list order changing it now will
        // change peoples progress :).
        private Dictionary<int, bool> OrderedMinibossesComplete => new()
        {
            [ModContent.NPCType<GiantClam>()] = MinibossesCompleted[8],
            [NPCID.DD2DarkMageT1] = MinibossesCompleted[4],
            [NPCID.SandElemental] = MinibossesCompleted[6],
            [ModContent.NPCType<ThiccWaifu>()] = MinibossesCompleted[7],
            [NPCID.BigMimicCorruption] = MinibossesCompleted[1],
            [NPCID.BigMimicCrimson] = MinibossesCompleted[2],
            [NPCID.BigMimicHallow] = MinibossesCompleted[3],
            [NPCID.DD2OgreT2] = MinibossesCompleted[5],
            [NPCID.DD2Betsy] = MinibossesCompleted[0],
            [ModContent.NPCType<NuclearTerror>()] = MinibossesCompleted[9],
        };
        #endregion

        #region Statics
        public static List<int> MinibossIDs => new()
        {
            NPCID.DD2Betsy,
            NPCID.BigMimicCorruption,
            NPCID.BigMimicCrimson,
            NPCID.BigMimicHallow,
            NPCID.DD2DarkMageT1,
            NPCID.DD2OgreT2,
            NPCID.SandElemental,
            ModContent.NPCType<ThiccWaifu>(),
            ModContent.NPCType<GiantClam>(),
            ModContent.NPCType<NuclearTerror>(),
            // These must be at the end.
            NPCID.DD2DarkMageT3,
            NPCID.DD2OgreT3
        };
        #endregion

        #region Overrides
        public override void Initialize()
        {
            TotalCompletion = 10;
            PositionInMainList = 7;
            UpdateCheck = AchievementUpdateCheck.NPCKill;
            CreateDict();
        }

        public override void LoadProgress(TagCompound tag)
        {
            if (!tag.ContainsKey("MinibossesDictInt") || !tag.ContainsKey("MinibossesDictBool"))
                CreateDict();
            else
            {
                List<int> keys = tag.Get<List<int>>("MinibossesDictInt");
                List<bool> values = tag.Get<List<bool>>("MinibossesDictBool");
                MinibossesCompleted = keys.Zip(values, (k, v) => new
                {
                    Key = k,
                    Value = v
                }).ToDictionary(k => k.Key, v => v.Value);
            }
            // Add the extra one if it doesn't exist already, I do not like how scuffed this feels. If we add another miniboss in future,
            // but someone hasnt run this code we'd need to check for that too and ugh.
            if (MinibossesCompleted.Count is 8 && !MinibossesCompleted.ContainsKey(8))
                MinibossesCompleted.Add(8, false);
            if (MinibossesCompleted.Count is 9 && !MinibossesCompleted.ContainsKey(9))
                MinibossesCompleted.Add(9, false);

            CurrentCompletion = tag.Get<int>("MinibossesCurrentCompletion");
            DoneCompletionEffects = tag.Get<bool>("MinibossesDoneCompletionEffects");
        }

        public override void SaveProgress(TagCompound tag)
        {
            tag["MinibossesDictInt"] = MinibossesCompleted.Keys.ToList();
            tag["MinibossesDictBool"] = MinibossesCompleted.Values.ToList();
            tag["MinibossesCurrentCompletion"] = CurrentCompletion;
            tag["MinibossesDoneCompletionEffects"] = DoneCompletionEffects;
        }

        public override void Update()
        {
            int currentCompletion = 0;
            for (int i = 0; i < MinibossesCompleted.Count; i++)
            {
                if (MinibossesCompleted[i])
                    currentCompletion++;
            }
            CurrentCompletion = currentCompletion;
        }

        public override void ExtraUpdate(Player player, int npcIndex)
        {
            bool updatedList = false;
            int npcID = Main.npc[npcIndex].type;
            if (MinibossIDs.Contains(npcID))
            {
                int darkMageIndex = MinibossIDs.IndexOf(NPCID.DD2DarkMageT1);
                if (npcID == NPCID.DD2DarkMageT3 && !MinibossesCompleted[darkMageIndex])
                {
                    MinibossesCompleted[MinibossIDs.IndexOf(NPCID.DD2DarkMageT1)] = true;
                    updatedList = true;
                }
                else if (npcID == NPCID.DD2OgreT3 && !MinibossesCompleted[MinibossIDs.IndexOf(NPCID.DD2OgreT2)])
                {
                    MinibossesCompleted[MinibossIDs.IndexOf(NPCID.DD2OgreT2)] = true;
                    updatedList = true;
                }
                else if (MinibossesCompleted.TryGetValue(MinibossIDs.IndexOf(npcID), out bool completed) && !completed)
                {
                    MinibossesCompleted[MinibossIDs.IndexOf(npcID)] = true;
                    updatedList = true;
                }
            }
            if (updatedList && MinibossesCompleted.Count(kv => kv.Value) != TotalCompletion)
                AchievementsNotificationTracker.AddAchievementAsUpdated(this);
        }
        #endregion

        #region Methods
        private void CreateDict()
        {
            MinibossesCompleted = new Dictionary<int, bool>();
            for (int i = 0; i < TotalCompletion; i++)
                MinibossesCompleted[i] = false;

            CurrentCompletion = 0;
            DoneCompletionEffects = false;
        }

        public string GetFirstUncompletedMiniBoss()
        {
            foreach (var item in OrderedMinibossesComplete)
            {
                if (!item.Value)
                {
                    return Utilities.GetNPCFullNameFromID(item.Key);
                }
            }
            return string.Empty;
        }
        #endregion
    }
}
