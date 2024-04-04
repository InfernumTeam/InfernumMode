using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs.DesertScourge;
using CalamityMod.NPCs.NormalNPCs;
using CalamityMod.NPCs.Perforator;
using CalamityMod.NPCs.SlimeGod;
using CalamityMod.NPCs.SunkenSea;
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

        private static NPCHPBalancingChange CreateBaseChange(int npcType, int hp) => new(npcType, hp, InfernumModeBasePriority, InfernumModeCondition);

        public override IEnumerable<NPCHPBalancingChange> GetNPCHPBalancingChanges()
        {
            #region Base Infernum HP
            yield return CreateBaseChange(ModContent.NPCType<KingSlimeJewel>(), 2000);
            yield return CreateBaseChange(ModContent.NPCType<DesertScourgeHead>(), 7200);
            yield return CreateBaseChange(ModContent.NPCType<GiantClam>(), 4100);
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
            yield return new NPCHPBalancingChange(NPCID.Spazmatism, (int)(29950 * 0.8f), InfernumModeBasePriority, InfernumFirstMechCondition);
            yield return new NPCHPBalancingChange(NPCID.Spazmatism, (int)(29950 * 0.9f), InfernumModeBasePriority, InfernumSecondMechCondition);
            yield return new NPCHPBalancingChange(NPCID.Spazmatism, 29950, InfernumModeBasePriority, InfernumFinalMechCondition);

            yield return new NPCHPBalancingChange(NPCID.Retinazer, (int)(29950 * 0.8f), InfernumModeBasePriority, InfernumFirstMechCondition);
            yield return new NPCHPBalancingChange(NPCID.Retinazer, (int)(29950 * 0.9f), InfernumModeBasePriority, InfernumSecondMechCondition);
            yield return new NPCHPBalancingChange(NPCID.Retinazer, 29950, InfernumModeBasePriority, InfernumFinalMechCondition);

            yield return new NPCHPBalancingChange(NPCID.SkeletronPrime, (int)(28000 * 0.8f), InfernumModeBasePriority, InfernumFirstMechCondition);
            yield return new NPCHPBalancingChange(NPCID.SkeletronPrime, (int)(28000 * 0.9f), InfernumModeBasePriority, InfernumSecondMechCondition);
            yield return new NPCHPBalancingChange(NPCID.SkeletronPrime, 28000, InfernumModeBasePriority, InfernumFinalMechCondition);

            yield return new NPCHPBalancingChange(NPCID.PrimeVice, (int)(PrimeHeadBehaviorOverride.BaseCollectiveCannonHP * 0.8f), InfernumModeBasePriority, InfernumFirstMechCondition);
            yield return new NPCHPBalancingChange(NPCID.PrimeVice, (int)(PrimeHeadBehaviorOverride.BaseCollectiveCannonHP * 0.9f), InfernumModeBasePriority, InfernumSecondMechCondition);
            yield return new NPCHPBalancingChange(NPCID.PrimeVice, PrimeHeadBehaviorOverride.BaseCollectiveCannonHP, InfernumModeBasePriority, InfernumFinalMechCondition);
            yield return new NPCHPBalancingChange(NPCID.PrimeSaw, (int)(PrimeHeadBehaviorOverride.BaseCollectiveCannonHP * 0.8f), InfernumModeBasePriority, InfernumFirstMechCondition);
            yield return new NPCHPBalancingChange(NPCID.PrimeSaw, (int)(PrimeHeadBehaviorOverride.BaseCollectiveCannonHP * 0.9f), InfernumModeBasePriority, InfernumSecondMechCondition);
            yield return new NPCHPBalancingChange(NPCID.PrimeSaw, PrimeHeadBehaviorOverride.BaseCollectiveCannonHP, InfernumModeBasePriority, InfernumFinalMechCondition);
            yield return new NPCHPBalancingChange(NPCID.PrimeCannon, (int)(PrimeHeadBehaviorOverride.BaseCollectiveCannonHP * 0.8f), InfernumModeBasePriority, InfernumFirstMechCondition);
            yield return new NPCHPBalancingChange(NPCID.PrimeCannon, (int)(PrimeHeadBehaviorOverride.BaseCollectiveCannonHP * 0.9f), InfernumModeBasePriority, InfernumSecondMechCondition);
            yield return new NPCHPBalancingChange(NPCID.PrimeCannon, PrimeHeadBehaviorOverride.BaseCollectiveCannonHP, InfernumModeBasePriority, InfernumFinalMechCondition);
            yield return new NPCHPBalancingChange(NPCID.PrimeLaser, (int)(PrimeHeadBehaviorOverride.BaseCollectiveCannonHP * 0.8f), InfernumModeBasePriority, InfernumFirstMechCondition);
            yield return new NPCHPBalancingChange(NPCID.PrimeLaser, (int)(PrimeHeadBehaviorOverride.BaseCollectiveCannonHP * 0.9f), InfernumModeBasePriority, InfernumSecondMechCondition);
            yield return new NPCHPBalancingChange(NPCID.PrimeLaser, PrimeHeadBehaviorOverride.BaseCollectiveCannonHP, InfernumModeBasePriority, InfernumFinalMechCondition);

            yield return new NPCHPBalancingChange(NPCID.TheDestroyer, (int)(111000 * 0.8f), InfernumModeBasePriority, InfernumFirstMechCondition);
            yield return new NPCHPBalancingChange(NPCID.TheDestroyer, (int)(111000 * 0.9f), InfernumModeBasePriority, InfernumSecondMechCondition);
            yield return new NPCHPBalancingChange(NPCID.TheDestroyer, 111000, InfernumModeBasePriority, InfernumFinalMechCondition);

            yield return new NPCHPBalancingChange(NPCID.Probe, (int)(700 * 0.8f), InfernumModeBasePriority, InfernumFirstMechCondition);
            yield return new NPCHPBalancingChange(NPCID.Probe, (int)(700 * 0.9f), InfernumModeBasePriority, InfernumSecondMechCondition);
            yield return new NPCHPBalancingChange(NPCID.Probe, 700, InfernumModeBasePriority, InfernumFinalMechCondition);
            #endregion
            #endregion
        }
    }
}
