using InfernumMode.Content.Items.Weapons.Rogue;
using InfernumMode.Content.Projectiles.Rogue;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Content.Achievements.DevWishes
{
    public class StormMaidenWish : Achievement
    {
        public override void Initialize()
        {
            Name = "Lamentation";
            Description = "Their tears blend with the raindrops, mourning over all that couldn't be\n" +
                $"[c/777777:Defeat the Exo Mechs and Calamitas and find the spear near your spawn point during the rain]";
            TotalCompletion = 1;
            PositionInMainList = 14;
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
