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
        public override string Lore =>
@"An unusal pair of solitary camaraderie.
Once a warrior of noble renown, it would seem that Argus was one of the few survivors to emerge from the ruins of Ilmeris.
Without purpose, he sought refuge in these enigmatic ruins, silently witnessing the passage of time until your arrival.
Fates like his are the consequence of misguided self-righteousness. Do not cause senseless pain in the pursuit of greater causes.";

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            DisplayName.SetDefault("The Bereft Vassal and Great Sand Shark");
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
