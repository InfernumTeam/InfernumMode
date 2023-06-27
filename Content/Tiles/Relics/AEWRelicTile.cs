using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class AEWRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<AEWRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/AEWRelicTile";
    }
}
