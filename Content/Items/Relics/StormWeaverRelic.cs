using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class StormWeaverRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Storm Weaver Relic";

        public override int TileID => ModContent.TileType<StormWeaverRelicTile>();
    }
}
