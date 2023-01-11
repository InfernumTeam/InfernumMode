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
                {
                    return "Spectacular work. You have conquered all of the major obstacles.\n" +
                        "Take pride in this accomplishment, for you are considerably stronger than you were when you began.";
                }

                return "You have done phenomenally. There is only one challenge left now-\n" +
                    "Face the Cosmic Engineer.";
            }
        }

        public override Color? PersonalMessageColor => Color.DarkRed;

        public override int TileID => ModContent.TileType<SupremeCalamitasRelicTile>();
    }
}
