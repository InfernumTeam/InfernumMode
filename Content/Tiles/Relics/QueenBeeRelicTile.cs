using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class QueenBeeRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<QueenBeeRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/QueenBeeRelicTile";
    }
}
