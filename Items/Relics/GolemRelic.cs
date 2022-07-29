using InfernumMode.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Items.Relics
{
    public class GolemRelic : BaseRelicItem
	{
        public override string DisplayNameToUse => "Infernal Golem Relic";

        public override int TileID => ModContent.TileType<GolemRelicTile>();
	}
}
