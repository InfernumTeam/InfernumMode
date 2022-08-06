using InfernumMode.Tiles.Relics;
using Terraria.ModLoader;

namespace InfernumMode.Items.Relics
{
    public class SignusRelic : BaseRelicItem
	{
        public override string DisplayNameToUse => "Infernal Signus Relic";

        public override int TileID => ModContent.TileType<SignusRelicTile>();
	}
}
