using CalamityMod.NPCs;
using CalamityMod.NPCs.Providence;
using InfernumMode.Core.GlobalInstances.Players;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Content.Achievements.InfernumAchievements
{
    public class NightProviAchievement : Achievement
    {
        #region Fields
        private bool HasBeenDay;
        #endregion

        #region Overrides
        public override void Initialize()
        {
            Name = "Night Knight";
            Description = "Challenge the Profaned Goddess under the gaze of the stars\n[c/777777:Beat Infernum Night Providence]";
            TotalCompletion = 1;
            PositionInMainList = 3;
            UpdateCheck = AchievementUpdateCheck.None;
        }
        public override void Update()
        {
            // If Provi was just killed.
            if (AchievementPlayer.ProviDefeated)
            {
                // And it has not been day.
                if (!HasBeenDay)
                {
                    CurrentCompletion++;
                    return;
                }
                // If one was used:
                else
                {
                    // Wait until she despawns to reset them.
                    if (NPC.AnyNPCs(ModContent.NPCType<Providence>()))
                        return;
                    else
                    {
                        // Reset stuff.
                        AchievementPlayer.ProviDefeated = false;
                        HasBeenDay = false;
                        return;
                    }
                }
            }
            // Check if PRovi is alive.
            if (CalamityGlobalNPC.holyBoss != -1)
            {
                // If at any point it is day, invalidate them completeling the achievement.
                if (Main.dayTime)
                    HasBeenDay = true;
            }
            else
            {
                HasBeenDay = false;
            }
        }
        public override void SaveProgress(TagCompound tag)
        {
            tag["NightProviCurrentCompletion"] = CurrentCompletion;
            tag["NightProviDoneCompletionEffects"] = DoneCompletionEffects;
        }
        public override void LoadProgress(TagCompound tag)
        {
            CurrentCompletion = tag.Get<int>("NightProviCurrentCompletion");
            DoneCompletionEffects = tag.Get<bool>("NightProviDoneCompletionEffects");
        }
        #endregion
    }
}
