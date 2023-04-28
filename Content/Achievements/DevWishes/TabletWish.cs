using CalamityMod.NPCs.AquaticScourge;
using InfernumMode.Content.BehaviorOverrides.BossAIs.AquaticScourge;
using InfernumMode.Content.Items.Pets;
using InfernumMode.Core.GlobalInstances.Systems;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Content.Achievements.DevWishes
{
    public class TabletWish : Achievement
    {
        public override void Initialize()
        {
            Name = "Unsullied";
            Description = "Not all beasts are monsters\n" +
                $"[c/777777:Defeat the Aquatic Scourge without ever letting the acid meter exceed 50% during the battle]";
            TotalCompletion = 1;
            PositionInMainList = 12;
            UpdateCheck = AchievementUpdateCheck.NPCKill;
            IsDevWish = true;
        }

        public override void ExtraUpdate(Player player, int extraInfo)
        {
            if (Main.npc[extraInfo].type == ModContent.NPCType<AquaticScourgeHead>() && WorldSaveSystem.InfernumMode && Main.npc[extraInfo].Infernum().ExtraAI[AquaticScourgeHeadBehaviorOverride.AcidMeterEverReachedHalfIndex] == 0f)
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
