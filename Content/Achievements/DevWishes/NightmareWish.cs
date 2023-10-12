using InfernumMode.Content.Items.Pets;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Content.Achievements.DevWishes
{
    public class NightmareWish : Achievement
    {
        public override string LocalizationCategory => "Achievements.Wishes";

        public override LocalizedText Description => GetLocalizedText(nameof(Description)).WithFormatArgs(NightmareCatcher.AchievementSleepTime);

        public override void Initialize()
        {
            TotalCompletion = 1;
            PositionInMainList = 11;
            UpdateCheck = AchievementUpdateCheck.NightmareCatcher;
            IsDevWish = true;
        }

        public override void ExtraUpdate(Player player, int extraInfo)
        {
            CurrentCompletion = TotalCompletion;
        }

        public override void OnCompletion(Player player)
        {
            WishCompletionEffects(player, ModContent.ItemType<NightmareCatcher>());
        }

        public override void SaveProgress(TagCompound tag)
        {
            tag["NightmareCurrentCompletion"] = CurrentCompletion;
            tag["NightmareDoneCompletionEffects"] = DoneCompletionEffects;
        }

        public override void LoadProgress(TagCompound tag)
        {
            CurrentCompletion = tag.Get<int>("NightmareCurrentCompletion");
            DoneCompletionEffects = tag.Get<bool>("NightmareDoneCompletionEffects");
        }
    }
}
