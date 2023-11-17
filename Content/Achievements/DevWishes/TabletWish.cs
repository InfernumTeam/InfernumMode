using CalamityMod.NPCs.AquaticScourge;
using InfernumMode.Content.BehaviorOverrides.BossAIs.AquaticScourge;
using InfernumMode.Content.Items.Misc;
using InfernumMode.Core.GlobalInstances.Systems;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Content.Achievements.DevWishes
{
    public class TabletWish : Achievement
    {
        public override string LocalizationCategory => "Achievements.Wishes";
        
        public override void Initialize()
        {
            TotalCompletion = 1;
            PositionInMainList = 12;
            UpdateCheck = AchievementUpdateCheck.NPCKill;
            IsDevWish = true;
        }

        public override void ExtraUpdate(Player player, int extraInfo)
        {
            if (Main.npc[extraInfo].type == ModContent.NPCType<AquaticScourgeHead>() && WorldSaveSystem.InfernumModeEnabled && Main.npc[extraInfo].Infernum().ExtraAI[AquaticScourgeHeadBehaviorOverride.AcidMeterEverReachedHalfIndex] == 0f)
                CurrentCompletion = TotalCompletion;
        }

        public override void OnCompletion(Player player)
        {
            WishCompletionEffects(player, ModContent.ItemType<DisenchantedTablet>());
        }

        public override void SaveProgress(TagCompound tag)
        {
            tag["TabletCurrentCompletion"] = CurrentCompletion;
            tag["TabletDoneCompletionEffects"] = DoneCompletionEffects;
        }

        public override void LoadProgress(TagCompound tag)
        {
            CurrentCompletion = tag.Get<int>("TabletCurrentCompletion");
            DoneCompletionEffects = tag.Get<bool>("TabletDoneCompletionEffects");
        }
    }
}
