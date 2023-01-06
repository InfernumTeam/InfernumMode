using InfernumMode.Content.Tiles.Relics;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class MoonLordRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Moon Lord Relic";

        public override string PersonalMessage => "You have done very well thus far.\nMay your tenacity guide you through the remaining challenges.";

        public override Color? PersonalMessageColor => Color.Lerp(Color.Cyan, Color.DarkGreen, 0.5f);

        public override int TileID => ModContent.TileType<MoonLordRelicTile>();
    }
}
