using InfernumMode.Content.Tiles.Relics;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class EyeOfCthulhuRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Eye of Cthulhu Relic";

        public override string PersonalMessage => "Remember to not force yourself too much in the pursuit of victory. Take breaks if you need to.\n" +
            "The most important thing is fun.";

        public override Color? PersonalMessageColor => Color.LightGray;

        public override int TileID => ModContent.TileType<EyeOfCthulhuRelicTile>();
    }
}
