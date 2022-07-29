using InfernumMode.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Items.Relics
{
    public class MoonLordRelic : BaseRelicItem
	{
        public override string DisplayNameToUse => "Infernal Moon Lord Relic";

        public override int TileID => ModContent.TileType<MoonLordRelicTile>();
	}
}
