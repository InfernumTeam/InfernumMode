using CalamityMod.Items.LoreItems;
using CalamityMod.Items.Materials;
using InfernumMode.Content.Items.Placeables;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.LoreItems
{
    public class KnowledgeBereftVassal : LoreItem
    {
        public override LocalizedText Tooltip => Language.GetText("Mods.InfernumMode.Lore.VassalLore");

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            // DisplayName.SetDefault("The Bereft Vassal and Great Sand Shark");
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.rare = ItemRarityID.Cyan;
            Item.consumable = false;
        }

        public override bool CanUseItem(Player player) => false;

        public override void AddRecipes()
        {
            CreateRecipe(1).
                AddTile(TileID.Bookcases).
                AddIngredient(ModContent.ItemType<BereftVassalTrophy>()).
                AddIngredient(ModContent.ItemType<PearlShard>(), 10).
                Register();
        }
    }
}
