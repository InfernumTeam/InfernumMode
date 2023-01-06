using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class CalamitasCloneRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<CalamitasCloneRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/CalamitasCloneRelicTile";
    }
}
