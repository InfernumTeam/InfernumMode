using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Achievements.InfernumAchievements
{
    public class ExoPathAchievement : Achievement
    {
        #region Fields
        public List<(int, int)> CompletedExoMechCombinations
        {
            get;
            internal set;
        } = new();

        #endregion

        #region Overrides
        public override void Initialize()
        {
            Name = "Lab Rat";
            Description = "Become Draedon's favorite test subject\n[c/777777:Beat all Infernum Exo Mech combinations]";
            TotalCompletion = Utilities.NumberOfCombinations(ExoMechManagement.ExoMechIDs.Count, 2);
            PositionInMainList = 5;
            InitializeCompletionVariables();
        }

        public override void Update()
        {
            CurrentCompletion = CompletedExoMechCombinations.Count;
        }

        public override void ExtraUpdateNPC(int npcIndex)
        {
            // If not an exo mech, leave.
            NPC mech = Main.npc[npcIndex];
            int npcID = mech.type;
            if (!ExoMechManagement.ExoMechIDs.Contains(npcID))
                return;
            
            // Don't count a kill if this is not the last mech.
            if (ExoMechManagement.TotalMechs <= 1)
            {
                (int, int) combination = new((int)mech.Infernum().ExtraAI[ExoMechManagement.InitialMechNPCTypeIndex], (int)mech.Infernum().ExtraAI[ExoMechManagement.SecondaryMechNPCTypeIndex]);
                (int, int) reverseCombination = new(combination.Item2, combination.Item1);
                
                if (!CompletedExoMechCombinations.Contains(combination) && !CompletedExoMechCombinations.Contains(reverseCombination))
                    CompletedExoMechCombinations.Add(combination);
            }
        }

        public override void LoadProgress(TagCompound tag)
        {
            if (!tag.ContainsKey("ExoMechCompletionsPrimary") || !tag.ContainsKey("ExoMechCompletionsSecondary"))
                InitializeCompletionVariables();
            else
            {
                IList<int> primaryList = tag.GetList<int>("ExoMechCompletionsPrimary");
                IList<int> secondaryList = tag.GetList<int>("ExoMechCompletionsSecondary");
                
                CompletedExoMechCombinations = new();
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
            CompletedExoMechCombinations = new();
            CurrentCompletion = 0;
            DoneCompletionEffects = false;
        }

        #endregion
    }
}
