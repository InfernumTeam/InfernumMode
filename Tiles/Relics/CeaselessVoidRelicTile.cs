using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class CeaselessVoidRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<CeaselessVoidRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/CeaselessVoidRelicTile";
    }
}
