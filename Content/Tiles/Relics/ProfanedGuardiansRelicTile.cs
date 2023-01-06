using InfernumMode.Content.Items.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Relics
{
    public class ProfanedGuardiansRelicTile : BaseInfernumBossRelic
    {
        public override int DropItemID => ModContent.ItemType<ProfanedGuardiansRelic>();

        public override string RelicTextureName => "InfernumMode/Content/Tiles/Relics/ProfanedGuardiansRelicTile";
    }
}
