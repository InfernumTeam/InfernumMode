using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class DarkMageRelic : BaseRelicItem
    {
        public override int TileID => ModContent.TileType<DarkMageRelicTile>();
    }
}
