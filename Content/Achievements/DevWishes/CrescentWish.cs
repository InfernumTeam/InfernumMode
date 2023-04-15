using CalamityMod.NPCs.CalClone;
using InfernumMode.Content.BehaviorOverrides.BossAIs.CalamitasClone;
using InfernumMode.Content.Items;
using InfernumMode.Core.GlobalInstances.Systems;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Content.Achievements.DevWishes
{
    public class CrescentWish : Achievement
    {
        public override void Initialize()
        {
            Name = "Post-apocalyptic";
            Description = "The witch's sins could never be fully atoned\n" +
                $"[c/777777:Defeat the {CalamitasCloneBehaviorOverride.CustomName} in the underworld]";
            TotalCompletion = 1;
            PositionInMainList = 11;
            UpdateCheck = AchievementUpdateCheck.NPCKill;
            IsDevWish = true;
        }

        public override void ExtraUpdate(Player player, int extraInfo)
        {
            if (Main.npc[extraInfo].type == ModContent.NPCType<CalamitasClone>() && WorldSaveSystem.InfernumMode && Main.npc[extraInfo].Infernum().ExtraAI[CalamitasCloneBehaviorOverride.FoughtInUnderworldIndex] == 1f)
                CurrentCompletion = TotalCompletion;
        }

        public override void OnCompletion(Player player)
        {
            WishCompletionEffects(player, ModContent.ItemType<BrimstoneCrescentStaff>());
        }

        public override void SaveProgress(TagCompound tag)
        {
            tag["CrescentCurrentCompletion"] = CurrentCompletion;
            tag["CrescentDoneCompletionEffects"] = DoneCompletionEffects;
        }

        public override void LoadProgress(TagCompound tag)
        {
            CurrentCompletion = tag.Get<int>("CrescentCurrentCompletion");
            DoneCompletionEffects = tag.Get<bool>("CrescentDoneCompletionEffects");
        }
    }
}
