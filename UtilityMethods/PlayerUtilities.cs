using CalamityMod;
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

            return dashType.CollisionType == CalamityMod.Enums.DashCollisionType.ShieldSlam;
        }
    }
}
