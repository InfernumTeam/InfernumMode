using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class DukeFishronRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<DukeFishronRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/DukeFishronRelicTile";
    }
}
