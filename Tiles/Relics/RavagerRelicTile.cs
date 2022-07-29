using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class RavagerRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<RavagerRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/RavagerRelicTile";
    }
}
