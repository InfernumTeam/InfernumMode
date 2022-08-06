using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class PolterghastRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<PolterghastRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/PolterghastRelicTile";
    }
}
