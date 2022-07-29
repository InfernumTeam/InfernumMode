using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class AstrumAureusRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<AstrumAureusRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/AstrumAureusRelicTile";
    }
}
