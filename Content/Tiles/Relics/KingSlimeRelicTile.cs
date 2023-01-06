using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class KingSlimeRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<KingSlimeRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/KingSlimeRelicTile";
    }
}
