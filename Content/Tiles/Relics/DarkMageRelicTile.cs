using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class DarkMageRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<DarkMageRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/DarkMageRelicTile";
    }
}
