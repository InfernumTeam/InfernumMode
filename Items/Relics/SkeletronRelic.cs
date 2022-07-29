using InfernumMode.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Items.Relics
{
    public class SkeletronRelic : BaseRelicItem
	{
        public override string DisplayNameToUse => "Infernal Skeletron Relic";

        public override int TileID => ModContent.TileType<SkeletronRelicTile>();
	}
}
