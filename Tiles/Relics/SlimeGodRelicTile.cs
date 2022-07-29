using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class SlimeGodRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<SlimeGodRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/SlimeGodRelicTile";
    }
}
