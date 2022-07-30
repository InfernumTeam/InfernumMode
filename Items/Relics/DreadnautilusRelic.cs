using InfernumMode.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Items.Relics
{
    public class DreadnautilusRelic : BaseRelicItem
	{
        public override string DisplayNameToUse => "Infernal Dreadnautilus Relic";

        public override int TileID => ModContent.TileType<DreadnautilusRelicTile>();
	}
}
