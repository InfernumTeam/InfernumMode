using InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark;
using InfernumMode.Subworlds;
using System.Collections.Generic;
    using System.Linq;
    using Terraria;
    using Terraria.ModLoader;
    using Terraria.ModLoader.IO;

namespace InfernumMode.Achievements.InfernumAchievements
{
    public class BereftVassalAchievement : Achievement
    {
        #region Overrides
        public override void Initialize()
        {
            Name = "Forgotten Sands";
            Description = "Best the Bereft Vassal in combat, found in the far reaches of the desert's dunes\n[c/777777:Defeat the Bereft Vassal]";
            TotalCompletion = 1;
            PositionInMainList = 2;
        }
        public override void ExtraUpdateNPC(int npcID)
        {
            if(npcID == ModContent.NPCType<BereftVassal>())
                CurrentCompletion++;
        }
        public override void SaveProgress(TagCompound tag)
        {
            tag["BereftVassalCurrentCompletion"] = CurrentCompletion;
            tag["BereftVassalDoneCompletionEffects"] = DoneCompletionEffects;
        }
        public override void LoadProgress(TagCompound tag)
        {
            if (LostColosseum.VassalWasCompleted)
            {
                LostColosseum.VassalWasCompleted = false;
                CurrentCompletion = 1;
                DoneCompletionEffects = true;
            }
            else
            {
                CurrentCompletion = tag.Get<int>("BereftVassalCurrentCompletion");
                DoneCompletionEffects = tag.Get<bool>("BereftVassalDoneCompletionEffects");
            }
        }
        #endregion
    }
}
