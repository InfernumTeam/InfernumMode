using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class EaterOfWorldsRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<EaterOfWorldsRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/EaterOfWorldsRelicTile";
    }
}
