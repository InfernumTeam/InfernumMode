using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class CrabulonRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<CrabulonRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/CrabulonRelicTile";
    }
}
