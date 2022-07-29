using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class QueenSlimeRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<QueenSlimeRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/QueenSlimeRelicTile";
    }
}
