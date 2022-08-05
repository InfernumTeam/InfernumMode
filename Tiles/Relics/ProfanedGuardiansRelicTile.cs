using InfernumMode.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Relics
{
    public class ProfanedGuardiansRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<ProfanedGuardiansRelic>();

        public override string RelicTextureName => "InfernumMode/Tiles/Relics/ProfanedGuardiansRelicTile";
    }
}
