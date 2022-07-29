using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class DestroyerRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<DestroyerRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/DestroyerRelicTile";
    }
}
