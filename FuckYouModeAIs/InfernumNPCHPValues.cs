using CalamityMod.NPCs.Bumblebirb;
using CalamityMod.NPCs.DesertScourge;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.Perforator;
using CalamityMod.NPCs.HiveMind;
using InfernumMode.FuckYouModeAIs.EoW;
using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;

using CrabulonBoss = CalamityMod.NPCs.Crabulon.CrabulonIdle;
using HiveMindP1Boss = CalamityMod.NPCs.HiveMind.HiveMind;
using ProvidenceBoss = CalamityMod.NPCs.Providence.Providence;
using OldDukeBoss = CalamityMod.NPCs.OldDuke.OldDuke;

namespace InfernumMode.FuckYouModeAIs.MainAI
{
	public static class InfernumNPCHPValues
    {
        public static Dictionary<int, int> HPValues = new Dictionary<int, int>()
        {
            [ModContent.NPCType<DesertScourgeHead>()] = 5550,
            [NPCID.KingSlime] = 4200,
            [NPCID.EyeofCthulhu] = 3560,
            [ModContent.NPCType<CrabulonBoss>()] = 8750,
            [NPCID.EaterofWorldsHead] = EoWHeadBehaviorOverride.TotalLifeAcrossWorm,
            [NPCID.EaterofWorldsBody] = EoWHeadBehaviorOverride.TotalLifeAcrossWorm,
            [NPCID.EaterofWorldsTail] = EoWHeadBehaviorOverride.TotalLifeAcrossWorm,
            [ModContent.NPCType<HiveMindP1Boss>()] = 1312,
            [ModContent.NPCType<HiveMindP2>()] = 4695,
            [ModContent.NPCType<PerforatorHive>()] = 6132,
            [NPCID.WallofFleshEye] = 4776,
            [NPCID.Spazmatism] = 36260,
            [NPCID.Retinazer] = 33915,
            [ModContent.NPCType<Bumblefuck>()] = 227550,
            [ModContent.NPCType<ProvidenceBoss>()] = 900000,
            [ModContent.NPCType<OldDukeBoss>()] = 872444,
            [ModContent.NPCType<DevourerofGodsHead>()] = 1400000,
            [ModContent.NPCType<DevourerofGodsHeadS>()] = 4180000
        };
    }
}