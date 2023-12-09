using InfernumMode.Content.Rarities.InfernumRarities;
using InfernumMode.Content.Tiles.Wishes;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Placeables
{
    // Dedicated to: Toasty
    public class CreditPainting : ModItem
    {
        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<CreditPaintingTile>());

            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 99;
            Item.rare = ModContent.RarityType<InfernumCreditRarity>();
            Item.value = Item.buyPrice(0, 1, 0, 0);
            Item.Infernum_Tooltips().DeveloperItem = true;
        }
    }
}
