using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class PlaguebringerGoliathRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<PlaguebringerGoliathRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/PlaguebringerGoliathRelicTile";
    }
}
