using InfernumMode.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Items.Relics
{
    public class EyeOfCthulhuRelic : BaseRelicItem
	{
        public override string DisplayNameToUse => "Infernal Eye of Cthulhu Relic";

        public override int TileID => ModContent.TileType<EyeOfCthulhuRelicTile>();
	}
}
