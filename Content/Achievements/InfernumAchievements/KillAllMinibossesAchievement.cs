using CalamityMod.NPCs.Abyss;
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
        public struct MinibossData
        {
            public Minibosses Miniboss;

            public int ID;

            public string DisplayName;

            public bool Downed;

            public MinibossData(Minibosses miniboss, int id)
            {
                Miniboss = miniboss;
                ID = id;
                DisplayName = Utilities.GetNPCFullNameFromID(id);
                Downed = false;
            }
        }

        #region Enumerations
        public enum Minibosses
        {
            GiantClam,
            DarkMageTier1,
            SandElemental,
            CloudElemental,
            CorruptionMimic,
            CrimsonMimic,
            HallowMimic,
            OgreTier2,
            Betsy,
            Eidolist,
            NuclearTerror,
            ColossalSquid,
            ReaperShark,
            EidolonWyrm,
        }
        #endregion

        #region Fields/Properties
        private List<MinibossData> Data;
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
            CreateDict();
            if (tag.ContainsKey("MinibossesDictInt") && tag.ContainsKey("MinibossesDictBool"))
            {
                List<int> keys = tag.Get<List<int>>("MinibossesDictInt");
                List<bool> values = tag.Get<List<bool>>("MinibossesDictBool");
                var dict = keys.Zip(values, (k, v) => new
                {
                    Key = k,
                    Value = v
                }).ToDictionary(k => k.Key, v => v.Value);

                for (int i = 0; i < Data.Count; i++)
                {
                    var entry = Data[i];

                    if (dict.TryGetValue((int)entry.Miniboss, out var downed))
                        entry.Downed = downed;
                }
            }

            CurrentCompletion = tag.Get<int>("MinibossesCurrentCompletion");
            DoneCompletionEffects = tag.Get<bool>("MinibossesDoneCompletionEffects");
        }

        public override void SaveProgress(TagCompound tag)
        {
            var intList = new List<int>();
            var boolList = new List<bool>();
            foreach  (var entry in Data)
            {
                intList.Add((int)entry.Miniboss);
                boolList.Add(entry.Downed);
            }
            tag["MinibossesDictInt"] = intList;
            tag["MinibossesDictBool"] = boolList;
            tag["MinibossesCurrentCompletion"] = CurrentCompletion;
            tag["MinibossesDoneCompletionEffects"] = DoneCompletionEffects;
        }

        public override void Update()
        {
            CurrentCompletion = Data.Where(entry => entry.Downed).Count();
        }

        public override void ExtraUpdate(Player player, int npcIndex)
        {
            bool updatedList = false;
            int npcID = Main.npc[npcIndex].type;

            // Why are these seperate IDs?
            if (npcID == NPCID.DD2DarkMageT3)
                npcID = NPCID.DD2DarkMageT1;
            else if (npcID == NPCID.DD2OgreT3)
                npcID = NPCID.DD2OgreT2;

            if (Data.Any(entry => entry.ID == npcID))
            {
                var entry = Data.First(entry => entry.ID == npcID);
                if (!entry.Downed)
                {
                    entry.Downed = true;
                    updatedList = true;
                }
            }

            if (updatedList && Data.Where(entry => entry.Downed).Count() != TotalCompletion)
                AchievementsNotificationTracker.AddAchievementAsUpdated(this);
        }
        #endregion

        #region Methods
        private void CreateDict()
        {
            CurrentCompletion = 0;
            DoneCompletionEffects = false;

            Data = new()
            {
                new MinibossData(Minibosses.GiantClam, ModContent.NPCType<GiantClam>()),
                new MinibossData(Minibosses.DarkMageTier1, NPCID.DD2DarkMageT1),
                new MinibossData(Minibosses.SandElemental, NPCID.SandElemental),
                new MinibossData(Minibosses.CloudElemental, ModContent.NPCType<ThiccWaifu>()),
                new MinibossData(Minibosses.CorruptionMimic, NPCID.BigMimicCorruption),
                new MinibossData(Minibosses.CrimsonMimic, NPCID.BigMimicCrimson),
                new MinibossData(Minibosses.HallowMimic, NPCID.BigMimicHallow),
                new MinibossData(Minibosses.OgreTier2, NPCID.DD2OgreT2),
                new MinibossData(Minibosses.Betsy, NPCID.DD2Betsy),
                new MinibossData(Minibosses.Eidolist, ModContent.NPCType<Eidolist>()),
                new MinibossData(Minibosses.NuclearTerror, ModContent.NPCType<NuclearTerror>()),
                new MinibossData(Minibosses.ColossalSquid, ModContent.NPCType<ColossalSquid>()),
                new MinibossData(Minibosses.ReaperShark, ModContent.NPCType<ReaperShark>()),
                new MinibossData(Minibosses.EidolonWyrm, ModContent.NPCType<EidolonWyrmHead>())
            };
        }

        public string GetFirstUncompletedMiniBoss()
        {
            foreach (var entry in Data)
            {
                if (!entry.Downed)
                    return entry.DisplayName;
            }
            return string.Empty;
        }
        #endregion
    }
}
