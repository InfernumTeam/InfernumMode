using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class YharonRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<YharonRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/YharonRelicTile";
    }
}
