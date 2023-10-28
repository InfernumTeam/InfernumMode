using CalamityMod;
using CalamityMod.InverseKinematics;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.Particles;
using CalamityMod.Skies;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Common.Graphics.ScreenEffects;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.ComboAttacks;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.DraedonBehaviorOverride;
using DraedonNPC = CalamityMod.NPCs.ExoMechs.Draedon;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class AresBodyBehaviorOverride : NPCBehaviorOverride
    {
        public enum AresBodyFrameType
        {
            Normal,
            Laugh
        }

        public enum AresBodyAttackType
        {
            IdleHover,
            LaserSpinBursts,
            DirectionChangingSpinBursts,

            // Energy katana attacks.
            EnergyBladeSlices,
            DownwardCrossSlices,
            ThreeDimensionalSuperslashes,

            // Ultimate attack. Only happens when in the final phase.
            PrecisionBlasts
        }

        public override int NPCOverrideType => ModContent.NPCType<AresBody>();

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            ExoMechManagement.Phase4LifeRatio
        };

        public const int BackArmSwapDelay = 1800;

        public const int AresLaserStartSoundDuration = 174;

        public const float Phase1ArmChargeupTime = 240f;

        public static int ProjectileDamageBoost
        {
            get
            {
                if (!Main.npc.IndexInRange(CalamityGlobalNPC.draedonExoMechPrime))
                    return 0;

                return (int)Main.npc[CalamityGlobalNPC.draedonExoMechPrime].Infernum().ExtraAI[ExoMechManagement.Ares_ProjectileDamageBoostIndex];
            }
            set
            {
                if (!Main.npc.IndexInRange(CalamityGlobalNPC.draedonExoMechPrime))
                    return;

                Main.npc[CalamityGlobalNPC.draedonExoMechPrime].Infernum().ExtraAI[ExoMechManagement.Ares_ProjectileDamageBoostIndex] = value;
            }
        }

        public static bool Enraged
        {
            get
            {
                if (!Main.npc.IndexInRange(CalamityGlobalNPC.draedonExoMechPrime))
                    return false;

                return Main.npc[CalamityGlobalNPC.draedonExoMechPrime].Infernum().ExtraAI[ExoMechManagement.Ares_EnragedIndex] == 1f;
            }
        }

        public static bool ShouldDrawBehindTiles
        {
            get
            {
                if (!Main.npc.IndexInRange(CalamityGlobalNPC.draedonExoMechPrime))
                    return false;

                return Main.npc[CalamityGlobalNPC.draedonExoMechPrime].ai[2] >= 0.2f;
            }
        }

        #region Netcode Syncs

        public override void SendExtraData(NPC npc, ModPacket writer) => writer.Write(npc.Opacity);

        public override void ReceiveExtraData(NPC npc, BinaryReader reader) => npc.Opacity = reader.ReadSingle();

        #endregion Netcode Syncs

        #region AI
        public override void SetDefaults(NPC npc)
        {
            // Set defaults that, if were to be changed by Calamity, would cause significant issues to the fight.
            npc.width = 220;
            npc.height = 252;
            npc.scale = 1f;
            npc.Opacity = 0f;
            npc.defense = 100;
            npc.DR_NERD(0.35f);
        }

        public override bool PreAI(NPC npc)
        {
            // Define the life ratio.
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Define the whoAmI variable.
            CalamityGlobalNPC.draedonExoMechPrime = npc.whoAmI;

            // Reset damage.
            npc.damage = 0;
            npc.Calamity().canBreakPlayerDefense = true;

            // Reset frame states.
            ref float frameType = ref npc.localAI[0];
            frameType = (int)AresBodyFrameType.Normal;

            // Define attack variables.
            bool performingDeathAnimation = ExoMechAIUtilities.PerformingDeathAnimation(npc);
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float zPosition = ref npc.ai[2];
            ref float armsHaveBeenSummoned = ref npc.ai[3];
            ref float armCycleCounter = ref npc.Infernum().ExtraAI[5];
            ref float armCycleTimer = ref npc.Infernum().ExtraAI[6];
            ref float hasSummonedComplementMech = ref npc.Infernum().ExtraAI[ExoMechManagement.HasSummonedComplementMechIndex];
            ref float complementMechIndex = ref npc.Infernum().ExtraAI[ExoMechManagement.ComplementMechIndexIndex];
            ref float wasNotInitialSummon = ref npc.Infernum().ExtraAI[ExoMechManagement.WasNotInitialSummonIndex];
            ref float finalMechIndex = ref npc.Infernum().ExtraAI[ExoMechManagement.FinalMechIndexIndex];
            ref float enraged = ref npc.Infernum().ExtraAI[ExoMechManagement.Ares_EnragedIndex];
            ref float backarmSwapTimer = ref npc.Infernum().ExtraAI[14];
            ref float laserPulseArmAreSwapped = ref npc.Infernum().ExtraAI[ExoMechManagement.Ares_BackArmsAreSwappedIndex];
            ref float finalPhaseAnimationTime = ref npc.Infernum().ExtraAI[ExoMechManagement.FinalPhaseTimerIndex];
            ref float deathAnimationTimer = ref npc.Infernum().ExtraAI[ExoMechManagement.DeathAnimationTimerIndex];
            ref float blenderSoundTimer = ref npc.Infernum().ExtraAI[ExoMechManagement.Ares_BlenderSoundTimerIndex];
            ref float blenderSoundIsLooping = ref npc.Infernum().ExtraAI[ExoMechManagement.Ares_BlenderSoundIsLoopingIndex];
            ref SlotId deathraySoundSlot = ref npc.ModNPC<AresBody>().DeathraySoundSlot;

            NPC initialMech = ExoMechManagement.FindInitialMech();
            NPC complementMech = complementMechIndex >= 0 && Main.npc[(int)complementMechIndex].active && Utilities.IsExoMech(Main.npc[(int)complementMechIndex]) ? Main.npc[(int)complementMechIndex] : null;
            NPC finalMech = ExoMechManagement.FindFinalMech();

            // Continuously reset the telegraph line things.
            npc.Infernum().ExtraAI[ExoMechManagement.Ares_LineTelegraphInterpolantIndex] = 0f;

            // Make the laser and pulse arms swap sometimes.
            if (backarmSwapTimer > BackArmSwapDelay)
            {
                backarmSwapTimer = 0f;
                laserPulseArmAreSwapped = laserPulseArmAreSwapped == 0f ? 1f : 0f;
                npc.netUpdate = true;
            }
            backarmSwapTimer++;

            // Become more resistant to damage as necessary.
            npc.takenDamageMultiplier = 1f;
            if (ExoMechManagement.ShouldHaveSecondComboPhaseResistance(npc))
                npc.takenDamageMultiplier *= 0.5f;

            // Disable natural draw layering if necessary.
            npc.hide = ShouldDrawBehindTiles;

            // Spawn initial arm cannons and initialize other things.
            if (Main.netMode != NetmodeID.MultiplayerClient && armsHaveBeenSummoned == 0f)
            {
                int totalArms = 6;
                for (int i = 0; i < totalArms; i++)
                {
                    int lol = 0;
                    switch (i)
                    {
                        // Summon the four main cannons.
                        case 0:
                            lol = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<AresLaserCannon>(), npc.whoAmI);
                            break;
                        case 1:
                            lol = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<AresPlasmaFlamethrower>(), npc.whoAmI);
                            break;
                        case 2:
                            lol = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<AresTeslaCannon>(), npc.whoAmI);
                            break;
                        case 3:
                            lol = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<AresPulseCannon>(), npc.whoAmI);
                            break;

                        // Summon two energy katanas on each side.
                        case 4:
                            lol = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<AresEnergyKatana>(), npc.whoAmI, 0f, 0f, -1f);
                            break;
                        case 5:
                            lol = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<AresEnergyKatana>(), npc.whoAmI, 0f, 0f, 1f);
                            break;
                        default:
                            break;
                    }

                    Main.npc[lol].realLife = npc.whoAmI;
                    Main.npc[lol].netUpdate = true;
                }
                complementMechIndex = -1f;
                finalMechIndex = -1f;
                armsHaveBeenSummoned = 1f;
                npc.netUpdate = true;
            }

            // Summon the complement mech and reset things once ready.
            if (hasSummonedComplementMech == 0f && lifeRatio < ExoMechManagement.Phase4LifeRatio)
            {
                if (attackType != (int)AresBodyAttackType.IdleHover)
                {
                    // Destroy all lasers and telegraphs.
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        if ((Main.projectile[i].type == ModContent.ProjectileType<AresDeathBeamTelegraph>() || Main.projectile[i].type == ModContent.ProjectileType<AresSpinningDeathBeam>()) && Main.projectile[i].active)
                            Main.projectile[i].Kill();
                    }
                }

                ExoMechManagement.SummonComplementMech(npc);
                hasSummonedComplementMech = 1f;
                attackTimer = 0f;
                zPosition = 0f;
                blenderSoundTimer = 0f;
                blenderSoundIsLooping = 0f;
                SelectNextAttack(npc);
                npc.netUpdate = true;
            }

            // Summon the final mech once ready.
            if (wasNotInitialSummon == 0f && finalMechIndex == -1f && complementMech != null && complementMech.life / (float)complementMech?.lifeMax < ExoMechManagement.ComplementMechInvincibilityThreshold)
            {
                ExoMechManagement.SummonFinalMech(npc);
                zPosition = 0f;
                npc.netUpdate = true;
            }

            // Become invincible if the complement mech is at high enough health or if in the middle of a death animation.
            npc.dontTakeDamage = performingDeathAnimation;
            if (complementMechIndex >= 0 && Main.npc[(int)complementMechIndex].active && Main.npc[(int)complementMechIndex].life > Main.npc[(int)complementMechIndex].lifeMax * ExoMechManagement.ComplementMechInvincibilityThreshold)
                npc.dontTakeDamage = true;

            // Get a target.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Become invincible and disappear if necessary.
            npc.Calamity().newAI[1] = 0f;
            if (ExoMechAIUtilities.ShouldExoMechVanish(npc))
            {
                npc.Opacity = Clamp(npc.Opacity - 0.08f, 0f, 1f);
                if (npc.Opacity <= 0f)
                    npc.Center = target.Center - Vector2.UnitY * 1500f;

                attackTimer = 0f;
                zPosition = 0f;
                attackType = (int)AresBodyAttackType.IdleHover;
                npc.Calamity().newAI[1] = (int)AresBody.SecondaryPhase.PassiveAndImmune;
                npc.Calamity().ShouldCloseHPBar = true;
                npc.dontTakeDamage = true;
            }
            else
                npc.Opacity = Clamp(npc.Opacity + 0.08f, 0f, 1f);

            // Reset things.
            ProjectileDamageBoost = ExoMechManagement.CurrentAresPhase >= 4 ? 50 : 0;

            // Despawn if the target is gone.
            if (!target.active || target.dead)
            {
                npc.TargetClosest(false);
                target = Main.player[npc.target];
                if (!target.active || target.dead)
                    npc.active = false;
            }

            // Increment the blender sound timer if it's activated.
            float blenderVolume = 3.2f;
            if (blenderSoundTimer >= 1f)
            {
                if (blenderSoundTimer == 2f)
                    SoundEngine.PlaySound(AresBody.LaserStartSound with
                    {
                        Volume = blenderVolume
                    }, npc.Center);

                blenderSoundTimer++;

                // Naturally fade into the loop sound after the start sound has finished.
                if (blenderSoundTimer >= AresLaserStartSoundDuration)
                {
                    blenderSoundTimer = 0f;
                    blenderSoundIsLooping = 1f;
                    npc.netUpdate = true;
                }
            }

            // Make the blender sound loop if necessary.
            if (blenderSoundIsLooping == 1f)
            {
                // Keep the laser sound playing at Ares' center, and ensure that it loops.
                if (SoundEngine.TryGetActiveSound(deathraySoundSlot, out var deathraySound))
                {
                    if (deathraySound.IsPlaying)
                        deathraySound.Position = npc.Center;
                    else
                        deathraySound.Resume();
                }
                else
                    deathraySoundSlot = SoundEngine.PlaySound(AresBody.LaserLoopSound with
                    {
                        Volume = blenderVolume
                    }, npc.Center);
            }

            // If the blender sound is disabled, turn it off immediately.
            else if (SoundEngine.TryGetActiveSound(deathraySoundSlot, out var deathraySound) && deathraySound.IsPlaying)
            {
                deathraySound.Stop();
                SoundEngine.PlaySound(AresBody.LaserEndSound with
                {
                    Volume = blenderVolume
                }, npc.Center);

                if (Utilities.IsAprilFirst())
                    SoundEngine.PlaySound(TheMicrowave.BeepSound with
                    {
                        Volume = blenderVolume
                    }, npc.Center);
            }

            // Handle the final phase transition.
            if (finalPhaseAnimationTime <= ExoMechManagement.FinalPhaseTransitionTime && ExoMechManagement.CurrentAresPhase >= 6 && !ExoMechManagement.ExoMechIsPerformingDeathAnimation)
            {
                attackType = (int)AresBodyAttackType.IdleHover;
                finalPhaseAnimationTime++;
                zPosition = 0f;
                npc.dontTakeDamage = true;
                DoBehavior_DoFinalPhaseTransition(npc, target, ref frameType, finalPhaseAnimationTime);
                return false;
            }

            // Use combo attacks as necessary.
            if (ExoMechManagement.TotalMechs >= 2 && (int)attackType < 100)
            {
                attackTimer = 0f;
                blenderSoundTimer = 0f;
                blenderSoundIsLooping = 0f;

                if (initialMech.whoAmI == npc.whoAmI)
                    SelectNextAttack(npc);

                attackType = initialMech.ai[0];
                npc.netUpdate = true;
            }

            // Reset the attack type if it was a combo attack but the respective mech is no longer present.
            if ((finalMech != null && finalMech.Opacity > 0f || ExoMechManagement.CurrentAresPhase >= 6) && attackType >= 100f)
            {
                blenderSoundTimer = 0f;
                blenderSoundIsLooping = 0f;
                attackTimer = 0f;
                attackType = 0f;
                npc.netUpdate = true;
            }

            // Go through the attack cycle.
            if (armCycleTimer >= 600f)
            {
                armCycleCounter += enraged == 1f ? 6f : 1f;
                armCycleTimer = 0f;
            }
            else
                armCycleTimer++;

            // Become enraged if the Twins are enraged.
            if (initialMech != null && initialMech.type == ModContent.NPCType<Apollo>() && initialMech.Infernum().ExtraAI[ExoMechManagement.Twins_ComplementMechEnrageTimerIndex] > 0f)
                enraged = 1f;

            // Automatically transition to the ultimate attack if close to dying in the final phase.
            if (ExoMechManagement.CurrentAresPhase >= 6 && npc.life < npc.lifeMax * 0.075f && attackType != (int)AresBodyAttackType.PrecisionBlasts)
            {
                SelectNextAttack(npc);
                attackType = (int)AresBodyAttackType.PrecisionBlasts;
            }

            if (!performingDeathAnimation)
            {
                // Perform specific behaviors.
                switch ((AresBodyAttackType)(int)attackType)
                {
                    case AresBodyAttackType.IdleHover:
                        DoBehavior_IdleHover(npc, target, ref attackTimer);
                        break;
                    case AresBodyAttackType.LaserSpinBursts:
                    case AresBodyAttackType.DirectionChangingSpinBursts:
                        DoBehavior_LaserSpinBursts(npc, target, ref enraged, ref attackTimer, ref frameType, ref blenderSoundTimer, ref blenderSoundIsLooping);
                        break;

                    case AresBodyAttackType.EnergyBladeSlices:
                        DoBehavior_EnergyBladeSlices(npc, target, ref attackTimer, ref frameType);
                        if (backarmSwapTimer >= 5f)
                            backarmSwapTimer--;
                        break;

                    case AresBodyAttackType.DownwardCrossSlices:
                        DoBehavior_DownwardCrossSlices(npc, target, ref attackTimer, ref frameType);
                        if (backarmSwapTimer >= 5f)
                            backarmSwapTimer--;
                        break;

                    case AresBodyAttackType.ThreeDimensionalSuperslashes:
                        DoBehavior_ThreeDimensionalSuperslashes(npc, target, ref attackTimer, ref frameType, ref zPosition);
                        if (backarmSwapTimer >= 5f)
                            backarmSwapTimer--;
                        break;

                    case AresBodyAttackType.PrecisionBlasts:
                        DoBehavior_PrecisionBlasts(npc, target, ref enraged, ref attackTimer, ref frameType, ref blenderSoundTimer, ref blenderSoundIsLooping);
                        backarmSwapTimer = 300f;
                        break;
                }
            }
            else
                DoBehavior_DeathAnimation(npc, ref deathAnimationTimer);

            // Perform specific combo attack behaviors.
            if (ExoMechComboAttackContent.UseTwinsAresComboAttack(npc, 1f, ref attackTimer, ref frameType))
                SelectNextAttack(npc);
            if (ExoMechComboAttackContent.UseThanatosAresComboAttack(npc, ref attackTimer, ref frameType))
                SelectNextAttack(npc);

            npc.rotation = npc.rotation.AngleLerp(npc.velocity.X * 0.0065f, 0.2f);
            if (npc.velocity.HasNaNs())
            {
                npc.velocity = Vector2.Zero;
                npc.rotation = 0f;
            }

            // Give the illusion of being in 3D space by shrinking.
            // Other parts of code cause Ares to layer behind things like trees to better sell the illusion.
            npc.scale = 1f / (zPosition + 1f);
            npc.ShowNameOnHover = true;
            if (Math.Abs(zPosition) >= 0.4f)
            {
                npc.dontTakeDamage = true;
                npc.ShowNameOnHover = false;
            }

            if (zPosition <= -0.96f)
                npc.scale = 0f;

            attackTimer++;
            return false;
        }

        public static void DoLaughEffect(NPC npc)
        {
            SoundEngine.PlaySound(InfernumSoundRegistry.AresLaughSound with
            {
                Volume = 3f
            });
            if (Main.netMode != NetmodeID.MultiplayerClient)
                Utilities.NewProjectileBetter(npc.Center - Vector2.UnitY.RotatedBy(npc.rotation) * 56f, Vector2.Zero, ModContent.ProjectileType<AresLaughBoom>(), 0, 0f);

            ScreenEffectSystem.SetBlurEffect(npc.Center, 1.3f, 45);
        }

        public static void DoBehavior_DeathAnimation(NPC npc, ref float deathAnimationTimer)
        {
            int implosionRingLifetime = 180;
            int pulseRingCreationRate = 32;
            int explosionTime = 240;
            float implosionRingScale = 1.5f;
            float explosionRingScale = 4f;
            Vector2 coreCenter = npc.Center + Vector2.UnitY * 24f;

            // Slow down dramatically.
            npc.velocity *= 0.9f;

            // Use close to the minimum HP.
            npc.life = 50000;

            // Clear away projectiles.
            ExoMechManagement.ClearAwayTransitionProjectiles();

            // Disable damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            // Close the boss HP bar.
            npc.Calamity().ShouldCloseHPBar = true;

            // Create the implosion ring on the first frame.
            if (deathAnimationTimer == 1f)
            {
                GeneralParticleHandler.SpawnParticle(new ElectricExplosionRing(coreCenter, Vector2.Zero, CalamityUtils.ExoPalette, implosionRingScale, implosionRingLifetime));
                SoundEngine.PlaySound(AresBody.EnragedSound, npc.Center);
            }

            // Create particles that fly outward every frame.
            if (deathAnimationTimer > 25f && deathAnimationTimer < implosionRingLifetime - 30f)
            {
                float particleScale = Main.rand.NextFloat(0.1f, 0.15f);
                Vector2 particleVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(10f, 32f);
                Color particleColor = CalamityUtils.MulticolorLerp(Main.rand.NextFloat(), CalamityUtils.ExoPalette);

                for (int j = 0; j < 4; j++)
                    GeneralParticleHandler.SpawnParticle(new StrongBloom(coreCenter, particleVelocity, particleColor, particleScale, 80));

                for (int i = 0; i < 2; i++)
                {
                    particleScale = Main.rand.NextFloat(1.5f, 2f);
                    particleVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(4.5f, 10f);
                    GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(coreCenter, particleVelocity, particleScale, Color.Cyan, 75, 1f, 12f));
                }
            }

            // Periodically create pulse rings.
            if (deathAnimationTimer > 10f && deathAnimationTimer < implosionRingLifetime - 30f && deathAnimationTimer % pulseRingCreationRate == pulseRingCreationRate - 1f)
            {
                float finalScale = Lerp(3f, 5f, Utils.GetLerpValue(25f, 160f, deathAnimationTimer, true));
                Color pulseColor = CalamityUtils.MulticolorLerp(Main.rand.NextFloat(), CalamityUtils.ExoPalette);

                for (int i = 0; i < 3; i++)
                    GeneralParticleHandler.SpawnParticle(new PulseRing(coreCenter, Vector2.Zero, pulseColor, 0.2f, finalScale, pulseRingCreationRate));
            }

            // Create an explosion.
            if (deathAnimationTimer == implosionRingLifetime)
            {
                GeneralParticleHandler.SpawnParticle(new ElectricExplosionRing(coreCenter, Vector2.Zero, CalamityUtils.ExoPalette, explosionRingScale, explosionTime));
                SoundEngine.PlaySound(InfernumSoundRegistry.WyrmChargeSound with
                {
                    Volume = 1.75f
                }, npc.Center);
            }

            deathAnimationTimer++;

            // Fade away as the explosion progresses.
            float opacityFadeInterpolant = Utils.GetLerpValue(implosionRingLifetime + explosionTime * 0.75f, implosionRingLifetime, deathAnimationTimer, true);
            npc.Opacity = Pow(opacityFadeInterpolant, 6.1f);

            if (deathAnimationTimer == (int)(implosionRingLifetime + explosionTime * 0.5f))
            {
                npc.life = 0;
                npc.HitEffect();
                NPC.HitInfo hit = new()
                {
                    Damage = 10,
                    Knockback = 0f,
                    HitDirection = 1
                };
                npc.StrikeNPC(hit);
                npc.checkDead();
            }
        }

        public static void DoBehavior_DoFinalPhaseTransition(NPC npc, Player target, ref float frame, float phaseTransitionAnimationTime)
        {
            // Clear away projectiles.
            ExoMechManagement.ClearAwayTransitionProjectiles();

            npc.velocity *= 0.925f;
            npc.rotation = 0f;

            // Determine frames.
            frame = (int)AresBodyFrameType.Laugh;

            // Heal HP.
            ExoMechAIUtilities.HealInFinalPhase(npc, phaseTransitionAnimationTime);

            // Play the transition sound at the start.
            if (phaseTransitionAnimationTime == 15f)
            {
                npc.Center = target.Center - Vector2.UnitY * 400f;
                npc.velocity = Vector2.Zero;
                npc.netUpdate = true;
                SoundEngine.PlaySound(InfernumSoundRegistry.ExoMechFinalPhaseSound, target.Center);
                DoLaughEffect(npc);

                // Destroy all lasers and telegraphs.
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if ((Main.projectile[i].type == ModContent.ProjectileType<AresDeathBeamTelegraph>() || Main.projectile[i].type == ModContent.ProjectileType<AresSpinningDeathBeam>()) && Main.projectile[i].active)
                        Main.projectile[i].Kill();
                }
            }
        }

        public static void DoBehavior_IdleHover(NPC npc, Player target, ref float attackTimer)
        {
            int attackTime = 936;
            if (ExoMechManagement.CurrentAresPhase >= 5)
                attackTime = 1150;
            if (ExoMechManagement.CurrentAresPhase >= 6)
                attackTime = 1050;

            Vector2 hoverDestination = target.Center - Vector2.UnitY * 410f;
            ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 24f, 75f);

            if (attackTimer > attackTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_LaserSpinBursts(NPC npc, Player target, ref float enraged, ref float attackTimer, ref float frameType, ref float blenderSoundTimer, ref float blenderSoundIsLooping)
        {
            int shootDelay = 90;
            int telegraphTime = 60;
            int spinTime = 600;
            int repositionTime = 50;
            int totalLasers = 11;
            int burstReleaseRate = 50;

            if (ExoMechManagement.CurrentAresPhase >= 5)
                burstReleaseRate -= 8;
            if (ExoMechManagement.CurrentAresPhase >= 6)
                burstReleaseRate -= 8;

            ref float generalAngularOffset = ref npc.Infernum().ExtraAI[0];
            ref float laserDirectionSign = ref npc.Infernum().ExtraAI[1];

            // Slow down.
            npc.velocity *= 0.935f;

            // Stay away from the top of the world, to ensure that the target can deal with the laser spin.
            if (npc.Top.Y <= 3600f)
                npc.position.Y += Utils.GetLerpValue(3600f, 2400f, npc.Top.Y, true) * 30f;

            // Determine an initial direction.
            if (laserDirectionSign == 0f)
            {
                laserDirectionSign = Main.rand.NextBool().ToDirectionInt();
                npc.netUpdate = true;
            }

            // Ensure that the backarm swap state is consistent.
            npc.Infernum().ExtraAI[14] = 240f;

            // Enforce an initial delay prior to firing.
            if (attackTimer < shootDelay)
            {
                if (Utilities.AnyProjectiles(ModContent.ProjectileType<AresSpinningDeathBeam>()))
                    attackTimer = shootDelay + 2f;
                else
                    return;
            }

            // Drift towards the target.
            bool lineOfSightIsClear = Collision.CanHitLine(npc.Center, npc.width, npc.height, npc.Center + npc.SafeDirectionTo(target.Center) * 1000f, npc.width, npc.height);
            if (attackTimer >= shootDelay && npc.ai[0] != (int)AresBodyAttackType.DirectionChangingSpinBursts && lineOfSightIsClear)
                npc.Center = npc.Center.MoveTowards(target.Center, 5.5f);

            // Delete projectiles after the delay has concluded.
            if (attackTimer == shootDelay + 1f)
                ExoMechManagement.ClearAwayTransitionProjectiles();

            // Laugh.
            frameType = (int)AresBodyFrameType.Laugh;
            if (attackTimer == shootDelay + 1f)
                DoLaughEffect(npc);

            // Create telegraph lines that show where the laserbeams will appear.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == shootDelay + 1f)
            {
                generalAngularOffset = Main.rand.NextFloat(TwoPi);
                for (int i = 0; i < totalLasers; i++)
                {
                    Vector2 laserDirection = (TwoPi * i / totalLasers).ToRotationVector2();

                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(telegraph =>
                    {
                        telegraph.localAI[0] = telegraphTime;
                    });
                    Utilities.NewProjectileBetter(npc.Center, laserDirection, ModContent.ProjectileType<AresDeathBeamTelegraph>(), 0, 0f, -1, 0f, npc.whoAmI);
                }
                npc.netUpdate = true;
            }

            // Create laser bursts.
            if (attackTimer == shootDelay + telegraphTime - 1f)
            {
                SoundEngine.PlaySound(TeslaCannon.FireSound, target.Center);

                // Create lightning bolts in the sky.
                int lightningBoltCount = ExoMechManagement.CurrentAresPhase >= 6 ? 55 : 30;
                if (Main.netMode != NetmodeID.Server)
                    ExoMechsSky.CreateLightningBolt(lightningBoltCount, true);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    generalAngularOffset = 0f;
                    blenderSoundTimer = 1f;
                    for (int i = 0; i < totalLasers; i++)
                    {
                        Vector2 laserDirection = (TwoPi * i / totalLasers).ToRotationVector2();

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(deathray =>
                        {
                            deathray.ModProjectile<AresSpinningDeathBeam>().LifetimeThing = spinTime;
                        });
                        Utilities.NewProjectileBetter(npc.Center, laserDirection, ModContent.ProjectileType<AresSpinningDeathBeam>(), PowerfulShotDamage, 0f, -1, 0f, npc.whoAmI);
                    }
                    npc.netUpdate = true;
                }
            }

            // Idly create explosions around the target.
            float adjustedTimer = attackTimer - (shootDelay + telegraphTime);
            bool aboutToTurn = npc.ai[0] == (int)AresBodyAttackType.DirectionChangingSpinBursts && Distance(adjustedTimer, spinTime * 0.5f) < 54f;
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % burstReleaseRate == burstReleaseRate - 1f && attackTimer > shootDelay + telegraphTime + 60f && !aboutToTurn)
            {
                Vector2 targetDirection = target.velocity.SafeNormalize(Main.rand.NextVector2Unit());
                Vector2 spawnPosition = target.Center - targetDirection.RotatedByRandom(1.1f) * Main.rand.NextFloat(325f, 650f) * new Vector2(1f, 0.6f);
                Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<AresBeamExplosion>(), StrongerNormalShotDamage, 0f);
            }

            // Make the laser spin.
            float spinSpeed = Utils.GetLerpValue(0f, 420f, adjustedTimer, true) * Pi / 196f;
            if (npc.ai[0] == (int)AresBodyAttackType.DirectionChangingSpinBursts)
            {
                if (adjustedTimer == (int)(spinTime * 0.5f) - 60)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.PBGMechanicalWarning, target.Center);

                    // Create lightning bolts in the sky.
                    int lightningBoltCount = ExoMechManagement.CurrentAresPhase >= 6 ? 55 : 30;
                    if (Main.netMode != NetmodeID.Server)
                        ExoMechsSky.CreateLightningBolt(lightningBoltCount, true);
                }

                if (adjustedTimer < spinTime * 0.5f)
                    spinSpeed *= Utils.GetLerpValue(spinTime * 0.5f, spinTime * 0.5f - 45f, adjustedTimer, true);
                else
                    spinSpeed *= -Utils.GetLerpValue(spinTime * 0.5f, spinTime * 0.5f + 45f, adjustedTimer, true);
                spinSpeed *= 0.84f;
            }

            // Stop the blender sound.
            if (adjustedTimer >= spinTime - 60)
                blenderSoundIsLooping = 0f;

            // Make the lasers slower in multiplayer.
            if (Main.netMode != NetmodeID.SinglePlayer)
                spinSpeed *= 0.65f;

            generalAngularOffset += spinSpeed * laserDirectionSign;

            // Get pissed off if the player attempts to leave the laser circle.
            if (!npc.WithinRange(target.Center, AresDeathBeamTelegraph.TelegraphWidth + 135f) && enraged == 0f)
            {
                if (Main.LocalPlayer.active && !Main.LocalPlayer.dead)
                    SoundEngine.PlaySound(AresBody.EnragedSound, target.Center);

                // Have Draedon comment on the player's attempts to escape.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.Status.Boss.DraedonAresEnrageText", DraedonNPC.TextColorEdgy);

                enraged = 1f;
                npc.netUpdate = true;
            }

            if (attackTimer >= shootDelay + telegraphTime + spinTime + repositionTime)
            {
                // Destroy all lasers, telegraphs, and bolts.
                Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<AresDeathBeamTelegraph>(), ModContent.ProjectileType<AresSpinningDeathBeam>(),
                    ModContent.ProjectileType<ExoburstSpark>(), ModContent.ProjectileType<AresBeamExplosion>());
                SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_EnergyBladeSlices(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            int attackTime = 420;
            ref float attackDelayTimer = ref npc.Infernum().ExtraAI[0];

            // Laugh.
            frameType = (int)AresBodyFrameType.Laugh;

            attackDelayTimer++;
            if (attackDelayTimer <= 60f)
            {
                if (attackDelayTimer == 1f)
                    DoLaughEffect(npc);

                attackTimer = 1f;
                npc.velocity *= 0.9f;
            }
            else
            {
                // Attempt to loosely hover above the player.
                Vector2 hoverDestination = target.Center - Vector2.UnitY * 320f;
                if (target.velocity.Y < 0f)
                    hoverDestination.Y += target.velocity.Y * 16f;

                Vector2 idealVelocity = (hoverDestination - npc.Center) * 0.09f;
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.04f);
            }

            if (attackTimer >= attackTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_DownwardCrossSlices(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            int sliceCount = 3;
            int anticipationTime = AresEnergyKatana.DownwardCrossSlicesAnticipationTime;
            int sliceTime = AresEnergyKatana.DownwardCrossSlicesSliceTime;
            int holdInPlaceTime = AresEnergyKatana.DownwardCrossSlicesHoldInPlaceTime;
            float wrappedAttackTimer = attackTimer % (anticipationTime + sliceTime + holdInPlaceTime);

            // Laugh.
            frameType = (int)AresBodyFrameType.Laugh;

            // Hover above the target during the anticipation.
            if (wrappedAttackTimer <= anticipationTime)
            {
                Vector2 hoverDestination = target.Center - Vector2.UnitY * 320f;
                if (target.velocity.Y < 0f)
                    hoverDestination.Y += target.velocity.Y * 16f;

                Vector2 idealVelocity = (hoverDestination - npc.Center) * 0.09f;
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.09f);
            }

            // Slow down after anticipation.
            if (wrappedAttackTimer >= anticipationTime)
                npc.velocity *= 0.85f;

            // Create a bunch of energy deathrays.
            if (wrappedAttackTimer == anticipationTime + sliceTime - 10f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float shootOffsetAngle = Main.rand.NextFloat(TwoPi);
                    for (int i = 0; i < 20; i++)
                    {
                        Vector2 shootDirection = (TwoPi * i / 20f + shootOffsetAngle).ToRotationVector2();
                        Utilities.NewProjectileBetter(npc.Center + Vector2.UnitY * 250f, shootDirection, ModContent.ProjectileType<AresEnergyDeathrayTelegraph>(), 0, 0f);
                    }
                }
            }

            if (wrappedAttackTimer == anticipationTime + sliceTime + AresEnergyDeathrayTelegraph.Lifetime - 10f)
                SoundEngine.PlaySound(AresLaserCannon.LaserbeamShootSound, npc.Center);

            if (attackTimer >= (anticipationTime + sliceTime + holdInPlaceTime) * sliceCount)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_ThreeDimensionalSuperslashes(NPC npc, Player target, ref float attackTimer, ref float frameType, ref float zPosition)
        {
            int anticipationTime = AresEnergyKatana.ThreeDimensionalSlicesAnticipationTime;
            int sliceTime = AresEnergyKatana.ThreeDimensionalSlicesSliceTime;
            int laserCount = 11;
            int sliceCount = 4;
            if (ExoMechManagement.CurrentAresPhase >= 6)
                laserCount += 4;

            // Laugh.
            frameType = (int)AresBodyFrameType.Laugh;

            float wrappedAttackTimer = attackTimer % (anticipationTime + sliceTime);
            ref float laserSoundCountdown = ref npc.Infernum().ExtraAI[0];

            // Move into the background and hover above the player.
            if (wrappedAttackTimer <= anticipationTime)
            {
                zPosition = wrappedAttackTimer / anticipationTime * 2.6f;
                npc.Center = Vector2.Lerp(npc.Center, target.Center + new Vector2(target.velocity.X * 12f, zPosition * -100f), 0.1f);
                npc.velocity.X *= 0.9f;

                // Clean up old projectiles.
                ExoMechManagement.ClearAwayTransitionProjectiles();

                if (attackTimer == anticipationTime - 40f)
                {
                    DoLaughEffect(npc);

                    // Play the impending death sound.
                    SoundEngine.PlaySound(InfernumSoundRegistry.ExoMechImpendingDeathSound with
                    {
                        Volume = 2f
                    }, target.Center);
                }
            }
            else
                zPosition -= 0.3f;

            if (wrappedAttackTimer == anticipationTime + sliceTime - 5f)
            {
                // Create slice impact lasers.
                target.Infernum_Camera().CurrentScreenShakePower = 13.5f;
                ScreenEffectSystem.SetFlashEffect(target.Center, 0.8f, 18);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float shootOffsetAngle = Main.rand.NextFloat(TwoPi);
                    for (int i = 0; i < laserCount; i++)
                    {
                        Vector2 shootDirection = (TwoPi * i / laserCount + shootOffsetAngle).ToRotationVector2();
                        Utilities.NewProjectileBetter(Vector2.Lerp(npc.Center, target.Center, 0.55f) + Vector2.UnitY * 150f, shootDirection, ModContent.ProjectileType<AresEnergyDeathrayTelegraph>(), 0, 0f);
                    }
                    laserSoundCountdown = AresEnergyDeathrayTelegraph.Lifetime;
                }

                npc.Center = target.Center - Vector2.UnitY * 1500f;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].realLife == npc.whoAmI)
                    {
                        Main.npc[i].Center = npc.Center;
                        Main.npc[i].netUpdate = true;
                    }
                }

                npc.velocity.Y = 0f;
                zPosition = 0f;
                npc.netUpdate = true;
            }

            // Wait for laser sounds to play.
            if (laserSoundCountdown >= 1f)
            {
                laserSoundCountdown--;
                if (laserSoundCountdown <= 0f)
                    SoundEngine.PlaySound(AresLaserCannon.LaserbeamShootSound, target.Center);
            }

            if (attackTimer >= (anticipationTime + sliceTime) * sliceCount)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_PrecisionBlasts(NPC npc, Player target, ref float enraged, ref float attackTimer, ref float frameType, ref float blenderSoundTimer, ref float blenderSoundIsLooping)
        {
            int startingShootDelay = 60;
            int endingShootDelay = 36;
            int textSubstateTime = 172;
            int cannonAttackTime = 960;
            int metalReleaseRate = 24;

            int laserbeamCount = 6;
            int laserbeamTelegraphTime = 60;
            int laserbeamSpinTime = 900;
            int sparkBurstReleaseRate = 45;
            int circularBoltCount = 17;
            int draedonIndex = NPC.FindFirstNPC(ModContent.NPCType<DraedonNPC>());
            Vector2 coreCenter = npc.Center + Vector2.UnitY * 24f;

            ref float laserAngularOffset = ref npc.Infernum().ExtraAI[0];
            ref float shootCountdown = ref npc.Infernum().ExtraAI[1];
            ref float shootDelay = ref npc.Infernum().ExtraAI[2];
            ref float cannonsCanShoot = ref npc.Infernum().ExtraAI[3];
            ref float cannonAttackTimer = ref npc.Infernum().ExtraAI[4];
            ref float overheatInterpolant = ref npc.localAI[3];
            ref float attackSubstate = ref npc.Infernum().ExtraAI[9];

            // Disable damage during this attack.
            npc.Calamity().DR = 0.9999999f;
            npc.Calamity().unbreakableDR = true;
            npc.Calamity().ShouldCloseHPBar = true;

            // Disable the enrage effect.
            enraged = 0f;

            // Reset the cannons. Attack substates can give them permission to attack.
            cannonsCanShoot = 0f;

            switch ((int)attackSubstate)
            {
                // Sit in place and give some warning text before attacking.
                case 0:
                    if (attackTimer == 1f)
                        DoLaughEffect(npc);

                    // Cease movement.
                    npc.velocity = Vector2.Zero;

                    // Reset the heat interpolant.
                    overheatInterpolant = 0f;

                    // Prevent a bug where the cannons fire too soon.
                    cannonAttackTimer = -5f;

                    if (attackTimer == textSubstateTime / 2)
                        CalamityUtils.DisplayLocalizedText("Mods.InfernumMode.Status.ExoMechDesperationAres1", AresTextColor);

                    if (attackTimer >= textSubstateTime)
                    {
                        CalamityUtils.DisplayLocalizedText("Mods.InfernumMode.Status.ExoMechDesperationAres2", AresTextColor);

                        attackTimer = 0f;
                        attackSubstate = 1f;
                        npc.netUpdate = true;
                    }

                    ExoMechManagement.ClearAwayTransitionProjectiles();

                    break;

                // Hover above the target and begin attacking.
                case 1:
                    Vector2 hoverDestination = target.Center - Vector2.UnitY * 300f;
                    ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 30f, 84f);

                    // Allow shooting.
                    cannonsCanShoot = 1f;

                    // Calculate the shoot delay.
                    int oldShootDelay = (int)shootDelay;
                    shootDelay = (int)Utils.Remap(attackTimer, 0f, cannonAttackTime * 0.55f, startingShootDelay, endingShootDelay);

                    // Calculate the overheat interpolant.
                    overheatInterpolant = Pow(Utils.GetLerpValue(0f, cannonAttackTime * 0.67f, attackTimer, true), 1.96f) * 0.56f;

                    // Account for discrepancies caused by countdowns in the charge delay.
                    if (shootDelay < oldShootDelay)
                        cannonAttackTimer -= oldShootDelay - shootDelay;
                    cannonAttackTimer++;
                    if (cannonAttackTimer >= shootDelay + 1f)
                    {
                        cannonAttackTimer = 0f;
                        npc.netUpdate = true;
                    }

                    // Periodically release chunks of metal into the air.
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % metalReleaseRate == metalReleaseRate - 1f)
                    {
                        Vector2 metalVelocity = -Vector2.UnitY.RotatedByRandom(0.66f) * Main.rand.NextFloat(14f, 17f);
                        Utilities.NewProjectileBetter(npc.Center - Vector2.UnitY * 90f + Main.rand.NextVector2Circular(30f, 30f), metalVelocity, ModContent.ProjectileType<HotMetal>(), StrongerNormalShotDamage, 0f, -1, npc.localAI[3]);
                    }

                    if (attackTimer >= cannonAttackTime)
                    {
                        attackTimer = 0f;
                        attackSubstate = 2f;

                        // Delete leftover projectiles.
                        Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<HotMetal>());

                        npc.netUpdate = true;
                    }

                    break;

                // Hover in place and laugh before performing a final, super-blender.
                case 2:
                    // Cease movement.
                    npc.velocity *= 0.9f;

                    // Cast telegraph lines outward.
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 1f)
                    {
                        for (int i = 0; i < laserbeamCount; i++)
                        {
                            Vector2 laserDirection = (TwoPi * i / laserbeamCount).ToRotationVector2();

                            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(telegraph =>
                            {
                                telegraph.localAI[0] = laserbeamTelegraphTime;
                            });
                            Utilities.NewProjectileBetter(npc.Center, laserDirection, ModContent.ProjectileType<AresDeathBeamTelegraph>(), 0, 0f, -1, 0f, npc.whoAmI);
                        }
                        laserAngularOffset = 0f;
                        npc.netUpdate = true;
                    }

                    // Disable cannon time effects.
                    cannonAttackTimer = 0f;

                    if (attackTimer >= laserbeamTelegraphTime)
                    {
                        DoLaughEffect(npc);

                        SoundEngine.PlaySound(TeslaCannon.FireSound, target.Center);

                        // Create lightning bolts in the sky.
                        if (Main.netMode != NetmodeID.Server)
                            ExoMechsSky.CreateLightningBolt(80, true);

                        // Create the blender.
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 0; i < laserbeamCount; i++)
                            {
                                Vector2 laserDirection = (TwoPi * i / laserbeamCount).ToRotationVector2();

                                ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(deathray =>
                                {
                                    deathray.ModProjectile<AresSpinningDeathBeam>().LifetimeThing = laserbeamSpinTime;
                                });
                                Utilities.NewProjectileBetter(npc.Center, laserDirection, ModContent.ProjectileType<AresSpinningDeathBeam>(), PowerfulShotDamage, 0f, -1, 0f, npc.whoAmI);
                            }

                            attackTimer = 0f;
                            attackSubstate = 3f;
                            blenderSoundTimer = 1f;
                            npc.netUpdate = true;
                        }
                    }

                    break;

                // Do things during the blender.
                case 3:
                    // Grant the target infinite flight time.
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player player = Main.player[i];
                        if (player.dead || !player.active || !npc.WithinRange(player.Center, 10000f))
                            continue;

                        player.DoInfiniteFlightCheck(Color.DarkRed);
                    }

                    // Make the laser spin.
                    float spinSpeedInterpolant = Utils.GetLerpValue(0f, 360f, attackTimer, true);
                    laserAngularOffset += ToRadians(spinSpeedInterpolant * 0.86f);

                    // Periodically release slow bursts of sparks in a spread.
                    if (attackTimer % sparkBurstReleaseRate == sparkBurstReleaseRate - 1f)
                    {
                        SoundEngine.PlaySound(InfernumSoundRegistry.AresTeslaShotSound, npc.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            // Fire a burst of circular sparks along with sparks that are loosely fired towards the target.
                            float circularSpreadAngularOffset = Main.rand.NextFloat(TwoPi);
                            for (int i = 0; i < circularBoltCount; i++)
                            {
                                Vector2 boltShootVelocity = (TwoPi * i / circularBoltCount + circularSpreadAngularOffset).ToRotationVector2() * 9f;
                                Vector2 boltSpawnPosition = coreCenter + boltShootVelocity.SafeNormalize(Vector2.UnitY) * 20f;
                                Utilities.NewProjectileBetter(boltSpawnPosition, boltShootVelocity, ModContent.ProjectileType<AresTeslaSpark>(), NormalShotDamage, 0f);
                            }
                        }
                    }

                    // Make Draedon become enraged if you leave the blender.
                    if (draedonIndex != -1 && Main.npc[draedonIndex].active && Main.npc[draedonIndex].Infernum().ExtraAI[1] == 0f && !npc.WithinRange(target.Center, AresDeathBeamTelegraph.TelegraphWidth + 40f))
                    {
                        SoundEngine.PlaySound(AresBody.EnragedSound with
                        {
                            Volume = 2f
                        });
                        CalamityUtils.DisplayLocalizedText("Mods.InfernumMode.Status.DraedonAresDesperationBlenderLeaveWarning", DraedonNPC.TextColorEdgy);

                        NPC draedon = Main.npc[draedonIndex];
                        draedon.Infernum().ExtraAI[1] = 1f;
                        draedon.netUpdate = true;
                    }

                    if (attackTimer >= laserbeamSpinTime - 60f)
                        blenderSoundIsLooping = 0f;

                    if (attackTimer >= laserbeamSpinTime)
                    {
                        attackTimer = 0f;
                        attackSubstate = 4f;

                        // Delete leftover projectiles.
                        Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<HotMetal>(), ModContent.ProjectileType<AresTeslaSpark>());

                        npc.netUpdate = true;
                    }
                    break;

                // Explode violently.
                case 4:
                     MoonlordDeathDrama.RequestLight(attackTimer * 0.04f, npc.Center);

                    if (attackTimer == 60f)
                    {
                        GeneralParticleHandler.SpawnParticle(new ElectricExplosionRing(coreCenter, Vector2.Zero, CalamityUtils.ExoPalette, 4f, 120));
                        SoundEngine.PlaySound(InfernumSoundRegistry.WyrmChargeSound with
                        {
                            Volume = 1.75f
                        }, npc.Center);
                    }

                    if (attackTimer >= 84f)
                    {
                        npc.life = 0;
                        npc.HitEffect();
                        NPC.HitInfo hit = new()
                        {
                            Damage = 10,
                            Knockback = 0f,
                            HitDirection = 1
                        };
                        npc.StrikeNPC(hit);
                        npc.checkDead();
                    }

                    break;
            }

            // Laugh.
            frameType = (int)AresBodyFrameType.Laugh;

            // Emit smoke once hot enough.
            var smokeDrawer = npc.ModNPC<AresBody>().SmokeDrawer;
            smokeDrawer.ParticleSpawnRate = int.MaxValue;
            if (npc.localAI[3] >= 0.36f)
            {
                smokeDrawer.ParticleSpawnRate = 1;
                smokeDrawer.BaseMoveRotation = npc.rotation + PiOver2;
                smokeDrawer.SpawnAreaCompactness = 120f;
            }
            smokeDrawer.Update();
        }

        public static void SelectNextAttack(NPC npc)
        {
            AresBodyAttackType oldAttackType = (AresBodyAttackType)(int)npc.ai[0];
            ref float previousSuperAttack = ref npc.Infernum().ExtraAI[ExoMechManagement.Ares_PreviousSuperAttackIndex];

            if (ExoMechComboAttackContent.ShouldSelectComboAttack(npc, out ExoMechComboAttackContent.ExoMechComboAttackType newAttack))
                npc.ai[0] = (int)newAttack;
            else
            {
                npc.ai[0] = (int)AresBodyAttackType.IdleHover;

                if (oldAttackType == AresBodyAttackType.IdleHover && ExoMechManagement.CurrentAresPhase >= 2)
                {
                    WeightedRandom<AresBodyAttackType> rng = new(Main.rand);

                    if (previousSuperAttack != (int)AresBodyAttackType.DirectionChangingSpinBursts && previousSuperAttack != (int)AresBodyAttackType.LaserSpinBursts)
                    {
                        rng.Add(AresBodyAttackType.DirectionChangingSpinBursts, 2.2);
                        rng.Add(AresBodyAttackType.LaserSpinBursts, 2.2);
                    }

                    if (ExoMechManagement.CurrentAresPhase >= 5)
                        rng.Add(AresBodyAttackType.ThreeDimensionalSuperslashes, 2.55);
                    else
                    {
                        rng.Add(AresBodyAttackType.DownwardCrossSlices, 1.6);
                        rng.Add(AresBodyAttackType.EnergyBladeSlices, 1.6);
                    }

                    do
                        npc.ai[0] = (int)rng.Get();
                    while (npc.ai[0] == previousSuperAttack);
                    previousSuperAttack = npc.ai[0];

                    // Use the ultimate attack in the final phase.
                    if (ExoMechManagement.CurrentAresPhase >= 6)
                        npc.ai[0] = (int)AresBodyAttackType.PrecisionBlasts;
                }
            }

            npc.ai[1] = 0f;
            npc.Infernum().ExtraAI[ExoMechManagement.Ares_BlenderSoundIsLoopingIndex] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            // Stop being enraged after an idle hover.
            if (oldAttackType == AresBodyAttackType.IdleHover || (int)oldAttackType >= 100f)
                npc.Infernum().ExtraAI[ExoMechManagement.Ares_EnragedIndex] = 0f;

            npc.netUpdate = true;
        }

        public static bool ArmIsDisabled(NPC npc)
        {
            if (CalamityGlobalNPC.draedonExoMechPrime == -1)
                return true;

            if (ExoMechAIUtilities.PerformingDeathAnimation(npc))
                return true;

            NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];

            // Cannons are disabled only when the attack says so during the ultimate attack.
            // Otherwise, all of them fire.
            if (aresBody.ai[0] == (int)AresBodyAttackType.PrecisionBlasts)
                return aresBody.Infernum().ExtraAI[3] == 0f || npc.type == ModContent.NPCType<AresEnergyKatana>();

            int thanatosIndex = NPC.FindFirstNPC(ModContent.NPCType<ThanatosHead>());
            if (thanatosIndex >= 0 && aresBody.ai[0] >= 100f && Main.npc[thanatosIndex].Infernum().ExtraAI[13] < 240f)
                return true;

            // The pulse and laser arm are disabled for 1 second before and after they swap.
            bool rightAboutToSwap = aresBody.Infernum().ExtraAI[14] > BackArmSwapDelay - 150f;
            bool justSwapped = aresBody.Infernum().ExtraAI[14] < 90f;
            if ((rightAboutToSwap || justSwapped) && (npc.type == ModContent.NPCType<AresLaserCannon>() || npc.type == ModContent.NPCType<AresPulseCannon>()))
                return true;

            // If Ares is specifically using a combo attack that specifies certain arms should be active, go based on which ones should be active.
            if (ExoMechComboAttackContent.AffectedAresArms.TryGetValue((ExoMechComboAttackContent.ExoMechComboAttackType)aresBody.ai[0], out int[] activeArms))
                return !activeArms.Contains(npc.type);

            bool chargingUp = aresBody.Infernum().ExtraAI[ExoMechManagement.FinalPhaseTimerIndex] is > 1f and < ExoMechManagement.FinalPhaseTransitionTime;
            if (aresBody.ai[0] == (int)AresBodyAttackType.LaserSpinBursts ||
                aresBody.ai[0] == (int)AresBodyAttackType.DirectionChangingSpinBursts ||
                aresBody.ai[0] == (int)ExoMechComboAttackContent.ExoMechComboAttackType.AresTwins_DualLaserCharges ||
                aresBody.ai[0] == (int)ExoMechComboAttackContent.ExoMechComboAttackType.ThanatosAres_LaserCircle ||
                chargingUp)
            {
                return true;
            }

            // Only the tesla and plasma arms may fire during this attack, and only after the delay has concluded (which is present in the form of a binary switch in ExtraAI[0]).
            if (aresBody.ai[0] == (int)ExoMechComboAttackContent.ExoMechComboAttackType.AresTwins_CircleAttack)
            {
                if (npc.type == ModContent.NPCType<AresTeslaCannon>() || npc.type == ModContent.NPCType<AresPlasmaFlamethrower>())
                    return aresBody.Infernum().ExtraAI[0] == 0f;

                return true;
            }

            if (aresBody.Opacity <= 0f)
                return true;

            // Rotated arm usability is as follows (This only applies after phase 2):
            // Pulse Cannon, Laser Cannon, and Tesla Cannon,
            // Laser Cannon, Tesla Cannon, and Plasma Flamethrower,
            // Tesla Cannon, Plasma Flamethrower, and Pulse Cannon
            // Katanas are completely exempt from this.

            // Rotated arm usability is as follows (This only applies before phase 2):
            // Pulse Cannon, Laser Cannon,
            // Laser Cannon, Tesla Cannon,
            // Tesla Cannon, Plasma Flamethrower,
            // Plasma Flamethrower, Pulse Cannon,
            // Katanas are completely exempt from this.

            if (aresBody.ai[0] is ((int)AresBodyAttackType.EnergyBladeSlices) or ((int)AresBodyAttackType.DownwardCrossSlices) or (int)(AresBodyAttackType.ThreeDimensionalSuperslashes))
                return npc.type != ModContent.NPCType<AresEnergyKatana>();

            if (npc.type == ModContent.NPCType<AresEnergyKatana>())
                return true;

            bool isPulseOrGauss = npc.type == ModContent.NPCType<AresPulseCannon>() || npc.type == ModContent.NPCType<AresGaussNuke>();
            if (ExoMechManagement.CurrentAresPhase <= 2)
            {
                return ((int)aresBody.Infernum().ExtraAI[5] % 4) switch
                {
                    0 => !isPulseOrGauss && npc.type != ModContent.NPCType<AresLaserCannon>(),
                    1 => npc.type != ModContent.NPCType<AresLaserCannon>() && npc.type != ModContent.NPCType<AresTeslaCannon>(),
                    2 => npc.type != ModContent.NPCType<AresTeslaCannon>() && npc.type != ModContent.NPCType<AresPlasmaFlamethrower>(),
                    3 => npc.type != ModContent.NPCType<AresPlasmaFlamethrower>() && !isPulseOrGauss,
                    _ => false,
                };
            }

            return ((int)aresBody.Infernum().ExtraAI[5] % 3) switch
            {
                0 => !isPulseOrGauss && npc.type != ModContent.NPCType<AresLaserCannon>() && npc.type != ModContent.NPCType<AresTeslaCannon>(),
                1 => npc.type != ModContent.NPCType<AresLaserCannon>() && npc.type != ModContent.NPCType<AresTeslaCannon>() && npc.type != ModContent.NPCType<AresPlasmaFlamethrower>(),
                2 => npc.type != ModContent.NPCType<AresTeslaCannon>() && npc.type != ModContent.NPCType<AresPlasmaFlamethrower>() && !isPulseOrGauss,
                _ => false,
            };
        }
        #endregion AI

        #region Frames and Drawcode
        public override void FindFrame(NPC npc, int frameHeight)
        {
            int framesInNormalState = 11;
            ref float currentFrame = ref npc.localAI[2];

            npc.frameCounter++;
            switch ((AresBodyFrameType)(int)npc.localAI[0])
            {
                case AresBodyFrameType.Normal:
                    if (npc.frameCounter >= 6D)
                    {
                        // Reset the frame counter.
                        npc.frameCounter = 0D;

                        // Increment the frame.
                        currentFrame++;

                        // Reset the frames to frame 0 after the animation cycle for the normal phase has concluded.
                        if (currentFrame > framesInNormalState)
                            currentFrame = 0;
                    }
                    break;
                case AresBodyFrameType.Laugh:
                    if (currentFrame is <= 35 or >= 47)
                        currentFrame = 36f;

                    if (npc.frameCounter >= 6D)
                    {
                        // Reset the frame counter.
                        npc.frameCounter = 0D;

                        // Increment the frame.
                        currentFrame++;
                    }
                    break;
            }

            npc.frame = new Rectangle(npc.width * (int)(currentFrame / 8), npc.height * (int)(currentFrame % 8), npc.width, npc.height);
        }

        public static float FlameTrailWidthFunctionBig(NPC npc, float completionRatio)
        {
            return SmoothStep(60f, 22f, completionRatio) * Utils.GetLerpValue(0f, 15f, npc.Infernum().ExtraAI[0], true);
        }

        public static Color FlameTrailColorFunctionBig(NPC npc, float completionRatio)
        {
            float trailOpacity = Utils.GetLerpValue(0.8f, 0.27f, completionRatio, true) * Utils.GetLerpValue(0f, 0.067f, completionRatio, true) * 1.3f;
            Color startingColor = Color.Lerp(Color.White, Color.Yellow, 0.25f);
            Color middleColor = Color.Lerp(Color.Orange, Color.White, 0.35f);
            Color endColor = Color.Lerp(Color.Red, Color.White, 0.17f);
            Color color = CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * Utils.GetLerpValue(0f, 15f, npc.Infernum().ExtraAI[0], true) * trailOpacity;
            color.A = 0;
            return color;
        }

        public static void DrawArm(NPC npc, Vector2 handPosition, Vector2 screenOffset, Color glowmaskColor, int direction, bool backArm, Color? colorToInterpolateTo = null, float colorInterpolant = 0f)
        {
            float scale = npc.scale;
            ref PrimitiveTrail lightningDrawer = ref npc.ModNPC<AresBody>().LightningDrawer;
            ref PrimitiveTrail lightningBackgroundDrawer = ref npc.ModNPC<AresBody>().LightningBackgroundDrawer;

            // Initialize lightning drawers.
            lightningDrawer ??= new PrimitiveTrail(npc.ModNPC<AresBody>().WidthFunction, npc.ModNPC<AresBody>().ColorFunction, PrimitiveTrail.RigidPointRetreivalFunction);
            lightningBackgroundDrawer ??= new PrimitiveTrail(npc.ModNPC<AresBody>().BackgroundWidthFunction, npc.ModNPC<AresBody>().BackgroundColorFunction, PrimitiveTrail.RigidPointRetreivalFunction);

            SpriteEffects spriteDirection = direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float distanceFromHand = npc.Distance(handPosition);
            float frameTime = Main.GlobalTimeWrappedHourly * 0.9f % 1f;

            // Draw back arms.
            if (backArm)
            {
                Texture2D shoulderTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresArmTopShoulder").Value;
                Texture2D armTexture1 = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresArmTopPart1").Value;
                Texture2D armSegmentTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresArmTopSegment").Value;
                Texture2D armTexture2 = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresArmTopPart2").Value;

                Texture2D shoulderGlowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresArmTopShoulderGlow").Value;
                Texture2D armSegmentGlowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresArmTopSegmentGlow").Value;
                Texture2D armGlowmask2 = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresArmTopPart2Glow").Value;

                Vector2 shoulderDrawPosition = npc.Center + scale * new Vector2(direction * 176f, -100f).RotatedBy(npc.spriteDirection * -npc.rotation);
                Vector2 arm1DrawPosition = shoulderDrawPosition + scale * new Vector2(direction * (shoulderTexture.Width + 16f), 10f);
                Vector2 armSegmentDrawPosition = arm1DrawPosition;

                // Determine frames.
                Rectangle shoulderFrame = shoulderTexture.Frame(1, 9, 0, (int)(frameTime * 9f));
                Rectangle armSegmentFrame = armSegmentTexture.Frame(1, 9, 0, (int)(frameTime * 9f));
                Rectangle arm2Frame = armTexture2.Frame(1, 9, 0, (int)(frameTime * 9f));

                Vector2 arm1Origin = armTexture1.Size() * new Vector2((direction == 1).ToInt(), 0.5f);
                Vector2 arm2Origin = arm2Frame.Size() * new Vector2((direction == 1).ToInt(), 0.5f);

                float arm1Rotation = Clamp(distanceFromHand * direction / 1200f, -0.12f, 0.12f);
                float arm2Rotation = (handPosition - armSegmentDrawPosition - Vector2.UnitY * 12f).ToRotation();
                if (direction == 1)
                    arm2Rotation += Pi;
                float armSegmentRotation = arm2Rotation;

                // Handle offsets for points.
                armSegmentDrawPosition += arm1Rotation.ToRotationVector2() * scale * direction * -14f;
                armSegmentDrawPosition -= arm2Rotation.ToRotationVector2() * scale * direction * 20f;
                Vector2 arm2DrawPosition = armSegmentDrawPosition;
                arm2DrawPosition -= arm2Rotation.ToRotationVector2() * direction * scale * 40f;
                arm2DrawPosition += (arm2Rotation - PiOver2).ToRotationVector2() * scale * 14f;

                // Calculate colors.
                Color shoulderLightColor = npc.GetAlpha(Lighting.GetColor((int)shoulderDrawPosition.X / 16, (int)shoulderDrawPosition.Y / 16));
                Color arm1LightColor = npc.GetAlpha(Lighting.GetColor((int)arm1DrawPosition.X / 16, (int)arm1DrawPosition.Y / 16));
                Color armSegmentLightColor = npc.GetAlpha(Lighting.GetColor((int)armSegmentDrawPosition.X / 16, (int)armSegmentDrawPosition.Y / 16));
                Color arm2LightColor = npc.GetAlpha(Lighting.GetColor((int)arm2DrawPosition.X / 16, (int)arm2DrawPosition.Y / 16));
                Color glowmaskAlphaColor = npc.GetAlpha(glowmaskColor);
                if (colorInterpolant >= 0f && colorToInterpolateTo.HasValue)
                {
                    shoulderLightColor = Color.Lerp(shoulderLightColor, colorToInterpolateTo.Value, colorInterpolant);
                    arm1LightColor = Color.Lerp(arm1LightColor, colorToInterpolateTo.Value, colorInterpolant);
                    armSegmentLightColor = Color.Lerp(armSegmentLightColor, colorToInterpolateTo.Value, colorInterpolant);
                    arm2LightColor = Color.Lerp(arm2LightColor, colorToInterpolateTo.Value, colorInterpolant);
                }

                // Draw electricity between arms.
                if (npc.Opacity > 0f && !npc.IsABestiaryIconDummy)
                {
                    List<Vector2> arm2ElectricArcPoints = AresTeslaOrb.DetermineElectricArcPoints(armSegmentDrawPosition, arm2DrawPosition + arm2Rotation.ToRotationVector2() * -direction * scale * 20f, 250290787);
                    lightningBackgroundDrawer.Draw(arm2ElectricArcPoints, -Main.screenPosition, 90);
                    lightningDrawer.Draw(arm2ElectricArcPoints, -Main.screenPosition, 90);

                    // Draw electricity between the final arm and the hand.
                    List<Vector2> handElectricArcPoints = AresTeslaOrb.DetermineElectricArcPoints(arm2DrawPosition - arm2Rotation.ToRotationVector2() * direction * scale * 100f, handPosition, 27182);
                    lightningBackgroundDrawer.Draw(handElectricArcPoints, -Main.screenPosition, 90);
                    lightningDrawer.Draw(handElectricArcPoints, -Main.screenPosition, 90);
                }

                shoulderDrawPosition += Vector2.UnitY * npc.gfxOffY - screenOffset;
                arm1DrawPosition += Vector2.UnitY * npc.gfxOffY - screenOffset;
                armSegmentDrawPosition += Vector2.UnitY * npc.gfxOffY - screenOffset;
                arm2DrawPosition += Vector2.UnitY * npc.gfxOffY - screenOffset;

                Main.spriteBatch.Draw(armTexture1, arm1DrawPosition, null, arm1LightColor, arm1Rotation, arm1Origin, scale, spriteDirection ^ SpriteEffects.FlipHorizontally, 0f);
                Main.spriteBatch.Draw(shoulderTexture, shoulderDrawPosition, shoulderFrame, shoulderLightColor, 0f, shoulderFrame.Size() * 0.5f, scale, spriteDirection, 0f);
                Main.spriteBatch.Draw(shoulderGlowmask, shoulderDrawPosition, shoulderFrame, glowmaskAlphaColor, 0f, shoulderFrame.Size() * 0.5f, scale, spriteDirection, 0f);
                Main.spriteBatch.Draw(armSegmentTexture, armSegmentDrawPosition, armSegmentFrame, armSegmentLightColor, armSegmentRotation, armSegmentFrame.Size() * 0.5f, scale, spriteDirection ^ SpriteEffects.FlipHorizontally, 0f);
                Main.spriteBatch.Draw(armSegmentGlowmask, armSegmentDrawPosition, armSegmentFrame, glowmaskAlphaColor, armSegmentRotation, armSegmentFrame.Size() * 0.5f, scale, spriteDirection ^ SpriteEffects.FlipHorizontally, 0f);
                Main.spriteBatch.Draw(armTexture2, arm2DrawPosition, arm2Frame, arm2LightColor, arm2Rotation, arm2Origin, scale, spriteDirection ^ SpriteEffects.FlipVertically, 0f);
                Main.spriteBatch.Draw(armGlowmask2, arm2DrawPosition, arm2Frame, glowmaskAlphaColor, arm2Rotation, arm2Origin, scale, spriteDirection ^ SpriteEffects.FlipVertically, 0f);
            }
            else
            {
                Texture2D shoulderTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresBottomArmShoulder").Value;
                Texture2D connectorTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresBottomArmConnector").Value;
                Texture2D armTexture1 = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresBottomArmPart1").Value;
                Texture2D armTexture2 = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresBottomArmPart2").Value;

                Texture2D shoulderGlowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresBottomArmShoulderGlow").Value;
                Texture2D armTexture1Glowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresBottomArmPart1Glow").Value;
                Texture2D armTexture2Glowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresBottomArmPart2Glow").Value;

                Vector2 shoulderDrawPosition = npc.Center + scale * new Vector2(direction * 110f, -54f).RotatedBy(npc.spriteDirection * -npc.rotation);
                Vector2 connectorDrawPosition = shoulderDrawPosition + scale * new Vector2(direction * 20f, 32f);
                Vector2 arm1DrawPosition = shoulderDrawPosition + scale * Vector2.UnitX * direction * 20f;

                // Determine frames.
                Rectangle arm1Frame = armTexture1.Frame(1, 9, 0, (int)(frameTime * 9f));
                Rectangle shoulderFrame = shoulderTexture.Frame(1, 9, 0, (int)(frameTime * 9f));
                Rectangle arm2Frame = armTexture2.Frame(1, 9, 0, (int)(frameTime * 9f));

                Vector2 arm1Origin = arm1Frame.Size() * new Vector2((direction == 1).ToInt(), 0.5f);
                Vector2 arm2Origin = arm2Frame.Size() * new Vector2((direction == 1).ToInt(), 0.5f);

                float arm1Rotation = CalamityUtils.WrapAngle90Degrees((handPosition - shoulderDrawPosition).ToRotation()) * 0.5f;
                connectorDrawPosition += arm1Rotation.ToRotationVector2() * scale * direction * -26f;
                arm1DrawPosition += arm1Rotation.ToRotationVector2() * scale * direction * (armTexture1.Width - 14f);
                float arm2Rotation = CalamityUtils.WrapAngle90Degrees((handPosition - arm1DrawPosition).ToRotation());

                Vector2 arm2DrawPosition = arm1DrawPosition + arm2Rotation.ToRotationVector2() * scale * direction * (armTexture2.Width + 16f) - Vector2.UnitY * scale * 16f;

                // Calculate colors.
                Color shoulderLightColor = npc.GetAlpha(Lighting.GetColor((int)shoulderDrawPosition.X / 16, (int)shoulderDrawPosition.Y / 16));
                Color arm1LightColor = npc.GetAlpha(Lighting.GetColor((int)arm1DrawPosition.X / 16, (int)arm1DrawPosition.Y / 16));
                Color arm2LightColor = npc.GetAlpha(Lighting.GetColor((int)arm2DrawPosition.X / 16, (int)arm2DrawPosition.Y / 16));
                Color glowmaskAlphaColor = npc.GetAlpha(glowmaskColor);
                if (colorInterpolant >= 0f && colorToInterpolateTo.HasValue)
                {
                    shoulderLightColor = Color.Lerp(shoulderLightColor, colorToInterpolateTo.Value, colorInterpolant);
                    arm1LightColor = Color.Lerp(arm1LightColor, colorToInterpolateTo.Value, colorInterpolant);
                    arm2LightColor = Color.Lerp(arm2LightColor, colorToInterpolateTo.Value, colorInterpolant);
                }

                // Draw electricity between arms.
                if (npc.Opacity > 0f && !npc.IsABestiaryIconDummy)
                {
                    List<Vector2> arm2ElectricArcPoints = AresTeslaOrb.DetermineElectricArcPoints(arm1DrawPosition - arm2Rotation.ToRotationVector2() * direction * scale * 10f, arm1DrawPosition + arm2Rotation.ToRotationVector2() * direction * 20f, 31416);
                    lightningBackgroundDrawer.Draw(arm2ElectricArcPoints, -Main.screenPosition, 44);
                    lightningDrawer.Draw(arm2ElectricArcPoints, -Main.screenPosition, 44);

                    // Draw electricity between the final arm and the hand.
                    List<Vector2> handElectricArcPoints = AresTeslaOrb.DetermineElectricArcPoints(arm2DrawPosition - arm2Rotation.ToRotationVector2() * direction * scale * 20f, handPosition, 27182);
                    lightningBackgroundDrawer.Draw(handElectricArcPoints, -Main.screenPosition, 44);
                    lightningDrawer.Draw(handElectricArcPoints, -Main.screenPosition, 44);
                }

                shoulderDrawPosition += Vector2.UnitY * npc.gfxOffY - screenOffset;
                connectorDrawPosition += Vector2.UnitY * npc.gfxOffY - screenOffset;
                arm1DrawPosition += Vector2.UnitY * npc.gfxOffY - screenOffset;
                arm2DrawPosition += Vector2.UnitY * npc.gfxOffY - screenOffset;

                Main.spriteBatch.Draw(shoulderTexture, shoulderDrawPosition, shoulderFrame, shoulderLightColor, arm1Rotation, shoulderFrame.Size() * 0.5f, scale, spriteDirection ^ SpriteEffects.FlipHorizontally, 0f);
                Main.spriteBatch.Draw(shoulderGlowmask, shoulderDrawPosition, shoulderFrame, glowmaskAlphaColor, arm1Rotation, shoulderFrame.Size() * 0.5f, scale, spriteDirection ^ SpriteEffects.FlipHorizontally, 0f);
                Main.spriteBatch.Draw(connectorTexture, connectorDrawPosition, null, shoulderLightColor, 0f, connectorTexture.Size() * 0.5f, scale, spriteDirection ^ SpriteEffects.FlipHorizontally, 0f);
                Main.spriteBatch.Draw(armTexture1, arm1DrawPosition, arm1Frame, arm1LightColor, arm1Rotation, arm1Origin, scale, spriteDirection ^ SpriteEffects.FlipHorizontally, 0f);
                Main.spriteBatch.Draw(armTexture1Glowmask, arm1DrawPosition, arm1Frame, glowmaskAlphaColor, arm1Rotation, arm1Origin, scale, spriteDirection ^ SpriteEffects.FlipHorizontally, 0f);
                Main.spriteBatch.Draw(armTexture2, arm2DrawPosition, arm2Frame, arm2LightColor, arm2Rotation, arm2Origin, scale, spriteDirection ^ SpriteEffects.FlipHorizontally, 0f);
                Main.spriteBatch.Draw(armTexture2Glowmask, arm2DrawPosition, arm2Frame, glowmaskAlphaColor, arm2Rotation, arm2Origin, scale, spriteDirection ^ SpriteEffects.FlipHorizontally, 0f);
            }
        }

        public static void DrawArmWithIK(NPC npc, NPC katana, Color glowmaskColor, int direction)
        {
            float scale = npc.scale;
            SpriteEffects spriteDirection = direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Color glowmaskAlphaColor = npc.GetAlpha(glowmaskColor);

            LimbCollection limbs = katana.ModNPC<AresEnergyKatana>()?.Limbs ?? null;
            if (limbs is null || limbs[0] is null || limbs[1] is null)
                return;

            Texture2D shoulderTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresBottomArmShoulder").Value;
            Texture2D armTexture1 = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresBottomArmPart1").Value;
            Texture2D armTexture2 = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresBottomArmPart2").Value;

            Texture2D shoulderGlowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresBottomArmShoulderGlow").Value;
            Texture2D armTexture1Glowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresBottomArmPart1Glow").Value;
            Texture2D armTexture2Glowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresBottomArmPart2Glow").Value;

            // Determine draw positions.
            Vector2 arm1DrawPosition = limbs.Limbs[0].ConnectPoint;
            arm1DrawPosition = npc.Center + (arm1DrawPosition - npc.Center) * scale - Main.screenPosition;
            Vector2 shoulderDrawPosition = arm1DrawPosition - Vector2.UnitY * scale * 26f;
            Vector2 arm2DrawPosition = arm1DrawPosition + ((float)limbs.Limbs[0].Rotation).ToRotationVector2() * (float)limbs.Limbs[0].Length * scale;

            // Determine frames.
            float frameTime = Main.GlobalTimeWrappedHourly * 0.9f % 1f;
            Rectangle arm1Frame = armTexture1.Frame(1, 9, 0, (int)(frameTime * 9f));
            Rectangle shoulderFrame = shoulderTexture.Frame(1, 9, 0, (int)(frameTime * 9f));
            Rectangle arm2Frame = armTexture2.Frame(1, 9, 0, (int)(frameTime * 9f));

            // Determine rotations.
            float arm1Rotation = (float)limbs[0].Rotation;
            float arm2Rotation = (float)limbs[1].Rotation;
            if (direction == -1)
            {
                arm1Rotation += Pi;
                arm2Rotation += Pi;
            }

            // Determine origins.
            Vector2 arm1Origin = arm1Frame.Size() * new Vector2(direction == -1 ? 1f : 0f, 0.5f);
            Vector2 arm2Origin = arm2Frame.Size() * new Vector2(direction == -1 ? 1f : 0f, 0.5f);

            // Determine colors.
            Color arm1Color = Lighting.GetColor((arm1DrawPosition + Main.screenPosition).ToTileCoordinates());
            Color arm2Color = Lighting.GetColor((arm2DrawPosition + Main.screenPosition).ToTileCoordinates());

            // Draw the shoulder.
            Main.spriteBatch.Draw(shoulderTexture, shoulderDrawPosition, shoulderFrame, npc.GetAlpha(Color.White), 0f, shoulderFrame.Size() * 0.5f, scale, spriteDirection ^ SpriteEffects.FlipHorizontally, 0f);
            Main.spriteBatch.Draw(shoulderGlowmask, shoulderDrawPosition, shoulderFrame, glowmaskAlphaColor, 0f, shoulderFrame.Size() * 0.5f, scale, spriteDirection ^ SpriteEffects.FlipHorizontally, 0f);

            // Draw the forearm.
            Main.spriteBatch.Draw(armTexture1, arm1DrawPosition, arm1Frame, npc.GetAlpha(arm1Color), arm1Rotation, arm1Origin, scale, spriteDirection ^ SpriteEffects.FlipHorizontally, 0f);
            Main.spriteBatch.Draw(armTexture1Glowmask, arm1DrawPosition, arm1Frame, glowmaskAlphaColor, arm1Rotation, arm1Origin, scale, spriteDirection ^ SpriteEffects.FlipHorizontally, 0f);

            // Draw the arm.
            Main.spriteBatch.Draw(armTexture2, arm2DrawPosition, arm2Frame, npc.GetAlpha(arm2Color), arm2Rotation, arm2Origin, scale, spriteDirection ^ SpriteEffects.FlipHorizontally, 0f);
            Main.spriteBatch.Draw(armTexture2Glowmask, arm2DrawPosition, arm2Frame, glowmaskAlphaColor, arm2Rotation, arm2Origin, scale, spriteDirection ^ SpriteEffects.FlipHorizontally, 0f);
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            // Draw arms.
            int photonRipperID = ModContent.NPCType<AresEnergyKatana>();
            int laserArm = NPC.FindFirstNPC(ModContent.NPCType<AresLaserCannon>());
            int pulseArm = NPC.FindFirstNPC(ModContent.NPCType<AresPulseCannon>());
            int teslaArm = NPC.FindFirstNPC(ModContent.NPCType<AresTeslaCannon>());
            int plasmaArm = NPC.FindFirstNPC(ModContent.NPCType<AresPlasmaFlamethrower>());
            List<NPC> katanas = Main.npc.Take(Main.maxNPCs).
                Where(n => n.active && n.type == photonRipperID).ToList();
            Color afterimageBaseColor = Color.White;
            float scale = npc.scale;

            // Become red if enraged.
            if (Enraged || ExoMechComboAttackContent.EnrageTimer > 0f)
                afterimageBaseColor = Color.Red;

            Color armGlowmaskColor = afterimageBaseColor;
            armGlowmaskColor.A = 184;

            // Interpolate towards overheat colors.
            Color baseInterpolateColor = Color.Red with
            {
                A = 100
            };
            lightColor = Color.Lerp(lightColor, baseInterpolateColor, npc.localAI[3] * 0.48f);
            armGlowmaskColor = Color.Lerp(armGlowmaskColor, Color.Red with
            {
                A = 0
            }, npc.localAI[3] * 0.48f);

            (int, bool)[] armProperties = new (int, bool)[]
            {
                // Laser arm.
                (-1, true),

                // Pulse arm.
                (1, true),

                // Telsa arm.
                (-1, false),

                // Plasma arm.
                (1, false),
            };

            // Swap arms as necessary
            if (npc.Infernum().ExtraAI[ExoMechManagement.Ares_BackArmsAreSwappedIndex] == 1f)
            {
                armProperties[0] = (1, true);
                armProperties[1] = (-1, true);
            }

            // Draw smoke.
            npc.ModNPC<AresBody>().SmokeDrawer.DrawSet(npc.Center);

            // Draw arms for each hand.
            if (npc.Opacity > 0.05f)
            {
                if (laserArm != -1)
                    DrawArm(npc, Main.npc[laserArm].Center, Main.screenPosition, armGlowmaskColor, armProperties[0].Item1, armProperties[0].Item2, baseInterpolateColor, npc.localAI[3] * 0.6f);
                if (pulseArm != -1)
                    DrawArm(npc, Main.npc[pulseArm].Center, Main.screenPosition, armGlowmaskColor, armProperties[1].Item1, armProperties[1].Item2, baseInterpolateColor, npc.localAI[3] * 0.6f);
                if (teslaArm != -1)
                    DrawArm(npc, Main.npc[teslaArm].Center, Main.screenPosition, armGlowmaskColor, armProperties[2].Item1, armProperties[2].Item2, baseInterpolateColor, npc.localAI[3] * 0.6f);
                if (plasmaArm != -1)
                    DrawArm(npc, Main.npc[plasmaArm].Center, Main.screenPosition, armGlowmaskColor, armProperties[3].Item1, armProperties[3].Item2, baseInterpolateColor, npc.localAI[3] * 0.6f);
            }

            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Rectangle frame = npc.frame;
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 center = npc.Center - Main.screenPosition;

            float finalPhaseGlowInterpolant = Utils.GetLerpValue(0f, ExoMechManagement.FinalPhaseTransitionTime * 0.75f, npc.Infernum().ExtraAI[ExoMechManagement.FinalPhaseTimerIndex], true);
            if (finalPhaseGlowInterpolant > 0f)
            {
                float backAfterimageOffset = finalPhaseGlowInterpolant * 6f + npc.localAI[3] * 10f;
                for (int i = 0; i < 8; i++)
                {
                    Color color = Main.hslToRgb((i / 8f + Main.GlobalTimeWrappedHourly * 0.6f) % 1f, 1f, 0.56f) * 0.5f;
                    color = Color.Lerp(color, Color.Red * 0.3f, npc.localAI[3]);
                    color.A = 0;

                    Vector2 drawOffset = (TwoPi * i / 8f + Main.GlobalTimeWrappedHourly * 0.8f).ToRotationVector2() * backAfterimageOffset;
                    Main.spriteBatch.Draw(texture, center + drawOffset, frame, npc.GetAlpha(color), npc.rotation, origin, scale, SpriteEffects.None, 0f);
                }
            }

            Main.spriteBatch.Draw(texture, center, frame, npc.GetAlpha(lightColor), npc.rotation, origin, scale, SpriteEffects.None, 0f);

            texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Ares/AresBodyGlow").Value;
            Main.spriteBatch.Draw(texture, center, frame, afterimageBaseColor * npc.Opacity, npc.rotation, origin, scale, SpriteEffects.None, 0);

            if (npc.Opacity > 0.05f)
            {
                foreach (NPC katana in katanas)
                {
                    int direction = (int)katana.ai[2];
                    DrawArmWithIK(npc, katana, armGlowmaskColor, direction);
                }
            }

            // Draw line telegraphs.
            float telegraphInterpolant = npc.Infernum().ExtraAI[ExoMechManagement.Ares_LineTelegraphInterpolantIndex];
            if (telegraphInterpolant > 0f)
            {
                Main.spriteBatch.SetBlendState(BlendState.Additive);

                Texture2D telegraphTexture = InfernumTextureRegistry.BloomLine.Value;
                float telegraphRotation = npc.Infernum().ExtraAI[ExoMechManagement.Ares_LineTelegraphRotationIndex];
                float telegraphScaleFactor = telegraphInterpolant * 1.2f;
                Vector2 telegraphStart = npc.Center + Vector2.UnitY * scale * 34f + telegraphRotation.ToRotationVector2() * scale * 20f - Main.screenPosition;
                Vector2 telegraphOrigin = new Vector2(0.5f, 0f) * telegraphTexture.Size();
                Vector2 telegraphScale = new(telegraphScaleFactor, 3f);
                Vector2 telegraphInnerScale = telegraphScale * 0.75f;
                Color telegraphColor = new Color(255, 55, 0) * Pow(telegraphInterpolant, 0.79f);
                Color innerColor = Color.Lerp(telegraphColor, Color.White, 0.35f);
                Main.spriteBatch.Draw(telegraphTexture, telegraphStart, null, telegraphColor, telegraphRotation - PiOver2, telegraphOrigin, telegraphScale, 0, 0f);
                Main.spriteBatch.Draw(telegraphTexture, telegraphStart, null, innerColor, telegraphRotation - PiOver2, telegraphOrigin, telegraphInnerScale, 0, 0f);
                Main.spriteBatch.ResetBlendState();
            }

            return false;
        }
        #endregion Frames and Drawcode

        #region Death Effects
        public override bool CheckDead(NPC npc)
        {
            if (npc.ai[0] == (int)AresBodyAttackType.PrecisionBlasts && ExoMechManagement.TotalMechs <= 1)
                return true;

            return ExoMechManagement.HandleDeathEffects(npc);
        }
        #endregion Death Effects

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Mods.InfernumMode.PetDialog.AresTip1";
            yield return n => "Mods.InfernumMode.PetDialog.AresTip2";
        }
        #endregion Tips
    }
}
