using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class OgreRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<OgreRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/OgreRelicTile";
    }
}
