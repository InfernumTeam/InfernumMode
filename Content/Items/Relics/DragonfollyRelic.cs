using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class DragonfollyRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Dragonfolly Relic";

        public override int TileID => ModContent.TileType<DragonfollyRelicTile>();
    }
}
