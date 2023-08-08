using InfernumMode.Content.Tiles.Misc;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Placeables
{
    public class AquaticScourgeSkull : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 102;
            Item.height = 82;
            Item.maxStack = 999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.value = Item.buyPrice(0, 20, 0, 0);
            Item.rare = ItemRarityID.LightPurple;
            Item.createTile = ModContent.TileType<AquaticScourgeSkullTile>();
        }
    }
}
