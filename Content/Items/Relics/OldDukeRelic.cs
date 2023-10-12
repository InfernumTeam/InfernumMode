using InfernumMode.Content.Tiles.Relics;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class OldDukeRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Old Duke Relic";

        public override string PersonalMessage => Utilities.GetLocalization("Items.OldDukeRelic.PersonalMessage").Value;

        public override Color? PersonalMessageColor => Color.Lerp(Color.Lime, Color.Yellow, 0.8f);

        public override int TileID => ModContent.TileType<OldDukeRelicTile>();
    }
}
