using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class WallOfFleshRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<WallOfFleshRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/WallOfFleshRelicTile";
    }
}
