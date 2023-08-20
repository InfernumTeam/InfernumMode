using CalamityMod.NPCs;
using CalamityMod.NPCs.DevourerofGods;
using InfernumMode.Core.GlobalInstances.Players;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Content.Achievements.InfernumAchievements
{
    public class RamlessDoGAchievement : Achievement
    {
        #region Fields
        private bool UsedADash;
        #endregion

        #region Overrides
        public override void Initialize()
        {
            TotalCompletion = 1;
            PositionInMainList = 4;
            UpdateCheck = AchievementUpdateCheck.None;
        }
        public override void Update()
        {
            // If DoG was just killed.
            if (AchievementPlayer.DoGDefeated)
            {
                // And a dash was not used.
                if (!UsedADash)
                {
                    CurrentCompletion++;
                    return;
                }
                // If one was used:
                else
                {
                    // Wait until he despawns to reset them.
                    if (NPC.AnyNPCs(ModContent.NPCType<DevourerofGodsHead>()))
                        return;
                    else
                    {
                        // Reset stuff.
                        AchievementPlayer.DoGDefeated = false;
                        UsedADash = false;
                        return;
                    }
                }
            }
            // Check if DoG is alive.
            if (CalamityGlobalNPC.DoGHead != -1)
            {
                // If at any point any player has a ram, invalidate them completing the achievement.
                if (Main.LocalPlayer.HasShieldBash())
                    UsedADash = true;
            }
            // Else, ensure the variables are reset
            else
            {
                UsedADash = false;
            }
        }
        public override void SaveProgress(TagCompound tag)
        {
            tag["RamDogCurrentCompletion"] = CurrentCompletion;
            tag["RamDogDoneCompletionEffects"] = DoneCompletionEffects;
        }
        public override void LoadProgress(TagCompound tag)
        {
            CurrentCompletion = tag.Get<int>("RamDogCurrentCompletion");
            DoneCompletionEffects = tag.Get<bool>("RamDogDoneCompletionEffects");
        }
        #endregion
    }
}
