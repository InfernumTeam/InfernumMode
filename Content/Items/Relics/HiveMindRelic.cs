using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class HiveMindRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Hive Mind Relic";

        public override int TileID => ModContent.TileType<HiveMindRelicTile>();
    }
}
