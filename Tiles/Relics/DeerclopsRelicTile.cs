using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class DeerclopsRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<DeerclopsRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/DeerclopsRelicTile";
    }
}
