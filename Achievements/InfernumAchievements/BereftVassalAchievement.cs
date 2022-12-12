using InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark;
using InfernumMode.Subworlds;
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
            Description = "Best the Bereft Vassal in combat, in the far reaches of the desert's dunes\n[c/777777:Defeat the Bereft Vassal]";
            TotalCompletion = 1;
            PositionInMainList = 2;
        }
        public override void ExtraUpdateNPC(int npcIndex)
        {
            if (Main.npc[npcIndex].type == ModContent.NPCType<BereftVassal>())
                CurrentCompletion++;
        }
        public override void SaveProgress(TagCompound tag)
        {
            tag["BereftVassalCurrentCompletion"] = CurrentCompletion;
            tag["BereftVassalDoneCompletionEffects"] = DoneCompletionEffects;
        }
        public override void LoadProgress(TagCompound tag)
        {
            if (LostColosseum.VassalWasBeaten)
            {
                LostColosseum.VassalWasBeaten = false;
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
