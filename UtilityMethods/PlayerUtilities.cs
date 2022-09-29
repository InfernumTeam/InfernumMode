using CalamityMod;
using CalamityMod.Enums;
using Terraria;

namespace InfernumMode
{
    public static partial class Utilities
    {
        public static bool HasShieldBash(this Player player)
        {
            var dashType = player.Calamity().UsedDash;
            if (dashType is null)
                return false;

            return dashType.CollisionType == DashCollisionType.ShieldSlam;
        }

        public static bool HasDash(this Player player)
        {
            var dashType = player.Calamity().UsedDash;
            return dashType is not null || player.dashType >= 1;
        }
    }
}
