using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class BrainOfCthulhuRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<BrainOfCthulhuRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/BrainOfCthulhuRelicTile";
    }
}
