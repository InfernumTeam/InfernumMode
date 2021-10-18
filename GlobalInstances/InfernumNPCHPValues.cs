using CalamityMod.NPCs.Bumblebirb;
using CalamityMod.NPCs.DesertScourge;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.Perforator;
using CalamityMod.NPCs.HiveMind;
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
using CalamityMod.NPCs.AstrumAureus;
using CalamityMod.NPCs.StormWeaver;
using CalamityMod.NPCs.BrimstoneElemental;
using CalamityMod.NPCs.Leviathan;
using CalamityMod.NPCs.PlaguebringerGoliath;

namespace InfernumMode.GlobalInstances
{
	public static class InfernumNPCHPValues
    {
        public static Dictionary<int, int> HPValues = new Dictionary<int, int>()
        {
            [ModContent.NPCType<DesertScourgeHead>()] = 5550,
            [NPCID.KingSlime] = 4200,
            [NPCID.EyeofCthulhu] = 3560,
            [NPCID.BrainofCthulhu] = 4545,
            [ModContent.NPCType<CrabulonBoss>()] = 8750,
            [NPCID.EaterofWorldsHead] = EoWHeadBehaviorOverride.TotalLifeAcrossWorm,
            [NPCID.EaterofWorldsBody] = EoWHeadBehaviorOverride.TotalLifeAcrossWorm,
            [NPCID.EaterofWorldsTail] = EoWHeadBehaviorOverride.TotalLifeAcrossWorm,
            [ModContent.NPCType<HiveMindP1Boss>()] = 1312,
            [ModContent.NPCType<HiveMindP2>()] = 4695,
            [ModContent.NPCType<PerforatorHive>()] = 6132,
            [ModContent.NPCType<PerforatorHeadSmall>()] = 2280,
            [ModContent.NPCType<PerforatorBodyMedium>()] = 160,
            [ModContent.NPCType<PerforatorHeadLarge>()] = 4055,
            [NPCID.QueenBee] = 7550,
            [NPCID.SkeletronHead] = 5517,
            [ModContent.NPCType<CrimulanSGBig>()] = 4020,
            [ModContent.NPCType<EbonianSGBig>()] = 4020,
            [NPCID.WallofFleshEye] = 3232,
            [NPCID.WallofFlesh] = 11875,
            [NPCID.Spazmatism] = 22223,
            [NPCID.Retinazer] = 21000,
            [NPCID.SkeletronPrime] = 44444,
            [ModContent.NPCType<BrimstoneElemental>()] = 51515,
            [ModContent.NPCType<Leviathan>()] = 103103,
            [ModContent.NPCType<Siren>()] = 39250,
            [ModContent.NPCType<AureusSpawn>()] = 25000,
            [ModContent.NPCType<PlaguebringerGoliath>()] = 126500,
            [NPCID.CultistBoss] = 56000,
            [ModContent.NPCType<Bumblefuck>()] = 227550,
            [ModContent.NPCType<ProvidenceBoss>()] = 900000,
            [ModContent.NPCType<StormWeaverHeadNaked>()] = 999998,
            [ModContent.NPCType<OldDukeBoss>()] = 872444,
            [ModContent.NPCType<DevourerofGodsHead>()] = 1400000,
            [ModContent.NPCType<DevourerofGodsHeadS>()] = 4180000
        };
    }
}
