using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class WallOfFleshRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<WallOfFleshRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/WallOfFleshRelicTile";
    }
}
