using CalamityMod.CalPlayer;
using InfernumMode.Core.GlobalInstances.Systems;
using Terraria;
using Terraria.ModLoader.IO;

namespace InfernumMode.Content.Achievements.InfernumAchievements
{
    public class BabysFirstAchievement : Achievement
    {
        #region Overrides
        public override void Initialize()
        {
            TotalCompletion = 1;
            PositionInMainList = 0;
            UpdateCheck = AchievementUpdateCheck.PlayerDeath;
        }

        public override void Update()
        {
            // Just auto-complete if the player is in hardcore.
            if (Main.LocalPlayer.difficulty == 2)
                CurrentCompletion++;
        }

        public override void ExtraUpdate(Player player, int extraInfo)
        {
            if (CalamityPlayer.areThereAnyDamnBosses && WorldSaveSystem.InfernumModeEnabled)
                CurrentCompletion++;
        }

        public override void SaveProgress(TagCompound tag)
        {
            tag["BabyCurrentCompletion"] = CurrentCompletion;
            tag["BabyDoneCompletionEffects"] = DoneCompletionEffects;
        }

        public override void LoadProgress(TagCompound tag)
        {
            CurrentCompletion = tag.Get<int>("BabyCurrentCompletion");
            DoneCompletionEffects = tag.Get<bool>("BabyDoneCompletionEffects");
        }
        #endregion
    }
}
