using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class AquaticScourgeRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<AquaticScourgeRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/AquaticScourgeRelicTile";
    }
}
