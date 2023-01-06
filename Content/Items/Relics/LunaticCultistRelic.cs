using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class LunaticCultistRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Lunatic Cultist Relic";

        public override int TileID => ModContent.TileType<LunaticCultistRelicTile>();
    }
}
