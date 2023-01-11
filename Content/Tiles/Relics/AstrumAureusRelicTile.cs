using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class AstrumAureusRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<AstrumAureusRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/AstrumAureusRelicTile";
    }
}
