using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class EyeOfCthulhuRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<EyeOfCthulhuRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/EyeOfCthulhuRelicTile";
    }
}
