using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class DarkMageRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<DarkMageRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/DarkMageRelicTile";
    }
}
