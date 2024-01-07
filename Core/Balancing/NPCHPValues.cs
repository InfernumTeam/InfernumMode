using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs.Abyss;
using CalamityMod.NPCs.PrimordialWyrm;
using CalamityMod.NPCs.AstrumAureus;
using CalamityMod.NPCs.AstrumDeus;
using CalamityMod.NPCs.BrimstoneElemental;
using CalamityMod.NPCs.Bumblebirb;
using CalamityMod.NPCs.CalClone;
using CalamityMod.NPCs.CeaselessVoid;
using CalamityMod.NPCs.DesertScourge;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.NPCs.GreatSandShark;
using CalamityMod.NPCs.Leviathan;
using CalamityMod.NPCs.NormalNPCs;
using CalamityMod.NPCs.Perforator;
using CalamityMod.NPCs.PlaguebringerGoliath;
using CalamityMod.NPCs.Polterghast;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.NPCs.Ravager;
using CalamityMod.NPCs.Signus;
using CalamityMod.NPCs.SlimeGod;
using CalamityMod.NPCs.StormWeaver;
using CalamityMod.NPCs.SunkenSea;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.NPCs.Yharon;
using InfernumMode.Content.BehaviorOverrides.BossAIs.EoW;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Prime;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CrabulonBoss = CalamityMod.NPCs.Crabulon.Crabulon;
using CrimulanSGBig = CalamityMod.NPCs.SlimeGod.CrimulanPaladin;
using EbonianSGBig = CalamityMod.NPCs.SlimeGod.CrimulanPaladin;
using HiveMindP1Boss = CalamityMod.NPCs.HiveMind.HiveMind;
using OldDukeBoss = CalamityMod.NPCs.OldDuke.OldDuke;
using ProvidenceBoss = CalamityMod.NPCs.Providence.Providence;

namespace InfernumMode.Core.Balancing
{
    public class NPCHPValues : ModSystem
    {
        public static Dictionary<int, int> HPValues
        {
            get;
            private set;
        }

        public override void Load()
        {
            HPValues = new()
            {
                [ModContent.NPCType<KingSlimeJewel>()] = BossRushEvent.BossRushActive ? 1176000 : 2000,
                [ModContent.NPCType<DesertScourgeHead>()] = BossRushEvent.BossRushActive ? 1185000 : 7200,
                [ModContent.NPCType<GiantClam>()] = Main.hardMode ? 16200 : 4100,
                [NPCID.KingSlime] = BossRushEvent.BossRushActive ? 420000 : 4200,
                [NPCID.EyeofCthulhu] = BossRushEvent.BossRushActive ? 770000 : 6100,
                [NPCID.BrainofCthulhu] = BossRushEvent.BossRushActive ? 689000 : 9389,
                [ModContent.NPCType<CrabulonBoss>()] = BossRushEvent.BossRushActive ? 1776000 : 10600,
                [NPCID.EaterofWorldsHead] = BossRushEvent.BossRushActive ? EoWHeadBehaviorOverride.TotalLifeAcrossWormBossRush : EoWHeadBehaviorOverride.TotalLifeAcrossWorm,
                [NPCID.EaterofWorldsBody] = BossRushEvent.BossRushActive ? EoWHeadBehaviorOverride.TotalLifeAcrossWormBossRush : EoWHeadBehaviorOverride.TotalLifeAcrossWorm,
                [NPCID.EaterofWorldsTail] = BossRushEvent.BossRushActive ? EoWHeadBehaviorOverride.TotalLifeAcrossWormBossRush : EoWHeadBehaviorOverride.TotalLifeAcrossWorm,
                [NPCID.DD2DarkMageT1] = 5000,
                [ModContent.NPCType<HiveMindP1Boss>()] = BossRushEvent.BossRushActive ? 606007 : 8100,
                [ModContent.NPCType<PerforatorHive>()] = BossRushEvent.BossRushActive ? 420419 : 9176,
                [ModContent.NPCType<PerforatorHeadSmall>()] = BossRushEvent.BossRushActive ? 239000 : 2000,
                [ModContent.NPCType<PerforatorHeadMedium>()] = BossRushEvent.BossRushActive ? 330000 : 2735,
                [ModContent.NPCType<PerforatorHeadLarge>()] = BossRushEvent.BossRushActive ? 296500 : 3960,
                [NPCID.QueenBee] = BossRushEvent.BossRushActive ? 611100 : 9669,
                [NPCID.Deerclops] = BossRushEvent.BossRushActive ? 927000 : 22844,
                [NPCID.SkeletronHead] = BossRushEvent.BossRushActive ? 2508105 : 13860,
                [ModContent.NPCType<SlimeGodCore>()] = BossRushEvent.BossRushActive ? 486500 : 3275,
                [ModContent.NPCType<CrimulanSGBig>()] = BossRushEvent.BossRushActive ? 213720 : 7464,
                [ModContent.NPCType<EbonianSGBig>()] = BossRushEvent.BossRushActive ? 213720 : 7464,
                [NPCID.WallofFleshEye] = BossRushEvent.BossRushActive ? 140800 : 3232,
                [NPCID.WallofFlesh] = BossRushEvent.BossRushActive ? 854000 : 10476,
                [ModContent.NPCType<ThiccWaifu>()] = 18000,
                [NPCID.DD2OgreT2] = 15100,
                [NPCID.QueenSlimeBoss] = BossRushEvent.BossRushActive ? 840000 : 30000,
                [NPCID.Spazmatism] = BossRushEvent.BossRushActive ? 833760 : CalculateMechHP(29950),
                [NPCID.Retinazer] = BossRushEvent.BossRushActive ? 840885 : CalculateMechHP(29950),
                [NPCID.SkeletronPrime] = BossRushEvent.BossRushActive ? 989515 : CalculateMechHP(28000),
                [NPCID.PrimeVice] = BossRushEvent.BossRushActive ? PrimeHeadBehaviorOverride.BaseCollectiveCannonHPBossRush : CalculateMechHP(PrimeHeadBehaviorOverride.BaseCollectiveCannonHP),
                [NPCID.PrimeSaw] = BossRushEvent.BossRushActive ? PrimeHeadBehaviorOverride.BaseCollectiveCannonHPBossRush : CalculateMechHP(PrimeHeadBehaviorOverride.BaseCollectiveCannonHP),
                [NPCID.PrimeCannon] = BossRushEvent.BossRushActive ? PrimeHeadBehaviorOverride.BaseCollectiveCannonHPBossRush : CalculateMechHP(PrimeHeadBehaviorOverride.BaseCollectiveCannonHP),
                [NPCID.PrimeLaser] = BossRushEvent.BossRushActive ? PrimeHeadBehaviorOverride.BaseCollectiveCannonHPBossRush : CalculateMechHP(PrimeHeadBehaviorOverride.BaseCollectiveCannonHP),
                [NPCID.TheDestroyer] = BossRushEvent.BossRushActive ? 1110580 : CalculateMechHP(111000),
                [NPCID.Probe] = BossRushEvent.BossRushActive ? 30000 : CalculateMechHP(700),
                [ModContent.NPCType<BrimstoneElemental>()] = BossRushEvent.BossRushActive ? 1105000 : 85515,
                [ModContent.NPCType<CalamitasClone>()] = BossRushEvent.BossRushActive ? 985000 : 76250,
                [ModContent.NPCType<Cataclysm>()] = BossRushEvent.BossRushActive ? 193380 : 20600,
                [ModContent.NPCType<Catastrophe>()] = BossRushEvent.BossRushActive ? 176085 : 13000,
                [ModContent.NPCType<SoulSeeker>()] = BossRushEvent.BossRushActive ? 24000 : 2100,
                [NPCID.Plantera] = BossRushEvent.BossRushActive ? 575576 : 110500,
                [ModContent.NPCType<Leviathan>()] = BossRushEvent.BossRushActive ? 1200000 : 102097,
                [ModContent.NPCType<AquaticAberration>()] = BossRushEvent.BossRushActive ? -1 : 900,
                [ModContent.NPCType<Anahita>()] = BossRushEvent.BossRushActive ? 450000 : 71000,
                [ModContent.NPCType<AureusSpawn>()] = 25000,
                [ModContent.NPCType<AstrumAureus>()] = BossRushEvent.BossRushActive ? 1230680 : 144074,
                [NPCID.DD2DarkMageT3] = 24500,
                [NPCID.DD2Betsy] = 66500,
                [NPCID.Golem] = BossRushEvent.BossRushActive ? 1250000 : 198700,
                [NPCID.GolemHead] = BossRushEvent.BossRushActive ? 1250000 : 198700,
                [NPCID.GolemHeadFree] = BossRushEvent.BossRushActive ? 1250000 : 198700,
                [NPCID.GolemFistLeft] = BossRushEvent.BossRushActive ? 1250000 : 198700,
                [NPCID.GolemFistRight] = BossRushEvent.BossRushActive ? 1250000 : 198700,
                [ModContent.NPCType<PlaguebringerGoliath>()] = BossRushEvent.BossRushActive ? 666666 : 136031,
                [ModContent.NPCType<GreatSandShark>()] = 107400,
                [NPCID.DukeFishron] = BossRushEvent.BossRushActive ? 1330000 : 100250,
                [NPCID.HallowBoss] = BossRushEvent.BossRushActive ? 2960000 : 220056,
                [ModContent.NPCType<RavagerHead>()] = BossRushEvent.BossRushActive ? -1 : 18000,
                [ModContent.NPCType<DevilFish>()] = 5000,
                [ModContent.NPCType<Eidolist>()] = 20000,
                [NPCID.CultistBoss] = BossRushEvent.BossRushActive ? 727272 : 104000,
                [NPCID.AncientCultistSquidhead] = BossRushEvent.BossRushActive ? -1 : 9020,
                [NPCID.CultistDragonHead] = BossRushEvent.BossRushActive ? -1 : 36500,
                [ModContent.NPCType<AstrumDeusHead>()] = BossRushEvent.BossRushActive ? 930000 : 287000,
                [NPCID.MoonLordHand] = BossRushEvent.BossRushActive ? 400000 : 50000,
                [NPCID.MoonLordHead] = BossRushEvent.BossRushActive ? 661110 : 61000,
                [NPCID.MoonLordCore] = BossRushEvent.BossRushActive ? 1600000 : 135000,
                [ModContent.NPCType<ProfanedGuardianCommander>()] = BossRushEvent.BossRushActive ? 720000 : 132000,
                [ModContent.NPCType<ProfanedGuardianDefender>()] = BossRushEvent.BossRushActive ? 205000 : 80000,
                [ModContent.NPCType<ProfanedGuardianHealer>()] = BossRushEvent.BossRushActive ? 205000 : 80000,
                [ModContent.NPCType<Bumblefuck>()] = BossRushEvent.BossRushActive ? 860000 : 256000,
                [ModContent.NPCType<Bumblefuck2>()] = BossRushEvent.BossRushActive ? -1 : 14300,
                [ModContent.NPCType<ProfanedRocks>()] = BossRushEvent.BossRushActive ? 7500 : 2300,
                [ModContent.NPCType<ProvidenceBoss>()] = BossRushEvent.BossRushActive ? 3900000 : 900000,
                [ModContent.NPCType<StormWeaverHead>()] = BossRushEvent.BossRushActive ? 1232100 : 646400,
                [ModContent.NPCType<CeaselessVoid>()] = BossRushEvent.BossRushActive ? 1040000 : 455525,
                [ModContent.NPCType<DarkEnergy>()] = BossRushEvent.BossRushActive ? 19000 : 5000,
                [ModContent.NPCType<Signus>()] = BossRushEvent.BossRushActive ? 848210 : 546102,
                [ModContent.NPCType<Polterghast>()] = BossRushEvent.BossRushActive ? 1575910 : 544440,
                [ModContent.NPCType<OldDukeBoss>()] = BossRushEvent.BossRushActive ? 1600000 : 936000,
                [ModContent.NPCType<DevourerofGodsHead>()] = BossRushEvent.BossRushActive ? 2960000 : 1776500,
                [ModContent.NPCType<Yharon>()] = BossRushEvent.BossRushActive ? 1618950 : 968420,
                [ModContent.NPCType<PrimordialWyrmHead>()] = 1260750,
                [ModContent.NPCType<ThanatosHead>()] = 2400000,
                [ModContent.NPCType<AresBody>()] = 2560000,
                [ModContent.NPCType<Artemis>()] = 2400000,
                [ModContent.NPCType<Apollo>()] = 2400000,
                [ModContent.NPCType<SupremeCataclysm>()] = 537200,
                [ModContent.NPCType<SupremeCatastrophe>()] = 537200,
                [ModContent.NPCType<SupremeCalamitas>()] = 3141592,
            };
        }

        public static int CalculateMechHP(int baseHP)
        {
            if (CalamityConfig.Instance.EarlyHardmodeProgressionRework && !BossRushEvent.BossRushActive)
            {
                if (!NPC.downedMechBossAny)
                    return (int)(baseHP * 0.8f);

                else if (!NPC.downedMechBoss1 && !NPC.downedMechBoss2 || !NPC.downedMechBoss2 && !NPC.downedMechBoss3 || !NPC.downedMechBoss3 && !NPC.downedMechBoss1)
                    return (int)(baseHP * 0.9f);
            }
            return baseHP;
        }

        public static void AdjustMaxHP(NPC npc, ref int maxHP)
        {
            // Calculate the amount of extra HP the boss should have based on quantity of players in the world.
            float hpMultiplier = 1f;
            float accumulatedFactor = 0.35f;
            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                for (int i = 1; i < (npc.Infernum().TotalPlayersAtStart ?? 1); i++)
                {
                    hpMultiplier += accumulatedFactor * 0.5f;
                    accumulatedFactor += (1f - accumulatedFactor) / 3f;
                }
            }
            if (hpMultiplier > 8f)
                hpMultiplier = (hpMultiplier * 2f + 8f) / 3f;

            if (hpMultiplier > 1000f)
                hpMultiplier = 1000f;

            maxHP = (int)(maxHP * hpMultiplier);

            // Add more to the HP if the config says so.
            maxHP += (int)(maxHP * CalamityConfig.Instance.BossHealthBoost * 0.01);
        }
    }
}
