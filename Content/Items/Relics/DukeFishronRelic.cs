using InfernumMode.Content.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class DukeFishronRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Duke Fishron Relic";

        public override int TileID => ModContent.TileType<DukeFishronRelicTile>();
    }
}
