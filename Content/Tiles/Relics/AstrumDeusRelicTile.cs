using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class AstrumDeusRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<AstrumDeusRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/AstrumDeusRelicTile";
    }
}
