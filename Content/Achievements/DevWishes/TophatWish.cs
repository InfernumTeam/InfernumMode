using CalamityMod.CalPlayer;
using InfernumMode.Content.Items.Pets;
using InfernumMode.Core.GlobalInstances.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Content.Achievements.DevWishes
{
    public class TophatWish : Achievement
    {
        #region Fields
        private int CurrentDeathsInARow;

        private const int DeathsNeeded = 3;
        #endregion
        #region Overrides

        public override string LocalizationCategory => "Achievements.Wishes";

        public override void Initialize()
        {
            TotalCompletion = 1;
            PositionInMainList = 10;
            UpdateCheck = AchievementUpdateCheck.PlayerDeath;
            IsDevWish = true;
        }

        public override void Update()
        {
            if (CurrentDeathsInARow >= DeathsNeeded || Main.LocalPlayer.difficulty == PlayerDifficultyID.Hardcore)
                CurrentCompletion++;
        }

        public override void ExtraUpdate(Player player, int extraInfo)
        {
            if (CalamityPlayer.areThereAnyDamnBosses && WorldSaveSystem.InfernumModeEnabled)
                CurrentDeathsInARow++;
            else
                CurrentDeathsInARow = 0;
        }

        public override void OnCompletion(Player player)
        {
            WishCompletionEffects(player, ModContent.ItemType<BlastedTophat>());
        }

        public override void SaveProgress(TagCompound tag)
        {
            tag["TophatCurrentCompletion"] = CurrentCompletion;
            tag["TophatDoneCompletionEffects"] = DoneCompletionEffects;
            tag["TophatCurrentDeathsInARow"] = CurrentDeathsInARow;
        }

        public override void LoadProgress(TagCompound tag)
        {
            CurrentCompletion = tag.Get<int>("TophatCurrentCompletion");
            DoneCompletionEffects = tag.Get<bool>("TophatDoneCompletionEffects");
            CurrentDeathsInARow = tag.Get<int>("TophatCurrentDeathsInARow");
        }
        #endregion
    }
}
