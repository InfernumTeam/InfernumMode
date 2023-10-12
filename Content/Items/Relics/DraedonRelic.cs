using CalamityMod;
using InfernumMode.Content.Tiles.Relics;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class DraedonRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Draedon Relic";

        public override string PersonalMessage
        {
            get
            {
                if (DownedBossSystem.downedCalamitas)
                    return Utilities.GetLocalization("Items.DraedonRelic.PersonalMessage.DownedCalamitasMessage").Value;

                return Utilities.GetLocalization("Items.DraedonRelic.PersonalMessage.DefaultMessage").Value;
            }
        }

        public override Color? PersonalMessageColor => Color.DarkRed;

        public override int TileID => ModContent.TileType<DraedonRelicTile>();
    }
}
