using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class EaterOfWorldsRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<EaterOfWorldsRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/EaterOfWorldsRelicTile";
    }
}
