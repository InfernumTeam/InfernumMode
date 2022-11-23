using InfernumMode.Sounds;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader.IO;

namespace InfernumMode.Achievements
{
    public abstract class Achievement
    {
        #region Fields
        internal int PositionInMainList;
        
        public string Name = "Name";
        public string Description = "Description";
        public int TotalCompletion = 1;
        public int CurrentCompletion;
        public bool DoneCompletionEffects = false;
        #endregion

        #region Properties
        public float CompletionRatio => CurrentCompletion / (float)TotalCompletion;
        public bool IsCompleted => CurrentCompletion >= TotalCompletion;
        #endregion

        #region Virtual Methods
        /// <summary>
        /// What should happen once the achievement is completed.
        /// </summary>
        public virtual void OnCompletion()
        {
            AchivementsNotificationTracker.AddAchievementAsCompleted(this);
            Main.NewText($"Achievement Completed! [c/ff884d:{Name}]");
            SoundEngine.PlaySound(InfernumSoundRegistry.InfernumAchievementCompletionSound);
        }
        
        /// <summary>
        /// Runs on load, use this  to set all of the following fields:<br />
        /// Name<br />
        /// Description<br />
        /// TotalCompletion<br />
        /// PositionInMainList>br />
        /// </summary>
        public virtual void Initialize()
        {

        }
        /// <summary>
        /// Override to update the completion total. Called every frame.
        /// </summary>
        public virtual void Update()
        {

        }
        /// <summary>
        /// Load the completion amount and DoneCompletionEffects here. Called from ModPlayer.LoadData(TagCompound tag).
        /// </summary>
        /// <param name="tag"></param>
        public virtual void LoadProgress(TagCompound tag)
        {

        }

        /// <summary>
        /// Save the completion amount and DoneCompletionEffects here. Called from ModPlayer.SaveData(TagCompound tag).
        /// </summary>
        /// <param name="tag"></param>
        public virtual void SaveProgress(TagCompound tag)
        {

        }
        #endregion

        #region ExtraUpdates
        /// <summary>
        /// An ExtraUpdate that takes no parameters. Call this for specific circumstances.
        /// </summary>
        public virtual void ExtraUpdate()
        {

        }
        /// <summary>
        /// An ExtraUpdate that takes in a npc index.
        /// </summary>
        public virtual void ExtraUpdateNPC(int npcIndex)
        {

        }
        /// <summary>
        /// An ExtraUpdate that takes in an item type.
        /// </summary>
        /// <param name="itemID"></param>
        public virtual void ExtraUpdateItem(int itemID)
        {

        }
        #endregion
    }
}
