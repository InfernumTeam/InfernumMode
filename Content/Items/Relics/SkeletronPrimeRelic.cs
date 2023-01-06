using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class SkeletronPrimeRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Skeletron Prime Relic";

        public override int TileID => ModContent.TileType<SkeletronPrimeRelicTile>();
    }
}
