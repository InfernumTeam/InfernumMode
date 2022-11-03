using Terraria.ModLoader;
using Terraria.ID;
using InfernumMode.Tiles;

namespace InfernumMode.Items
{
    public class SeaShellItem : ModItem
    {
        public override void SetStaticDefaults()
        {
            SacrificeTotal = 100;
            DisplayName.SetDefault("Non-Falling Sea Shell");
        }

        public override void SetDefaults()
        {
            Item.createTile = ModContent.TileType<SeaShell>();
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
