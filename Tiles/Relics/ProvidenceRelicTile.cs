using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class ProvidenceRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<ProvidenceRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/ProvidenceRelicTile";
    }
}
