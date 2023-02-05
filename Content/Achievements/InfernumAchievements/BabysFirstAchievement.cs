using Terraria;
using Terraria.ModLoader.IO;

namespace InfernumMode.Content.Achievements.InfernumAchievements
{
    public class BabysFirstAchievement : Achievement
    {
        #region Overrides
        public override void Initialize()
        {
            Name = "First Of Many";
            Description = "The higher the count, the more you've learnt\n[c/777777:Die to an Infernum boss]";
            TotalCompletion = 1;
            PositionInMainList = 0;
            UpdateCheck = AchievementUpdateCheck.PlayerDeath;
        }

        public override void Update()
        {
            // Just auto-complete if the player is in hardcore.
            if (Main.LocalPlayer.difficulty == 2)
                CurrentCompletion++;
        }

        public override void ExtraUpdate(Player player, int extraInfo) => CurrentCompletion++;

        public override void SaveProgress(TagCompound tag)
        {
            tag["BabyCurrentCompletion"] = CurrentCompletion;
            tag["BabyDoneCompletionEffects"] = DoneCompletionEffects;
        }

        public override void LoadProgress(TagCompound tag)
        {
            CurrentCompletion = tag.Get<int>("BabyCurrentCompletion");
            DoneCompletionEffects = tag.Get<bool>("BabyDoneCompletionEffects");
        }
        #endregion
    }
}
