using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class WallOfFleshRelic : BaseRelicItem
    {
        public override int TileID => ModContent.TileType<WallOfFleshRelicTile>();
    }
}
