using InfernumMode.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Items.Relics
{
    public class AstrumAureusRelic : BaseRelicItem
	{
        public override string DisplayNameToUse => "Infernal Astrum Aureus Relic";

        public override int TileID => ModContent.TileType<AstrumAureusRelicTile>();
	}
}
