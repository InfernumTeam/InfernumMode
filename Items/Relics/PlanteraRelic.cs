using InfernumMode.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Items.Relics
{
    public class PlanteraRelic : BaseRelicItem
	{
        public override string DisplayNameToUse => "Infernal Plantera Relic";

        public override int TileID => ModContent.TileType<PlanteraRelicTile>();
	}
}
