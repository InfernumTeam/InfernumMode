using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class DreadnautilusRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<DreadnautilusRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/DreadnautilusRelicTile";
    }
}
