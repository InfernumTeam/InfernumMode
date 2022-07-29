using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class TwinsRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<TwinsRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/TwinsRelicTile";
    }
}
