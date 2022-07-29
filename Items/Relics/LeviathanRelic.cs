using InfernumMode.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Items.Relics
{
    public class LeviathanRelic : BaseRelicItem
	{
        public override string DisplayNameToUse => "Infernal Leviathan Relic";

        public override int TileID => ModContent.TileType<LeviathanRelicTile>();
	}
}
