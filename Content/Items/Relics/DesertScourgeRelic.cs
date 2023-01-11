using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class DesertScourgeRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Desert Scourge Relic";

        public override int TileID => ModContent.TileType<DesertScourgeRelicTile>();
    }
}
