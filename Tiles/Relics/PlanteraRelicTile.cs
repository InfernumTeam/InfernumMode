using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class PlanteraRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<PlanteraRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/PlanteraRelicTile";
    }
}
