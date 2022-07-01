using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatBuffs;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Events;
using CalamityMod.Items.Materials;
using CalamityMod.NPCs;
using CalamityMod.NPCs.AdultEidolonWyrm;
using CalamityMod.NPCs.AstrumAureus;
using CalamityMod.NPCs.Bumblebirb;
using CalamityMod.NPCs.Calamitas;
using CalamityMod.NPCs.Crabulon;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.NPCs.GreatSandShark;
using CalamityMod.NPCs.Perforator;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.NPCs.Yharon;
using CalamityMod.UI;
using InfernumMode.Balancing;
using InfernumMode.BehaviorOverrides.BossAIs.BoC;
using InfernumMode.BehaviorOverrides.BossAIs.EoW;
using InfernumMode.BehaviorOverrides.BossAIs.WallOfFlesh;
using InfernumMode.Buffs;
using InfernumMode.OverridingSystem;
using InfernumMode.Systems;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using CryogenNPC = CalamityMod.NPCs.Cryogen.Cryogen;
using OldDukeNPC = CalamityMod.NPCs.OldDuke.OldDuke;
using PolterghastNPC = CalamityMod.NPCs.Polterghast.Polterghast;
using SlimeGodCore = CalamityMod.NPCs.SlimeGod.SlimeGodCore;

namespace InfernumMode.GlobalInstances
{
    public partial class GlobalNPCOverrides : GlobalNPC
    {
        #region Instance and Variables
        public override bool InstancePerEntity => true;

        public static bool MLSealTeleport = false;
        public const int TotalExtraAISlots = 100;

        // I'll be fucking damned if this isn't enough
        public float[] ExtraAI = new float[TotalExtraAISlots];
        public Vector2 angleTarget = default;
        public Rectangle arenaRectangle = default;
        public bool canTelegraph = false;
        public PrimitiveTrailCopy OptionalPrimitiveDrawer;

        public static int Cryogen = -1;
        public static int AstrumAureus = -1;
        public static int Athena = -1;

        #endregion

        #region Reset Effects
        public override void ResetEffects(NPC npc)
        {
            void ResetSavedIndex(ref int type, int type1, int type2 = -1)
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
        }
        #endregion Reset Effects

        #region Overrides

        #region Get Alpha
        public override Color? GetAlpha(NPC npc, Color drawColor)
        {
            if (npc.type == ModContent.NPCType<CalamitasClone>() && WorldSaveSystem.InfernumMode && OverridingListManager.Registered(npc.type))
            {
                bool brotherAlive = false;
                if (CalamityGlobalNPC.cataclysm != -1)
                {
                    if (Main.npc[CalamityGlobalNPC.cataclysm].active)
                    {
                        brotherAlive = true;
                    }
                }
                if (CalamityGlobalNPC.catastrophe != -1)
                {
                    if (Main.npc[CalamityGlobalNPC.catastrophe].active)
                    {
                        brotherAlive = true;
                    }
                }
                if (WorldSaveSystem.InfernumMode && brotherAlive)
                    return new Color(100, 0, 0, 127);
            }
            return base.GetAlpha(npc, drawColor);
        }
        #endregion

        public override void SetDefaults(NPC npc)
        {
            angleTarget = default;
            for (int i = 0; i < ExtraAI.Length; i++)
                ExtraAI[i] = 0f;

            OptionalPrimitiveDrawer = null;

            if (InfernumMode.CanUseCustomAIs)
            {
                if (OverridingListManager.InfernumSetDefaultsOverrideList.ContainsKey(npc.type))
                    OverridingListManager.InfernumSetDefaultsOverrideList[npc.type].DynamicInvoke(npc);
            }
        }

        public static void AdjustMaxHP(ref int maxHP)
        {
            float hpMultiplier = 1f;
            float accumulatedFactor = 0.35f;
            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                int activePlayerCount = 0;
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    if (Main.player[i].active)
                        activePlayerCount++;
                }

                for (int i = 1; i < activePlayerCount; i++)
                {
                    hpMultiplier += accumulatedFactor;
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
            if (InfernumMode.CanUseCustomAIs)
            {
                // Correct an enemy's life depending on its cached true life value.
                if (NPCHPValues.HPValues.ContainsKey(npc.type) && NPCHPValues.HPValues[npc.type] >= 0)
                {
                    int maxHP = NPCHPValues.HPValues[npc.type];
                    AdjustMaxHP(ref maxHP);

                    if (maxHP != npc.lifeMax)
                    {
                        npc.life = npc.lifeMax = maxHP;
                        if (BossHealthBarManager.Bars.Any(b => b.NPCIndex == npc.whoAmI))
                            BossHealthBarManager.Bars.First(b => b.NPCIndex == npc.whoAmI).InitialMaxLife = npc.lifeMax;

                        npc.netUpdate = true;
                    }
                }

                // Make perf worms immune to debuffs.
                int[] perforatorIDs = new int[]
                {
                    ModContent.NPCType<PerforatorHeadLarge>(),
                    ModContent.NPCType<PerforatorBodyLarge>(),
                    ModContent.NPCType<PerforatorTailLarge>(),
                    ModContent.NPCType<PerforatorHeadMedium>(),
                    ModContent.NPCType<PerforatorBodyMedium>(),
                    ModContent.NPCType<PerforatorTailMedium>(),
                    ModContent.NPCType<PerforatorHeadSmall>(),
                    ModContent.NPCType<PerforatorBodySmall>(),
                    ModContent.NPCType<PerforatorTailSmall>()
                };
                if (perforatorIDs.Contains(npc.type) && OverridingListManager.Registered<PerforatorHive>())
                {
                    for (int k = 0; k < npc.buffImmune.Length; k++)
                        npc.buffImmune[k] = true;
                }

                if (OverridingListManager.InfernumNPCPreAIOverrideList.ContainsKey(npc.type))
                {
                    // Disable the effects of timed DR.
                    if (npc.Calamity().KillTime > 0 && npc.Calamity().AITimer < npc.Calamity().KillTime)
                        npc.Calamity().AITimer = npc.Calamity().KillTime;

                    // If any boss NPC is active, apply Zen to nearby players to reduce spawn rate.
                    if (Main.netMode != NetmodeID.Server && CalamityConfig.Instance.BossZen && (npc.Calamity().KillTime > 0 || npc.type == ModContent.NPCType<Draedon>()))
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

                    return OverridingListManager.InfernumNPCPreAIOverrideList[npc.type].Invoke(npc);
                }
            }
            return base.PreAI(npc);
        }

        public override bool PreKill(NPC npc)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return base.PreKill(npc);

            if (npc.type == NPCID.EaterofWorldsHead && OverridingListManager.Registered(npc.type))
            {
                if (npc.realLife != -1 && Main.npc[npc.realLife].Infernum().ExtraAI[9] == 0f)
                {
                    Main.npc[npc.realLife].NPCLoot();
                    Main.npc[npc.realLife].Infernum().ExtraAI[9] = 1f;
                    return false;
                }

                if (npc.ai[2] >= 2f)
                {
                    npc.boss = true;

                    if (npc.Infernum().ExtraAI[10] == 0f)
                    {
                        npc.Infernum().ExtraAI[10] = 1f;
                        if (BossRushEvent.BossRushActive)
                            typeof(BossRushEvent).GetMethod("OnBossKill", Utilities.UniversalBindingFlags).Invoke(null, new object[] { npc, Mod });
                        else
                            npc.NPCLoot();
                    }
                }

                else if (npc.realLife == -1 && npc.Infernum().ExtraAI[10] == 0f)
                {
                    npc.Infernum().ExtraAI[10] = 1f;
                    EoWHeadBehaviorOverride.HandleSplit(npc, ref npc.ai[2]);
                }

                return npc.ai[2] >= 2f;
            }

            // Clear lightning.
            if (npc.type == NPCID.BrainofCthulhu && OverridingListManager.Registered(npc.type))
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].type == ProjectileID.CultistBossLightningOrbArc || Main.projectile[i].type == ModContent.ProjectileType<PsionicOrb>())
                        Main.projectile[i].active = false;
                }
            }

            if (npc.type == ModContent.NPCType<OldDukeNPC>() && OverridingListManager.Registered(npc.type))
                CalamityMod.CalamityMod.StopRain();

            return base.PreKill(npc);
        }

        public override void OnKill(NPC npc)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return;

            if (npc.type == NPCID.WallofFlesh && OverridingListManager.Registered(npc.type))
            {
                for (int i = 0; i < Main.rand.Next(18, 29 + 1); i++)
                {
                    int soul = Utilities.NewProjectileBetter(npc.Center, Main.rand.NextVector2CircularEdge(8f, 8f), ModContent.ProjectileType<CursedSoul>(), 95, 0f);
                    Main.projectile[soul].localAI[1] = Main.rand.NextBool().ToDirectionInt();
                }
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

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return;

            BalancingChangesManager.ApplyFromProjectile(npc, ref damage, projectile);
        }

        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            switch (npc.type)
            {
                case NPCID.BloodNautilus:
                    npcLoot.AddIf(() => InfernumMode.CanUseCustomAIs && OverridingListManager.Registered(NPCID.BloodNautilus), ModContent.ItemType<BloodOrb>(), 1, 85, 105);
                    break;
            }
        }

        public override bool CheckDead(NPC npc)
        {
            if (!InfernumMode.CanUseCustomAIs)
                return base.CheckDead(npc);

            if (npc.type == NPCID.WallofFleshEye && OverridingListManager.Registered(NPCID.WallofFlesh))
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < Main.rand.Next(11, 15 + 1); i++)
                        Utilities.NewProjectileBetter(npc.Center, Main.rand.NextVector2CircularEdge(8f, 8f), ModContent.ProjectileType<CursedSoul>(), 55, 0f);
                    if (Main.npc.IndexInRange(Main.wofNPCIndex))
                        Main.npc[Main.wofNPCIndex].StrikeNPC(1550, 0f, 0);
                }

                npc.life = 1;
                npc.ai[1] = 0f;
                npc.Infernum().ExtraAI[2] = 1f;
                npc.active = true;
                npc.netUpdate = true;
                return false;
            }

            if (npc.type is NPCID.Spazmatism or NPCID.Retinazer)
            {
                bool otherTwinHasCreatedShield = false;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (!Main.npc[i].active)
                        continue;
                    if (Main.npc[i].type is not NPCID.Retinazer and not NPCID.Spazmatism)
                        continue;
                    if (Main.npc[i].type == npc.type)
                        continue;

                    if (Main.npc[i].Infernum().ExtraAI[3] == 1f)
                    {
                        otherTwinHasCreatedShield = true;
                        break;
                    }
                }

                if (npc.Infernum().ExtraAI[3] == 0f && !otherTwinHasCreatedShield)
                {
                    npc.life = 1;
                    npc.active = true;
                    npc.netUpdate = true;
                    npc.dontTakeDamage = true;
                    return false;
                }
            }
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
            if (npc.type == ModContent.NPCType<GreatSandShark>() && OverridingListManager.Registered(npc.type))
                return false;

            return base.CheckActive(npc);
        }

        public override void OnHitPlayer(NPC npc, Player target, int damage, bool crit)
        {
            if (!WorldSaveSystem.InfernumMode)
                return;

            if (npc.type == ModContent.NPCType<Crabulon>() && OverridingListManager.Registered(npc.type))
            {
                target.AddBuff(BuffID.Poisoned, 180);
                target.AddBuff(ModContent.BuffType<ArmorCrunch>(), 180);
            }

            if (npc.type == NPCID.QueenBee && OverridingListManager.Registered(npc.type))
                target.AddBuff(ModContent.BuffType<ArmorCrunch>(), 180);

            if (npc.type == ModContent.NPCType<SlimeGodCore>() && OverridingListManager.Registered(npc.type))
            {
                target.AddBuff(ModContent.BuffType<BurningBlood>(), 120);
                target.AddBuff(ModContent.BuffType<Shadowflame>(), 90);
                target.AddBuff(BuffID.Slimed, 240);
                target.AddBuff(BuffID.Slow, 240);
            }
            if (npc.type == NPCID.Retinazer && !NPC.AnyNPCs(NPCID.Spazmatism) && OverridingListManager.Registered(npc.type))
                target.AddBuff(ModContent.BuffType<RedSurge>(), 180);
            if (npc.type == NPCID.Spazmatism && !NPC.AnyNPCs(NPCID.Retinazer) && OverridingListManager.Registered(npc.type))
                target.AddBuff(ModContent.BuffType<ShadowflameInferno>(), 180);

            if ((npc.type is NPCID.PrimeSaw or NPCID.PrimeVice) && OverridingListManager.Registered(NPCID.SkeletronPrime))
            {
                target.AddBuff(BuffID.BrokenArmor, 180);
                target.AddBuff(ModContent.BuffType<ArmorCrunch>(), 180);
                target.AddBuff(BuffID.Bleeding, 300);
            }

            if (npc.type == NPCID.SkeletronPrime && OverridingListManager.Registered(npc.type))
                target.AddBuff(BuffID.Bleeding, 420);
        }

        #endregion
    }
}