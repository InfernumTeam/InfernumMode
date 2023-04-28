using CalamityMod.NPCs.ExoMechs;
using InfernumMode.Content.Items;
using InfernumMode.Core.GlobalInstances.Systems;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Content.Achievements.DevWishes
{
    public class MatrixWish : Achievement
    {
        public override void Initialize()
        {
            Name = "The Scientific Method";
            Description = "To experiment is to fail. To fail is to learn. To learn is to advance\n" +
                "[c/777777:Defeat every single boss and Exo Mech combination]";
            TotalCompletion = 1;
            PositionInMainList = 17;
            UpdateCheck = AchievementUpdateCheck.NPCKill;
            IsDevWish = true;
        }

        public override void ExtraUpdate(Player player, int extraInfo)
        {
            if (Main.npc[extraInfo].type == ModContent.NPCType<Draedon>() && WorldSaveSystem.InfernumMode)
                CurrentCompletion = TotalCompletion;
        }

        public override void OnCompletion(Player player)
        {
            WishCompletionEffects(player, ModContent.ItemType<HyperplaneMatrix>());
        }

        public override void SaveProgress(TagCompound tag)
        {
            tag["MatrixCurrentCompletion"] = CurrentCompletion;
            tag["MatrixDoneCompletionEffects"] = DoneCompletionEffects;
        }

        public override void LoadProgress(TagCompound tag)
        {
            CurrentCompletion = tag.Get<int>("MatrixCurrentCompletion");
            DoneCompletionEffects = tag.Get<bool>("MatrixDoneCompletionEffects");
        }
    }
}
