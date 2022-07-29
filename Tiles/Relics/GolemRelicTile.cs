using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class GolemRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<GolemRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/GolemRelicTile";
    }
}
