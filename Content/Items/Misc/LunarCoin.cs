using InfernumMode.Content.Rarities.InfernumRarities;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Misc
{
    // Dedicated to: Pengolin, Fire Devourer
    public class LunarCoin : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Lunar Coin");
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.value = 0;
            Item.rare = ModContent.RarityType<InfernumPurityRarity>();
            Item.Infernum_Tooltips().DeveloperItem = true;
        }
    }
}
