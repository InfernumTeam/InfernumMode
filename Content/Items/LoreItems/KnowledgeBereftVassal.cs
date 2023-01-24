using CalamityMod.Items.LoreItems;
using CalamityMod.Items.Materials;
using InfernumMode.Content.Items.Placeables;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.LoreItems
{
    public class KnowledgeBereftVassal : LoreItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("The Bereft Vassal and Great Sand Shark");
            Tooltip.SetDefault("A most unusual allegiance.\n" +
                "Once a great warrior under the Sea King, it appears that the destruction of his home had left him despondent.\n" +
                "And yet, it would seem that fighting you has reignited his spirits.");
            SacrificeTotal = 1;
            ItemID.Sets.ItemNoGravity[Item.type] = true;
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
