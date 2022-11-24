using Terraria.ModLoader.IO;

namespace InfernumMode.Achievements.InfernumAchievements
{
    public class InfernalChaliceAchievement : Achievement
    {
        #region Overrides
        public override void Initialize()
        {
            Name = "Baptized By Hellfire";
            Description = "Complete the final challenge, and earn your reward\n[c/777777:Obtain the Infernal Chalice]";
            TotalCompletion = 1;
            PositionInMainList = 6;
        }
        // This takes in itemID, but doesn't need it as its checked before this is sent due to only needing
        // to check one thing.
        public override void ExtraUpdateItem(int itemID)
        {
            CurrentCompletion++;
        }
        public override void SaveProgress(TagCompound tag)
        {
            tag["ChaliceCurrentCompletion"] = CurrentCompletion;
            tag["ChaliceDoneCompletionEffects"] = DoneCompletionEffects;
        }
        public override void LoadProgress(TagCompound tag)
        {
            CurrentCompletion = tag.Get<int>("ChaliceCurrentCompletion");
            DoneCompletionEffects = tag.Get<bool>("ChaliceDoneCompletionEffects");
        }
        #endregion
    }
}
