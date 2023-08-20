using InfernumMode.Content.BehaviorOverrides.BossAIs.GreatSandShark;
using InfernumMode.Content.Subworlds;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Content.Achievements.InfernumAchievements
{
    public class BereftVassalAchievement : Achievement
    {
        #region Overrides
        public override void Initialize()
        {
            TotalCompletion = 1;
            PositionInMainList = 2;
            UpdateCheck = AchievementUpdateCheck.NPCKill;
        }

        public override void ExtraUpdate(Player player, int npcIndex)
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
