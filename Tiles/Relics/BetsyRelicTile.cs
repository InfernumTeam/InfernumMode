using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class BetsyRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<BetsyRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/BetsyRelicTile";
    }
}
