using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class QueenBeeRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Queen Bee Relic";

        public override int TileID => ModContent.TileType<QueenBeeRelicTile>();
    }
}
