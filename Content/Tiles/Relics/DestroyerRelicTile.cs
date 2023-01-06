using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class DestroyerRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<DestroyerRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/DestroyerRelicTile";
    }
}
