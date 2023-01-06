using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class ProfanedGuardiansRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Profaned Guardians Relic";

        public override int TileID => ModContent.TileType<ProfanedGuardiansRelicTile>();
    }
}
