using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class DeerclopsRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Deerclops Relic";

        public override int TileID => ModContent.TileType<DeerclopsRelicTile>();
    }
}
