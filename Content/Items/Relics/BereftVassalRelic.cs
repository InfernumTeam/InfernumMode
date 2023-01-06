using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class BereftVassalRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Bereft Vassal Relic";

        public override int TileID => ModContent.TileType<BereftVassalRelicTile>();
    }
}
