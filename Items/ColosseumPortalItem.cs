using Terraria.ModLoader;
using Terraria.ID;
using InfernumMode.Tiles;

namespace InfernumMode.Items
{
    public class ColosseumPortalItem : ModItem
    {
        public override void SetStaticDefaults()
        {
            SacrificeTotal = 100;
            DisplayName.SetDefault("Lost Colosseum Portal");
        }

        public override void SetDefaults()
        {
            Item.createTile = ModContent.TileType<ColosseumPortal>();
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTurn = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.autoReuse = true;
            Item.consumable = true;
            Item.width = 16;
            Item.height = 16;
            Item.maxStack = 9999;
        }
    }
}
