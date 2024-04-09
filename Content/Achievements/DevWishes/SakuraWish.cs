using InfernumMode.Content.Items.Misc;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Content.Achievements.DevWishes
{
    public class SakuraWish : Achievement
    {
        public override string LocalizationCategory => "Achievements.Wishes";

        public override void Initialize()
        {
            TotalCompletion = 1;
            PositionInMainList = 9;
            UpdateCheck = AchievementUpdateCheck.Sakura;
            IsDevWish = true;
        }

        public override void ExtraUpdate(Player player, int extraInfo)
        {
            CurrentCompletion = TotalCompletion;
        }

        public override void OnCompletion(Player player)
        {
            WishCompletionEffects(player, ModContent.ItemType<SakuraBud>());
        }

        public override void SaveProgress(TagCompound tag)
        {
            tag["SakuraCurrentCompletion"] = CurrentCompletion;
            tag["SakuraDoneCompletionEffects"] = DoneCompletionEffects;
        }

        public override void LoadProgress(TagCompound tag)
        {
            CurrentCompletion = tag.Get<int>("SakuraCurrentCompletion");
            DoneCompletionEffects = tag.Get<bool>("SakuraDoneCompletionEffects");
        }
    }
}
