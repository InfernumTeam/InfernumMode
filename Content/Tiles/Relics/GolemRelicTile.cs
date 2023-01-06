using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class GolemRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<GolemRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/GolemRelicTile";
    }
}
