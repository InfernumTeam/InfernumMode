using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class HiveMindRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<HiveMindRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/HiveMindRelicTile";
    }
}
