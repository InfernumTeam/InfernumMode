using CalamityMod.NPCs;
using InfernumMode.Content.Items;
using InfernumMode.Content.Items.Accessories;
using InfernumMode.Core.GlobalInstances.Players;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Content.Achievements.DevWishes
{
    public class PurityWish : Achievement
    {
        public override void Initialize()
        {
            Name = "The Tanning Starseed";
            Description = "It feels like solar winds, and solar chimes\n" +
                "[c/777777:Defeat Infernum Night Providence]";
            TotalCompletion = 1;
            PositionInMainList = 16;
            UpdateCheck = AchievementUpdateCheck.NPCKill;
            IsDevWish = true;
        }

        public override void Update()
        {
            if (AchievementPlayer.NightProviDefeated)
            {
                CurrentCompletion = TotalCompletion;
                AchievementPlayer.NightProviDefeated = false;
            }
        }

        public override void OnCompletion(Player player)
        {
            WishCompletionEffects(player, ModContent.ItemType<LunarCoin>());
        }

        public override void SaveProgress(TagCompound tag)
        {
            tag["PurityCurrentCompletion"] = CurrentCompletion;
            tag["PurityDoneCompletionEffects"] = DoneCompletionEffects;
        }

        public override void LoadProgress(TagCompound tag)
        {
            CurrentCompletion = tag.Get<int>("PurityCurrentCompletion");
            DoneCompletionEffects = tag.Get<bool>("PurityDoneCompletionEffects");
        }
    }
}
