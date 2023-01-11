using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class TwinsRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<TwinsRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/TwinsRelicTile";
    }
}
