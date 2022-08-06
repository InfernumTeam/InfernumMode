using InfernumMode.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Items.Relics
{
    public class PolterghastRelic : BaseRelicItem
	{
        public override string DisplayNameToUse => "Infernal Polterghast Relic";

        public override int TileID => ModContent.TileType<PolterghastRelicTile>();
	}
}
