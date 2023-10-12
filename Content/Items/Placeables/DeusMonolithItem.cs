using CalamityMod.Items.Materials;
using InfernumMode.Content.Tiles.Misc;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using CalamityMod.Items.Placeables;

namespace InfernumMode.Content.Items.Placeables
{
    public class DeusMonolithItem : ModItem
    {
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
            Item.rare = ItemRarityID.Cyan;
            Item.createTile = ModContent.TileType<DeusMonolithTile>();
            Item.accessory = true;
            Item.vanity = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (!hideVisual)
                player.Infernum_Biome().AstralMonolithEffect = true;
        }

        public override void UpdateVanity(Player player) => player.Infernum_Biome().AstralMonolithEffect = true;

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<AstralBar>(), 5);
            recipe.AddIngredient(ModContent.ItemType<AstralStone>(), 15);
            recipe.AddTile(TileID.LunarCraftingStation);
            recipe.Register();
        }
    }
}
