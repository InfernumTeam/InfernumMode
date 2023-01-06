using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class SkeletronRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<SkeletronRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/SkeletronRelicTile";
    }
}
