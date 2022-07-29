using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class SkeletronRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<SkeletronRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/SkeletronRelicTile";
    }
}
