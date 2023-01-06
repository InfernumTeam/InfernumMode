using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class DeerclopsRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<DeerclopsRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/DeerclopsRelicTile";
    }
}
