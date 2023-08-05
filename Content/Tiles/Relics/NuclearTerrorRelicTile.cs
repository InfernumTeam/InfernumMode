using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    internal class NuclearTerrorRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<NuclearTerrorRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/NuclearTerrorRelicTile";
    }
}
