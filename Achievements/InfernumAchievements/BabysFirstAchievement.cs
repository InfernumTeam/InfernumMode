using Terraria.ModLoader.IO;

namespace InfernumMode.Achievements.InfernumAchievements
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
        }
        public override void ExtraUpdate()
        {
            CurrentCompletion++;
        }
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
