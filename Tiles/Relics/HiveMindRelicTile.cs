using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class HiveMindRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<HiveMindRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/HiveMindRelicTile";
    }
}
