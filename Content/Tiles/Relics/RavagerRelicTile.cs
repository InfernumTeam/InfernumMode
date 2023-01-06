using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class RavagerRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<RavagerRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/RavagerRelicTile";
    }
}
