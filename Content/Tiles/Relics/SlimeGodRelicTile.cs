using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class SlimeGodRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<SlimeGodRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/SlimeGodRelicTile";
    }
}
