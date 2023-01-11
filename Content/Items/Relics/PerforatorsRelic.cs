using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class PerforatorsRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Perforator Hive Relic";

        public override int TileID => ModContent.TileType<PerforatorsRelicTile>();
    }
}
