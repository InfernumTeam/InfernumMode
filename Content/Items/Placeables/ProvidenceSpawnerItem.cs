using InfernumMode.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Placeables
{
    public class ProvidenceSpawnerItem : ModItem
    {
        public override void SetStaticDefaults()
        {
            SacrificeTotal = 1;
            DisplayName.SetDefault("Providence Shrine Debug Item");
            Tooltip.SetDefault("Summons Providence when right clicked with a Profaned Core");
        }

        public override void SetDefaults()
        {
            Item.width = 74;
            Item.height = 74;
            Item.maxStack = 999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.value = 0;
            Item.createTile = ModContent.TileType<ProvidenceSummoner>();
        }
    }
}
