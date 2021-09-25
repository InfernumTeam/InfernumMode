using CalamityMod.NPCs.DesertScourge;
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
            [ModContent.NPCType<PerforatorHeadLarge>()] = 4055,
            [NPCID.QueenBee] = 7550,
            [NPCID.SkeletronHead] = 5517,
            [ModContent.NPCType<CrimulanSGBig>()] = 4020,
            [ModContent.NPCType<EbonianSGBig>()] = 4020,
            [NPCID.WallofFleshEye] = 3232,
            [NPCID.WallofFlesh] = 11875,
        };
    }
}