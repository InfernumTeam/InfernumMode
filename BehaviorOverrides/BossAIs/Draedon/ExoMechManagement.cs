using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.Projectiles.Boss;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.Thanatos;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using DraedonNPC = CalamityMod.NPCs.ExoMechs.Draedon;
using AresPlasmaFireballInfernum = InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares.AresPlasmaFireball;
using AresTeslaOrbInfernum = InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares.AresTeslaOrb;
using ArtemisLaserInfernum= InfernumMode.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo.ArtemisLaser;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo.ApolloBehaviorOverride;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
    public static class ExoMechManagement
    {
        public const int HasSummonedComplementMechIndex = 7;
        public const int ComplementMechIndexIndex = 10;
        public const int WasNotInitialSummonIndex = 11;
        public const int FinalMechIndexIndex = 12;
        public const int FinalPhaseTimerIndex = 16;
        public const int DeathAnimationTimerIndex = 19;
        public const int DeathAnimationHasStartedIndex = 22;
        public const int SecondaryMechNPCTypeIndex = 24;
        public const int StartingFinalPhaseAnimationHPIndex = 27;
        public const int InitialMechNPCTypeIndex = 28;

        public const int Thanatos_AttackDelayIndex = 13;
        public const int Thanatos_FinalPhaseAttackCounter = 14;

        public const int Ares_ProjectileDamageBoostIndex = 8;
        public const int Ares_EnragedIndex = 13;
        public const int Ares_BackArmsAreSwappedIndex = 15;
        public const int Ares_LineTelegraphInterpolantIndex = 17;
        public const int Ares_LineTelegraphRotationIndex = 18;

        public const int Twins_ComplementMechEnrageTimerIndex = 26;
        public const int Twins_SideSwitchDelayIndex = 18;

        public const float Phase2LifeRatio = 0.85f;
        public const float Phase3LifeRatio = 0.625f;
        public const float Phase4LifeRatio = 0.5f;
        public const int FinalPhaseTransitionTime = 290;
        public const float ComplementMechInvincibilityThreshold = 0.5f;

        public static int CurrentAresPhase
        {
            get
            {
                if (CalamityGlobalNPC.draedonExoMechPrime == -1)
                    return 0;

                NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];

                if (FindFinalMech() is null && aresBody.Infernum().ExtraAI[FinalMechIndexIndex] >= 0f)
                    return TotalMechs == 1 ? 6 : 3;
                if (FindFinalMech() == aresBody)
                    return 5;
                if (ComplementMechIsPresent(aresBody) || aresBody.Infernum().ExtraAI[HasSummonedComplementMechIndex] == 1f)
                    return 4;
                if (aresBody.life <= aresBody.lifeMax * Phase3LifeRatio)
                    return 3;
                if (aresBody.life <= aresBody.lifeMax * Phase2LifeRatio)
                    return 2;

                return 1;
            }
        }

        public static int CurrentThanatosPhase
        {
            get
            {
                if (CalamityGlobalNPC.draedonExoMechWorm == -1)
                    return 0;

                NPC thanatosHead = Main.npc[CalamityGlobalNPC.draedonExoMechWorm];

                if (FindFinalMech() is null && thanatosHead.Infernum().ExtraAI[FinalMechIndexIndex] >= 0f)
                    return TotalMechs == 1 ? 6 : 3;
                if (FindFinalMech() == thanatosHead)
                    return 5;
                if (ComplementMechIsPresent(thanatosHead) || thanatosHead.Infernum().ExtraAI[HasSummonedComplementMechIndex] == 1f)
                    return 4;
                if (thanatosHead.life <= thanatosHead.lifeMax * Phase3LifeRatio)
                    return 3;
                if (thanatosHead.life <= thanatosHead.lifeMax * Phase2LifeRatio)
                    return 2;

                return 1;
            }
        }

        public static int CurrentTwinsPhase
        {
            get
            {
                if (!NPC.AnyNPCs(ModContent.NPCType<Apollo>()))
                    return 0;

                NPC apollo = Main.npc[NPC.FindFirstNPC(ModContent.NPCType<Apollo>())];

                // Check to ensure that Apollo's phase 2 animation has finished.
                if (apollo.ai[3] < Phase2TransitionTime)
                    return 1;

                if (FindFinalMech() is null && apollo.Infernum().ExtraAI[FinalMechIndexIndex] >= 0f)
                    return TotalMechs == 1 ? 6 : 3;
                if (FindFinalMech() == apollo)
                    return 5;
                if (ComplementMechIsPresent(apollo) || apollo.Infernum().ExtraAI[HasSummonedComplementMechIndex] == 1f)
                    return 4;
                if (apollo.life <= apollo.lifeMax * Phase3LifeRatio)
                    return 3;
                if (apollo.life <= apollo.lifeMax * Phase2LifeRatio)
                    return 2;

                return 1;
            }
        }

        public static bool ExoTwinsAreEnteringSecondPhase
        {
            get
            {
                if (!NPC.AnyNPCs(ModContent.NPCType<Apollo>()))
                    return false;

                NPC apollo = Main.npc[NPC.FindFirstNPC(ModContent.NPCType<Apollo>())];
                return apollo.ai[3] is > 0f and < Phase2TransitionTime;
            }
        }

        public static bool ExoTwinsAreInSecondPhase
        {
            get
            {
                if (!NPC.AnyNPCs(ModContent.NPCType<Apollo>()))
                    return false;

                NPC apollo = Main.npc[NPC.FindFirstNPC(ModContent.NPCType<Apollo>())];
                return apollo.ai[3] >= Phase2TransitionTime;
            }
        }

        public static bool ExoMechIsPerformingDeathAnimation
        {
            get
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Utilities.IsExoMech(Main.npc[i]) && ExoMechAIUtilities.PerformingDeathAnimation(Main.npc[i]))
                        return true;
                }
                return false;
            }
        }

        // Artemis is intentionally not counted in this list due to primarily deferring to Apollo, the "leader" of the two for management.
        public static List<int> ExoMechIDs => new()
        {
            ModContent.NPCType<AresBody>(),
            ModContent.NPCType<Apollo>(),
            ModContent.NPCType<ThanatosHead>(),
        };

        public static int GetComplementMechType(NPC npc)
        {
            int secondaryMechNPCType = (int)npc.Infernum().ExtraAI[SecondaryMechNPCTypeIndex];
            if (npc.type == ModContent.NPCType<ThanatosHead>())
                return secondaryMechNPCType;

            if (npc.type == ModContent.NPCType<AresBody>())
                return secondaryMechNPCType;

            if (npc.type == ModContent.NPCType<Apollo>() || npc.type == ModContent.NPCType<Artemis>())
                return secondaryMechNPCType;
            return 0;
        }

        public static int GetFinalMechType(NPC npc)
        {
            int secondaryMechNPCType = (int)npc.Infernum().ExtraAI[SecondaryMechNPCTypeIndex];
            List<int> mechsInUse = ExoMechIDs;
            mechsInUse.Remove(npc.type);
            mechsInUse.Remove(secondaryMechNPCType);
            return mechsInUse.First();
        }

        public static bool ComplementMechIsPresent(NPC npc) => NPC.AnyNPCs(GetComplementMechType(npc));

        public static bool ShouldHaveSecondComboPhaseResistance(NPC npc)
        {
            if (npc.realLife >= 0)
                return ShouldHaveSecondComboPhaseResistance(Main.npc[npc.realLife]);

            return FindFinalMech() is null && npc.Infernum().ExtraAI[FinalMechIndexIndex] >= 0f && TotalMechs > 1 && npc == FindInitialMech();
        }

        public static NPC FindInitialMech()
        {
            NPC initialMech = null;

            // Find the initial mech. If it cannot be found, return nothing.
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (!ExoMechIDs.Contains(Main.npc[i].type))
                    continue;

                if (!Main.npc[i].active)
                    continue;

                if (Main.npc[i].Infernum().ExtraAI[WasNotInitialSummonIndex] == 0f)
                {
                    initialMech = Main.npc[i];
                    break;
                }
            }

            return initialMech;
        }

        public static NPC FindFinalMech()
        {
            NPC initialMech = FindInitialMech();

            if (initialMech is null)
                return null;

            // Check to see if the initial mech believes that the final mech index is in use by a mech.
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (!ExoMechIDs.Contains(Main.npc[i].type))
                    continue;

                if (!Main.npc[i].active)
                    continue;

                if (initialMech.Infernum().ExtraAI[FinalMechIndexIndex] == i)
                    return Main.npc[i];
            }
            return null;
        }

        public static void ClearAwayTransitionProjectiles()
        {
            // Clear away old projectiles.
            int[] projectilesToDelete = new int[]
            {
                ModContent.ProjectileType<ApolloAcceleratingPlasmaSpark>(),
                ModContent.ProjectileType<ApolloFlamethrower>(),
                ModContent.ProjectileType<ApolloPlasmaFireball>(),
                ModContent.ProjectileType<ApolloRocket>(),
                ModContent.ProjectileType<ApolloRocketInfernum>(),
                ModContent.ProjectileType<ApolloTelegraphedPlasmaSpark>(),
                ModContent.ProjectileType<AresBeamExplosion>(),
                ModContent.ProjectileType<AresCannonLaser>(),
                ModContent.ProjectileType<AresGaussNukeProjectile>(),
                ModContent.ProjectileType<AresGaussNukeProjectileBoom>(),
                ModContent.ProjectileType<AresGaussNukeProjectileSpark>(),
                ModContent.ProjectileType<AresLaserDeathray>(),
                ModContent.ProjectileType<AresPlasmaFireballInfernum>(),
                ModContent.ProjectileType<AresPlasmaBolt>(),
                ModContent.ProjectileType<AresPulseBlast>(),
                ModContent.ProjectileType<AresPulseDeathray>(),
                ModContent.ProjectileType<AresSpinningDeathBeam>(),
                ModContent.ProjectileType<AresSpinningRedDeathray>(),
                ModContent.ProjectileType<AresTeslaOrbInfernum>(),
                ModContent.ProjectileType<AresTeslaSpark>(),
                ModContent.ProjectileType<AresTeslaGasField>(),
                ModContent.ProjectileType<ArtemisGasFireballBlast>(),
                ModContent.ProjectileType<ArtemisGatlingLaser>(),
                ModContent.ProjectileType<ArtemisLaserInfernum>(),
                ModContent.ProjectileType<ArtemisSpinLaser>(),
                ModContent.ProjectileType<ArtemisSweepLaserbeam>(),
                ModContent.ProjectileType<DetatchedThanatosLaser>(),
                ModContent.ProjectileType<ExoburstSpark>(),
                ModContent.ProjectileType<ExolaserBomb>(),
                ModContent.ProjectileType<ExolaserSpark>(),
                ModContent.ProjectileType<LightOverloadRay>(),
                ModContent.ProjectileType<PhotonRipperCrystal>(),
                ModContent.ProjectileType<PlasmaGas>(),
                ModContent.ProjectileType<RefractionRotor>(),
                ModContent.ProjectileType<SmallPlasmaSpark>(),
                ModContent.ProjectileType<SuperheatedExofireGas>(),
                ModContent.ProjectileType<ThanatosAresComboLaser>()
            };
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (projectilesToDelete.Contains(Main.projectile[i].type))
                    Main.projectile[i].active = false;
            }
        }

        public static void SummonComplementMech(NPC npc)
        {
            MakeDraedonSayThings(1);

            // Don't summon NPCs clientside.
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Clear away projectiles.
            ClearAwayTransitionProjectiles();

            int complementMechType = GetComplementMechType(npc);
            if (npc.type == ModContent.NPCType<Artemis>())
                return;

            Vector2 mechSpawnPosition = Main.player[npc.target].Center - Vector2.UnitY * 1500f;
            int complementMechIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)mechSpawnPosition.X, (int)mechSpawnPosition.Y, complementMechType, 1);
            NPC complementMech = Main.npc[complementMechIndex];

            // Tell the newly summoned mech that it is not the initial mech and that it cannot summon more mechs on its own.
            complementMech.Infernum().ExtraAI[HasSummonedComplementMechIndex] = 1f;
            complementMech.Infernum().ExtraAI[WasNotInitialSummonIndex] = 1f;
            complementMech.Infernum().ExtraAI[InitialMechNPCTypeIndex] = npc.Infernum().ExtraAI[InitialMechNPCTypeIndex];
            complementMech.Infernum().ExtraAI[SecondaryMechNPCTypeIndex] = npc.Infernum().ExtraAI[SecondaryMechNPCTypeIndex];
            complementMech.velocity = complementMech.SafeDirectionTo(Main.player[npc.target].Center) * 40f;
            complementMech.Opacity = 0.01f;
            complementMech.netUpdate = true;

            npc.Infernum().ExtraAI[WasNotInitialSummonIndex] = 0f;
            npc.Infernum().ExtraAI[ComplementMechIndexIndex] = complementMechIndex;
            npc.netUpdate = true;
        }

        public static void SummonFinalMech(NPC npc)
        {
            MakeDraedonSayThings(3);

            // Don't summon NPCs clientside.
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Clear away projectiles.
            ClearAwayTransitionProjectiles();

            int finalMechType = GetFinalMechType(npc);
            if (npc.type == ModContent.NPCType<Artemis>())
                return;

            Vector2 mechSpawnPosition = Main.player[npc.target].Center - Vector2.UnitY * 2100f;
            int finalMechIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)mechSpawnPosition.X, (int)mechSpawnPosition.Y, finalMechType, 1);
            NPC finalMech = Main.npc[finalMechIndex];
            npc.Infernum().ExtraAI[FinalMechIndexIndex] = finalMechIndex;
            Main.npc[(int)npc.Infernum().ExtraAI[ComplementMechIndexIndex]].Infernum().ExtraAI[FinalMechIndexIndex] = finalMechIndex;

            // Tell the newly summoned mech that it is not the initial mech and that it cannot summon more mechs on its own.
            finalMech.Infernum().ExtraAI[HasSummonedComplementMechIndex] = 1f;
            finalMech.Infernum().ExtraAI[WasNotInitialSummonIndex] = 1f;

            finalMech.netUpdate = true;
        }

        public static int TotalMechs
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (!ExoMechIDs.Contains(Main.npc[i].type))
                        continue;

                    if (!Main.npc[i].active || ExoMechAIUtilities.ShouldExoMechVanish(Main.npc[i]))
                        continue;

                    count++;
                }

                return count;
            }
        }

        public static void MakeDraedonSayThings(int statementType)
        {
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.draedon))
                return;

            if (Main.npc[CalamityGlobalNPC.draedon].localAI[0] < statementType)
            {
                Main.npc[CalamityGlobalNPC.draedon].localAI[0] = statementType;
                Main.npc[CalamityGlobalNPC.draedon].ai[0] = DraedonNPC.ExoMechPhaseDialogueTime;
            }
        }

        public static bool HandleDeathEffects(NPC npc)
        {
            bool hasPerformedDeathAnimation = npc.Infernum().ExtraAI[DeathAnimationHasStartedIndex] != 0f;
            if (npc.realLife >= 0)
                hasPerformedDeathAnimation = Main.npc[npc.realLife].Infernum().ExtraAI[DeathAnimationHasStartedIndex] != 0f;

            // Execute battle event triggers if the exo mech in question has finished its death animation.
            if (hasPerformedDeathAnimation)
            {
                bool finalMechKilled = FindFinalMech() == npc;
                if (npc.realLife >= 0)
                    finalMechKilled = FindFinalMech() == Main.npc[npc.realLife];
                if (finalMechKilled)
                    MakeDraedonSayThings(4);
                else if (TotalMechs - 1 == 1)
                    MakeDraedonSayThings(5);
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
                    Main.npc[npc.realLife].Infernum().ExtraAI[DeathAnimationHasStartedIndex] = 1f;
                    Main.npc[npc.realLife].netUpdate = true;
                    Main.npc[npc.realLife].UpdateNPC(npc.realLife);
                }
                else
                {
                    npc.Infernum().ExtraAI[DeathAnimationHasStartedIndex] = 1f;
                    
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
                ClearAwayTransitionProjectiles();

                return false;
            }
            return true;
        }
    }
}
