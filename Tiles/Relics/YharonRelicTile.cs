using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class YharonRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<YharonRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/YharonRelicTile";
    }
}
