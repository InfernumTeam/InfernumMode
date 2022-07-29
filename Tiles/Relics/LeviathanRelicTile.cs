using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class LeviathanRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<LeviathanRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/LeviathanRelicTile";
    }
}
