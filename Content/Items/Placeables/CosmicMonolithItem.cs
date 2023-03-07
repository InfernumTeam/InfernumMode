using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using InfernumMode.Content.Tiles;
using CalamityMod.Rarities;
using CalamityMod.Items.Materials;
using CalamityMod.Tiles.Furniture.CraftingStations;

namespace InfernumMode.Content.Items.Placeables
{
    public class CosmicMonolithItem : ModItem
    {
        public override void SetStaticDefaults()
        {
            SacrificeTotal = 1;
            DisplayName.SetDefault("Cosmic Monolith");
        }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 42;
            Item.maxStack = 999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.value = Item.buyPrice(0, 4, 0, 0);
            Item.rare = ModContent.RarityType<DarkBlue>();
            Item.createTile = ModContent.TileType<CosmicMonolithTile>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<CosmiliteBar>());
            recipe.AddIngredient(ItemID.Glass, 5);
            recipe.AddTile(ModContent.TileType<CosmicAnvil>());
            recipe.Register();
        }
    }
}
