using InfernumMode.Content.Tiles.Relics;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class EyeOfCthulhuRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Eye of Cthulhu Relic";

        public override string PersonalMessage => Utilities.GetLocalization("Items.EyeOfCthulhuRelic.PersonalMessage").Value;

        public override Color? PersonalMessageColor => Color.LightGray;

        public override int TileID => ModContent.TileType<EyeOfCthulhuRelicTile>();
    }
}
