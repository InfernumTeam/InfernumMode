using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class CalamitasCloneRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<CalamitasCloneRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/CalamitasCloneRelicTile";
    }
}
