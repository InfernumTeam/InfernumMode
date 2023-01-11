using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class PerforatorsRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<PerforatorsRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/PerforatorsRelicTile";
    }
}
