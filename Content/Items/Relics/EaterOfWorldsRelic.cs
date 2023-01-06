using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class EaterOfWorldsRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Eater of Worlds Relic";

        public override int TileID => ModContent.TileType<EaterOfWorldsRelicTile>();
    }
}
