using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Placeables.Ores;
using InfernumMode.Content.Items.Misc;
using InfernumMode.Content.Rarities.InfernumRarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Accessories
{
    public class Purity : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Purity");
            /* Tooltip.SetDefault("30% increased damage and attack speed\n" +
                "Grants immunity to Nightwither\n" +
                "[c/ff5d5d:You are unlucky]\n" +
                "[c/59aae7:It sounds like two brothers, chasing glass frogs in the sun]"); */
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.accessory = true;
            Item.width = 60;
            Item.height = 56;
            Item.rare = ModContent.RarityType<InfernumPurityRarity>();
            Item.value = CalamityGlobalItem.RarityTurquoiseBuyPrice;
            Item.Infernum_Tooltips().DeveloperItem = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) => player.Infernum_Accessory().Purity = true;

        public override void AddRecipes()
        {
            CreateRecipe().
                AddTile(TileID.LunarCraftingStation).
                AddIngredient(ModContent.ItemType<LunarCoin>()).
                AddIngredient(ModContent.ItemType<ExodiumCluster>(), 25).
                AddIngredient(ModContent.ItemType<CoreofEleum>(), 10).
                Register();
        }
    }
}
