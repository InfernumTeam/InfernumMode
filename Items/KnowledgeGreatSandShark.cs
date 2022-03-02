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
            ItemID.Sets.ItemNoGravity[item.type] = true;
        }

        public override void SetDefaults()
        {
            item.width = 20;
            item.height = 20;
            item.rare = ItemRarityID.Yellow;
            item.consumable = false;
        }

        public override bool CanUseItem(Player player) => false;

        public override void AddRecipes()
        {
            ModRecipe r = new ModRecipe(mod);
            r.SetResult(this);
            r.AddTile(TileID.Bookcases);
            r.AddIngredient(ModContent.ItemType<GreatSandSharkBanner>());
            r.AddIngredient(ModContent.ItemType<VictoryShard>(), 10);
            r.AddRecipe();
        }
    }
}
