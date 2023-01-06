using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class QueenSlimeRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Queen Slime Relic";

        public override int TileID => ModContent.TileType<QueenSlimeRelicTile>();
    }
}
