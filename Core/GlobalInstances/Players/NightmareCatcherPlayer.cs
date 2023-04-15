using CalamityMod;
using InfernumMode.Content.Achievements;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class NightmareCatcherPlayer : ModPlayer
    {
        public int BrimstoneCragsSleepTimer
        {
            get;
            set;
        }

        public const int AchievementSleepTime = 15;

        public override void PostUpdate()
        {
            if (Player.sleeping.FullyFallenAsleep && Player.Calamity().ZoneCalamity)
                BrimstoneCragsSleepTimer++;
            else
                BrimstoneCragsSleepTimer = 0;

            // Apply the achievement if sleeping for long enough.
            if (BrimstoneCragsSleepTimer >= CalamityUtils.SecondsToFrames(AchievementSleepTime))
            {
                AchievementPlayer.ExtraUpdateHandler(Player, AchievementUpdateCheck.NightmareCatcher);
                BrimstoneCragsSleepTimer = 0;
            }
        }
    }
}
