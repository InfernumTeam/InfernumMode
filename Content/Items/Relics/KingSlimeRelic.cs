using InfernumMode.Content.Tiles.Relics;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class KingSlimeRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal King Slime Relic";

        public override string PersonalMessage => "Even seasoned players may struggle somewhat in the face of something new and unfamiliar. Adaptability is key.";

        public override Color? PersonalMessageColor => Color.Lerp(Color.DeepSkyBlue, Color.LightGray, 0.6f);

        public override int TileID => ModContent.TileType<KingSlimeRelicTile>();
    }
}
