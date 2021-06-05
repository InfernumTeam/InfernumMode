using CalamityMod.NPCs.Bumblebirb;
using CalamityMod.NPCs.DesertScourge;
using CalamityMod.NPCs.DevourerofGods;
using InfernumMode.FuckYouModeAIs.EoW;
using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;

using CrabulonBoss = CalamityMod.NPCs.Crabulon.CrabulonIdle;
using ProvidenceBoss = CalamityMod.NPCs.Providence.Providence;
using OldDukeBoss = CalamityMod.NPCs.OldDuke.OldDuke;

namespace InfernumMode.FuckYouModeAIs.MainAI
{
	public static class InfernumNPCHPValues
    {
        public static Dictionary<int, int> HPValues = new Dictionary<int, int>()
        {
            [ModContent.NPCType<DesertScourgeHead>()] = 5550,
            [NPCID.EyeofCthulhu] = 3560,
            [ModContent.NPCType<CrabulonBoss>()] = 8750,
            [NPCID.EaterofWorldsHead] = EoWAIClass.TotalLifeAcrossWorm,
            [NPCID.EaterofWorldsBody] = EoWAIClass.TotalLifeAcrossWorm,
            [NPCID.EaterofWorldsTail] = EoWAIClass.TotalLifeAcrossWorm,
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