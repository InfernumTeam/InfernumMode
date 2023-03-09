using Terraria.ModLoader;
using Terraria.ID;
using InfernumMode.Content.Tiles.Abyss;

namespace InfernumMode.Content.Items
{
    public class LargeLumenylCrystalItem : ModItem
    {
        public override void SetStaticDefaults()
        {
            SacrificeTotal = 100;
            DisplayName.SetDefault("Large Lumenyl Crystal");
        }

        public override void SetDefaults()
        {
            Item.createTile = ModContent.TileType<LargeLumenylCrystal>();
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
