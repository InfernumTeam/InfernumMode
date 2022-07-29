using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class AquaticScourgeRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<AquaticScourgeRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/AquaticScourgeRelicTile";
    }
}
