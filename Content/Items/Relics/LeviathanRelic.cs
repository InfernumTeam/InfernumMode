using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class LeviathanRelic : BaseRelicItem
    {
        public override int TileID => ModContent.TileType<LeviathanRelicTile>();
    }
}
