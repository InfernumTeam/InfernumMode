using InfernumMode.Content.Rarities.InfernumRarities;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items
{
    public class LunarCoin : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Lunar Coin");
            SacrificeTotal = 1;
        }

        public override void SetDefaults()
        {
            Item.value = 0;
            Item.rare = ModContent.RarityType<InfernumPurityRarity>();
            Item.Infernum_Tooltips().DeveloperItem = true;
        }
    }
}
