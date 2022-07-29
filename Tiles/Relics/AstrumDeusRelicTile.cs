using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class AstrumDeusRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<AstrumDeusRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/AstrumDeusRelicTile";
    }
}
