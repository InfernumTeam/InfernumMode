using InfernumMode.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Items.Relics
{
    public class DarkMageRelic : BaseRelicItem
	{
        public override string DisplayNameToUse => "Infernal Dark Mage Relic";

        public override int TileID => ModContent.TileType<DarkMageRelicTile>();
	}
}
