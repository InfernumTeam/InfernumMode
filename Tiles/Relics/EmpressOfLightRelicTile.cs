using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class EmpressOfLightRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<EmpressOfLightRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/EmpressOfLightRelicTile";
    }
}
