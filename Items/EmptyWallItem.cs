using Terraria.ModLoader;
using Terraria.ID;
using InfernumMode.Tiles;
using InfernumMode.Walls;

namespace InfernumMode.Items
{
    public class EmptyWallItem : ModItem
    {
        public override void SetStaticDefaults()
        {
            SacrificeTotal = 100;
            DisplayName.SetDefault("Connecting Wall");
        }

        public override void SetDefaults()
        {
            Item.createWall = ModContent.WallType<EmptyWall>();
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
