using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class OldDukeRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<OldDukeRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/OldDukeRelicTile";
    }
}
