using InfernumMode.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Items.Relics
{
    public class EmpressOfLightRelic : BaseRelicItem
	{
        public override string DisplayNameToUse => "Infernal Empress of Light Relic";

        public override int TileID => ModContent.TileType<EmpressOfLightRelicTile>();
	}
}
