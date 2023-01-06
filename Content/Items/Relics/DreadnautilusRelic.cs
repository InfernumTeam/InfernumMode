using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class DreadnautilusRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Dreadnautilus Relic";

        public override int TileID => ModContent.TileType<DreadnautilusRelicTile>();
    }
}
