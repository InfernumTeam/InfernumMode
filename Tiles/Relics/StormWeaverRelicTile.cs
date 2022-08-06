using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class StormWeaverRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<StormWeaverRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/StormWeaverRelicTile";
    }
}
