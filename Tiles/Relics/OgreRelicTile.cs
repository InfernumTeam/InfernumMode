using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class OgreRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<OgreRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/OgreRelicTile";
    }
}
