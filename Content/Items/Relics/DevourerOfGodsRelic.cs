using InfernumMode.Content.Tiles.Relics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class DevourerOfGodsRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Devourer of Gods Relic";

        public override string PersonalMessage => Utilities.GetLocalization("Items.DevourerOfGodsRelic.PersonalMessage").Value;

        public override Color? PersonalMessageColor => Color.Lerp(Color.Cyan, Color.Fuchsia, Cos(Main.GlobalTimeWrappedHourly * 2.3f) * 0.5f + 0.5f);

        public override int TileID => ModContent.TileType<DevourerOfGodsRelicTile>();
    }
}
