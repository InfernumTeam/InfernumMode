using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class OgreRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Ogre Relic";

        public override int TileID => ModContent.TileType<OgreRelicTile>();
    }
}
