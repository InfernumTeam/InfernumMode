using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class TwinsRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Twins Relic";

        public override int TileID => ModContent.TileType<TwinsRelicTile>();
    }
}
