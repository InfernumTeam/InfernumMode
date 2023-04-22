using InfernumMode.Content.Achievements;
using InfernumMode.Content.Items;
using InfernumMode.Core.GlobalInstances.Players;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.GlobalItems
{
    public class ChaliceAchievementGlobalItem : GlobalItem
    {
        public override bool OnPickup(Item item, Player player)
        {
            if (item.type == ModContent.ItemType<DemonicChaliceOfInfernum>())
                AchievementPlayer.ExtraUpdateHandler(player, AchievementUpdateCheck.ItemPickup, item.type);
            return true;
        }
    }
}
