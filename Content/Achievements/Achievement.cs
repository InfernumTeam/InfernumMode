using InfernumMode.Assets.Sounds;
using Terraria;
using Terraria.Audio;
using Terraria.Localization;
using Terraria.ModLoader.IO;

namespace InfernumMode.Content.Achievements
{
    // Ideally these would use modtypes, but I don't know what I was smoking when loading these and I don't feel like remaking it.
    public abstract class Achievement
    {
        #region Fields
        internal int PositionInMainList;
        
        public int TotalCompletion = 1;

        public int CurrentCompletion;

        public bool DoneCompletionEffects;

        public AchievementUpdateCheck UpdateCheck;

        public bool IsDevWish;
        #endregion

        #region Properties
        public LocalizedText DisplayName => GetLocalizedText(nameof(DisplayName));

        public virtual LocalizedText Description => GetLocalizedText(nameof(Description));

        public virtual string LocalizationCategory => "Achievements";

        public virtual bool ObtainableDuringBossRush => false;

        public float CompletionRatio => CurrentCompletion / (float)TotalCompletion;

        public bool IsCompleted => CurrentCompletion >= TotalCompletion;
        #endregion

        #region Virtual Methods
        /// <summary>
        /// What should happen once the achievement is completed.
        /// </summary>
        public virtual void OnCompletion(Player player)
        {
            AchievementsNotificationTracker.AddAchievementAsCompleted(this);
            Main.NewText(Utilities.GetLocalization("Status.AchievementCompletionText") + $"[c/ff884d: {DisplayName.Value}]");
            SoundEngine.PlaySound(InfernumSoundRegistry.InfernumAchievementCompletionSound);
        }

        /// <summary>
        /// Runs on load, use this  to set all of the following fields:<br />
        /// Name<br />
        /// Description<br />
        /// TotalCompletion<br />
        /// PositionInMainList<br />
        /// UpdateCheck
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

        #region ExtraUpdate
        /// <summary>
        /// Called when the set <see cref="UpdateCheck"/> occures.
        /// </summary>
        /// <param name="player">The player that called this update</param>
        /// <param name="extraInfo">What this contains depends on the <see cref="UpdateCheck"/>, ranging from item types to npc indexes.</param>
        public virtual void ExtraUpdate(Player player, int extraInfo)
        {

        }
        #endregion

        #region Methods
        protected void WishCompletionEffects(Player player, int assosiatedItemType)
        {
            AchievementsNotificationTracker.AddAchievementAsCompleted(this);
            Main.NewText(Utilities.GetLocalization("Status.AchievementCompletionText") + $"[c/ff884d: {DisplayName.Value}]");
            SoundEngine.PlaySound(InfernumSoundRegistry.InfernumAchievementCompletionSound);
            player.QuickSpawnItem(Entity.GetSource_None(), assosiatedItemType, 1);
        }
        
        protected LocalizedText GetLocalizedText(string key)
        {
            string suffix = $"{LocalizationCategory}.{GetType().Name}";
            string localizationKey = $"{suffix}.{key}";
            
            return Utilities.GetLocalization(localizationKey);
        }
        #endregion
    }
}
