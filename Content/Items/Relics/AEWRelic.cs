using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class AEWRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Adult Eidolon Wyrm Relic";

        public override int TileID => ModContent.TileType<AEWRelicTile>();
    }
}
