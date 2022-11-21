﻿using CalamityMod.NPCs.SunkenSea;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Achievements.InfernumAchievements
{
    public class KillAllMinibossesAchievement : Achievement
    {
        #region Fields
        private Dictionary<int, bool> MinibossesCompleted;
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
            ModContent.NPCType<GiantClam>()
        };
        #endregion

        #region Overrides
        public override void Initialize()
        {
            Name = "Mini-Meany!";
            Description = "Defeat the various minor threats across the world!\n[c/777777:Beat every Infernum Miniboss]";
            TotalCompletion = 8;
            PositionInMainList = 7;
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
                MinibossesCompleted = keys.Zip(values, (int k, bool v) => new
                {
                    Key = k,
                    Value = v
                }).ToDictionary(k => k.Key, v => v.Value);
            }
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
                {
                    currentCompletion++;
                }
            }
            CurrentCompletion = currentCompletion;
        }
        public override void ExtraUpdateNPC(int npcID)
        {
            if(MinibossIDs.Contains(npcID))
                MinibossesCompleted[MinibossIDs.IndexOf(npcID)] = true;
        }
        #endregion

        #region Methods
        private void CreateDict()
        {
            MinibossesCompleted = new Dictionary<int, bool>();
            for (int i = 0; i < TotalCompletion; i++)
            {
                MinibossesCompleted[i] = false;
            }
            CurrentCompletion = 0;
            DoneCompletionEffects = false;
        }
        #endregion
    }
}