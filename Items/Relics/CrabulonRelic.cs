using InfernumMode.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Items.Relics
{
    public class CrabulonRelic : BaseRelicItem
	{
        public override string DisplayNameToUse => "Infernal Crabulon Relic";

        public override int TileID => ModContent.TileType<CrabulonRelicTile>();
	}
}
