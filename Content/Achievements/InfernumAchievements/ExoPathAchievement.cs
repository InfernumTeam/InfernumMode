using CalamityMod.Events;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Content.Achievements.InfernumAchievements
{
    public class ExoPathAchievement : Achievement
    {
        #region Fields
        public List<(string, string)> CompletedExoMechCombinations
        {
            get;
            internal set;
        } = [];

        #endregion

        #region Overrides
        public override void Initialize()
        {
            TotalCompletion = Utilities.NumberOfCombinations(ExoMechManagement.ExoMechIDs.Count, 2);
            PositionInMainList = 5;
            UpdateCheck = AchievementUpdateCheck.NPCKill;
            InitializeCompletionVariables();
        }

        public override void Update()
        {
            CurrentCompletion = CompletedExoMechCombinations.Count;
        }

        public override void ExtraUpdate(Player player, int npcIndex)
        {
            // Don't count Boss Rush kills.
            if (BossRushEvent.BossRushActive)
                return;

            // If not an exo mech, leave.
            NPC mech = Main.npc[npcIndex];
            int npcID = mech.type;
            if (!ExoMechManagement.ExoMechIDs.Contains(npcID))
                return;

            // Don't count a kill if this is not the last mech.
            if (ExoMechManagement.TotalMechs <= 1)
            {
                var initialMech = NPCLoader.GetNPC((int)mech.Infernum().ExtraAI[ExoMechManagement.InitialMechNPCTypeIndex]);
                var complementMech = NPCLoader.GetNPC((int)mech.Infernum().ExtraAI[ExoMechManagement.SecondaryMechNPCTypeIndex]);

                (string, string) combination = new(initialMech.FullName, complementMech.FullName);
                (string, string) reverseCombination = new(combination.Item2, combination.Item1);

                if (!CompletedExoMechCombinations.Contains(combination) && !CompletedExoMechCombinations.Contains(reverseCombination))
                {
                    CompletedExoMechCombinations.Add(combination);
                    AchievementsNotificationTracker.AddAchievementAsUpdated(this);
                }
            }
        }

        public override void LoadProgress(TagCompound tag)
        {
            if (!tag.ContainsKey("ExoMechCompletionsPrimary") || !tag.ContainsKey("ExoMechCompletionsSecondary"))
                InitializeCompletionVariables();
            else
            {
                IList<string> primaryList = tag.GetList<string>("ExoMechCompletionsPrimary");
                IList<string> secondaryList = tag.GetList<string>("ExoMechCompletionsSecondary");

                CompletedExoMechCombinations = [];
                for (int i = 0; i < primaryList.Count; i++)
                    CompletedExoMechCombinations.Add(new(primaryList[i], secondaryList[i]));
            }
            CurrentCompletion = tag.Get<int>("ExosCurrentCompletion");
            DoneCompletionEffects = tag.Get<bool>("ExosDoneCompletionEffects");
        }

        public override void SaveProgress(TagCompound tag)
        {
            tag["ExoMechCompletionsPrimary"] = CompletedExoMechCombinations.Select(c => c.Item1).ToList();
            tag["ExoMechCompletionsSecondary"] = CompletedExoMechCombinations.Select(c => c.Item2).ToList();
            tag["ExosCurrentCompletion"] = CurrentCompletion;
            tag["ExosDoneCompletionEffects"] = DoneCompletionEffects;
        }
        #endregion

        #region Methods
        private void InitializeCompletionVariables()
        {
            CompletedExoMechCombinations = [];
            CurrentCompletion = 0;
            DoneCompletionEffects = false;
        }

        #endregion
    }
}
