using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class GiantClamRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<GiantClamRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/GiantClamRelicTile";
    }
}
