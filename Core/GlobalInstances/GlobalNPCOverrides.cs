using CalamityMod;
using CalamityMod.Buffs.StatBuffs;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.AstrumAureus;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.NPCs.GreatSandShark;
using CalamityMod.NPCs.NormalNPCs;
using CalamityMod.NPCs.Providence;
using CalamityMod.NPCs.SlimeGod;
using CalamityMod.NPCs.Yharon;
using CalamityMod.UI;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics;
using InfernumMode.Content.Achievements.InfernumAchievements;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Cryogen;
using InfernumMode.Content.BehaviorOverrides.BossAIs.DoG;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Thanatos;
using InfernumMode.Content.BehaviorOverrides.BossAIs.MoonLord;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Prime;
using InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians;
using InfernumMode.Content.BehaviorOverrides.BossAIs.SlimeGod;
using InfernumMode.Content.WorldGeneration;
using InfernumMode.Core.Balancing;
using InfernumMode.Core.GlobalInstances.Players;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using CryogenNPC = CalamityMod.NPCs.Cryogen.Cryogen;

namespace InfernumMode.GlobalInstances
{
    public partial class GlobalNPCOverrides : GlobalNPC
    {
        #region Instance and Variables
        public override bool InstancePerEntity => true;

        // I'll be fucking damned if this isn't enough.
        public const int TotalExtraAISlots = 100;

        public int? TotalPlayersAtStart = null;

        public bool ShouldUseSaturationBlur = false;

        public bool IsAbyssPredator = false;

        public bool IsAbyssPrey = false;

        public bool HasResetHP = false;

        // I'll be fucking damned if this isn't enough.
        public float[] ExtraAI = new float[TotalExtraAISlots];

        public Rectangle Arena = default;

        public PrimitiveTrailCopy OptionalPrimitiveDrawer;

        internal static int Cryogen = -1;
        internal static int AstrumAureus = -1;
        internal static int ProfanedCrystal = -1;
        internal static int Yharon = -1;

        #endregion

        #region Reset Effects
        public override void ResetEffects(NPC npc)
        {
            static void ResetSavedIndex(ref int type, int type1, int type2 = -1)
            {
                if (type >= 0)
                {
                    if (!Main.npc[type].active)
                    {
                        type = -1;
                    }
                    else if (type2 == -1)
                    {
                        if (Main.npc[type].type != type1)
                            type = -1;
                    }
                    else
                    {
                        if (Main.npc[type].type != type1 && Main.npc[type].type != type2)
                            type = -1;
                    }
                }
            }

            ResetSavedIndex(ref Cryogen, ModContent.NPCType<CryogenNPC>());
            ResetSavedIndex(ref AstrumAureus, ModContent.NPCType<AstrumAureus>());
            ResetSavedIndex(ref ProfanedCrystal, ModContent.NPCType<HealerShieldCrystal>());
            ResetSavedIndex(ref Yharon, ModContent.NPCType<Yharon>());
        }
        #endregion Reset Effects

        #region Overrides

        public override void SetDefaults(NPC npc)
        {
            for (int i = 0; i < ExtraAI.Length; i++)
                ExtraAI[i] = 0f;

            ShouldUseSaturationBlur = false;
            IsAbyssPredator = false;
            IsAbyssPrey = false;
            HasResetHP = false;
            OptionalPrimitiveDrawer = null;

            if (InfernumMode.CanUseCustomAIs)
            {
                if (OverridingListManager.InfernumSetDefaultsOverrideList.ContainsKey(npc.type))
                    OverridingListManager.InfernumSetDefaultsOverrideList[npc.type].DynamicInvoke(npc);
            }
        }
        
        public override void SetStaticDefaults()
        {
            NPCID.Sets.BossBestiaryPriority.Add(ModContent.NPCType<GreatSandShark>());
        }

        public void AdjustMaxHP(ref int maxHP)
        {
            float hpMultiplier = 1f;
            float accumulatedFactor = 0.35f;
            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                for (int i = 1; i < (TotalPlayersAtStart ?? 1); i++)
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
            maxHP += (int)(maxHP * CalamityConfig.Instance.BossHealthBoost * 0.01);
        }

        public override bool PreAI(NPC npc)
        {
            // Reset the saturation blur state.
            ShouldUseSaturationBlur = false;

            // Initialize the amount of players the NPC had when it spawned.
            if (!TotalPlayersAtStart.HasValue)
            {
                int activePlayerCount = 0;
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    if (Main.player[i].active)
                        activePlayerCount++;
                }
                TotalPlayersAtStart = activePlayerCount;
                npc.netUpdate = true;
            }
            
            if (InfernumMode.CanUseCustomAIs)
            {
                // Correct an enemy's life depending on its cached true life value.
                if (!HasResetHP && NPCHPValues.HPValues.TryGetValue(npc.type, out int maxHP) && maxHP >= 0)
                {
                    AdjustMaxHP(ref maxHP);

                    if (maxHP != npc.lifeMax)
                    {
                        npc.life = npc.lifeMax = maxHP;
                        if (BossHealthBarManager.Bars.Any(b => b.NPCIndex == npc.whoAmI))
                            BossHealthBarManager.Bars.First(b => b.NPCIndex == npc.whoAmI).InitialMaxLife = npc.lifeMax;

                        npc.netUpdate = true;
                    }
                }
                HasResetHP = true;

                if (OverridingListManager.InfernumNPCPreAIOverrideList.ContainsKey(npc.type))
                {
                    // Disable the effects of timed DR.
                    if (npc.Calamity().KillTime > 0 && npc.Calamity().AITimer < npc.Calamity().KillTime)
                        npc.Calamity().AITimer = npc.Calamity().KillTime;

                    // If any boss NPC is active, apply Zen to nearby players to reduce spawn rate.
                    if (Main.netMode != NetmodeID.Server && CalamityConfig.Instance.BossZen && (npc.Calamity().KillTime > 0 || npc.type == ModContent.NPCType<Draedon>() || npc.type == ModContent.NPCType<ThiccWaifu>()))
                    {
                        if (!Main.player[Main.myPlayer].dead && Main.player[Main.myPlayer].active && npc.WithinRange(Main.player[Main.myPlayer].Center, 6400f))
                            Main.player[Main.myPlayer].AddBuff(ModContent.BuffType<BossEffects>(), 2);
                    }

                    // Decrement each immune timer if it's greater than 0.
                    for (int i = 0; i < CalamityGlobalNPC.maxPlayerImmunities; i++)
                    {
                        if (npc.Calamity().dashImmunityTime[i] > 0)
                            npc.Calamity().dashImmunityTime[i]--;
                    }

                    // Disable netOffset effects.
                    npc.netOffset = Vector2.Zero;

                    bool result = OverridingListManager.InfernumNPCPreAIOverrideList[npc.type].Invoke(npc);
                    if (ShouldUseSaturationBlur && !BossRushEvent.BossRushActive)
                        ScreenSaturationBlurSystem.ShouldEffectBeActive = true;

                    return result;
                }
            }
            return base.PreAI(npc);
        }

        public override void OnKill(NPC npc)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return;
            
            bool bigSlimeGod = npc.type == ModContent.NPCType<EbonianSlimeGod>() || npc.type == ModContent.NPCType<CrimulanSlimeGod>();
            if (bigSlimeGod && OverridingListManager.Registered(npc.type))
            {
                for (int i = 0; i < 12; i++)
                {
                    int slime = NPC.NewNPC(npc.GetSource_Death(), (int)npc.Center.X, (int)npc.Center.Y, npc.type, ModContent.NPCType<SplitBigSlimeAnimation>());
                    Main.npc[slime].velocity = Main.rand.NextVector2Circular(8f, 8f);
                }
            }

            if (!WeakReferenceSupport.InAnySubworld())
            {
                // Create a profaned temple after the moon lord is killed if it doesn't exist yet, for backwards world compatibility reasons.
                if (npc.type == NPCID.MoonLordCore && !WorldSaveSystem.HasGeneratedProfanedShrine)
                {
                    Utilities.DisplayText("A profaned shrine has erupted from the ashes at the underworld's edge!", Color.Orange);
                    ProfanedGarden.Generate(new(), new(new()));
                    WorldSaveSystem.HasGeneratedProfanedShrine = true;
                }

                // Create a lost colosseum entrance after the cultistis killed if it doesn't exist yet, for backwards world compatibility reasons.
                if (npc.type == NPCID.CultistBoss && !WorldSaveSystem.HasGeneratedColosseumEntrance)
                {
                    Utilities.DisplayText("Mysterious ruins have materialized in the heart of the desert!", Color.Lerp(Color.Orange, Color.Yellow, 0.65f));
                    LostColosseumEntrance.Generate(new(), new(new()));
                    WorldSaveSystem.HasGeneratedColosseumEntrance = true;
                }
            }

            if (npc.type == ModContent.NPCType<Providence>())
            {
                if (!Main.dayTime && !WorldSaveSystem.HasBeatedInfernumProvRegularly)
                    WorldSaveSystem.HasBeatedInfernumNightProvBeforeDay = true;
                WorldSaveSystem.HasBeatedInfernumProvRegularly = true;
                CalamityNetcode.SyncWorld();
            }

            // Trigger achievement checks.
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (!Main.player[i].active)
                    continue;

                Player player = Main.player[i];
                if (npc.boss)
                    AchievementPlayer.ExtraUpdateAchievements(player, new UpdateContext(npc.whoAmI));
                else if (KillAllMinibossesAchievement.MinibossIDs.Contains(npc.type))
                    AchievementPlayer.ExtraUpdateAchievements(player, new UpdateContext(npc.whoAmI));
            }
        }

        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
        {
            if (npc.type == ModContent.NPCType<DevourerofGodsBody>() && OverridingListManager.Registered<DevourerofGodsHead>())
            {
                cooldownSlot = 0;
                return npc.alpha == 0;
            }
            return base.CanHitPlayer(npc, target, ref cooldownSlot);
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, int damage, float knockback, bool crit)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return;

            // Make Cryogen release ice particles when hit.
            if (npc.type == ModContent.NPCType<CryogenNPC>() && OverridingListManager.Registered(npc.type))
                CryogenBehaviorOverride.OnHitIceParticles(npc, projectile, crit);
        }

        public override void HitEffect(NPC npc, int hitDirection, double damage)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return;

            // Play GSS' custom hit sound.
            if (npc.type == ModContent.NPCType<GreatSandShark>() && npc.soundDelay <= 0)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.GreatSandSharkHitSound with { Volume = 2f }, npc.Center);
                npc.soundDelay = 11;
            }

            // Ensure that Prime's saw ends the saw sound if it's unexpectedly killed.
            if (npc.type == NPCID.PrimeSaw && npc.life <= 0)
                PrimeViceBehaviorOverride.DoBehavior_SlowSparkShrapnelMeleeCharges(npc, Main.player[npc.target], false);
        }

        public override bool StrikeNPC(NPC npc, ref double damage, int defense, ref float knockback, int hitDirection, ref bool crit)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return base.StrikeNPC(npc, ref damage, defense, ref knockback, hitDirection, ref crit);

            if (npc.type == ModContent.NPCType<Yharon>() && OverridingListManager.Registered(npc.type))
            {
                if (npc.life - (int)Math.Ceiling(damage) <= 0)
                    npc.NPCLoot();
            }

            double realDamage = crit ? damage * 2D : damage;

            // Make DoG enter the second phase once ready.
            bool isDoG = npc.type == ModContent.NPCType<DevourerofGodsHead>() || npc.type == ModContent.NPCType<DevourerofGodsBody>() || npc.type == ModContent.NPCType<DevourerofGodsTail>();
            if (isDoG && OverridingListManager.Registered<DevourerofGodsHead>())
                DoGPhase1HeadBehaviorOverride.HandleDoGLifeBasedHitTriggers(npc, realDamage, ref damage);

            if ((npc.type is NPCID.MoonLordHand or NPCID.MoonLordHead) && OverridingListManager.Registered(NPCID.MoonLordCore))
                MoonLordCoreBehaviorOverride.HandleBodyPartDeathTriggers(npc, realDamage);

            // Make Thanatos' head take a flat multiplier in terms of final damage, as a means of allowing direct hits to be effective.
            if (npc.type == ModContent.NPCType<ThanatosHead>() && OverridingListManager.Registered(npc.type))
            {
                damage = (int)(damage * ThanatosHeadBehaviorOverride.FlatDamageBoostFactor);
                if (npc.Calamity().DR > 0.999f)
                {
                    damage = 0D;
                    return false;
                }
            }

            return base.StrikeNPC(npc, ref damage, defense, ref knockback, hitDirection, ref crit);
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return;

            BalancingChangesManager.ApplyFromProjectile(npc, ref damage, projectile);
        }

        public override bool CheckDead(NPC npc)
        {
            if (InfernumMode.CanUseCustomAIs && OverridingListManager.InfernumCheckDeadOverrideList.ContainsKey(npc.type))
                return (bool)OverridingListManager.InfernumCheckDeadOverrideList[npc.type].DynamicInvoke(npc);
            
            return base.CheckDead(npc);
        }

        public override bool CheckActive(NPC npc)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return base.CheckActive(npc);

            if (npc.type == NPCID.KingSlime && OverridingListManager.Registered(npc.type))
                return false;
            if (npc.type == NPCID.SkeletronHand && OverridingListManager.Registered(NPCID.SkeletronHead))
                return false;
            if (npc.type == NPCID.AncientCultistSquidhead && OverridingListManager.Registered(NPCID.CultistBoss))
                return false;
            if (npc.type == NPCID.MoonLordFreeEye && OverridingListManager.Registered(NPCID.MoonLordCore))
                return false;
            if (npc.type == ModContent.NPCType<Eidolist>() && OverridingListManager.Registered<Eidolist>())
                return false;

            return base.CheckActive(npc);
        }

        #endregion
    }
}