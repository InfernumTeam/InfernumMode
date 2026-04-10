using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class HiveMindRelic : BaseRelicItem
    {
        public override int TileID => ModContent.TileType<HiveMindRelicTile>();
    }
}
