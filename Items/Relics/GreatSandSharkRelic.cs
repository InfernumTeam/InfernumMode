using InfernumMode.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Items.Relics
{
    public class GreatSandSharkRelic : BaseRelicItem
	{
        public override string DisplayNameToUse => "Infernal Great Sand Shark Relic";

        public override int TileID => ModContent.TileType<GreatSandSharkRelicTile>();
	}
}
