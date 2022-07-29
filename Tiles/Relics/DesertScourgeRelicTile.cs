using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class DesertScourgeRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<DesertScourgeRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/DesertScourgeRelicTile";
    }
}
