using InfernumMode.Content.Tiles.Relics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class DevourerOfGodsRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Devourer of Gods Relic";

        public override string PersonalMessage => "Sometimes pure reaction skill is the most valuable thing to cultivate.\n" +
            "You are in the final stretch. Your determination has proven invaluable up to this point.\n" +
            "May it guide you through the last challenges.";

        public override Color? PersonalMessageColor => Color.Lerp(Color.Cyan, Color.Fuchsia, Cos(Main.GlobalTimeWrappedHourly * 2.3f) * 0.5f + 0.5f);

        public override int TileID => ModContent.TileType<DevourerOfGodsRelicTile>();
    }
}
