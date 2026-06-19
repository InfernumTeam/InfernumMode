using CalamityMod.Items.LoreItems;
using CalamityMod.Items.Materials;
using InfernumMode.Content.Items.Placeables;
using Terraria;
using Terraria.ID;

namespace InfernumMode.Content.Items.LoreItems
{
    public class KnowledgeBereftVassal : LoreItem
    {
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.rare = ItemRarityID.Cyan;
            Item.consumable = false;
            Item.Infernum_Tooltips().InfernumItem = true;
        }

        public override bool CanUseItem(Player player) => false;

        public override void AddRecipes()
        {
            CreateRecipe(1).
                AddTile(TileID.Bookcases).
                AddIngredient<BereftVassalTrophy>().
                AddIngredient<PearlShard>(10).
                DisableDecraft().
                Register();
        }
    }
}
