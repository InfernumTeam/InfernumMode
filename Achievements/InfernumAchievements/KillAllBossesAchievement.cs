using CalamityMod.NPCs.AquaticScourge;
using CalamityMod.NPCs.AstrumAureus;
using CalamityMod.NPCs.AstrumDeus;
using CalamityMod.NPCs.BrimstoneElemental;
using CalamityMod.NPCs.Bumblebirb;
using CalamityMod.NPCs.Calamitas;
using CalamityMod.NPCs.CeaselessVoid;
using CalamityMod.NPCs.Crabulon;
using CalamityMod.NPCs.Cryogen;
using CalamityMod.NPCs.DesertScourge;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.NPCs.HiveMind;
using CalamityMod.NPCs.Leviathan;
using CalamityMod.NPCs.OldDuke;
using CalamityMod.NPCs.Perforator;
using CalamityMod.NPCs.PlaguebringerGoliath;
using CalamityMod.NPCs.Polterghast;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.NPCs.Providence;
using CalamityMod.NPCs.Ravager;
using CalamityMod.NPCs.Signus;
using CalamityMod.NPCs.SlimeGod;
using CalamityMod.NPCs.StormWeaver;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.NPCs.Yharon;
using InfernumMode.BehaviorOverrides.BossAIs.GreatSandShark;
using Terraria;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace InfernumMode.Achievements.InfernumAchievements
{
    public class KillAllBossesAchievement : Achievement
    {
        private Dictionary<int, bool> BossesCompleted;

        private void CreateNewDict()
        {
            BossesCompleted = new Dictionary<int, bool>();
            for (int i = 0; i < TotalCompletion; i++)
            {
                BossesCompleted[i] = false;
            }
            CurrentCompletion = 0;
            DoneCompletionEffects = false;
        }

        private static List<int> ExoMechIDs => new()
        {
            ModContent.NPCType<Artemis>(),
            ModContent.NPCType<Apollo>(),
            ModContent.NPCType<AresBody>(),
            ModContent.NPCType<ThanatosHead>()
        };

        private static bool AnyOtherExoMechs(int idToIgnore)
        {
            foreach (int exo in ExoMechIDs)
            {
                if (NPC.AnyNPCs(exo) && exo != idToIgnore)
                {
                    return true;
                }
            }
            return false;
        }

        #region Overrides
        public override void Initialize()
        {
            Name = "Infer-it-all!";
            Description = "Rip and tear, until it is done\n[c/777777:Beat every Infernum Boss]";
            TotalCompletion = 46;
            PositionInMainList = 8;
            CreateNewDict();
        }
        public override void Update()
        {
            int currentCompletion = 0;
            for(int i = 0; i < BossesCompleted.Count; i++)
            {
                if (BossesCompleted[i])
                {
                    currentCompletion++;
                }
            }
            CurrentCompletion = currentCompletion;
        }
        public override void SaveProgress(TagCompound tag)
        {
            tag["BossesDictInt"] = BossesCompleted.Keys.ToList();
            tag["BossesDictBool"] = BossesCompleted.Values.ToList();
            tag["BossesCurrentCompletion"] = CurrentCompletion;
            tag["BossesDoneCompletionEffects"] = DoneCompletionEffects;
        }
        public override void LoadProgress(TagCompound tag)
        {
            if (!tag.ContainsKey("BossesDictInt") || !tag.ContainsKey("BossesDictBool"))
                CreateNewDict();
            else
            {
                List<int> keys = tag.Get<List<int>>("BossesDictInt");
                List<bool> values = tag.Get<List<bool>>("BossesDictBool");
                BossesCompleted = keys.Zip(values, (int k, bool v) => new
                {
                    Key = k,
                    Value = v
                }).ToDictionary(k => k.Key, v => v.Value);
            }
            CurrentCompletion = tag.Get<int>("BossesCurrentCompletion");
            DoneCompletionEffects = tag.Get<bool>("BossesDoneCompletionEffects");
        }

        public override void ExtraUpdateNPC(int npcIndex)
        {
            bool breakOut = false;

            // The way these are done, all the Calamity + vanilla bosses go first, and any extra ones infernum add are at the end.
            // Vanilla ones
            int npcID = Main.npc[npcIndex].type;
            switch (npcID)
            {
                case NPCID.KingSlime:
                    BossesCompleted[0] = true;
                    breakOut = true;
                    break;
                case NPCID.EyeofCthulhu:
                    BossesCompleted[2] = true;
                    breakOut = true;
                    break;
                case NPCID.EaterofWorldsHead:
                    BossesCompleted[4] = true;
                    breakOut = true;
                    break;
                case NPCID.BrainofCthulhu:
                    BossesCompleted[5] = true;
                    breakOut = true;
                    break;
                case NPCID.QueenBee:
                    BossesCompleted[8] = true;
                    breakOut = true;
                    break;
                case NPCID.Deerclops:
                    BossesCompleted[9] = true;
                    breakOut = true;
                    break;
                case NPCID.SkeletronHead:
                    BossesCompleted[10] = true;
                    breakOut = true;
                    break;
                case NPCID.WallofFlesh:
                    BossesCompleted[12] = true;
                    breakOut = true;
                    break;
                case NPCID.QueenSlimeBoss:
                    BossesCompleted[13] = true;
                    breakOut = true;
                    break;
                case NPCID.Retinazer:
                    // Only count this if the other boss(es) arent alive.
                    if (!NPC.AnyNPCs(NPCID.Spazmatism))
                    {
                        BossesCompleted[15] = true;
                        breakOut = true;
                    }
                    break;
                case NPCID.Spazmatism:
                    if (!NPC.AnyNPCs(NPCID.Retinazer))
                    {
                        BossesCompleted[15] = true;
                        breakOut = true;
                    }
                    break;
                case NPCID.TheDestroyer:
                    BossesCompleted[17] = true;
                    breakOut = true;
                    break;
                case NPCID.SkeletronPrime:
                    BossesCompleted[19] = true;
                    breakOut = true;
                    break;
                case NPCID.Plantera:
                    BossesCompleted[21] = true;
                    breakOut = true;
                    break;
                case NPCID.Golem:
                    BossesCompleted[24] = true;
                    breakOut = true;
                    break;
                case NPCID.HallowBoss:
                    BossesCompleted[25] = true;
                    breakOut = true;
                    break;
                case NPCID.DukeFishron:
                    BossesCompleted[27] = true;
                    breakOut = true;
                    break;
                case NPCID.CultistBoss:
                    BossesCompleted[29] = true;
                    breakOut = true;
                    break;
                case NPCID.MoonLordCore:
                    BossesCompleted[31] = true;
                    breakOut = true;
                    break;
                case NPCID.BloodNautilus:
                    BossesCompleted[45] = true;
                    breakOut = true;
                    break;
            }

            // If it was set in the switch case, leave.
            if (breakOut)
                return;

            // Modded ones
            if (npcID == ModContent.NPCType<DesertScourgeHead>())
                BossesCompleted[1] = true;
            else if (npcID == ModContent.NPCType<Crabulon>())
                BossesCompleted[3] = true;
            else if (npcID == ModContent.NPCType<HiveMind>())
                BossesCompleted[6] = true;
            else if (npcID == ModContent.NPCType<PerforatorHive>())
                BossesCompleted[7] = true;
            else if (npcID == ModContent.NPCType<SlimeGodCore>())
                BossesCompleted[11] = true;
            else if (npcID == ModContent.NPCType<Cryogen>())
                BossesCompleted[14] = true;
            else if (npcID == ModContent.NPCType<BrimstoneElemental>())
                BossesCompleted[16] = true;
            else if (npcID == ModContent.NPCType<AquaticScourgeHead>())
                BossesCompleted[18] = true;
            else if (npcID == ModContent.NPCType<CalamitasClone>())
                BossesCompleted[20] = true;
            else if (npcID == ModContent.NPCType<Leviathan>())
            {
                if (!NPC.AnyNPCs(ModContent.NPCType<Anahita>()))
                    BossesCompleted[22] = true;
            }
            else if (npcID == ModContent.NPCType<Anahita>())
            {
                if (!NPC.AnyNPCs(ModContent.NPCType<Leviathan>()))
                    BossesCompleted[22] = true;
            }
            else if (npcID == ModContent.NPCType<AstrumAureus>())
                BossesCompleted[23] = true;
            else if (npcID == ModContent.NPCType<PlaguebringerGoliath>())
                BossesCompleted[26] = true;
            else if (npcID == ModContent.NPCType<RavagerBody>())
                BossesCompleted[28] = true;
            else if (npcID == ModContent.NPCType<AstrumDeusHead>())
                BossesCompleted[30] = true;
            else if (npcID == ModContent.NPCType<ProfanedGuardianCommander>())
                BossesCompleted[32] = true;
            else if (npcID == ModContent.NPCType<Bumblefuck>())
                BossesCompleted[33] = true;
            else if (npcID == ModContent.NPCType<Providence>())
                BossesCompleted[34] = true;
            else if (npcID == ModContent.NPCType<CeaselessVoid>())
                BossesCompleted[35] = true;
            else if (npcID == ModContent.NPCType<StormWeaverHead>())
                BossesCompleted[36] = true;
            else if (npcID == ModContent.NPCType<Signus>())
                BossesCompleted[37] = true;
            else if (npcID == ModContent.NPCType<Polterghast>())
                BossesCompleted[38] = true;
            else if (npcID == ModContent.NPCType<OldDuke>())
                BossesCompleted[39] = true;
            else if (npcID == ModContent.NPCType<DevourerofGodsHead>())
                BossesCompleted[40] = true;
            else if (npcID == ModContent.NPCType<Yharon>())
                BossesCompleted[41] = true;
            else if (npcID == ModContent.NPCType<Artemis>())
            {
                if (!AnyOtherExoMechs(ModContent.NPCType<Artemis>()))
                {
                    BossesCompleted[42] = true;
                }
            }
            else if (npcID == ModContent.NPCType<Apollo>())
            {
                if (!AnyOtherExoMechs(ModContent.NPCType<Apollo>()))
                {
                    BossesCompleted[42] = true;
                }
            }
            else if (npcID == ModContent.NPCType<AresBody>())
            {
                if (!AnyOtherExoMechs(ModContent.NPCType<AresBody>()))
                {
                    BossesCompleted[42] = true;
                }
            }
            else if (npcID == ModContent.NPCType<ThanatosHead>())
            {
                if (!AnyOtherExoMechs(ModContent.NPCType<ThanatosHead>()))
                {
                    BossesCompleted[42] = true;
                }
            }
            else if (npcID == ModContent.NPCType<SupremeCalamitas>())
                BossesCompleted[43] = true;
            else if (npcID == ModContent.NPCType<BereftVassal>())
                BossesCompleted[44] = true;
        }
        #endregion
    }
}
