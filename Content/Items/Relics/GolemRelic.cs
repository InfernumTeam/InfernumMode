using InfernumMode.Content.Tiles.Relics;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class GolemRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Golem Relic";

        public override string PersonalMessage => Utilities.GetLocalization("Items.GolemRelic.PersonalMessage").Value;

        public override Color? PersonalMessageColor => Color.Lerp(Color.Orange, Color.Brown, 0.4f);

        public override int TileID => ModContent.TileType<GolemRelicTile>();
    }
}
