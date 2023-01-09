using InfernumMode.Content.Items;
using InfernumMode.Core.GlobalInstances.Players;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.GlobalInstances.GlobalItems
{
    public class ChaliceAchievementGlobalItem : GlobalItem
    {
        public override bool OnPickup(Item item, Player player)
        {
            if (item.type == ModContent.ItemType<DemonicChaliceOfInfernum>())
                AchievementPlayer.ExtraUpdateAchievements(player, new UpdateContext(itemType: item.type));
            return true;
        }
    }
}
