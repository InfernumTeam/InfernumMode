using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Perforator;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Boss;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Content.BehaviorOverrides.BossAIs.BoC;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Perforators
{
    public class PerforatorHiveBehaviorOverride : NPCBehaviorOverride
    {
        public enum PerforatorHiveAttackState
        {
            DiagonalBloodCharge,
            HorizontalCrimeraSpawnCharge,
            IchorBlasts,
            IchorSpinDash,
            SmallWormBursts,
            CrimeraWalls,
            MediumWormBursts,
            IchorRain,
            LargeWormBursts,
            IchorFountainCharge
        }

        public static int ToothBallDamage => 85;

        public static int CrimeraWallDamage => 90;

        public static int BloodSpitDamage => 95;

        public static int IchorBlobDamage => 95;

        public static int IchorSpitDamage => 95;

        public const int DeathTimerIndex = 5;

        public const int DeathAnimationBasePositionXIndex = 6;

        public const int DeathAnimationBasePositionYIndex = 7;

        public const int HasSpawnedLegsIndex = 8;

        public const int DeathAnimationLength = 160;

        public const float Phase2LifeRatio = 0.7f;

        public const float Phase3LifeRatio = 0.5f;

        public const float Phase4LifeRatio = 0.25f;

        public override int NPCOverrideType => ModContent.NPCType<PerforatorHive>();

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio,
            Phase3LifeRatio,
            Phase4LifeRatio
        };

        #region AI
        public override void SetDefaults(NPC npc)
        {
            // Set defaults that, if were to be changed by Calamity, would cause significant issues to the fight.
            npc.width = 110;
            npc.height = 100;
            npc.scale = 1f;
            npc.defense = 4;
        }

        public override bool PreAI(NPC npc)
        {
            // Set a global whoAmI variable.
            CalamityGlobalNPC.perfHive = npc.whoAmI;

            // Set damage.
            npc.defDamage = 75;
            npc.damage = npc.defDamage;
            npc.dontTakeDamage = false;

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float wormSummonState = ref npc.ai[2];
            ref float finalPhaseTransitionTimer = ref npc.ai[3];
            ref float backafterimageGlowInterpolant = ref npc.localAI[0];
            ref float backgroundStrength = ref npc.localAI[1];
            ref float deathTimer = ref npc.Infernum().ExtraAI[DeathTimerIndex];

            // Reset certain things.
            npc.Calamity().DR = 0.1f;
            backafterimageGlowInterpolant = Clamp(backafterimageGlowInterpolant - 0.1f, 0f, 1f);

            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
            if (!Main.player.IndexInRange(npc.target) || !Main.player[npc.target].active || Main.player[npc.target].dead || !npc.WithinRange(Main.player[npc.target].Center, 6400f))
            {
                DoDespawnEffects(npc);
                return false;
            }

            bool inPhase2 = lifeRatio <= Phase2LifeRatio;
            bool inPhase3 = lifeRatio <= Phase3LifeRatio;
            bool inPhase4 = lifeRatio <= Phase4LifeRatio;
            Player target = Main.player[npc.target];

            // Prepare worm summon states.
            HandleWormPhaseTriggers(npc, inPhase2, inPhase3, inPhase4, ref attackState, ref wormSummonState);

            // Calculate rotation, if not performing the death animation.
            if (deathTimer <= 0f)
                npc.rotation = Clamp(npc.velocity.X * 0.04f, -Pi / 6f, Pi / 6f);

            // Make the background glow crimson in the form phase, once the large worm is dead.
            if (inPhase4 && attackState != (int)PerforatorHiveAttackState.LargeWormBursts)
            {
                // Handle transition effects.
                if (finalPhaseTransitionTimer < 180f)
                {
                    // Slow down dramatically at first.
                    if (finalPhaseTransitionTimer < 90f)
                        npc.velocity *= 0.93f;

                    // Rise upward and create an explosion sound.
                    if (finalPhaseTransitionTimer == 45f)
                    {
                        HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.PerforatorsFinalPhaseTip");
                        SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                        SoundEngine.PlaySound(SoundID.DD2_BetsyFireballImpact with { Volume = 2.2f, Pitch = -0.4f }, npc.Center);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<PerforatorWave>(), 0, 0f);

                        npc.velocity = -Vector2.UnitY * 12f;
                        npc.netUpdate = true;
                    }
                }

                finalPhaseTransitionTimer++;

                backgroundStrength = Utils.GetLerpValue(45f, 150f, finalPhaseTransitionTimer, true);
            }

            // I'm so fucking tired, man.
            if (target.HasBuff(ModContent.BuffType<BurningBlood>()))
                target.ClearBuff(ModContent.BuffType<BurningBlood>());

            if (deathTimer > 0)
            {
                npc.Calamity().ShouldCloseHPBar = true;
                DoBehavior_DeathAnimation(npc, target, ref deathTimer);
                deathTimer++;
                return false;
            }

            switch ((PerforatorHiveAttackState)attackState)
            {
                case PerforatorHiveAttackState.DiagonalBloodCharge:
                    DoBehavior_DiagonalBloodCharge(npc, target, inPhase2, inPhase3, inPhase4, ref attackTimer);
                    break;
                case PerforatorHiveAttackState.HorizontalCrimeraSpawnCharge:
                    DoBehavior_HorizontalCrimeraSpawnCharge(npc, target, ref attackTimer);
                    break;
                case PerforatorHiveAttackState.IchorBlasts:
                    DoBehavior_IchorBlasts(npc, target, inPhase2, ref attackTimer);
                    break;
                case PerforatorHiveAttackState.IchorSpinDash:
                    DoBehavior_IchorSpinDash(npc, target, inPhase2, inPhase3, inPhase4, ref attackTimer);
                    break;
                case PerforatorHiveAttackState.SmallWormBursts:
                    DoBehavior_SmallWormBursts(npc, target, ref attackTimer);
                    break;
                case PerforatorHiveAttackState.CrimeraWalls:
                    DoBehavior_CrimeraWalls(npc, target, inPhase3, inPhase4, ref attackTimer);
                    break;
                case PerforatorHiveAttackState.MediumWormBursts:
                    DoBehavior_MediumWormBursts(npc, target, ref attackTimer, ref backafterimageGlowInterpolant);
                    break;
                case PerforatorHiveAttackState.IchorRain:
                    DoBehavior_IchorRain(npc, target, inPhase4, ref attackTimer);
                    break;
                case PerforatorHiveAttackState.LargeWormBursts:
                    DoBehavior_LargeWormBursts(npc, target, ref attackTimer);
                    break;
                case PerforatorHiveAttackState.IchorFountainCharge:
                    DoBehavior_IchorFountainCharge(npc, target, ref attackTimer);
                    break;
            }

            attackTimer++;
            return false;
        }

        public static void DoBehavior_DeathAnimation(NPC npc, Player player, ref float deathTimer)
        {
            int flinchInterval = 30;
            float animationCompletion = deathTimer / DeathAnimationLength;
            int bloodReleaseRate = (int)Lerp(1f, 4f, animationCompletion);
            ref float basePositionX = ref npc.Infernum().ExtraAI[DeathAnimationBasePositionXIndex];
            ref float basePositionY = ref npc.Infernum().ExtraAI[DeathAnimationBasePositionYIndex];

            // Don't deal or take any damage.
            npc.dontTakeDamage = true;
            npc.damage = 0;

            // Rapidly slow to a halt.
            if (npc.velocity != Vector2.Zero)
                npc.velocity *= 0.75f;

            // Play the sound, and save the original position.
            if (deathTimer == 1)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.PerforatorDeathAnimation with { Volume = 1.2f }, npc.Center);
                basePositionX = npc.Center.X;
                basePositionY = npc.Center.Y;

                // Clear any leftover projectiles.
                CleanUpStrayProjectiles();
            }

            // Screenshake.
            Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = animationCompletion * 8f;

            // After a second, start releasing blood everywhere.
            if (deathTimer >= 10)
            {
                for (int i = 0; i < bloodReleaseRate; i++)
                {
                    Vector2 bloodSpawnPosition = npc.Center;
                    Vector2 bloodVelocity = Main.rand.NextVector2CircularEdge(40, 40);
                    Particle bloodParticle = new EoCBloodParticle(bloodSpawnPosition, bloodVelocity * 0.5f, 20, Main.rand.NextFloat(0.9f, 1.2f), (Main.rand.NextBool(3) ? Color.Gold : Color.Crimson) * 0.75f, 10);
                    GeneralParticleHandler.SpawnParticle(bloodParticle);
                }
            }

            if (deathTimer % flinchInterval == 0)
            {
                // Move slightly to emulate flinching from something inside the hive.
                npc.velocity = npc.SafeDirectionTo(player.Center).RotatedByRandom(TwoPi) * 6;
            }
            if (deathTimer % flinchInterval + 20 == 0)
            {
                // Snap back to the original position.
                npc.velocity = Vector2.Zero;
                npc.Center = new(basePositionX, basePositionY);
            }

            // Spawn a wave.
            if (deathTimer == DeathAnimationLength - 10)
            {
                SoundEngine.PlaySound(SoundID.ForceRoarPitched, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Utilities.CreateShockwave(npc.Center, 40, 4, 40f, false);
                }
            }

            // Die and drop loot.
            if (deathTimer >= DeathAnimationLength)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath19 with { Volume = 1.2f }, npc.Center);
                npc.NPCLoot();
                npc.active = false;

                // Spawn visual stuff
                // Spawn a few larger blood splatters
                for (int i = 0; i < 3; i++)
                {
                    Vector2 bloodVelocity = Main.rand.NextVector2CircularEdge(40, 40);
                    Particle bloodSplatter = new BloodParticle2(npc.Center, bloodVelocity * 0.05f, 30, Main.rand.NextFloat(0.85f, 1f), Color.Crimson);
                    GeneralParticleHandler.SpawnParticle(bloodSplatter);
                }

                // Spawn Gores
                if (Main.netMode != NetmodeID.Server)
                {
                    int goreAmount = 4;
                    for (int i = 1; i <= goreAmount; i++)
                    {
                        Vector2 goreVelocity = npc.SafeDirectionTo(player.Center).RotatedByRandom(TwoPi) * 3;
                        Gore.NewGore(npc.GetSource_FromAI(), npc.position, goreVelocity, InfernumMode.Instance.Find<ModGore>("Perf" + i).Type, 1f);
                    }
                }

                // Spawn harmless projectiles, and a bunch of blood.
                for (int i = 0; i < 12; i++)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 velocity = -Vector2.UnitY.RotatedByRandom(1.3f) * Main.rand.NextFloat(2f, 3.5f);
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, velocity * 3, ModContent.ProjectileType<IchorBlob>(), 0, 0, player.whoAmI);
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, -velocity, ModContent.ProjectileType<IchorBlob>(), 0, 0, player.whoAmI);
                    }

                    for (int j = 0; j < 6; j++)
                    {
                        Vector2 bloodVelocity = Main.rand.NextVector2Circular(40, 40) * Main.rand.NextFloat(1f, 1.3f);

                        Particle bloodParticle = new EoCBloodParticle(npc.Center, bloodVelocity * 0.5f, 120, Main.rand.NextFloat(0.9f, 1.2f), (Main.rand.NextBool() ? Color.Crimson : Color.Gold) * 0.75f);
                        GeneralParticleHandler.SpawnParticle(bloodParticle);
                    }
                }

                if (Main.netMode == NetmodeID.MultiplayerClient)
                    return;

                Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<PerforatorWave>(), 0, 0, player.whoAmI);

                // Spawn some enemies to make it appear they burst out of the hive.
                int crimeraCount = Main.rand.Next(3, 6);
                int leechCount = Main.rand.Next(2, 4);

                for (int i = 0; i < crimeraCount; i++)
                {
                    Vector2 offset = Main.rand.NextVector2Circular(120, 120);
                    int crimera = NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X + offset.X), (int)(npc.Center.Y + offset.Y), NPCID.Crimera);
                    NPC crimeraNPC = Main.npc[crimera];
                    crimeraNPC.velocity = npc.SafeDirectionTo(crimeraNPC.Center).SafeNormalize(Vector2.One) * 9;
                    crimeraNPC.target = npc.target;
                }
                for (int i = 0; i < leechCount; i++)
                {
                    Vector2 offset = Main.rand.NextVector2Circular(120, 120);
                    int leech = NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X + offset.X), (int)(npc.Center.Y + offset.Y), NPCID.GiantWormHead);
                    NPC leechNPC = Main.npc[leech];
                    leechNPC.velocity = npc.SafeDirectionTo(leechNPC.Center).SafeNormalize(Vector2.One) * 12;
                    leechNPC.target = npc.target;
                }
            }
        }

        public static void HandleWormPhaseTriggers(NPC npc, bool inPhase2, bool inPhase3, bool inPhase4, ref float attackState, ref float wormSummonState)
        {
            // Small worm phase.
            if (inPhase2 && wormSummonState == 0f)
            {
                SelectNextAttack(npc);
                CleanUpStrayProjectiles();
                attackState = (int)PerforatorHiveAttackState.SmallWormBursts;
                wormSummonState = 1f;
                return;
            }

            // Medium worm phase.
            if (inPhase3 && wormSummonState == 1f)
            {
                SelectNextAttack(npc);
                CleanUpStrayProjectiles();
                attackState = (int)PerforatorHiveAttackState.MediumWormBursts;
                wormSummonState = 2f;
                return;
            }

            // Large worm phase.
            if (inPhase4 && wormSummonState == 2f)
            {
                SelectNextAttack(npc);
                CleanUpStrayProjectiles();
                attackState = (int)PerforatorHiveAttackState.LargeWormBursts;
                wormSummonState = 3f;
                return;
            }
        }

        public static void MakeWormEruptFromHive(NPC npc, Vector2 eruptDirection, float splatterIntensity, int wormHeadID)
        {
            Vector2 bloodSpawnPosition = npc.Center + Main.rand.NextVector2Circular(npc.width, npc.height) * 0.04f + eruptDirection * 50f;

            // Create a bunch of blood particles.
            for (int i = 0; i < 21; i++)
            {
                int bloodLifetime = Main.rand.Next(33, 54);
                float bloodScale = Main.rand.NextFloat(0.7f, 0.95f);
                Color bloodColor = Color.Lerp(Color.Red, Color.DarkRed, Main.rand.NextFloat());
                bloodColor = Color.Lerp(bloodColor, new Color(51, 22, 94), Main.rand.NextFloat(0.65f));

                if (Main.rand.NextBool(20))
                    bloodScale *= 2f;

                Vector2 bloodVelocity = eruptDirection.RotatedByRandom(0.81f) * splatterIntensity * Main.rand.NextFloat(11f, 23f);
                bloodVelocity.Y -= splatterIntensity * 12f;
                BloodParticle blood = new(bloodSpawnPosition, bloodVelocity, bloodLifetime, bloodScale, bloodColor);
                GeneralParticleHandler.SpawnParticle(blood);
            }
            for (int i = 0; i < 10; i++)
            {
                float bloodScale = Main.rand.NextFloat(0.35f, 0.4f);
                Color bloodColor = Color.Lerp(Color.Red, Color.DarkRed, Main.rand.NextFloat(0.5f, 1f));
                Vector2 bloodVelocity = eruptDirection.RotatedByRandom(0.9f) * splatterIntensity * Main.rand.NextFloat(9f, 14.5f);
                BloodParticle2 blood = new(bloodSpawnPosition, bloodVelocity, 35, bloodScale, bloodColor);
                GeneralParticleHandler.SpawnParticle(blood);
            }

            // Spawn the Worm Bosstm.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, wormHeadID, 1);

                // Reel back in pain, indicating that the worm physically burrowed out of the hive.
                npc.velocity = eruptDirection * -8f;
                npc.netUpdate = true;
            }
        }

        public static void DoDespawnEffects(NPC npc)
        {
            npc.damage = 0;
            npc.velocity = Vector2.Lerp(npc.Center, Vector2.UnitY * 21f, 0.08f);
            if (npc.timeLeft > 225)
                npc.timeLeft = 225;
        }

        public static void CleanUpStrayProjectiles()
        {
            Utilities.DeleteAllProjectiles(true,
                ModContent.ProjectileType<FallingIchor>(),
                ModContent.ProjectileType<FlyingIchor>(),
                ModContent.ProjectileType<IchorBlast>(),
                ModContent.ProjectileType<IchorSpit>(),
                ModContent.ProjectileType<ToothBall>(),
                ModContent.ProjectileType<IchorBlob>());
        }

        public static void DoBehavior_DiagonalBloodCharge(NPC npc, Player target, bool inPhase2, bool inPhase3, bool inPhase4, ref float attackTimer)
        {
            int chargeDelay = 55;
            int burstIchorCount = 5;
            int fallingIchorCount = 8;
            int chargeTime = 45;
            int chargeCount = 3;
            float chargeSpeed = 20f;
            float maxHorizontalSpeed = 16f;

            if (inPhase2)
            {
                chargeDelay -= 8;
                chargeTime -= 4;
                chargeSpeed += 2.75f;
            }
            if (inPhase3)
            {
                chargeCount--;
                fallingIchorCount += 2;
                chargeSpeed += 1.5f;
            }
            if (inPhase4)
            {
                chargeDelay -= 5;
                fallingIchorCount += 2;
                burstIchorCount += 2;
            }
            if (BossRushEvent.BossRushActive)
            {
                chargeDelay -= 23;
                chargeTime -= 17;
                chargeSpeed *= 1.7f;
                maxHorizontalSpeed += 13f;
                fallingIchorCount += 11;
            }

            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[1];

            // Hover into position.
            if (attackSubstate == 0f)
            {
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 350f, -200f);
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 20f;

                npc.SimpleFlyMovement(idealVelocity, idealVelocity.Length() / 12f);
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.032f);

                // Slow down and go to the next attack substate if sufficiently close to the hover destination.
                if (npc.WithinRange(hoverDestination, 75f))
                {
                    attackTimer = 0f;
                    attackSubstate = 1f;
                    npc.velocity *= 0.5f;
                    npc.netUpdate = true;
                }

                // Give up and perform a different attack if unable to reach the hover destination in time.
                if (attackTimer >= 480f)
                    SelectNextAttack(npc);
            }

            // Slow down in anticipation of a charge.
            if (attackSubstate == 1f)
            {
                npc.velocity *= 0.925f;

                // Create blood pulses periodically as an indicator of charging.
                if (attackTimer % 14f == 0f)
                {
                    for (int i = -4; i <= 4; i++)
                    {
                        if (i == 0)
                            continue;
                        Vector2 offsetDirection = Vector2.UnitY.RotatedBy(i * 0.22f + Main.rand.NextFloat(-0.32f, 0.32f));
                        Vector2 baseSpawnPosition = npc.Center + offsetDirection * 180;
                        for (int j = 0; j < 8; j++)
                        {
                            Vector2 dustSpawnPosition = baseSpawnPosition + Main.rand.NextVector2Circular(9f, 9f);
                            Vector2 dustVelocity = (npc.Center - dustSpawnPosition) * 0.07f;

                            Dust blood = Dust.NewDustPerfect(dustSpawnPosition, 5);
                            blood.scale = Main.rand.NextFloat(2.6f, 3f);
                            blood.velocity = dustVelocity;
                            blood.noGravity = true;
                        }
                    }
                }

                // Release ichor into the air that slowly falls and charge at the target.
                if (attackTimer >= chargeDelay)
                {
                    SoundEngine.PlaySound(SoundID.NPCHit20, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < fallingIchorCount; i++)
                        {
                            float projectileOffsetInterpolant = i / (float)(fallingIchorCount - 1f);
                            float horizontalSpeed = Lerp(-maxHorizontalSpeed, maxHorizontalSpeed, projectileOffsetInterpolant) + Main.rand.NextFloatDirection() / fallingIchorCount * 5f;
                            float verticalSpeed = Main.rand.NextFloat(-8f, -7f);
                            Vector2 ichorVelocity = new(horizontalSpeed, verticalSpeed);
                            Utilities.NewProjectileBetter(npc.Top + Vector2.UnitY * 10f, ichorVelocity, ModContent.ProjectileType<FallingIchor>(), IchorSpitDamage, 0f);
                        }

                        for (int i = 0; i < burstIchorCount; i++)
                        {
                            float projectileOffsetInterpolant = i / (float)(burstIchorCount - 1f);
                            float offsetAngle = Lerp(-0.55f, 0.55f, projectileOffsetInterpolant);
                            Vector2 ichorVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(offsetAngle) * 6.5f;
                            Utilities.NewProjectileBetter(npc.Center + ichorVelocity * 3f, ichorVelocity, ModContent.ProjectileType<FlyingIchor>(), IchorSpitDamage, 0f);
                            CreateBloodParticles(npc.Center + ichorVelocity * 3f, ichorVelocity, Main.rand.NextBool() ? Color.Red : Color.Gold, 60);
                        }

                        attackTimer = 0f;
                        attackSubstate = 2f;
                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                        npc.netUpdate = true;
                    }
                }
            }

            // Charge.
            if (attackSubstate == 2f)
            {
                if (attackTimer >= chargeTime)
                {
                    chargeCounter++;

                    if (chargeCounter >= chargeCount)
                        SelectNextAttack(npc);

                    attackSubstate = 0f;
                    attackTimer = 0f;
                    npc.velocity *= 0.45f;
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_HorizontalCrimeraSpawnCharge(NPC npc, Player target, ref float attackTimer)
        {
            int chargeDelay = 20;
            int chargeTime = 60;
            int crimeraSpawnCount = 1;
            int crimeraLimit = 3;
            float hoverOffset = 500f;

            if (BossRushEvent.BossRushActive)
            {
                SelectNextAttack(npc);
                return;
            }

            int crimeraSpawnRate = chargeTime / crimeraSpawnCount;
            float chargeSpeed = hoverOffset / chargeTime * 2f;
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];

            // Hover into position.
            if (attackSubstate == 0f)
            {
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * hoverOffset, -300f);
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 20f;

                npc.SimpleFlyMovement(idealVelocity, idealVelocity.Length() / 20f);
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.05f);

                // Slow down and go to the next attack substate if sufficiently close to the hover destination.
                if (npc.WithinRange(hoverDestination, 75f))
                {
                    attackTimer = 0f;
                    attackSubstate = 1f;
                    npc.velocity *= 0.5f;
                    npc.netUpdate = true;
                }

                // Give up and perform a different attack if unable to reach the hover destination in time.
                if (attackTimer >= 480f)
                    SelectNextAttack(npc);
            }

            // Slow down in anticipation of a charge.
            if (attackSubstate == 1f)
            {
                npc.velocity *= 0.925f;

                // Release ichor into the air that slowly falls and charge at the target.
                if (attackTimer >= chargeDelay)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        attackTimer = 0f;
                        attackSubstate = 2f;
                        npc.velocity = Vector2.UnitX * (target.Center.X > npc.Center.X).ToDirectionInt() * chargeSpeed;
                        npc.netUpdate = true;
                    }
                }
            }

            // Charge.
            if (attackSubstate == 2f)
            {
                // Summon Crimeras.
                bool enoughCrimerasAreAround = NPC.CountNPCS(NPCID.Crimera) >= crimeraLimit;
                if (attackTimer % crimeraSpawnRate == crimeraSpawnRate / 2 && !enoughCrimerasAreAround)
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath23, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, NPCID.Crimera, npc.whoAmI);
                }

                if (attackTimer >= chargeTime)
                {
                    SelectNextAttack(npc);
                    npc.velocity *= 0.45f;
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_IchorBlasts(NPC npc, Player target, bool inPhase2, ref float attackTimer)
        {
            int fireDelay = 50;
            int shootRate = 45;
            int blastCount = 9;

            if (inPhase2)
                shootRate -= 4;

            if (BossRushEvent.BossRushActive)
            {
                shootRate -= 8;
                blastCount -= 2;
            }

            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float reboundCoundown = ref npc.Infernum().ExtraAI[1];
            ref float universalTimer = ref npc.Infernum().ExtraAI[2];

            universalTimer++;

            float verticalHoverOffset = Sin(universalTimer / 13f) * 100f - 50f;
            Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 480f, verticalHoverOffset);
            if (reboundCoundown <= 0f)
            {
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 20f;

                npc.SimpleFlyMovement(idealVelocity, idealVelocity.Length() / 60f);
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.02f);
                if (Distance(npc.Center.X, hoverDestination.X) < 35f)
                {
                    npc.position.X = hoverDestination.X - npc.width * 0.5f;
                    npc.velocity.X = 0f;
                }
            }
            else
            {
                reboundCoundown--;
            }

            // Hover into position.
            if (attackSubstate == 0f)
            {
                // Slow down and go to the next attack substate if sufficiently close to the hover destination.
                if (npc.WithinRange(hoverDestination, 75f))
                {
                    attackTimer = 0f;
                    attackSubstate = 1f;
                    npc.netUpdate = true;
                }

                // Give up and perform a different attack if unable to reach the hover destination in time.
                if (attackTimer >= 480f)
                    SelectNextAttack(npc);
            }

            // Slow down in preparation of firing.
            if (attackSubstate == 1f)
            {
                // Slow down.
                reboundCoundown = 1f;
                npc.velocity = (npc.velocity * 0.95f).MoveTowards(Vector2.Zero, 0.75f);

                if (attackTimer >= fireDelay)
                {
                    attackTimer = 0f;
                    attackSubstate = 2f;
                    npc.netUpdate = true;
                }
            }

            // Fire ichor blasts.
            if (attackSubstate == 2f)
            {
                if (attackTimer % shootRate == shootRate - 1f)
                {
                    SoundEngine.PlaySound(SoundID.NPCHit20, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            float offsetAngle = Lerp(-0.41f, 0.41f, i / 2f);
                            Vector2 shootVelocity = Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X) * 3.5f;
                            shootVelocity = shootVelocity.RotatedBy(offsetAngle);
                            Utilities.NewProjectileBetter(npc.Center + shootVelocity * 3f, shootVelocity, ModContent.ProjectileType<IchorBlast>(), IchorBlobDamage, 0f);
                            for (int j = 0; j < 3; j++)
                                CreateBloodParticles(npc.Center + shootVelocity * 3f, shootVelocity, Main.rand.NextBool(3) ? Color.Gold : Color.Red, 60);
                        }
                        npc.netUpdate = true;
                    }
                }

                if (attackTimer >= blastCount * shootRate)
                    SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_IchorSpinDash(NPC npc, Player target, bool inPhase2, bool inPhase3, bool inPhase4, ref float attackTimer)
        {
            int blobReleaseRate = 10;
            int spinTime = 120;
            int chargeBlobCount = 8;
            int chargeTime = 35;
            int chargeSlowdownTime = 108;
            float spinRadius = 325f;
            float totalSpinArc = TwoPi;
            float chargeSpeed = 16.5f;
            float blobSpeedFactor = -1f;

            if (inPhase2)
                blobSpeedFactor *= -0.7f;

            if (inPhase3)
            {
                spinTime -= 15;
                blobReleaseRate--;
                chargeSpeed += 1.5f;
                blobSpeedFactor *= 1.1f;
            }

            if (inPhase4)
            {
                blobReleaseRate--;
                spinRadius += 75f;
                totalSpinArc *= 1.5f;
                chargeSpeed += 1.5f;
                blobSpeedFactor *= 1.1f;
                chargeBlobCount += 3;
            }

            if (BossRushEvent.BossRushActive)
            {
                blobReleaseRate--;
                spinTime -= 25;
                chargeBlobCount += 5;
                spinRadius += 66f;
                chargeSpeed *= 1.7f;
                blobSpeedFactor *= 1.3f;
            }

            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float spinDirection = ref npc.Infernum().ExtraAI[1];
            ref float spinOffsetAngle = ref npc.Infernum().ExtraAI[2];

            Vector2 spinCenter = target.Center + spinOffsetAngle.ToRotationVector2() * spinRadius;

            // Intiialize the ideal spin offset angle on the first frame.
            if (attackSubstate == 0f && attackTimer == 1f)
            {
                spinOffsetAngle = target.Center.X > npc.Center.X ? Pi : 0f;
                spinDirection = Main.rand.NextBool().ToDirectionInt();
                npc.netUpdate = true;
            }

            // Hover into position for the spin.
            if (attackSubstate == 0f)
            {
                npc.Center = Vector2.Lerp(npc.Center, spinCenter, 0.045f).MoveTowards(spinCenter, 12.5f);
                npc.velocity = Vector2.Zero;

                // Begin spinning once close enough to the ideal position.
                if (npc.WithinRange(spinCenter, 25f))
                {
                    attackSubstate = 1f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Begin spinning.
            if (attackSubstate == 1f)
            {
                npc.Center = spinCenter;
                npc.velocity = Vector2.Zero;

                // Make the spin slow down near the end, to make the impending charge readable.
                float spinSlowdownFactor = Utils.GetLerpValue(spinTime - 1f, spinTime * 0.65f, attackTimer, true);
                spinOffsetAngle += totalSpinArc / spinTime * spinDirection * spinSlowdownFactor;

                // Release blobs away from the player periodically. These serve as arena obstacles for successive attacks.
                // Blobs are not fired if there are nearby tiles in the way of the blob's potential path.
                if (attackTimer % blobReleaseRate == blobReleaseRate - 1f)
                {
                    Vector2 blobVelocity = npc.SafeDirectionTo(target.Center) * blobSpeedFactor * 10f;
                    bool lineOfSightIsClear = Collision.CanHit(npc.Center, 1, 1, npc.Center + blobVelocity * 12f, 1, 1);

                    if (lineOfSightIsClear)
                    {
                        SoundEngine.PlaySound(SoundID.NPCHit20, npc.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Utilities.NewProjectileBetter(npc.Center + blobVelocity, blobVelocity, ModContent.ProjectileType<IchorBlob>(), IchorBlobDamage, 0f, -1, 0f, target.Center.Y);

                            for (int i = 0; i < 10; i++)
                                CreateBloodParticles(npc.Center + blobVelocity, blobVelocity, Main.rand.NextBool(3) ? Color.Gold : Color.Red, 60);
                        }
                    }
                }

                // Charge at the target after the spin concludes.
                if (attackTimer >= spinTime)
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath23, npc.Center);

                    Vector2 shootDirection = npc.SafeDirectionTo(target.Center) * (blobSpeedFactor > 0f).ToDirectionInt();
                    bool lineOfSightIsClear = Collision.CanHit(npc.Center, 1, 1, npc.Center + shootDirection * 120f, 1, 1);

                    if (Main.netMode != NetmodeID.MultiplayerClient && lineOfSightIsClear)
                    {
                        for (int i = 0; i < chargeBlobCount; i++)
                        {
                            Vector2 blobVelocity = (shootDirection * 14.5f + Main.rand.NextVector2Circular(4f, 4f)) * Math.Abs(blobSpeedFactor);
                            Utilities.NewProjectileBetter(npc.Center + blobVelocity, blobVelocity, ModContent.ProjectileType<IchorBlob>(), IchorBlobDamage, 0f, -1, 0f, target.Center.Y);

                            for (int j = 0; j < chargeBlobCount * 2; j++)
                                CreateBloodParticles(npc.Center + blobVelocity, blobVelocity, Main.rand.NextBool(3) ? Color.Gold : Color.Red, 60);

                        }
                    }

                    attackTimer = 0f;
                    attackSubstate = 2f;
                    npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                    npc.netUpdate = true;
                }
            }

            // Post-charge behaviors.
            if (attackSubstate == 2f)
            {
                if (attackTimer >= chargeTime)
                    npc.velocity *= 0.92f;

                if (attackTimer >= chargeTime + chargeSlowdownTime)
                    SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_SmallWormBursts(NPC npc, Player target, ref float attackTimer)
        {
            int wormSummonTime = 150;
            int reelBackTime = 25;
            int chargeRedirectTime = 30;
            int chargeTime = 60;
            float chargeHoverSpeed = 19.5f;
            float chargeSpeed = 26f;
            float maxHoverSpeed = 11f;
            bool doneReelingBack = attackTimer >= wormSummonTime + reelBackTime;

            ref float chargeTimer = ref npc.Infernum().ExtraAI[0];
            ref float chargeDirection = ref npc.Infernum().ExtraAI[1];

            // Disable significant contact damage.
            npc.Calamity().DR = 0.999999f;

            // Go invincible if too close to the next phase ratio.
            if (((float)npc.life / npc.lifeMax) <= Phase3LifeRatio + 0.1f)
                npc.dontTakeDamage = true;

            // Hover above the player and slow down.
            if (attackTimer < wormSummonTime)
            {
                npc.damage = 0;

                float hoverSpeed = Lerp(maxHoverSpeed, 0f, Utils.GetLerpValue(wormSummonTime - 45f, 0f, attackTimer, true));
                if (doneReelingBack)
                    hoverSpeed = maxHoverSpeed;

                Vector2 hoverDestination = target.Center - Vector2.UnitY * 325f;
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * hoverSpeed;

                if (!npc.WithinRange(hoverDestination, 50f))
                {
                    npc.SimpleFlyMovement(idealVelocity, 0.5f);
                    npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.025f);
                }
            }

            // Do horizontal charges once done reeling back.
            if (doneReelingBack)
            {
                // Initialize the charge direction.
                if (chargeTimer == 1f)
                {
                    chargeDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                    npc.netUpdate = true;
                }

                // Hover into position before charging.
                if (chargeTimer <= chargeRedirectTime)
                {
                    Vector2 hoverDestination = target.Center + Vector2.UnitX * chargeDirection * -420f;
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * chargeHoverSpeed, chargeHoverSpeed * 0.16f);
                    npc.Center = npc.Center.MoveTowards(hoverDestination, 12.5f);
                    if (npc.WithinRange(hoverDestination, 25f))
                        npc.velocity = Vector2.Zero;

                    if (chargeTimer == chargeRedirectTime)
                        npc.velocity *= 0.3f;
                    npc.rotation = 0f;
                }
                else if (chargeTimer <= chargeRedirectTime + chargeTime)
                {
                    npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitX * chargeDirection * chargeSpeed, 0.1f);
                    if (chargeTimer == chargeRedirectTime + chargeTime)
                        npc.velocity *= 0.7f;
                }
                else
                    npc.velocity *= 0.92f;

                if (chargeTimer >= chargeRedirectTime + chargeTime + 8f)
                {
                    chargeTimer = 0f;
                    npc.netUpdate = true;
                }

                chargeTimer++;
            }

            // Have the worm erupt from the hive.
            if (attackTimer == wormSummonTime)
            {
                MakeWormEruptFromHive(npc, -Vector2.UnitY, 1f, ModContent.NPCType<PerforatorHeadSmall>());
                HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.PerforatorsWorm1Tip");
            }


            // Go to the next attack if the small perforator is dead.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= wormSummonTime + 1f && !NPC.AnyNPCs(ModContent.NPCType<PerforatorHeadSmall>()))
                SelectNextAttack(npc);
        }

        public static void DoBehavior_CrimeraWalls(NPC npc, Player target, bool inPhase3, bool inPhase4, ref float attackTimer)
        {
            int riseTime = 45;
            int wallCreationTime = 22;
            int attackSwitchDelay = 120;
            float offsetPerCrimera = 160f;
            float wallSpeed = 8f;
            float aimAtTargetInterpolant = 0f;

            if (inPhase3)
                offsetPerCrimera -= 10f;

            if (inPhase4)
            {
                offsetPerCrimera -= 15f;
                wallSpeed -= 0.95f;
                aimAtTargetInterpolant += 0.125f;
            }

            if (BossRushEvent.BossRushActive)
            {
                offsetPerCrimera -= 20f;
                wallSpeed += 3f;
            }

            ref float horizontalWallOffset = ref npc.Infernum().ExtraAI[0];

            // Use a bit more DR than usual.
            npc.Calamity().DR = 0.3f;
            npc.Calamity().CurrentlyIncreasingDefenseOrDR = true;

            // Perform the initial rise.
            if (attackTimer == 1f)
            {
                npc.velocity = Vector2.UnitY * -12f;
                npc.netUpdate = true;
            }

            // Slow down after rising.
            if (attackTimer < riseTime)
                npc.velocity *= 0.95f;

            // Prepare wall attack stuff.
            if (attackTimer == riseTime)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath23, npc.Center);

                npc.velocity = Vector2.Zero;
                horizontalWallOffset = Main.rand.NextFloat(-35f, 35f);
                npc.netUpdate = true;
            }

            // Release the walls.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer > riseTime && attackTimer <= riseTime + wallCreationTime)
            {
                horizontalWallOffset += offsetPerCrimera;
                Vector2 wallSpawnOffset = new(horizontalWallOffset - 1800f, -925f);
                Vector2 wallVelocity = Vector2.Lerp(Vector2.UnitY, -wallSpawnOffset.SafeNormalize(Vector2.UnitY), aimAtTargetInterpolant);
                wallVelocity = wallVelocity.SafeNormalize(Vector2.UnitY) * wallSpeed;

                Utilities.NewProjectileBetter(target.Center + wallSpawnOffset, wallVelocity, ModContent.ProjectileType<Crimera>(), CrimeraWallDamage, 1f);

                wallSpawnOffset.X += 48f;
                Utilities.NewProjectileBetter(target.Center + wallSpawnOffset * new Vector2(1f, -1f), -wallVelocity, ModContent.ProjectileType<Crimera>(), CrimeraWallDamage, 1f);
            }

            if (attackTimer > riseTime + attackSwitchDelay)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_MediumWormBursts(NPC npc, Player target, ref float attackTimer, ref float backafterimageGlowInterpolant)
        {
            int wormSummonTime = 150;
            int reelBackTime = 25;
            int burstIchorCount = 3;
            int ichorBurstReleaseRate = 100;
            float maxHoverSpeed = 11f;
            bool doneReelingBack = attackTimer >= wormSummonTime + reelBackTime;
            ref float postWormSummonAttackTimer = ref npc.Infernum().ExtraAI[0];
            ref float hoverOffsetDirection = ref npc.Infernum().ExtraAI[1];

            // Disable contact damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            // Hover above the player and slow down.
            if (attackTimer < wormSummonTime)
            {
                npc.damage = 0;

                float hoverSpeed = maxHoverSpeed;
                Vector2 hoverDestination = target.Center - Vector2.UnitY * 325f;
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * hoverSpeed;

                if (!npc.WithinRange(hoverDestination, 50f))
                {
                    npc.SimpleFlyMovement(idealVelocity, 0.5f);
                    npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.025f);
                }
            }

            // Periodically release bursts of ichor at the target and hover to their side.
            if (doneReelingBack)
            {
                postWormSummonAttackTimer++;

                // Initialize the hover offset direction if necessary.
                if (hoverOffsetDirection == 0f)
                {
                    hoverOffsetDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                    npc.netUpdate = true;
                }

                if (postWormSummonAttackTimer >= 300f)
                    backafterimageGlowInterpolant = Utils.GetLerpValue(300f, 350f, postWormSummonAttackTimer, true);

                // Switch directions after enough time has passed.
                if (postWormSummonAttackTimer >= 380f)
                {
                    hoverOffsetDirection *= -1f;
                    postWormSummonAttackTimer = 0f;
                    npc.netUpdate = true;
                }

                Vector2 hoverDestination = target.Center + new Vector2(hoverOffsetDirection * 560f, -220f) - npc.velocity;
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 12f;
                npc.SimpleFlyMovement(idealVelocity, 0.5f);

                if (attackTimer % ichorBurstReleaseRate == ichorBurstReleaseRate - 1f && npc.WithinRange(hoverDestination, 150f))
                {
                    SoundEngine.PlaySound(SoundID.NPCHit20, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < burstIchorCount; i++)
                        {
                            float projectileOffsetInterpolant = i / (float)(burstIchorCount - 1f);
                            float offsetAngle = Lerp(-0.49f, 0.49f, projectileOffsetInterpolant);
                            Vector2 ichorVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(offsetAngle) * 5.4f;
                            Utilities.NewProjectileBetter(npc.Center + ichorVelocity * 3f, ichorVelocity, ModContent.ProjectileType<FlyingIchor>(), IchorSpitDamage, 0f);
                        }
                        npc.netUpdate = true;
                    }
                }
            }

            // Have the worm erupt from the hive.
            if (attackTimer == wormSummonTime)
            {
                MakeWormEruptFromHive(npc, -Vector2.UnitY, 1f, ModContent.NPCType<PerforatorHeadMedium>());
                HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.PerforatorsWorm2Tip");
            }

            // Go to the next attack if the small perforator is dead.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= wormSummonTime + 1f && !NPC.AnyNPCs(ModContent.NPCType<PerforatorHeadMedium>()))
                SelectNextAttack(npc);
        }

        public static void DoBehavior_IchorRain(NPC npc, Player target, bool inPhase4, ref float attackTimer)
        {
            int chargeDelay = 20;
            int chargeTime = 60;
            int ichorReleaseRate = 5;
            float hoverOffset = 600f;
            float chargeSpeed = hoverOffset / chargeTime * 2.75f;

            if (BossRushEvent.BossRushActive)
            {
                chargeTime -= 16;
                chargeSpeed *= 1.36f;
            }

            if (inPhase4)
            {
                ichorReleaseRate--;
                chargeSpeed *= 1.225f;
            }

            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];

            // Use a bit more DR than usual.
            npc.Calamity().DR = 0.3f;
            npc.Calamity().CurrentlyIncreasingDefenseOrDR = true;

            // Hover into position.
            if (attackSubstate == 0f)
            {
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * hoverOffset, -270f);
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 20f;

                npc.SimpleFlyMovement(idealVelocity, idealVelocity.Length() / 20f);
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.05f);

                // Slow down and go to the next attack substate if sufficiently close to the hover destination.
                if (npc.WithinRange(hoverDestination, 75f))
                {
                    attackTimer = 0f;
                    attackSubstate = 1f;
                    npc.velocity *= 0.5f;
                    npc.netUpdate = true;
                }

                // Give up and perform a different attack if unable to reach the hover destination in time.
                if (attackTimer >= 480f)
                    SelectNextAttack(npc);
            }

            // Slow down in anticipation of a charge.
            if (attackSubstate == 1f)
            {
                npc.velocity *= 0.925f;

                // Release ichor into the air that slowly falls and charge at the target.
                if (attackTimer >= chargeDelay)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        attackTimer = 0f;
                        attackSubstate = 2f;
                        npc.velocity = Vector2.UnitX * (target.Center.X > npc.Center.X).ToDirectionInt() * chargeSpeed;
                        npc.netUpdate = true;
                    }
                }
            }

            // Charge.
            if (attackSubstate == 2f)
            {
                // Release ichor upward.
                if (attackTimer % ichorReleaseRate == ichorReleaseRate / 2 && attackTimer < chargeTime * 0.67)
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath23, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 ichorVelocity = -Vector2.UnitY.RotatedByRandom(0.2f) * 7f;
                        ichorVelocity.X += npc.velocity.X * 0.02f;
                        Utilities.NewProjectileBetter(npc.Center, ichorVelocity, ModContent.ProjectileType<IchorBlob>(), 80, 0f, -1, 0f, target.Center.Y);
                    }
                }

                if (attackTimer >= chargeTime)
                {
                    SelectNextAttack(npc);
                    npc.velocity *= 0.45f;
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_LargeWormBursts(NPC npc, Player target, ref float attackTimer)
        {
            int wormSummonTime = 150;
            int reelBackTime = 25;
            int chargeRedirectTime = 60;
            int chargeTime = 32;
            int ichorBlobCount = 3;
            float chargeHoverSpeed = 19.5f;
            float chargeSpeed = 26f;
            float maxHoverSpeed = 11f;
            bool doneReelingBack = attackTimer >= wormSummonTime + reelBackTime;

            ref float chargeTimer = ref npc.Infernum().ExtraAI[0];
            ref float chargeDirection = ref npc.Infernum().ExtraAI[1];

            // Disable significant contact damage.
            npc.Calamity().DR = 0.999999f;

            // Go invincible if too close to dying.
            if (((float)npc.life / npc.lifeMax) <= + 0.1f)
                npc.dontTakeDamage = true;

            // Hover above the player and slow down.
            if (attackTimer < wormSummonTime)
            {
                npc.damage = 0;

                float hoverSpeed = maxHoverSpeed;
                Vector2 hoverDestination = target.Center - Vector2.UnitY * 325f;
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * hoverSpeed;

                if (!npc.WithinRange(hoverDestination, 50f))
                {
                    npc.SimpleFlyMovement(idealVelocity, 0.5f);
                    npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.025f);
                }
            }

            // Do vertical charges once done reeling back.
            if (doneReelingBack)
            {
                // Hover into position before charging.
                if (chargeTimer <= chargeRedirectTime)
                {
                    Vector2 hoverDestination = target.Center - Vector2.UnitY * 335f;
                    Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * chargeHoverSpeed;

                    // Avoid colliding directly with the target by turning the ideal velocity 90 degrees.
                    // The side at which angular directions happen is dependant on whichever angle has the greatest disparity between the direction to the target.
                    // This means that the direction that gets the hive farther from the player is the one that is favored.
                    if (idealVelocity.AngleBetween(npc.SafeDirectionTo(target.Center)) < PiOver4)
                    {
                        float leftAngleDisparity = idealVelocity.RotatedBy(-PiOver2).AngleBetween(npc.SafeDirectionTo(target.Center));
                        float rightAngleDisparity = idealVelocity.RotatedBy(PiOver2).AngleBetween(npc.SafeDirectionTo(target.Center));
                        idealVelocity = idealVelocity.RotatedBy(leftAngleDisparity > rightAngleDisparity ? -PiOver2 : PiOver2);
                    }

                    npc.SimpleFlyMovement(idealVelocity, chargeHoverSpeed * 0.05f);
                    npc.Center = npc.Center.MoveTowards(hoverDestination, 10f);
                    if (npc.WithinRange(hoverDestination, 25f))
                        npc.velocity = Vector2.Zero;

                    if (chargeTimer == chargeRedirectTime)
                    {
                        // Try again instead of slamming downward if not within the range of the hover destination.
                        if (!npc.WithinRange(hoverDestination, 130f))
                            chargeTimer = 0f;

                        // Otherwise slow down in anticipation of the target and release ichor blobs upward.
                        else
                        {
                            SoundEngine.PlaySound(SoundID.NPCDeath23, npc.Center);
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                for (int i = 0; i < ichorBlobCount; i++)
                                {
                                    float projectileShootInterpolant = i / (float)(ichorBlobCount - 1f);
                                    float horizontalShootSpeed = Lerp(-10f, 10f, projectileShootInterpolant) + Main.rand.NextFloatDirection() * 0.64f;
                                    Vector2 blobVelocity = new(horizontalShootSpeed, -7f);
                                    Utilities.NewProjectileBetter(npc.Center + blobVelocity, blobVelocity, ModContent.ProjectileType<IchorBlob>(), IchorBlobDamage, 0f, -1, 0f, target.Center.Y);
                                }
                            }

                            npc.velocity *= 0.25f;
                        }
                        npc.netUpdate = true;
                    }
                    npc.rotation = 0f;
                }
                else if (chargeTimer <= chargeRedirectTime + chargeTime)
                {
                    npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * chargeSpeed, 0.06f);
                    if (chargeTimer == chargeRedirectTime + chargeTime)
                        npc.velocity *= 0.7f;
                }
                else
                    npc.velocity *= 0.92f;

                if (chargeTimer >= chargeRedirectTime + chargeTime + 8f)
                {
                    chargeTimer = 0f;
                    npc.netUpdate = true;
                }

                chargeTimer++;
            }

            // Have the worm erupt from the hive.
            if (attackTimer == wormSummonTime)
                MakeWormEruptFromHive(npc, -Vector2.UnitY, 1.75f, ModContent.NPCType<PerforatorHeadLarge>());

            // Go to the next attack if the small perforator is dead.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= wormSummonTime + 1f && !NPC.AnyNPCs(ModContent.NPCType<PerforatorHeadLarge>()))
            {
                CleanUpStrayProjectiles();
                SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_IchorFountainCharge(NPC npc, Player target, ref float attackTimer)
        {
            int moveDelay = 60;
            int attackDelay = 150;
            int mouthIchorReleaseRate = 5;
            int ichorWallFireRate = 50;
            int ichorWallShotCount = 5;
            int attackTime = 420;
            int attackTransitionDelay = 120;
            int bloodReleaseRate = 10;
            float ichorWallSpacing = 40f;
            Vector2 hoverDestination = target.Center - Vector2.UnitY * 300f;

            if (BossRushEvent.BossRushActive)
            {
                ichorWallShotCount += 9;
                attackDelay -= 30;
                attackTime -= 105;
            }

            Vector2 leftMouthPosition = npc.Center + new Vector2(-68f, 6f).RotatedBy(-npc.rotation);
            Vector2 rightMouthPosition = npc.Center + new Vector2(48f, -36f).RotatedBy(npc.rotation);
            Vector2[] mouthPositions = new[]
            {
                leftMouthPosition,
                rightMouthPosition
            };

            if (attackTimer <= moveDelay)
            {
                // Slow down dramatically at first.
                npc.velocity *= 0.935f;

                // Rise upward and create an explosion sound.
                if (attackTimer == moveDelay)
                {
                    SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                    SoundEngine.PlaySound(SoundID.DD2_BetsyFireballImpact with { Volume = 2.2f, Pitch = -0.4f }, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<PerforatorWave>(), 0, 0f);

                    npc.velocity = -Vector2.UnitY * 12f;
                    npc.netUpdate = true;
                }
            }

            // Attempt to hover above the target.
            else if (attackTimer < attackDelay)
            {
                if (!npc.WithinRange(hoverDestination, 25f))
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 14.5f, 0.5f);
                npc.Center = npc.Center.MoveTowards(hoverDestination, 8f);
            }

            // Continue hovering over the target, but move at slower horizontal pace.
            // Also spew ichor from the mouths as an effective barrier while creating lines of ichor from the sides that accelerate at the target.
            else
            {
                if (!npc.WithinRange(hoverDestination, 30f))
                {
                    Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * new Vector2(9.5f, 17f);
                    npc.SimpleFlyMovement(idealVelocity, 0.6f);
                    if (npc.Center.Y > hoverDestination.Y + 80f)
                        npc.velocity.Y -= 0.8f;
                }

                // Create walls of ichor
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % ichorWallFireRate == ichorWallFireRate - 1f && attackTimer < attackDelay + attackTime)
                {
                    float ichorOffsetDirection = Main.rand.NextBool().ToDirectionInt();
                    for (int i = 0; i < ichorWallShotCount; i++)
                    {
                        float ichorShootInterpolant = i / (float)(ichorWallShotCount - 1f);
                        float verticalWallOffset = Lerp(-0.5f, 0.5f, ichorShootInterpolant) * ichorWallSpacing * ichorWallShotCount;
                        Vector2 wallOffset = new(ichorOffsetDirection * 560f, verticalWallOffset);
                        Vector2 wallVelocity = Vector2.UnitX * ichorOffsetDirection * -6.5f;
                        Utilities.NewProjectileBetter(target.Center + wallOffset, wallVelocity, ModContent.ProjectileType<IchorBolt>(), IchorSpitDamage, 0f);
                    }
                }

                // Release ichor from the mouth.
                if (attackTimer % mouthIchorReleaseRate == mouthIchorReleaseRate - 1f)
                {
                    SoundEngine.PlaySound(SoundID.NPCHit20, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        foreach (Vector2 mouthPosition in mouthPositions)
                        {
                            Vector2 ichorVelocity = npc.SafeDirectionTo(mouthPosition) * 12f;
                            Utilities.NewProjectileBetter(mouthPosition, ichorVelocity, ModContent.ProjectileType<FallingIchorBlast>(), IchorBlobDamage, 0f);
                        }

                    }
                }
                if (attackTimer % bloodReleaseRate == bloodReleaseRate - 1f)
                {
                    // Spawn blood particles below the hive.
                    Vector2 bloodPosition = npc.Center + new Vector2(0, 70) + new Vector2(Main.rand.NextFloat(-10, 10));
                    Vector2 bloodVelocity = npc.SafeDirectionTo(bloodPosition) * 2;
                    CreateBloodParticles(bloodPosition, bloodVelocity.RotatedBy(ToRadians(Main.rand.NextFloat(-20, 20))), Main.rand.NextBool(3) ? Color.Gold : Color.Crimson, 30);
                }
            }

            if (attackTimer >= attackDelay + attackTime + attackTransitionDelay)
                SelectNextAttack(npc);
        }

        public static void SelectNextAttack(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool inPhase2 = lifeRatio <= Phase2LifeRatio;
            bool inPhase3 = lifeRatio <= Phase3LifeRatio;
            bool inPhase4 = lifeRatio <= Phase4LifeRatio;
            int crimeraAttackType = inPhase2 ? (int)PerforatorHiveAttackState.CrimeraWalls : (int)PerforatorHiveAttackState.HorizontalCrimeraSpawnCharge;
            int ichorBlastAttackType = inPhase3 ? (int)PerforatorHiveAttackState.IchorRain : (int)PerforatorHiveAttackState.IchorBlasts;
            int ichorFromAboveAttackType = inPhase4 ? (int)PerforatorHiveAttackState.IchorFountainCharge : (int)PerforatorHiveAttackState.DiagonalBloodCharge;

            npc.ai[0] = (PerforatorHiveAttackState)npc.ai[0] switch
            {
                PerforatorHiveAttackState.HorizontalCrimeraSpawnCharge or PerforatorHiveAttackState.CrimeraWalls => ichorBlastAttackType,
                PerforatorHiveAttackState.IchorBlasts or PerforatorHiveAttackState.IchorRain => (int)PerforatorHiveAttackState.IchorSpinDash,
                PerforatorHiveAttackState.IchorSpinDash => ichorFromAboveAttackType,
                _ => crimeraAttackType,
            };
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.netUpdate = true;
        }

        public static void CreateSegments(NPC npc, int wormLength, int bodyType, int tailType)
        {
            int previousIndex = npc.whoAmI;
            for (int i = 0; i < wormLength; i++)
            {
                int nextIndex;
                if (i < wormLength - 1)
                    nextIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, bodyType, npc.whoAmI + 1);
                else
                    nextIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, tailType, npc.whoAmI + 1);

                Main.npc[nextIndex].realLife = npc.whoAmI;
                Main.npc[nextIndex].ai[2] = npc.whoAmI;
                Main.npc[nextIndex].ai[1] = previousIndex;
                Main.npc[previousIndex].ai[0] = nextIndex;

                // Force sync the new segment into existence.
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, nextIndex, 0f, 0f, 0f, 0);

                previousIndex = nextIndex;
            }
        }

        #endregion AI

        #region Drawcode

        public static void CreateBloodParticles(Vector2 spawnPosition, Vector2 velocity, Color color, int lifetime)
        {
            // Spawn blood particles to add atmosphere
            Particle blood = new EoCBloodParticle(spawnPosition, velocity * 2, lifetime, Main.rand.NextFloat(0.7f, 1f), color, 3);
            GeneralParticleHandler.SpawnParticle(blood);
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            SpriteEffects direction = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                direction = SpriteEffects.FlipHorizontally;

            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Color glowmaskColor = Color.Lerp(Color.White, Color.Yellow, 0.5f);
            Vector2 origin = npc.frame.Size() * 0.5f;
            Vector2 baseDrawPosition = npc.Center - Main.screenPosition;
            float backafterimageGlowInterpolant = npc.localAI[0];

            if (backafterimageGlowInterpolant > 0f)
            {
                Color backAfterimageColor = Color.Yellow * backafterimageGlowInterpolant;
                backAfterimageColor.A = 0;
                for (int i = 0; i < 6; i++)
                {
                    Vector2 drawPosition = baseDrawPosition + (TwoPi * i / 6f).ToRotationVector2() * backafterimageGlowInterpolant * 4f;
                    Main.spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(backAfterimageColor), npc.rotation, origin, npc.scale, direction, 0f);
                }
            }

            ref float deathTimer = ref npc.Infernum().ExtraAI[DeathTimerIndex];
            // If performing the death animation, use a red tint shader. This simply tints the texture to the provided color.
            if (deathTimer > 0)
            {
                // Enter the shader region.
                Main.spriteBatch.EnterShaderRegion();

                // Get the opacity interpolent.
                float opacityInterpolent = deathTimer / DeathAnimationLength;
                // Set the opacity of the shader.
                InfernumEffectsRegistry.BasicTintShader.UseSaturation(opacityInterpolent);
                InfernumEffectsRegistry.BasicTintShader.UseOpacity(lightColor.ToGreyscale());
                // Set the color of the shader.
                InfernumEffectsRegistry.BasicTintShader.UseColor(Color.Red);
                // Apply the shader.
                InfernumEffectsRegistry.BasicTintShader.Apply();

                // And draw the texture.
                Main.spriteBatch.Draw(texture, baseDrawPosition, npc.frame, Color.White, npc.rotation, origin, npc.scale, direction, 0f);
                // Along with the glowmask.
                Main.spriteBatch.Draw(texture, baseDrawPosition, npc.frame, glowmaskColor, npc.rotation, origin, npc.scale, direction, 0f);

                // Exit the shader region.
                Main.spriteBatch.ExitShaderRegion();

                // And return to avoid drawing the texture again.
                return false;
            }

            Main.spriteBatch.Draw(texture, baseDrawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, direction, 0f);

            texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/Perforator/PerforatorHiveGlow").Value;

            Main.spriteBatch.Draw(texture, baseDrawPosition, npc.frame, glowmaskColor, npc.rotation, origin, npc.scale, direction, 0f);
            return false;
        }
        #endregion

        #region Death Effects
        public override bool CheckDead(NPC npc)
        {
            // Just die as usual if the Perforator Hive is killed during the death animation. This is done so that Cheat Sheet and other butcher effects can kill it quickly.
            if (npc.Infernum().ExtraAI[DeathTimerIndex] >= 1f)
                return true;

            // Jumpstart the death timer.
            npc.Infernum().ExtraAI[DeathTimerIndex] = 1f;
            npc.life = 1;
            npc.dontTakeDamage = true;
            npc.active = true;
            npc.netUpdate = true;
            return false;
        }
        #endregion Death Effects

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Mods.InfernumMode.PetDialog.PerforatorsTip1";
            yield return n => "Mods.InfernumMode.PetDialog.PerforatorsProjectilesTip";
        }
        #endregion Tips
    }
}
