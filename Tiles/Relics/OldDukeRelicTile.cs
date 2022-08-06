using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class OldDukeRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<OldDukeRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/OldDukeRelicTile";
    }
}
