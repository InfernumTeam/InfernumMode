using CalamityMod.Items.LoreItems;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Placeables.Banners;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Items
{
    public class KnowledgeGreatSandShark : LoreItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("The Great Sand Shark");
            Tooltip.SetDefault("Nature proves itself to be a formidable mistress.\n" +
                "Even in the most extreme of environmental circumstances it seems that there is no shortage of such displays of resilience.");
            ItemID.Sets.ItemNoGravity[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.rare = ItemRarityID.Yellow;
            Item.consumable = false;
        }

        public override bool CanUseItem(Player player) => false;

        public override void AddRecipes()
        {
            Recipe r = CreateRecipe();
            r.AddTile(TileID.Bookcases);
            r.AddIngredient(ModContent.ItemType<GreatSandSharkBanner>());
            r.AddIngredient(ModContent.ItemType<VictoryShard>(), 10);
            r.Register();
        }
    }
}
