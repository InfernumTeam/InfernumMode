using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class NuclearTerrorRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<NuclearTerrorRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/NuclearTerrorRelicTile";
    }
}
