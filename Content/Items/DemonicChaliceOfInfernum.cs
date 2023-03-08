using InfernumMode.Content.Rarities.InfernumRarities;
using InfernumMode.Content.Tiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items
{
    public class DemonicChaliceOfInfernum : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Demonic Chalice of Infernum");
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(6, 8));
            SacrificeTotal = 1;
        }

        public override void SetDefaults()
        {
            Item.rare = ModContent.RarityType<InfernumRedRarity>();
            Item.width = 50;
            Item.height = 96;
            Item.maxStack = 999;
            Item.DefaultToPlaceableTile(ModContent.TileType<InfernalChaliceTile>());
        }
    }
}
