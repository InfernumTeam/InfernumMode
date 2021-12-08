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
using CalamityMod.World;
using InfernumMode.BehaviorOverrides.BossAIs.CalamitasClone;
using InfernumMode.BehaviorOverrides.BossAIs.EoW;
using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;

using CrabulonBoss = CalamityMod.NPCs.Crabulon.CrabulonIdle;
using HiveMindP1Boss = CalamityMod.NPCs.HiveMind.HiveMind;
using CrimulanSGBig = CalamityMod.NPCs.SlimeGod.SlimeGodRun;
using EbonianSGBig = CalamityMod.NPCs.SlimeGod.SlimeGod;
using ProvidenceBoss = CalamityMod.NPCs.Providence.Providence;
using OldDukeBoss = CalamityMod.NPCs.OldDuke.OldDuke;
using CalamityMod.Events;
using CalamityMod.NPCs.Yharon;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.SlimeGod;

namespace InfernumMode.GlobalInstances
{
    public static class InfernumNPCHPValues
    {
        public static Dictionary<int, int> HPValues => new Dictionary<int, int>()
        {
            [ModContent.NPCType<DesertScourgeHead>()] = BossRushEvent.BossRushActive ? 1185000 : 5200,
            [NPCID.KingSlime] = BossRushEvent.BossRushActive ? 499920 : 3370,
            [NPCID.EyeofCthulhu] = BossRushEvent.BossRushActive ? 770000 : 3560,
            [NPCID.BrainofCthulhu] = BossRushEvent.BossRushActive ? 289000 : 5420,
            [ModContent.NPCType<CrabulonBoss>()] = BossRushEvent.BossRushActive ? 1776000 : 8750,
            [NPCID.EaterofWorldsHead] = BossRushEvent.BossRushActive ? EoWHeadBehaviorOverride.TotalLifeAcrossWormBossRush : EoWHeadBehaviorOverride.TotalLifeAcrossWorm,
            [NPCID.EaterofWorldsBody] = BossRushEvent.BossRushActive ? EoWHeadBehaviorOverride.TotalLifeAcrossWormBossRush : EoWHeadBehaviorOverride.TotalLifeAcrossWorm,
            [NPCID.EaterofWorldsTail] = BossRushEvent.BossRushActive ? EoWHeadBehaviorOverride.TotalLifeAcrossWormBossRush : EoWHeadBehaviorOverride.TotalLifeAcrossWorm,
            [ModContent.NPCType<HiveMindP1Boss>()] = BossRushEvent.BossRushActive ? 606007 : 8000,
            [ModContent.NPCType<PerforatorHive>()] = BossRushEvent.BossRushActive ? 420419 : 6645,
            [ModContent.NPCType<PerforatorHeadSmall>()] = BossRushEvent.BossRushActive ? 119000 : 2720,
            [ModContent.NPCType<PerforatorBodyMedium>()] = BossRushEvent.BossRushActive ? 7675 : 175,
            [ModContent.NPCType<PerforatorHeadLarge>()] = BossRushEvent.BossRushActive ? 174500 : 5500,
            [NPCID.QueenBee] = BossRushEvent.BossRushActive ? 511100 : 6969,
            [NPCID.SkeletronHead] = BossRushEvent.BossRushActive ? 418105 : 8880,
            [ModContent.NPCType<SlimeGodCore>()] = BossRushEvent.BossRushActive ? 486500 : 2730,
            [ModContent.NPCType<CrimulanSGBig>()] = BossRushEvent.BossRushActive ? 213720 : 6220,
            [ModContent.NPCType<EbonianSGBig>()] = BossRushEvent.BossRushActive ? 213720 : 6220,
            [NPCID.WallofFleshEye] = BossRushEvent.BossRushActive ? 246800 : 3232,
            [NPCID.WallofFlesh] = BossRushEvent.BossRushActive ? 1068000 : 11875,
            [NPCID.Spazmatism] = BossRushEvent.BossRushActive ? 833760 : 22223,
            [NPCID.Retinazer] = BossRushEvent.BossRushActive ? 840885 : 24500,
            [NPCID.SkeletronPrime] = BossRushEvent.BossRushActive ? 289515 : 53333,
            [NPCID.TheDestroyer] = BossRushEvent.BossRushActive ? 610580 : 111000,
            [ModContent.NPCType<BrimstoneElemental>()] = BossRushEvent.BossRushActive ? 1105000 : 51515,
            [ModContent.NPCType<CalamitasRun3>()] = BossRushEvent.BossRushActive ? 485000 : CalamityWorld.downedProvidence && CalamitasCloneBehaviorOverride.ReadyToUseBuffedAI ? 244444 : 82800,
            [ModContent.NPCType<CalamitasRun>()] = BossRushEvent.BossRushActive ? 193380 : CalamityWorld.downedProvidence && CalamitasCloneBehaviorOverride.ReadyToUseBuffedAI ? 55000 : 19500,
            [ModContent.NPCType<CalamitasRun2>()] = BossRushEvent.BossRushActive ? 176085 : CalamityWorld.downedProvidence && CalamitasCloneBehaviorOverride.ReadyToUseBuffedAI ? 41000 : 13000,
            [NPCID.Plantera] = BossRushEvent.BossRushActive ? 575576 : 110500,
            [ModContent.NPCType<Leviathan>()] = BossRushEvent.BossRushActive ? 1200000 : 103103,
            [ModContent.NPCType<Siren>()] = BossRushEvent.BossRushActive ? 400000 : 39250,
            [ModContent.NPCType<AureusSpawn>()] = 25000,
            [ModContent.NPCType<PlaguebringerGoliath>()] = BossRushEvent.BossRushActive ? 666666 : 111776,
            [NPCID.CultistBoss] = BossRushEvent.BossRushActive ? 727272 : 56000,
            [NPCID.MoonLordHand] = BossRushEvent.BossRushActive ? 275200 : 43390,
            [NPCID.MoonLordHead] = BossRushEvent.BossRushActive ? 281110 : 52525,
            [NPCID.MoonLordCore] = BossRushEvent.BossRushActive ? 510000 : 99990,
            [ModContent.NPCType<Bumblefuck>()] = BossRushEvent.BossRushActive ? 560000 : 177550,
            [ModContent.NPCType<ProvidenceBoss>()] = BossRushEvent.BossRushActive ? 2015000 : 520000,
            [ModContent.NPCType<StormWeaverHead>()] = BossRushEvent.BossRushActive ? 632100 : 465432,
            [ModContent.NPCType<OldDukeBoss>()] = BossRushEvent.BossRushActive ? 686868 : 542444,
            [ModContent.NPCType<DevourerofGodsHead>()] = BossRushEvent.BossRushActive ? 2450000 : 1116000,
            [ModContent.NPCType<Yharon>()] = BossRushEvent.BossRushActive ? 3333330 : 2718280,
            [ModContent.NPCType<Artemis>()] = BossRushEvent.BossRushActive ? 2050000 : -1,
            [ModContent.NPCType<Apollo>()] = BossRushEvent.BossRushActive ? 2050000 : -1,
            [ModContent.NPCType<ThanatosHead>()] = BossRushEvent.BossRushActive ? 2168000 : 1376160,
            [ModContent.NPCType<AresBody>()] = BossRushEvent.BossRushActive ? 2110000 : -1,
            [ModContent.NPCType<SupremeCalamitas>()] = BossRushEvent.BossRushActive ? 3456780 : -1,
        };
    }
}
