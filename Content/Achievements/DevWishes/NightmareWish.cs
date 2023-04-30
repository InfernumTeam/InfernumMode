using InfernumMode.Content.Items.Pets;
using InfernumMode.Core.GlobalInstances.Players;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Content.Achievements.DevWishes
{
    public class NightmareWish : Achievement
    {
        public override void Initialize()
        {
            Name = "It Awakens";
            Description = "It demands a sacrifice. You seem like a good choice\n" +
                $"[c/777777:Sleep in the brimstone crags for {NightmareCatcherPlayer.AchievementSleepTime} seconds]";
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
