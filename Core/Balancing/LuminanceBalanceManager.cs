using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs.Abyss;
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
using CalamityMod.NPCs.PrimordialWyrm;
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
using Luminance.Core.Balancing;
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
    public class LuminanceBalanceManager : BalancingManager
    {
        public const BalancePriority InfernumModeBasePriority = BalancePriority.VeryHigh;

        public const BalancePriority InfernumModeHigherPriority = (BalancePriority)9;

        // Here at the InfernumMod Team (just me), we LOVE boss rush!
        public static readonly Func<bool> InfernumModeCondition = () => InfernumMode.CanUseCustomAIs && !BossRushEvent.BossRushActive;

        public static readonly Func<bool> InfernumModeBossRushCondition = () => InfernumMode.CanUseCustomAIs && BossRushEvent.BossRushActive;

        public static readonly Func<bool> InfernumHardmodeCondition = () => InfernumMode.CanUseCustomAIs && Main.hardMode && !BossRushEvent.BossRushActive;

        // This is a lot, I do not enjoy the fact calamity does this.
        public static Func<bool> InfernumFirstMechCondition => () => InfernumMode.CanUseCustomAIs && GetMechsDowned() == 0 && CalamityConfig.Instance.EarlyHardmodeProgressionRework && !BossRushEvent.BossRushActive;

        public static Func<bool> InfernumSecondMechCondition => () => InfernumMode.CanUseCustomAIs && GetMechsDowned() == 1 && CalamityConfig.Instance.EarlyHardmodeProgressionRework && !BossRushEvent.BossRushActive;

        public static Func<bool> InfernumFinalMechCondition => () => InfernumMode.CanUseCustomAIs && GetMechsDowned() >= 2 && !InfernumFirstMechCondition() && !BossRushEvent.BossRushActive;

        private static int GetMechsDowned()
        {
            int value = 0;
            if (NPC.downedMechBoss1)
                value++;
            if (NPC.downedMechBoss2)
                value++;
            if (NPC.downedMechBoss3)
                value++;
            return value;
        }

        private static int AccountForExpertHP(int hp) => (int)(hp - (hp * 0.375f));

        private static NPCHPBalancingChange CreateBaseChange(int npcType, int hp) => new(npcType, AccountForExpertHP(hp), InfernumModeBasePriority, InfernumModeCondition);

        private static NPCHPBalancingChange CreateBossRushChange(int npcType, int hp) => new(npcType, AccountForExpertHP(hp), InfernumModeBasePriority, InfernumModeBossRushCondition);

        public override IEnumerable<NPCHPBalancingChange> GetNPCHPBalancingChanges()
        {
            #region Base Infernum HP
            yield return CreateBaseChange(ModContent.NPCType<KingSlimeJewel>(), 2000);
            yield return CreateBaseChange(ModContent.NPCType<DesertScourgeHead>(), 7200);
            yield return new(ModContent.NPCType<GiantClam>(), AccountForExpertHP(4100), InfernumModeBasePriority, () => InfernumMode.CanUseCustomAIs && !Main.hardMode);
            yield return new(ModContent.NPCType<GiantClam>(), AccountForExpertHP(16200), InfernumModeBasePriority, () => InfernumMode.CanUseCustomAIs && Main.hardMode);
            yield return CreateBaseChange(NPCID.KingSlime, 4200);
            yield return CreateBaseChange(NPCID.EyeofCthulhu, 6100);
            yield return CreateBaseChange(NPCID.BrainofCthulhu, 9389);
            yield return CreateBaseChange(ModContent.NPCType<CrabulonBoss>(), 10600);
            yield return CreateBaseChange(NPCID.EaterofWorldsHead, EoWHeadBehaviorOverride.TotalLifeAcrossWorm);
            yield return CreateBaseChange(NPCID.EaterofWorldsBody, EoWHeadBehaviorOverride.TotalLifeAcrossWorm);
            yield return CreateBaseChange(NPCID.EaterofWorldsTail, EoWHeadBehaviorOverride.TotalLifeAcrossWorm);
            yield return CreateBaseChange(NPCID.DD2DarkMageT1, 5000);
            yield return CreateBaseChange(ModContent.NPCType<HiveMindP1Boss>(), 8100);
            yield return CreateBaseChange(ModContent.NPCType<PerforatorHive>(), 9176);
            yield return CreateBaseChange(ModContent.NPCType<PerforatorHeadSmall>(), 2000);
            yield return CreateBaseChange(ModContent.NPCType<PerforatorHeadMedium>(), 2735);
            yield return CreateBaseChange(ModContent.NPCType<PerforatorHeadLarge>(), 3960);
            yield return CreateBaseChange(NPCID.QueenBee, 9669);
            yield return CreateBaseChange(NPCID.Deerclops, 22844);
            yield return CreateBaseChange(NPCID.SkeletronHead, 13860);
            yield return CreateBaseChange(ModContent.NPCType<SlimeGodCore>(), 3275);
            yield return CreateBaseChange(ModContent.NPCType<CrimulanSGBig>(), 7464);
            yield return CreateBaseChange(ModContent.NPCType<EbonianSGBig>(), 7464);
            yield return CreateBaseChange(NPCID.WallofFleshEye, 3232);
            yield return CreateBaseChange(NPCID.WallofFlesh, 10476);

            yield return CreateBaseChange(ModContent.NPCType<ThiccWaifu>(), 18000);
            yield return CreateBaseChange(NPCID.DD2OgreT2, 15100);
            yield return CreateBaseChange(NPCID.QueenSlimeBoss, 30000);

            #region Mech Bosses
            yield return new NPCHPBalancingChange(NPCID.Spazmatism, AccountForExpertHP((int)(29950 * 0.8f)), InfernumModeBasePriority, InfernumFirstMechCondition);
            yield return new NPCHPBalancingChange(NPCID.Spazmatism, AccountForExpertHP((int)(29950 * 0.9f)), InfernumModeBasePriority, InfernumSecondMechCondition);
            yield return new NPCHPBalancingChange(NPCID.Spazmatism, AccountForExpertHP(29950), InfernumModeBasePriority, InfernumFinalMechCondition);

            yield return new NPCHPBalancingChange(NPCID.Retinazer, AccountForExpertHP((int)(29950 * 0.8f)), InfernumModeBasePriority, InfernumFirstMechCondition);
            yield return new NPCHPBalancingChange(NPCID.Retinazer, AccountForExpertHP((int)(29950 * 0.9f)), InfernumModeBasePriority, InfernumSecondMechCondition);
            yield return new NPCHPBalancingChange(NPCID.Retinazer, AccountForExpertHP(29950), InfernumModeBasePriority, InfernumFinalMechCondition);

            yield return new NPCHPBalancingChange(NPCID.SkeletronPrime, AccountForExpertHP((int)(28000 * 0.8f)), InfernumModeBasePriority, InfernumFirstMechCondition);
            yield return new NPCHPBalancingChange(NPCID.SkeletronPrime, AccountForExpertHP((int)(28000 * 0.9f)), InfernumModeBasePriority, InfernumSecondMechCondition);
            yield return new NPCHPBalancingChange(NPCID.SkeletronPrime, AccountForExpertHP(28000), InfernumModeBasePriority, InfernumFinalMechCondition);

            yield return new NPCHPBalancingChange(NPCID.PrimeVice, AccountForExpertHP((int)(PrimeHeadBehaviorOverride.BaseCollectiveCannonHP * 0.8f)), InfernumModeBasePriority, InfernumFirstMechCondition);
            yield return new NPCHPBalancingChange(NPCID.PrimeVice, AccountForExpertHP((int)(PrimeHeadBehaviorOverride.BaseCollectiveCannonHP * 0.9f)), InfernumModeBasePriority, InfernumSecondMechCondition);
            yield return new NPCHPBalancingChange(NPCID.PrimeVice, AccountForExpertHP(PrimeHeadBehaviorOverride.BaseCollectiveCannonHP), InfernumModeBasePriority, InfernumFinalMechCondition);
            yield return new NPCHPBalancingChange(NPCID.PrimeSaw, AccountForExpertHP((int)(PrimeHeadBehaviorOverride.BaseCollectiveCannonHP * 0.8f)), InfernumModeBasePriority, InfernumFirstMechCondition);
            yield return new NPCHPBalancingChange(NPCID.PrimeSaw, AccountForExpertHP((int)(PrimeHeadBehaviorOverride.BaseCollectiveCannonHP * 0.9f)), InfernumModeBasePriority, InfernumSecondMechCondition);
            yield return new NPCHPBalancingChange(NPCID.PrimeSaw, AccountForExpertHP(PrimeHeadBehaviorOverride.BaseCollectiveCannonHP), InfernumModeBasePriority, InfernumFinalMechCondition);
            yield return new NPCHPBalancingChange(NPCID.PrimeCannon, AccountForExpertHP((int)(PrimeHeadBehaviorOverride.BaseCollectiveCannonHP * 0.8f)), InfernumModeBasePriority, InfernumFirstMechCondition);
            yield return new NPCHPBalancingChange(NPCID.PrimeCannon, AccountForExpertHP((int)(PrimeHeadBehaviorOverride.BaseCollectiveCannonHP * 0.9f)), InfernumModeBasePriority, InfernumSecondMechCondition);
            yield return new NPCHPBalancingChange(NPCID.PrimeCannon, AccountForExpertHP(PrimeHeadBehaviorOverride.BaseCollectiveCannonHP), InfernumModeBasePriority, InfernumFinalMechCondition);
            yield return new NPCHPBalancingChange(NPCID.PrimeLaser, AccountForExpertHP((int)(PrimeHeadBehaviorOverride.BaseCollectiveCannonHP * 0.8f)), InfernumModeBasePriority, InfernumFirstMechCondition);
            yield return new NPCHPBalancingChange(NPCID.PrimeLaser, AccountForExpertHP((int)(PrimeHeadBehaviorOverride.BaseCollectiveCannonHP * 0.9f)), InfernumModeBasePriority, InfernumSecondMechCondition);
            yield return new NPCHPBalancingChange(NPCID.PrimeLaser, AccountForExpertHP(PrimeHeadBehaviorOverride.BaseCollectiveCannonHP), InfernumModeBasePriority, InfernumFinalMechCondition);

            yield return new NPCHPBalancingChange(NPCID.TheDestroyer, AccountForExpertHP((int)(111000 * 0.8f)), InfernumModeBasePriority, InfernumFirstMechCondition);
            yield return new NPCHPBalancingChange(NPCID.TheDestroyer, AccountForExpertHP((int)(111000 * 0.9f)), InfernumModeBasePriority, InfernumSecondMechCondition);
            yield return new NPCHPBalancingChange(NPCID.TheDestroyer, AccountForExpertHP(111000), InfernumModeBasePriority, InfernumFinalMechCondition);

            yield return new NPCHPBalancingChange(NPCID.Probe, AccountForExpertHP((int)(700 * 0.8f)), InfernumModeBasePriority, InfernumFirstMechCondition);
            yield return new NPCHPBalancingChange(NPCID.Probe, AccountForExpertHP((int)(700 * 0.9f)), InfernumModeBasePriority, InfernumSecondMechCondition);
            yield return new NPCHPBalancingChange(NPCID.Probe, AccountForExpertHP(700), InfernumModeBasePriority, InfernumFinalMechCondition);
            #endregion

            yield return CreateBaseChange(ModContent.NPCType<BrimstoneElemental>(), 85515);
            yield return CreateBaseChange(ModContent.NPCType<CalamitasClone>(), 76250);
            yield return CreateBaseChange(ModContent.NPCType<Cataclysm>(), 20600);
            yield return CreateBaseChange(ModContent.NPCType<Catastrophe>(), 13000);
            yield return CreateBaseChange(ModContent.NPCType<SoulSeeker>(), 2100);
            yield return CreateBaseChange(NPCID.Plantera, 110500);
            yield return CreateBaseChange(ModContent.NPCType<Leviathan>(), 102097);
            yield return CreateBaseChange(ModContent.NPCType<AquaticAberration>(), 900);
            yield return CreateBaseChange(ModContent.NPCType<Anahita>(), 71000);
            yield return CreateBaseChange(ModContent.NPCType<AureusSpawn>(), 2500);
            yield return CreateBaseChange(ModContent.NPCType<AstrumAureus>(), 144074);
            yield return CreateBaseChange(NPCID.DD2DarkMageT3, 24500);
            yield return CreateBaseChange(NPCID.DD2Betsy, 66500);
            yield return CreateBaseChange(NPCID.Golem, 198700);
            yield return CreateBaseChange(NPCID.GolemHead, 198700);
            yield return CreateBaseChange(NPCID.GolemHeadFree, 198700);
            yield return CreateBaseChange(NPCID.GolemFistLeft, 198700);
            yield return CreateBaseChange(NPCID.GolemFistRight, 198700);
            yield return CreateBaseChange(ModContent.NPCType<PlaguebringerGoliath>(), 136031);
            yield return CreateBaseChange(ModContent.NPCType<GreatSandShark>(), 107400);
            yield return CreateBaseChange(NPCID.DukeFishron, 100250);
            yield return CreateBaseChange(NPCID.HallowBoss, 22056);
            yield return CreateBaseChange(ModContent.NPCType<RavagerHead>(), 18000);
            yield return CreateBaseChange(ModContent.NPCType<DevilFish>(), 5000);
            yield return CreateBaseChange(ModContent.NPCType<Eidolist>(), 20000);
            yield return CreateBaseChange(NPCID.CultistBoss, 104000);
            yield return CreateBaseChange(NPCID.AncientCultistSquidhead, 9020);
            yield return CreateBaseChange(NPCID.CultistDragonHead, 36500);
            yield return CreateBaseChange(ModContent.NPCType<AstrumDeusHead>(), 287000);
            yield return CreateBaseChange(NPCID.MoonLordHand, 50000);
            yield return CreateBaseChange(NPCID.MoonLordHead, 61000);
            yield return CreateBaseChange(NPCID.MoonLordCore, 135000);

            yield return CreateBaseChange(ModContent.NPCType<ProfanedGuardianCommander>(), 132000);
            yield return CreateBaseChange(ModContent.NPCType<ProfanedGuardianDefender>(), 80000);
            yield return CreateBaseChange(ModContent.NPCType<ProfanedGuardianHealer>(), 80000);
            yield return CreateBaseChange(ModContent.NPCType<Bumblefuck>(), 256000);
            yield return CreateBaseChange(ModContent.NPCType<Bumblefuck2>(), 14300);
            yield return CreateBaseChange(ModContent.NPCType<ProfanedRocks>(), 2300);
            yield return CreateBaseChange(ModContent.NPCType<ProvidenceBoss>(), 900000);
            yield return CreateBaseChange(ModContent.NPCType<StormWeaverHead>(), 646400);
            yield return CreateBaseChange(ModContent.NPCType<CeaselessVoid>(), 455525);
            yield return CreateBaseChange(ModContent.NPCType<DarkEnergy>(), 5000);
            yield return CreateBaseChange(ModContent.NPCType<Signus>(), 546102);
            yield return CreateBaseChange(ModContent.NPCType<Polterghast>(), 544440);
            yield return CreateBaseChange(ModContent.NPCType<OldDukeBoss>(), 936000);
            yield return CreateBaseChange(ModContent.NPCType<DevourerofGodsHead>(), 1776500);
            yield return CreateBaseChange(ModContent.NPCType<Yharon>(), 968420);
            yield return CreateBaseChange(ModContent.NPCType<PrimordialWyrmHead>(), 1260750);
            yield return CreateBaseChange(ModContent.NPCType<ThanatosHead>(), 2400000);
            yield return CreateBaseChange(ModContent.NPCType<AresBody>(), 2560000);
            yield return CreateBaseChange(ModContent.NPCType<Artemis>(), 2400000);
            yield return CreateBaseChange(ModContent.NPCType<Apollo>(), 2400000);
            yield return CreateBaseChange(ModContent.NPCType<SupremeCataclysm>(), 537200);
            yield return CreateBaseChange(ModContent.NPCType<SupremeCatastrophe>(), 537200);
            yield return CreateBaseChange(ModContent.NPCType<SupremeCalamitas>(), 3141592);
            #endregion

            #region Boss Rush HP
            yield return CreateBossRushChange(ModContent.NPCType<KingSlimeJewel>(), 1176000);
            yield return CreateBossRushChange(ModContent.NPCType<DesertScourgeHead>(), 1185000);
            yield return CreateBossRushChange(NPCID.KingSlime, 420000);
            yield return CreateBossRushChange(NPCID.EyeofCthulhu, 770000);
            yield return CreateBossRushChange(NPCID.BrainofCthulhu, 689000);
            yield return CreateBossRushChange(ModContent.NPCType<CrabulonBoss>(), 1776000);
            yield return CreateBossRushChange(NPCID.EaterofWorldsHead, EoWHeadBehaviorOverride.TotalLifeAcrossWormBossRush);
            yield return CreateBossRushChange(NPCID.EaterofWorldsBody, EoWHeadBehaviorOverride.TotalLifeAcrossWormBossRush);
            yield return CreateBossRushChange(NPCID.EaterofWorldsTail, EoWHeadBehaviorOverride.TotalLifeAcrossWormBossRush);
            yield return CreateBossRushChange(ModContent.NPCType<HiveMindP1Boss>(), 606007);
            yield return CreateBossRushChange(ModContent.NPCType<PerforatorHive>(), 420419);
            yield return CreateBossRushChange(ModContent.NPCType<PerforatorHeadSmall>(), 239000);
            yield return CreateBossRushChange(ModContent.NPCType<PerforatorHeadMedium>(), 330000);
            yield return CreateBossRushChange(ModContent.NPCType<PerforatorHeadLarge>(), 296500);
            yield return CreateBossRushChange(NPCID.QueenBee, 611100);
            yield return CreateBossRushChange(NPCID.Deerclops, 927000);
            yield return CreateBossRushChange(NPCID.SkeletronHead, 2508105);
            yield return CreateBossRushChange(ModContent.NPCType<SlimeGodCore>(), 486500);
            yield return CreateBossRushChange(ModContent.NPCType<CrimulanSGBig>(), 213720);
            yield return CreateBossRushChange(ModContent.NPCType<EbonianSGBig>(), 213720);
            yield return CreateBossRushChange(NPCID.WallofFleshEye, 140800);
            yield return CreateBossRushChange(NPCID.WallofFlesh, 854000);

            yield return CreateBossRushChange(NPCID.QueenSlimeBoss, 840000);
            yield return CreateBossRushChange(NPCID.Spazmatism, 833760);
            yield return CreateBossRushChange(NPCID.Retinazer, 840885);
            yield return CreateBossRushChange(NPCID.SkeletronPrime, 989515);
            yield return CreateBossRushChange(NPCID.PrimeVice, PrimeHeadBehaviorOverride.BaseCollectiveCannonHPBossRush);
            yield return CreateBossRushChange(NPCID.PrimeSaw, PrimeHeadBehaviorOverride.BaseCollectiveCannonHPBossRush);
            yield return CreateBossRushChange(NPCID.PrimeCannon, PrimeHeadBehaviorOverride.BaseCollectiveCannonHPBossRush);
            yield return CreateBossRushChange(NPCID.PrimeLaser, PrimeHeadBehaviorOverride.BaseCollectiveCannonHPBossRush);
            yield return CreateBossRushChange(NPCID.TheDestroyer, 1110580);
            yield return CreateBossRushChange(NPCID.Probe, 30000);
            yield return CreateBossRushChange(ModContent.NPCType<BrimstoneElemental>(), 1105000);
            yield return CreateBossRushChange(ModContent.NPCType<CalamitasClone>(), 985000);
            yield return CreateBossRushChange(ModContent.NPCType<Cataclysm>(), 193380);
            yield return CreateBossRushChange(ModContent.NPCType<Catastrophe>(), 176085);
            yield return CreateBossRushChange(ModContent.NPCType<SoulSeeker>(), 24000);
            yield return CreateBossRushChange(NPCID.Plantera, 575576);
            yield return CreateBossRushChange(ModContent.NPCType<Leviathan>(), 1200000);
            yield return CreateBossRushChange(ModContent.NPCType<AquaticAberration>(), -1);
            yield return CreateBossRushChange(ModContent.NPCType<Anahita>(), 450000);
            yield return CreateBossRushChange(ModContent.NPCType<AstrumAureus>(), 1230680);
            yield return CreateBossRushChange(NPCID.Golem, 1250000);
            yield return CreateBossRushChange(NPCID.GolemHead, 1250000);
            yield return CreateBossRushChange(NPCID.GolemHeadFree, 1250000);
            yield return CreateBossRushChange(NPCID.GolemFistLeft, 1250000);
            yield return CreateBossRushChange(NPCID.GolemFistRight, 1250000);
            yield return CreateBossRushChange(ModContent.NPCType<PlaguebringerGoliath>(), 666666);
            yield return CreateBossRushChange(NPCID.DukeFishron, 1330000);
            yield return CreateBossRushChange(NPCID.HallowBoss, 2960000);
            yield return CreateBossRushChange(ModContent.NPCType<RavagerHead>(), -1);
            yield return CreateBossRushChange(NPCID.CultistBoss, 727272);
            yield return CreateBossRushChange(NPCID.AncientCultistSquidhead, -1);
            yield return CreateBossRushChange(NPCID.CultistDragonHead, -1);
            yield return CreateBossRushChange(ModContent.NPCType<AstrumDeusHead>(), 930000);
            yield return CreateBossRushChange(NPCID.MoonLordHand, 400000);
            yield return CreateBossRushChange(NPCID.MoonLordHead, 661110);
            yield return CreateBossRushChange(NPCID.MoonLordCore, 1600000);

            yield return CreateBossRushChange(ModContent.NPCType<ProfanedGuardianCommander>(), 720000);
            yield return CreateBossRushChange(ModContent.NPCType<ProfanedGuardianDefender>(), 205000);
            yield return CreateBossRushChange(ModContent.NPCType<ProfanedGuardianHealer>(), 205000);
            yield return CreateBossRushChange(ModContent.NPCType<Bumblefuck>(), 860000);
            yield return CreateBossRushChange(ModContent.NPCType<Bumblefuck2>(), -1);
            yield return CreateBossRushChange(ModContent.NPCType<ProfanedRocks>(), 7500);
            yield return CreateBossRushChange(ModContent.NPCType<ProvidenceBoss>(), 3900000);
            yield return CreateBossRushChange(ModContent.NPCType<StormWeaverHead>(), 1232100);
            yield return CreateBossRushChange(ModContent.NPCType<CeaselessVoid>(), 1040000);
            yield return CreateBossRushChange(ModContent.NPCType<DarkEnergy>(), 19000);
            yield return CreateBossRushChange(ModContent.NPCType<Signus>(), 848210);
            yield return CreateBossRushChange(ModContent.NPCType<Polterghast>(), 1575910);
            yield return CreateBossRushChange(ModContent.NPCType<OldDukeBoss>(), 1600000);
            yield return CreateBossRushChange(ModContent.NPCType<DevourerofGodsHead>(), 2960000);
            yield return CreateBossRushChange(ModContent.NPCType<Yharon>(), 1618950);
            #endregion
        }
    }
}
