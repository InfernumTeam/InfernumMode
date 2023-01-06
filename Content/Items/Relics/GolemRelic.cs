using InfernumMode.Content.Tiles.Relics;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class GolemRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Golem Relic";

        public override string PersonalMessage => "Simple methodical planning goes a long way. It will be invaluable against future obstacles.";

        public override Color? PersonalMessageColor => Color.Lerp(Color.Orange, Color.Brown, 0.4f);

        public override int TileID => ModContent.TileType<GolemRelicTile>();
    }
}
