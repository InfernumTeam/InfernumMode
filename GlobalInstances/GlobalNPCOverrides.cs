using CalamityMod;
using CalamityMod.Buffs.StatBuffs;
using CalamityMod.NPCs;
using CalamityMod.NPCs.AstrumAureus;
using CalamityMod.NPCs.Bumblebirb;
using CalamityMod.NPCs.CeaselessVoid;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.ExoMechs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.NPCs.GreatSandShark;
using CalamityMod.NPCs.Providence;
using CalamityMod.NPCs.SlimeGod;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.NPCs.Yharon;
using CalamityMod.UI;
using InfernumMode.Balancing;
using InfernumMode.BehaviorOverrides.BossAIs.CeaselessVoid;
using InfernumMode.BehaviorOverrides.BossAIs.Cultist;
using InfernumMode.BehaviorOverrides.BossAIs.DoG;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.Thanatos;
using InfernumMode.BehaviorOverrides.BossAIs.EoW;
using InfernumMode.BehaviorOverrides.BossAIs.MoonLord;
using InfernumMode.BehaviorOverrides.BossAIs.SlimeGod;
using InfernumMode.BehaviorOverrides.BossAIs.WallOfFlesh;
using InfernumMode.OverridingSystem;
using InfernumMode.Sounds;
using InfernumMode.Subworlds;
using InfernumMode.Systems;
using Microsoft.Xna.Framework;
using SubworldLibrary;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using CryogenNPC = CalamityMod.NPCs.Cryogen.Cryogen;
using OldDukeNPC = CalamityMod.NPCs.OldDuke.OldDuke;
using PolterghastNPC = CalamityMod.NPCs.Polterghast.Polterghast;

namespace InfernumMode.GlobalInstances
{
    public partial class GlobalNPCOverrides : GlobalNPC
    {
        #region Instance and Variables
        public override bool InstancePerEntity => true;

        public const int TotalExtraAISlots = 100;

        // I'll be fucking damned if this isn't enough
        public float[] ExtraAI = new float[TotalExtraAISlots];
        public Rectangle Arena = default;
        public PrimitiveTrailCopy OptionalPrimitiveDrawer;

        internal static int Cryogen = -1;
        internal static int AstrumAureus = -1;
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
            ResetSavedIndex(ref Yharon, ModContent.NPCType<Yharon>());
        }
        #endregion Reset Effects

        #region Overrides

        public override void SetDefaults(NPC npc)
        {
            for (int i = 0; i < ExtraAI.Length; i++)
                ExtraAI[i] = 0f;

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
                return EoWHeadBehaviorOverride.PerformDeathEffect(npc);

            if (npc.type == ModContent.NPCType<OldDukeNPC>() && OverridingListManager.Registered(npc.type))
                CalamityMod.CalamityMod.StopRain();

            int apolloID = ModContent.NPCType<Apollo>();
            int thanatosID = ModContent.NPCType<ThanatosHead>();
            int aresID = ModContent.NPCType<AresBody>();
            int totalExoMechs = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].type != apolloID && Main.npc[i].type != thanatosID && Main.npc[i].type != aresID)
                    continue;
                if (!Main.npc[i].active)
                    continue;

                totalExoMechs++;
            }
            if (InfernumMode.CanUseCustomAIs && totalExoMechs >= 2 && Utilities.IsExoMech(npc) && OverridingListManager.Registered<Apollo>())
                return false;

            // Prevent wandering eye fishes from dropping loot if they were spawned by a dreadnautilus.
            if (InfernumMode.CanUseCustomAIs && npc.type == NPCID.EyeballFlyingFish && NPC.AnyNPCs(NPCID.BloodNautilus))
                DropHelper.BlockDrops(ItemID.ChumBucket, ItemID.VampireFrogStaff, ItemID.BloodFishingRod, ItemID.BloodRainBow, ItemID.MoneyTrough, ItemID.BloodMoonStarter);

            return base.PreKill(npc);
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

            if (npc.type == NPCID.MoonLordCore && !WorldSaveSystem.HasGeneratedProfanedShrine)
            {
                Utilities.DisplayText("A profaned shrine has erupted from the ashes at the underworld's edge!", Color.Orange);
                WorldgenSystem.GenerateProfanedArena(new(), new(new()));
                WorldSaveSystem.HasGeneratedProfanedShrine = true;
            }

            if (npc.type == ModContent.NPCType<Providence>())
            {
                if (!Main.dayTime && !WorldSaveSystem.HasBeatedInfernumProvRegularly)
                    WorldSaveSystem.HasBeatedInfernumNightProvBeforeDay = true;
                WorldSaveSystem.HasBeatedInfernumProvRegularly = true;
                CalamityNetcode.SyncWorld();
            }
        }

        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (player.Infernum().ZoneProfaned || SubworldSystem.IsActive<LostColosseum>())
            {
                spawnRate *= 40000;
                maxSpawns = 0;
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
            if (!InfernumMode.CanUseCustomAIs)
                return base.CheckDead(npc);

            if (npc.type == NPCID.WallofFleshEye && OverridingListManager.Registered(NPCID.WallofFlesh))
                return WallOfFleshEyeBehaviorOverride.HandleDeathEffects(npc);

            if (npc.type == ModContent.NPCType<DevourerofGodsHead>() && OverridingListManager.Registered(npc.type))
            {
                npc.life = 1;
                npc.dontTakeDamage = true;
                npc.active = true;
                npc.netUpdate = true;
                return false;
            }

            if (npc.type == ModContent.NPCType<PolterghastNPC>() && OverridingListManager.Registered(npc.type) && npc.Infernum().ExtraAI[11] == 0f)
            {
                if (npc.Infernum().ExtraAI[6] > 0f)
                    return true;

                npc.Infernum().ExtraAI[6] = 1f;
                npc.Infernum().ExtraAI[11] = 1f;
                npc.life = 1;
                npc.netUpdate = true;
                npc.dontTakeDamage = true;

                return false;
            }

            if (npc.type == ModContent.NPCType<Bumblefuck2>() && OverridingListManager.Registered<Bumblefuck>())
            {
                if (npc.ai[0] != 3f && npc.ai[3] > 0f)
                {
                    npc.life = npc.lifeMax;
                    npc.dontTakeDamage = true;
                    npc.ai[0] = 3f;
                    npc.ai[1] = 0f;
                    npc.ai[2] = 0f;
                    npc.netUpdate = true;
                }
                return false;
            }

            if ((npc.type is NPCID.Spazmatism or NPCID.Retinazer) && OverridingListManager.Registered(NPCID.Spazmatism))
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

            if (npc.type == NPCID.CultistBoss && OverridingListManager.Registered(npc.type))
            {
                CultistBehaviorOverride.ClearAwayEntities();
                npc.Infernum().ExtraAI[6] = 1f;
                npc.active = true;
                npc.dontTakeDamage = true;
                npc.life = 1;
                npc.ai[1] = 0f;
                npc.netUpdate = true;

                return false;
            }

            if (npc.type == ModContent.NPCType<SupremeCalamitas>() && OverridingListManager.Registered(npc.type))
            {
                npc.active = true;
                npc.dontTakeDamage = true;
                npc.life = 1;
                npc.Infernum().ExtraAI[7] = 1f;
                npc.netUpdate = true;

                return false;
            }

            if (npc.type == ModContent.NPCType<CeaselessVoid>() && OverridingListManager.Registered(npc.type))
            {
                CeaselessVoidBehaviorOverride.HandleDeathStuff(npc);
                return false;
            }

            if (Utilities.IsExoMech(npc) && OverridingListManager.Registered<Apollo>())
            {
                bool hasPerformedDeathAnimation = npc.Infernum().ExtraAI[ExoMechManagement.DeathAnimationHasStartedIndex] != 0f;
                if (npc.realLife >= 0)
                    hasPerformedDeathAnimation = Main.npc[npc.realLife].Infernum().ExtraAI[ExoMechManagement.DeathAnimationHasStartedIndex] != 0f;

                // Execute battle event triggers if the exo mech in question has finished its death animation.
                if (hasPerformedDeathAnimation)
                {
                    bool finalMechKilled = ExoMechManagement.FindFinalMech() == npc;
                    if (npc.realLife >= 0)
                        finalMechKilled = ExoMechManagement.FindFinalMech() == Main.npc[npc.realLife];
                    if (finalMechKilled)
                        ExoMechManagement.MakeDraedonSayThings(4);
                    else if (ExoMechManagement.TotalMechs - 1 == 1)
                        ExoMechManagement.MakeDraedonSayThings(5);
                }

                // Otherwise, trigger the exo mech's death animation.
                // Once it ends this code will be called again.
                else
                {
                    npc.life = npc.lifeMax;
                    npc.dontTakeDamage = true;
                    npc.active = true;
                    if (npc.realLife >= 0)
                    {
                        Main.npc[npc.realLife].life = Main.npc[npc.realLife].lifeMax;
                        Main.npc[npc.realLife].dontTakeDamage = true;
                        Main.npc[npc.realLife].active = true;
                        Main.npc[npc.realLife].Infernum().ExtraAI[ExoMechManagement.DeathAnimationHasStartedIndex] = 1f;
                        Main.npc[npc.realLife].netUpdate = true;
                        Main.npc[npc.realLife].UpdateNPC(npc.realLife);
                    }
                    else
                    {
                        npc.Infernum().ExtraAI[ExoMechManagement.DeathAnimationHasStartedIndex] = 1f;

                        // If Apollo is the one being checked, ensure that Artemis stays alive.
                        if (npc.type == ModContent.NPCType<Apollo>())
                        {
                            int artemisID = ModContent.NPCType<Artemis>();
                            for (int i = 0; i < Main.maxNPCs; i++)
                            {
                                if (Main.npc[i].type == artemisID && Main.npc[i].realLife == npc.whoAmI)
                                {
                                    Main.npc[i].life = npc.life;
                                    Main.npc[i].active = true;
                                }
                            }
                        }
                        npc.UpdateNPC(npc.whoAmI);
                    }

                    npc.netUpdate = true;
                    ExoMechManagement.ClearAwayTransitionProjectiles();

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
            if (npc.type == NPCID.AncientCultistSquidhead && OverridingListManager.Registered(NPCID.CultistBoss))
                return false;
            if (npc.type == NPCID.MoonLordFreeEye && OverridingListManager.Registered(NPCID.MoonLordCore))
                return false;

            return base.CheckActive(npc);
        }

        #endregion
    }
}