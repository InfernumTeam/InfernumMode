using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader.IO;

namespace InfernumMode.Achievements
{
    public abstract class Achievement
    {
        #region Fields
        public string Name = "Name";
        public string Description = "Description";
        public int TotalCompletion = 1;
        public int CurrentCompletion;
        internal int PositionInMainList;
        public bool DoneCompletionEffects = false;
        #endregion

        #region Pointers
        public float CompletionRatio => CurrentCompletion / (float)TotalCompletion;
        public bool IsCompleted => CurrentCompletion >= TotalCompletion;
        #endregion

        #region Virtual Methods
        /// <summary>
        /// What should happen once the achievement is completed.
        /// </summary>
        public virtual void OnCompletion()
        {
            AchievementCompletionAnimationManager.AchievementsToShow.Add(this);
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
        /// Load the completion amount and DoneCompletionEffects here. Called from OnWorldLoad(TagCompound tag).
        /// </summary>
        /// <param name="tag"></param>
        public virtual void LoadProgress(TagCompound tag)
        {

        }
        /// <summary>
        /// Save the completion amount and DoneCompletionEffects here. Called from OnWorldSave(TagCompound tag).
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
        /// An ExtraUpdate that takes in a npc type.
        /// </summary>
        /// <param name="npcID"></param>
        public virtual void ExtraUpdateNPC(int npcID)
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
