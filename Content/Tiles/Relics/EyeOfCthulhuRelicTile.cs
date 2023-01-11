using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class EyeOfCthulhuRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<EyeOfCthulhuRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/EyeOfCthulhuRelicTile";
    }
}
