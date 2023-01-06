using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class LeviathanRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<LeviathanRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/LeviathanRelicTile";
    }
}
