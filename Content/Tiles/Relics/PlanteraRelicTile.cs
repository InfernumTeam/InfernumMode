using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class PlanteraRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<PlanteraRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/PlanteraRelicTile";
    }
}
