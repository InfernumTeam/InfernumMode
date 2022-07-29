using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class DukeFishronRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<DukeFishronRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/DukeFishronRelicTile";
    }
}
