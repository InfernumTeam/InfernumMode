using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using InfernumMode.ILEditingStuff;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Achievements.InfernumAchievements
{
    public class ExoPathAchievement : Achievement
    {
        // 1 ares twin -> than
        // 2 than ares -> twin
        // 3 twin Than -> ares
        #region Fields
        private Dictionary<int, bool> ExoCombosHaveHappened;

        private static int Ares => ModContent.NPCType<AresBody>();
        private static int Artemis => ModContent.NPCType<Artemis>();
        private static int Apollo => ModContent.NPCType<Apollo>();
        private static int Thanatos => ModContent.NPCType<ThanatosHead>();

        public static List<int> ExoMechIDS => new()
        {
            Ares,
            Artemis,
            Apollo,
            Thanatos
        };

        private const int AresIndex = 0;
        private const int TwinsIndex = 1;
        private const int ThanatosIndex = 2;
        #endregion

        #region Overrides
        public override void Initialize()
        {
            Name = "Lab Rat";
            Description = "Become Draedon's favorite test subject\n[c/777777:Beat all Infernum Exo Mech combinations]";
            TotalCompletion = 3;
            PositionInMainList = 5;
            CreateDict();
        }
        public override void Update()
        {
            int currentCompletion = 0;
            for (int i = 0; i < ExoCombosHaveHappened.Count; i++)
            {
                if (ExoCombosHaveHappened[i])
                {
                    currentCompletion++;
                }
            }
            CurrentCompletion = currentCompletion;
        }
        public override void ExtraUpdateNPC(int npcID)
        {
            // If not an exo mech, leave.
            if (npcID != Ares && npcID != Apollo && npcID != Artemis && npcID != Thanatos)
                return;
            // If there arent any exo mechs alive.
            if (!AnyOtherExosAlive(npcID))
            {
                // Set it to true.
                if (npcID == Ares)
                    ExoCombosHaveHappened[AresIndex] = true;
                else if (npcID == Artemis || npcID == Apollo)
                    ExoCombosHaveHappened[TwinsIndex] = true;
                else if (npcID == Thanatos)
                    ExoCombosHaveHappened[ThanatosIndex] = true;
            }
        }
        public override void LoadProgress(TagCompound tag)
        {
            if (!tag.ContainsKey("ExosDictInt") || !tag.ContainsKey("ExosDictBool"))
                CreateDict();
            else
            {
                List<int> keys = tag.Get<List<int>>("ExosDictInt");
                List<bool> values = tag.Get<List<bool>>("ExosDictBool");
                ExoCombosHaveHappened = keys.Zip(values, (int k, bool v) => new
                {
                    Key = k,
                    Value = v
                }).ToDictionary(k => k.Key, v => v.Value);
            }
            CurrentCompletion = tag.Get<int>("ExosCurrentCompletion");
            DoneCompletionEffects = tag.Get<bool>("ExosDoneCompletionEffects");
        }
        public override void SaveProgress(TagCompound tag)
        {
            tag["ExosDictInt"] = ExoCombosHaveHappened.Keys.ToList();
            tag["ExosDictBool"] = ExoCombosHaveHappened.Values.ToList();
            tag["ExosCurrentCompletion"] = CurrentCompletion;
            tag["ExosDoneCompletionEffects"] = DoneCompletionEffects;
        }
        #endregion

        #region Methods
        private void CreateDict()
        {
            ExoCombosHaveHappened = new();
            for (int i = 0; i < TotalCompletion; i++)
            {
                ExoCombosHaveHappened[i] = false;
            }
            CurrentCompletion = 0;
            DoneCompletionEffects = false;
        }
        private static bool AnyOtherExosAlive(int npcID)
        {
            foreach (var type in ExoMechIDS)
            {
                if(type != npcID)
                {
                    // If another is alive, and they both aren't exo twins.
                    if (NPC.AnyNPCs(type) && (!IsExoTwin(npcID) && !IsExoTwin(type)))
                        return true;
                }
            }
            return false;
        }
        private static bool IsExoTwin(int npcID)
        {
            return npcID == Artemis || npcID == Apollo;
        }

        #endregion
    }
    public enum FirstExoCombos
    {
        AresTwins,
        TwinsAres,
        AresThanatos,
        ThanatosAres,
        TwinsThanatos,
        ThanatosTwins
    }
}
