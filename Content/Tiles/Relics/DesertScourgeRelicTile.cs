using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class DesertScourgeRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<DesertScourgeRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/DesertScourgeRelicTile";
    }
}
