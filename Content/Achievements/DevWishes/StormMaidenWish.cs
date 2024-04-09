using InfernumMode.Content.Items.Weapons.Rogue;
using InfernumMode.Content.Projectiles.Rogue;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Content.Achievements.DevWishes
{
    public class StormMaidenWish : Achievement
    {
        public override string LocalizationCategory => "Achievements.Wishes";

        public override void Initialize()
        {
            TotalCompletion = 1;
            PositionInMainList = 17;
            UpdateCheck = AchievementUpdateCheck.ProjectileKill;
            IsDevWish = true;
        }

        public override void ExtraUpdate(Player player, int extraInfo)
        {
            if (Main.projectile[extraInfo].type == ModContent.ProjectileType<StormMaidensRetributionWorldProj>())
                CurrentCompletion = TotalCompletion;
        }

        public override void OnCompletion(Player player)
        {
            WishCompletionEffects(player, ModContent.ItemType<StormMaidensRetribution>());
        }

        public override void SaveProgress(TagCompound tag)
        {
            tag["MaidenCurrentCompletion"] = CurrentCompletion;
            tag["MaidenDoneCompletionEffects"] = DoneCompletionEffects;
        }

        public override void LoadProgress(TagCompound tag)
        {
            CurrentCompletion = tag.Get<int>("MaidenCurrentCompletion");
            DoneCompletionEffects = tag.Get<bool>("MaidenDoneCompletionEffects");
        }
    }
}
