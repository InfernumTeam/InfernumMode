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
using CalamityMod.NPCs.ExoMechs;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon;
using CalamityMod.Events;

namespace InfernumMode.Achievements.InfernumAchievements
{
    public class KillAllBossesAchievement : Achievement
    {
        private Dictionary<string, bool> BossesCompleted;

        public static List<int> BossList => new()
        {
            NPCID.KingSlime,
            ModContent.NPCType<DesertScourgeHead>(),
            NPCID.EyeofCthulhu,
            ModContent.NPCType<Crabulon>(),
            NPCID.EaterofWorldsHead,
            NPCID.BrainofCthulhu,
            ModContent.NPCType<HiveMind>(),
            ModContent.NPCType<PerforatorHive>(),
            NPCID.QueenBee,
            NPCID.Deerclops,
            NPCID.SkeletronHead,
            ModContent.NPCType<SlimeGodCore>(),
            NPCID.WallofFlesh,
            NPCID.BloodNautilus,
            NPCID.QueenSlimeBoss,
            ModContent.NPCType<Cryogen>(),
            NPCID.Spazmatism,
            ModContent.NPCType<BrimstoneElemental>(),
            NPCID.TheDestroyer,
            ModContent.NPCType<AquaticScourgeHead>(),
            NPCID.SkeletronPrime,
            ModContent.NPCType<CalamitasClone>(),
            NPCID.Plantera,
            ModContent.NPCType<Leviathan>(),
            ModContent.NPCType<AstrumAureus>(),
            NPCID.Golem,
            ModContent.NPCType<PlaguebringerGoliath>(),
            NPCID.HallowBoss,
            NPCID.DukeFishron,
            ModContent.NPCType<RavagerBody>(),
            NPCID.CultistBoss,
            ModContent.NPCType<AstrumDeusHead>(),
            ModContent.NPCType<BereftVassal>(),
            NPCID.MoonLordCore,
            ModContent.NPCType<ProfanedGuardianCommander>(),
            ModContent.NPCType<Bumblefuck>(),
            ModContent.NPCType<Providence>(),
            ModContent.NPCType<CeaselessVoid>(),
            ModContent.NPCType<StormWeaverHead>(),
            ModContent.NPCType<Signus>(),
            ModContent.NPCType<Polterghast>(),
            ModContent.NPCType<OldDuke>(),
            ModContent.NPCType<DevourerofGodsHead>(),
            ModContent.NPCType<Yharon>(),
            ModContent.NPCType<Draedon>(),
            ModContent.NPCType<SupremeCalamitas>(),
        };

        private void CreateNewDict()
        {
            BossesCompleted = new Dictionary<string, bool>();
            foreach (int bossID in BossList)
                BossesCompleted[Utilities.GetNPCNameFromID(bossID)] = false;

            CurrentCompletion = 0;
            DoneCompletionEffects = false;
        }

        #region Overrides
        public override void Initialize()
        {
            Name = "Infer-it-all!";
            Description = "Rip and tear, until it is done\n[c/777777:Beat every Infernum Boss]";
            TotalCompletion = BossList.Count;
            PositionInMainList = 8;
            CreateNewDict();
        }
        public override void Update()
        {
            CurrentCompletion = BossesCompleted.Count(kv => kv.Value);
        }

        public override void SaveProgress(TagCompound tag)
        {
            tag["BossesDictNames"] = BossesCompleted.Keys.ToList();
            tag["BossesDictBool"] = BossesCompleted.Values.ToList();
            tag["BossesCurrentCompletion"] = CurrentCompletion;
            tag["BossesDoneCompletionEffects"] = DoneCompletionEffects;
        }
        
        public override void LoadProgress(TagCompound tag)
        {
            if (!tag.ContainsKey("BossesDictNames") || !tag.ContainsKey("BossesDictBool"))
                CreateNewDict();
            else
            {
                IList<string> keys = tag.GetList<string>("BossesDictNames");
                IList<bool> values = tag.GetList<bool>("BossesDictBool");
                BossesCompleted = keys.Zip(values, (string k, bool v) => new
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
            // Don't count Boss Rush kills.
            if (BossRushEvent.BossRushActive)
                return;

            int npcID = Main.npc[npcIndex].type;
            switch (npcID)
            {
                case NPCID.Retinazer:
                case NPCID.Spazmatism:
                    // Only count this if the other boss(es) arent alive.
                    if (NPC.CountNPCS(NPCID.Spazmatism) + NPC.CountNPCS(NPCID.Retinazer) <= 1)
                        BossesCompleted[Utilities.GetNPCNameFromID(NPCID.Spazmatism)] = true;
                    break;
                default:
                    int leviathanID = ModContent.NPCType<Leviathan>();
                    int draedonID = ModContent.NPCType<Draedon>();
                    if (npcID == leviathanID)
                    {
                        if (!NPC.AnyNPCs(ModContent.NPCType<Anahita>()))
                            BossesCompleted[Utilities.GetNPCNameFromID(leviathanID)] = true;
                    }
                    else if (npcID == ModContent.NPCType<Anahita>())
                    {
                        if (!NPC.AnyNPCs(leviathanID))
                            BossesCompleted[Utilities.GetNPCNameFromID(leviathanID)] = true;
                    }
                    else if (ExoMechManagement.ExoMechIDs.Contains(npcID))
                    {
                        if (ExoMechManagement.TotalMechs <= 1)
                            BossesCompleted[Utilities.GetNPCNameFromID(draedonID)] = true;
                    }
                    else if (BossList.Contains(npcID))
                        BossesCompleted[Utilities.GetNPCNameFromID(npcID)] = true;
                    break;
            }
        }
        #endregion
    }
}
