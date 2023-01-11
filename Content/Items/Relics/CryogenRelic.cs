using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class CryogenRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Cryogen Relic";

        public override int TileID => ModContent.TileType<CryogenRelicTile>();
    }
}
