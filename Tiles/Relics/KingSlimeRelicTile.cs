using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class KingSlimeRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<KingSlimeRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/KingSlimeRelicTile";
    }
}
