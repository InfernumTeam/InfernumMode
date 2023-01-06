using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class StormWeaverRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<StormWeaverRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/StormWeaverRelicTile";
    }
}
