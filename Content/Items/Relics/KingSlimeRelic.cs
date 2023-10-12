using InfernumMode.Content.Tiles.Relics;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class KingSlimeRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal King Slime Relic";

        public override string PersonalMessage => Utilities.GetLocalization("Items.KingSlimeRelic.PersonalMessage").Value;

        public override Color? PersonalMessageColor => Color.Lerp(Color.DeepSkyBlue, Color.LightGray, 0.6f);

        public override int TileID => ModContent.TileType<KingSlimeRelicTile>();
    }
}
