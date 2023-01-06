using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class SlimeGodRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Slime God Relic";

        public override int TileID => ModContent.TileType<SlimeGodRelicTile>();
    }
}
