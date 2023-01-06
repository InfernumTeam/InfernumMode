using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class QueenSlimeRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<QueenSlimeRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/QueenSlimeRelicTile";
    }
}
