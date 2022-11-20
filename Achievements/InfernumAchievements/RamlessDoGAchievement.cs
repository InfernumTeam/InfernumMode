using CalamityMod.Items.Accessories;
using CalamityMod.NPCs;
using CalamityMod.NPCs.DevourerofGods;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Achievements.InfernumAchievements
{
    public class RamlessDoGAchievement : Achievement
    {
        #region Fields
        private bool UsedADash;
        private static List<int> RamIDs => new()
        {
            ModContent.ItemType<AsgardsValor>(),
            ModContent.ItemType<ElysianAegis>(),
            ModContent.ItemType<AsgardianAegis>()
        };
        #endregion
        #region Overrides
        public override void Initialize()
        {
            Name = "Ramification";
            Description = "Best the Devourer at his own game; without a ram!\n[c/777777:Beat the Infernum Devourer of Gods without using a ram dash]";
            TotalCompletion = 1;
            PositionInMainList = 4;
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
                // If at any point any player has a ram, invalidate them completeling the achievement.
                if (PlayerHasRam())
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
        #region Methods
        private bool PlayerHasRam()
        {
            Player player = Main.LocalPlayer;
            for (int i = 0; i <= 7 + player.GetAmountOfExtraAccessorySlotsToShow(); i++)
            {
                foreach (int item in RamIDs)
                {
                    if (player.armor[i].type == item)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        #endregion
    }
}
