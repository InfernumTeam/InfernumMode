using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class PerforatorsRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<PerforatorsRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/PerforatorsRelicTile";
    }
}
