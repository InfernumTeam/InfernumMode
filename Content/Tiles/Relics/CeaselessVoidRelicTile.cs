using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class CeaselessVoidRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<CeaselessVoidRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/CeaselessVoidRelicTile";
    }
}
