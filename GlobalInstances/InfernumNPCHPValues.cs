using CalamityMod.NPCs.Bumblebirb;
using CalamityMod.NPCs.DesertScourge;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.Perforator;
using CalamityMod.NPCs.HiveMind;
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

namespace InfernumMode.GlobalInstances
{
	public static class InfernumNPCHPValues
    {
        public static Dictionary<int, int> HPValues => new Dictionary<int, int>()
        {
            [ModContent.NPCType<DesertScourgeHead>()] = 5550,
            [NPCID.KingSlime] = 4200,
            [NPCID.EyeofCthulhu] = 3560,
            [NPCID.BrainofCthulhu] = 4545,
            [ModContent.NPCType<CrabulonBoss>()] = 8750,
            [NPCID.EaterofWorldsHead] = EoWHeadBehaviorOverride.TotalLifeAcrossWorm,
            [NPCID.EaterofWorldsBody] = EoWHeadBehaviorOverride.TotalLifeAcrossWorm,
            [NPCID.EaterofWorldsTail] = EoWHeadBehaviorOverride.TotalLifeAcrossWorm,
            [ModContent.NPCType<HiveMindP1Boss>()] = 6007,
            [ModContent.NPCType<PerforatorHive>()] = 6132,
            [ModContent.NPCType<PerforatorHeadSmall>()] = 2280,
            [ModContent.NPCType<PerforatorBodyMedium>()] = 160,
            [ModContent.NPCType<PerforatorHeadLarge>()] = 4055,
            [NPCID.QueenBee] = 7550,
            [NPCID.SkeletronHead] = 7600,
            [ModContent.NPCType<CrimulanSGBig>()] = 4020,
            [ModContent.NPCType<EbonianSGBig>()] = 4020,
            [NPCID.WallofFleshEye] = 3232,
            [NPCID.WallofFlesh] = 11875,
            [NPCID.Spazmatism] = 22223,
            [NPCID.Retinazer] = 24500,
            [NPCID.SkeletronPrime] = 53333,
            [NPCID.TheDestroyer] = 111000,
            [ModContent.NPCType<BrimstoneElemental>()] = 51515,
            [ModContent.NPCType<CalamitasRun3>()] = CalamityWorld.downedProvidence && CalamitasCloneBehaviorOverride.ReadyToUseBuffedAI ? 244444 : 82800,
            [ModContent.NPCType<CalamitasRun>()] = CalamityWorld.downedProvidence && CalamitasCloneBehaviorOverride.ReadyToUseBuffedAI ? 55000 : 19500,
            [ModContent.NPCType<CalamitasRun2>()] = CalamityWorld.downedProvidence && CalamitasCloneBehaviorOverride.ReadyToUseBuffedAI ? 41000 : 13000,
            [NPCID.Plantera] = 110500,
            [ModContent.NPCType<Leviathan>()] = 103103,
            [ModContent.NPCType<Siren>()] = 39250,
            [ModContent.NPCType<AureusSpawn>()] = 25000,
            [ModContent.NPCType<PlaguebringerGoliath>()] = 111776,
            [NPCID.CultistBoss] = 56000,
            [NPCID.MoonLordHand] = 43390,
            [NPCID.MoonLordHead] = 52525,
            [NPCID.MoonLordCore] = 99990,
            [ModContent.NPCType<Bumblefuck>()] = 177550,
            [ModContent.NPCType<ProvidenceBoss>()] = 430000,
            [ModContent.NPCType<StormWeaverHead>()] = 515432,
            [ModContent.NPCType<OldDukeBoss>()] = 872444,
            [ModContent.NPCType<DevourerofGodsHead>()] = 1116000
        };
    }
}
