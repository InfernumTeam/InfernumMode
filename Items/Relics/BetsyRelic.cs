using InfernumMode.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Items.Relics
{
    public class BetsyRelic : BaseRelicItem
	{
        public override string DisplayNameToUse => "Infernal Betsy Relic";

        public override int TileID => ModContent.TileType<BetsyRelicTile>();
	}
}
