using InfernumMode.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Items.Relics
{
    public class AquaticScourgeRelic : BaseRelicItem
	{
        public override string DisplayNameToUse => "Infernal Aquatic Scourge Relic";

        public override int TileID => ModContent.TileType<AquaticScourgeRelicTile>();
	}
}
