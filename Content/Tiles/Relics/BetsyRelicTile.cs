using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class BetsyRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<BetsyRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/BetsyRelicTile";
    }
}
