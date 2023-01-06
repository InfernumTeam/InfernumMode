using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class PolterghastRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<PolterghastRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/PolterghastRelicTile";
    }
}
