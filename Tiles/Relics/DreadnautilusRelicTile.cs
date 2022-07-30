using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class DreadnautilusRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<DreadnautilusRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/DreadnautilusRelicTile";
    }
}
