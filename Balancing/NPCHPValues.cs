using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.SlimeGod;
using CalamityMod.NPCs.Bumblebirb;
using CalamityMod.NPCs.DesertScourge;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.Perforator;
using CalamityMod.NPCs.AstrumAureus;
using CalamityMod.NPCs.StormWeaver;
using CalamityMod.NPCs.BrimstoneElemental;
using CalamityMod.NPCs.Leviathan;
using CalamityMod.NPCs.PlaguebringerGoliath;
using CalamityMod.NPCs.Calamitas;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.NPCs.Yharon;
using CalamityMod.World;
using InfernumMode.BehaviorOverrides.BossAIs.CalamitasClone;
using InfernumMode.BehaviorOverrides.BossAIs.EoW;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using CrabulonBoss = CalamityMod.NPCs.Crabulon.CrabulonIdle;
using HiveMindP1Boss = CalamityMod.NPCs.HiveMind.HiveMind;
using CrimulanSGBig = CalamityMod.NPCs.SlimeGod.SlimeGodRun;
using EbonianSGBig = CalamityMod.NPCs.SlimeGod.SlimeGod;
using ProvidenceBoss = CalamityMod.NPCs.Providence.Providence;
using OldDukeBoss = CalamityMod.NPCs.OldDuke.OldDuke;

namespace InfernumMode.Balancing
{
    public static class NPCHPValues
    {
        public static Dictionary<int, int> HPValues => new Dictionary<int, int>()
        {
            [ModContent.NPCType<DesertScourgeHead>()] = BossRushEvent.BossRushActive ? 1185000 : 7200,
            [NPCID.KingSlime] = BossRushEvent.BossRushActive ? 420000 : 4200,
            [NPCID.EyeofCthulhu] = BossRushEvent.BossRushActive ? 770000 : 6100,
            [NPCID.BrainofCthulhu] = BossRushEvent.BossRushActive ? 289000 : 11089,
            [ModContent.NPCType<CrabulonBoss>()] = BossRushEvent.BossRushActive ? 1776000 : 12400,
            [NPCID.EaterofWorldsHead] = BossRushEvent.BossRushActive ? EoWHeadBehaviorOverride.TotalLifeAcrossWormBossRush : EoWHeadBehaviorOverride.TotalLifeAcrossWorm,
            [NPCID.EaterofWorldsBody] = BossRushEvent.BossRushActive ? EoWHeadBehaviorOverride.TotalLifeAcrossWormBossRush : EoWHeadBehaviorOverride.TotalLifeAcrossWorm,
            [NPCID.EaterofWorldsTail] = BossRushEvent.BossRushActive ? EoWHeadBehaviorOverride.TotalLifeAcrossWormBossRush : EoWHeadBehaviorOverride.TotalLifeAcrossWorm,
            [ModContent.NPCType<HiveMindP1Boss>()] = BossRushEvent.BossRushActive ? 606007 : 9600,
            [ModContent.NPCType<PerforatorHive>()] = BossRushEvent.BossRushActive ? 420419 : 7265,
            [ModContent.NPCType<PerforatorHeadSmall>()] = BossRushEvent.BossRushActive ? 119000 : 3000,
            [ModContent.NPCType<PerforatorBodyMedium>()] = BossRushEvent.BossRushActive ? 7675 : 160,
            [ModContent.NPCType<PerforatorHeadLarge>()] = BossRushEvent.BossRushActive ? 174500 : 5500,
            [NPCID.QueenBee] = BossRushEvent.BossRushActive ? 611100 : 9669,
            [NPCID.SkeletronHead] = BossRushEvent.BossRushActive ? 608105 : 17960,
            [ModContent.NPCType<SlimeGodCore>()] = BossRushEvent.BossRushActive ? 486500 : 3275,
            [ModContent.NPCType<CrimulanSGBig>()] = BossRushEvent.BossRushActive ? 213720 : 7464,
            [ModContent.NPCType<EbonianSGBig>()] = BossRushEvent.BossRushActive ? 213720 : 7464,
            [NPCID.WallofFleshEye] = BossRushEvent.BossRushActive ? 246800 : 3232,
            [NPCID.WallofFlesh] = BossRushEvent.BossRushActive ? 1068000 : 10476,
            [NPCID.Spazmatism] = BossRushEvent.BossRushActive ? 833760 : CalculateMechHP(29950),
            [NPCID.Retinazer] = BossRushEvent.BossRushActive ? 840885 : CalculateMechHP(29950),
            [NPCID.SkeletronPrime] = BossRushEvent.BossRushActive ? 289515 : CalculateMechHP(44444),
            [NPCID.PrimeVice] = BossRushEvent.BossRushActive ? -1 : CalculateMechHP(6300),
            [NPCID.PrimeSaw] = BossRushEvent.BossRushActive ? -1 : CalculateMechHP(6300),
            [NPCID.PrimeCannon] = BossRushEvent.BossRushActive ? -1 : CalculateMechHP(5005),
            [NPCID.PrimeLaser] = BossRushEvent.BossRushActive ? -1 : CalculateMechHP(5005),
            [NPCID.TheDestroyer] = BossRushEvent.BossRushActive ? 610580 : CalculateMechHP(111000),
            [NPCID.Probe] = BossRushEvent.BossRushActive ? 15000 : CalculateMechHP(170),
            [ModContent.NPCType<BrimstoneElemental>()] = BossRushEvent.BossRushActive ? 1105000 : 51515,
            [ModContent.NPCType<CalamitasRun3>()] = BossRushEvent.BossRushActive ? 485000 : CalamityWorld.downedProvidence && CalamitasCloneBehaviorOverride.ReadyToUseBuffedAI ? 244444 : 76250,
            [ModContent.NPCType<CalamitasRun>()] = BossRushEvent.BossRushActive ? 193380 : CalamityWorld.downedProvidence && CalamitasCloneBehaviorOverride.ReadyToUseBuffedAI ? 55000 : 20600,
            [ModContent.NPCType<CalamitasRun2>()] = BossRushEvent.BossRushActive ? 176085 : CalamityWorld.downedProvidence && CalamitasCloneBehaviorOverride.ReadyToUseBuffedAI ? 41000 : 13000,
            [ModContent.NPCType<SoulSeeker>()] = BossRushEvent.BossRushActive ? 24000 : 2100,
            [NPCID.Plantera] = BossRushEvent.BossRushActive ? 575576 : 110500,
            [ModContent.NPCType<Leviathan>()] = BossRushEvent.BossRushActive ? 1200000 : 116096,
            [ModContent.NPCType<AquaticAberration>()] = BossRushEvent.BossRushActive ? -1 : 900,
            [ModContent.NPCType<Siren>()] = BossRushEvent.BossRushActive ? 450000 : 67560,
            [ModContent.NPCType<AureusSpawn>()] = 25000,
            [ModContent.NPCType<PlaguebringerGoliath>()] = BossRushEvent.BossRushActive ? 666666 : 111776,
            [NPCID.CultistBoss] = BossRushEvent.BossRushActive ? 727272 : 56000,
            [NPCID.MoonLordHand] = BossRushEvent.BossRushActive ? 275200 : 43390,
            [NPCID.MoonLordHead] = BossRushEvent.BossRushActive ? 281110 : 52525,
            [NPCID.MoonLordCore] = BossRushEvent.BossRushActive ? 510000 : 99990,
            [ModContent.NPCType<ProfanedGuardianBoss>()] = BossRushEvent.BossRushActive ? 620000 : 160000,
            [ModContent.NPCType<ProfanedGuardianBoss2>()] = BossRushEvent.BossRushActive ? 205000 : 72000,
            [ModContent.NPCType<ProfanedGuardianBoss3>()] = BossRushEvent.BossRushActive ? 205000 : 72000,
            [ModContent.NPCType<Bumblefuck>()] = BossRushEvent.BossRushActive ? 860000 : 280000,
            [ModContent.NPCType<ProvidenceBoss>()] = BossRushEvent.BossRushActive ? 2015000 : 520000,
            [ModContent.NPCType<StormWeaverHead>()] = BossRushEvent.BossRushActive ? 632100 : 465432,
            [ModContent.NPCType<OldDukeBoss>()] = BossRushEvent.BossRushActive ? 1600000 : 875000,
            [ModContent.NPCType<DevourerofGodsHead>()] = BossRushEvent.BossRushActive ? 2450000 : 1116000,
            [ModContent.NPCType<Yharon>()] = BossRushEvent.BossRushActive ? 3333330 : 2718280,
            [ModContent.NPCType<ThanatosHead>()] = BossRushEvent.BossRushActive ? 2168000 : 1376160,
            [ModContent.NPCType<AresBody>()] = BossRushEvent.BossRushActive ? 2670000 : -1,
            [ModContent.NPCType<Artemis>()] = 3333330,
            [ModContent.NPCType<Apollo>()] = 3333330,
            [ModContent.NPCType<SupremeCalamitas>()] = BossRushEvent.BossRushActive ? 3456780 : -1,
        };

        public static int CalculateMechHP(int baseHP)
        {
            if (CalamityConfig.Instance.EarlyHardmodeProgressionRework && !BossRushEvent.BossRushActive)
            {
                if (!NPC.downedMechBossAny)
                    return (int)(baseHP * 0.8f);

                else if ((!NPC.downedMechBoss1 && !NPC.downedMechBoss2) || (!NPC.downedMechBoss2 && !NPC.downedMechBoss3) || (!NPC.downedMechBoss3 && !NPC.downedMechBoss1))
                    return (int)(baseHP * 0.9f);
            }
            return baseHP;
        }
    }
}
