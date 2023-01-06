using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class ProvidenceRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<ProvidenceRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/ProvidenceRelicTile";
    }
}
