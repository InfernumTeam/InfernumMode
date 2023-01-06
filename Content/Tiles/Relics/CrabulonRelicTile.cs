using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class CrabulonRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<CrabulonRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/CrabulonRelicTile";
    }
}
