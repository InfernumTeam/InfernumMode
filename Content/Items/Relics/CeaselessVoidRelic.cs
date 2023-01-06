using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class CeaselessVoidRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Ceaseless Void Relic";

        public override int TileID => ModContent.TileType<CeaselessVoidRelicTile>();
    }
}
