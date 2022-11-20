using InfernumMode.Achievements;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.GlobalInstances
{
    public class InfernumGlobalItem : GlobalItem
    {
        #region Overrides
        public override bool OnPickup(Item item, Player player)
        {
            if(item.type == ModContent.ItemType<DemonicChaliceOfInfernum>())
            {
                AchievementManager.ExtraUpdateAchievements(new UpdateContext(-1, item.type));
            }
            return true;
        }
        #endregion
    }
}
