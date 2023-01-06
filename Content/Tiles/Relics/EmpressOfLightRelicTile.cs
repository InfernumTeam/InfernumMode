using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class EmpressOfLightRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<EmpressOfLightRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/EmpressOfLightRelicTile";
    }
}
