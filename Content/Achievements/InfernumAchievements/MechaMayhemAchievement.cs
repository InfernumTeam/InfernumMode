using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader.IO;

namespace InfernumMode.Content.Achievements.InfernumAchievements
{
    public class MechaMayhemAchievement : Achievement
    {
        #region Fields
        private bool MayhemIsOccuring;
        private bool MayhemShouldEnd;
        private static List<int> Mechs => new()
        {
            NPCID.TheDestroyer,
            NPCID.SkeletronPrime,
            NPCID.Retinazer,
            NPCID.Spazmatism
        };
        #endregion

        #region Overrides
        public override void Initialize()
        {
            TotalCompletion = 1;
            PositionInMainList = 1;
            UpdateCheck = AchievementUpdateCheck.NPCKill;
        }
        public override void Update()
        {
            if (MayhemShouldEnd)
            {
                MayhemShouldEnd = false;
                MayhemIsOccuring = false;
                return;
            }
            if (MayhemIsOccuring)
            {
                // If none of the mechs are alive.
                if (!NPC.AnyNPCs(NPCID.SkeletronPrime) && !NPC.AnyNPCs(NPCID.TheDestroyer) && !NPC.AnyNPCs(NPCID.Retinazer) && !NPC.AnyNPCs(NPCID.Spazmatism))
                {
                    // Mark the mayhem as ending next frame.
                    MayhemShouldEnd = true;
                }
            }
            else
            {
                // If every mech is alive at once.
                if (NPC.AnyNPCs(NPCID.SkeletronPrime) && NPC.AnyNPCs(NPCID.TheDestroyer) && NPC.AnyNPCs(NPCID.Retinazer) && NPC.AnyNPCs(NPCID.Spazmatism))
                {
                    // Mayhem is happing.
                    MayhemIsOccuring = true;
                }
            }
        }
        public override void ExtraUpdate(Player player, int npcIndex)
        {
            if (!MayhemIsOccuring)
                return;

            int npcID = Main.npc[npcIndex].type;
            if (IsAMech(npcID))
            {
                if (AnyOtherMechsActive(npcID))
                    return;
                else
                {
                    CurrentCompletion++;
                }
            }
        }
        public override void SaveProgress(TagCompound tag)
        {
            tag["MechaMayhemCurrentCompletion"] = CurrentCompletion;
            tag["MechaMayhemCompletionEffects"] = DoneCompletionEffects;
        }
        public override void LoadProgress(TagCompound tag)
        {
            CurrentCompletion = tag.Get<int>("MechaMayhemCurrentCompletion");
            DoneCompletionEffects = tag.Get<bool>("MechaMayhemCompletionEffects");
        }
        #endregion

        #region Methods
        private static bool IsAMech(int npcID) => npcID is NPCID.TheDestroyer or NPCID.SkeletronPrime or NPCID.Retinazer or NPCID.Spazmatism;

        private static bool AnyOtherMechsActive(int npcID)
        {
            foreach (int mechID in Mechs)
            {
                if (mechID != npcID)
                {
                    if (NPC.AnyNPCs(mechID))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        #endregion
    }
}
