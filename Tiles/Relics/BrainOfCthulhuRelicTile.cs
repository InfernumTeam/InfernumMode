using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class BrainOfCthulhuRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<BrainOfCthulhuRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/BrainOfCthulhuRelicTile";
    }
}
