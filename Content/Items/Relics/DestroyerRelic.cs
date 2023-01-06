using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class DestroyerRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Destroyer Relic";

        public override int TileID => ModContent.TileType<DestroyerRelicTile>();
    }
}
