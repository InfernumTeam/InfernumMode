using CalamityMod;
using InfernumMode.Content.Tiles.Relics;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Relics
{
    public class SupremeCalamitasRelic : BaseRelicItem
    {
        public override string DisplayNameToUse => "Infernal Supreme Calamitas Relic";

        public override string PersonalMessage
        {
            get
            {
                if (DownedBossSystem.downedExoMechs)
                    return Utilities.GetLocalization("Items.SupremeCalamitasRelic.PersonalMessage.DownedExoMechsMessage").Value;

                return Utilities.GetLocalization("Items.SupremeCalamitasRelic.PersonalMessage.DefaultMessage").Value;
            }
        }

        public override Color? PersonalMessageColor => Color.DarkRed;

        public override int TileID => ModContent.TileType<SupremeCalamitasRelicTile>();
    }
}
