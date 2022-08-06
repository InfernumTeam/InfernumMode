using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class SupremeCalamitasRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<SupremeCalamitasRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/SupremeCalamitasRelicTile";
    }
}
