using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class YharonRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Yharon Relic";

        public override int TileID => ModContent.TileType<YharonRelicTile>();
    }
}
