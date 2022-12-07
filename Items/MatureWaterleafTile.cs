using Terraria.ModLoader;
using Terraria.ID;
using InfernumMode.Tiles;
using Terraria;

namespace InfernumMode.Items
{
    public class MatureWaterleafTile : ModItem
    {
        public override void SetStaticDefaults()
        {
            SacrificeTotal = 100;
            DisplayName.SetDefault("Mature Waterleaf Block");
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileID.BloomingHerbs, 4);
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTurn = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.autoReuse = true;
            Item.consumable = false;
            Item.width = 16;
            Item.height = 16;
        }

        public override bool? UseItem(Player player)
        {
            if (player.InInteractionRange(Player.tileTargetX, Player.tileTargetY))
            {
                if (WorldGen.PlaceTile(Player.tileTargetX, Player.tileTargetY, TileID.BloomingHerbs, true, false, player.whoAmI, 4))
                    Main.tile[Player.tileTargetX, Player.tileTargetY].TileType = TileID.BloomingHerbs;
            }
            return null;
        }
    }
}
