using CalamityMod;
using CalamityMod.Enums;
using InfernumMode.Content.Cooldowns;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

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
            if (player.Calamity().blockAllDashes)
                return false;

            return dashType is not null || player.dashType >= 1;
        }

        public static void DoInfiniteFlightCheck(this Player player, Color textColor)
        {
            if (!player.HasCooldown(InfiniteFlight.ID))
            {
                CombatText.NewText(player.Hitbox, textColor, "Infinite flight granted!", true);
                SoundEngine.PlaySound(SoundID.Item35 with { Volume = 4f, Pitch = 0.3f }, player.Center);
            }
            player.AddCooldown(InfiniteFlight.ID, CalamityUtils.SecondsToFrames(0.5f));
            player.wingTime = player.wingTimeMax;
        }
    }
}
