using CalamityMod;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Skies;
using CalamityMod.Sounds;
using InfernumMode;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Common.Graphics.ScreenEffects;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.ComboAttacks;
using InfernumMode.Core.GlobalInstances;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.DraedonBehaviorOverride;
using AresPlasmaFireballInfernum = InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares.AresPlasmaFireball;
using DraedonNPC = CalamityMod.NPCs.ExoMechs.Draedon;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo
{
    public class ApolloBehaviorOverride : NPCBehaviorOverride
    {
        public enum TwinsAttackType
        {
            BasicShots,
            FireCharge,
            ApolloPlasmaCharges,
            ArtemisLaserRay,
            GatlingLaserAndPlasmaFlames,
            SlowLaserRayAndPlasmaBlasts,

            // Ultimate attack. Only happens when in the final phase.
            ThermonuclearBlitz
        }

        public override int NPCOverrideType => ModContent.NPCType<Apollo>();

        public const int Phase2TransitionTime = 270;

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            ExoMechManagement.Phase3LifeRatio,
            ExoMechManagement.Phase4LifeRatio
        };

        #region Loading
        public override void Load()
        {
            GlobalNPCOverrides.BossHeadSlotEvent += ChangeMapIconConditions;
        }

        private void ChangeMapIconConditions(NPC npc, ref int index)
        {
            if (npc.type == ModContent.NPCType<Artemis>())
            {
                if (npc.Opacity <= 0f)
                    index = -1;
                else if (ExoMechManagement.ExoTwinsAreInSecondPhase)
                    index = Artemis.phase2IconIndex;
                else
                    index = Artemis.phase1IconIndex;
            }
            if (npc.type == ModContent.NPCType<Apollo>())
            {
                if (npc.Opacity <= 0f)
                    index = -1;
                else if (ExoMechManagement.ExoTwinsAreInSecondPhase)
                    index = Apollo.phase2IconIndex;
                else
                    index = Apollo.phase1IconIndex;
            }
        }
        #endregion Loading

        #region Netcode Syncs

        public override void SendExtraData(NPC npc, ModPacket writer) => writer.Write(npc.Opacity);

        public override void ReceiveExtraData(NPC npc, BinaryReader reader) => npc.Opacity = reader.ReadSingle();

        #endregion Netcode Syncs

        #region AI
        public override void SetDefaults(NPC npc)
        {
            // Set defaults that, if were to be changed by Calamity, would cause significant issues to the fight.
            npc.width = 204;
            npc.height = 226;
            npc.scale = 1f;
            npc.Opacity = 0f;
            npc.defense = 80;
            npc.DR_NERD(0.25f);
        }

        public override bool PreAI(NPC npc)
        {
            // Define the life ratio.
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Define the whoAmI variable.
            CalamityGlobalNPC.draedonExoMechTwinGreen = npc.whoAmI;

            // Define attack variables.
            bool performingDeathAnimation = ExoMechAIUtilities.PerformingDeathAnimation(npc);
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float hoverSide = ref npc.ai[2];
            ref float phaseTransitionAnimationTime = ref npc.ai[3];
            ref float frame = ref npc.localAI[0];
            ref float hasDoneInitializations = ref npc.Infernum().ExtraAI[5];
            ref float hasSummonedComplementMech = ref npc.Infernum().ExtraAI[ExoMechManagement.HasSummonedComplementMechIndex];
            ref float complementMechIndex = ref npc.Infernum().ExtraAI[ExoMechManagement.ComplementMechIndexIndex];
            ref float wasNotInitialSummon = ref npc.Infernum().ExtraAI[ExoMechManagement.WasNotInitialSummonIndex];
            ref float finalMechIndex = ref npc.Infernum().ExtraAI[ExoMechManagement.FinalMechIndexIndex];
            ref float enrageTimer = ref npc.Infernum().ExtraAI[ExoMechManagement.Twins_ComplementMechEnrageTimerIndex];
            ref float finalPhaseAnimationTime = ref npc.Infernum().ExtraAI[ExoMechManagement.FinalPhaseTimerIndex];
            ref float sideSwitchAttackDelay = ref npc.Infernum().ExtraAI[ExoMechManagement.Twins_SideSwitchDelayIndex];
            ref float deathAnimationTimer = ref npc.Infernum().ExtraAI[ExoMechManagement.DeathAnimationTimerIndex];

            NPC initialMech = ExoMechManagement.FindInitialMech();
            NPC complementMech = complementMechIndex >= 0 && Main.npc[(int)complementMechIndex].active && Utilities.IsExoMech(Main.npc[(int)complementMechIndex]) ? Main.npc[(int)complementMechIndex] : null;
            NPC finalMech = ExoMechManagement.FindFinalMech();
            if (initialMech != null && !ExoMechAIUtilities.ShouldExoMechVanish(initialMech))
                enrageTimer = ref initialMech.Infernum().ExtraAI[ExoMechManagement.Twins_ComplementMechEnrageTimerIndex];

            // Intialize things and summon Artemis.
            if (Main.netMode != NetmodeID.MultiplayerClient && hasDoneInitializations == 0f)
            {
                hoverSide = 1f;
                complementMechIndex = -1f;
                finalMechIndex = -1f;
                sideSwitchAttackDelay = 60f;
                hasDoneInitializations = 1f;

                int artemis = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<Artemis>(), npc.whoAmI);
                if (Main.npc.IndexInRange(artemis))
                    Main.npc[artemis].realLife = npc.whoAmI;

                npc.netUpdate = true;
            }

            // Reset things.
            npc.damage = 0;
            npc.defDamage = TwinsChargeContactDamage;
            npc.dontTakeDamage = false;
            npc.Calamity().newAI[0] = (int)Apollo.Phase.ChargeCombo;
            npc.Calamity().DR = 0.3f;
            npc.Calamity().unbreakableDR = false;

            // Decrement the enrage timer.
            if (enrageTimer > 0f)
                enrageTimer--;

            // Summon the complement mech and reset things once ready.
            if (hasSummonedComplementMech == 0f && lifeRatio < ExoMechManagement.Phase4LifeRatio)
            {
                ExoMechManagement.SummonComplementMech(npc);
                hasSummonedComplementMech = 1f;
                attackTimer = 0f;
                SelectNextAttack(npc);

                // Clear away projectiles to prevent lingering, unfair things so that the combo attacks have a clean, open area.
                ExoMechManagement.ClearAwayTransitionProjectiles();

                npc.netUpdate = true;
            }

            // Summon the final mech once ready.
            if (wasNotInitialSummon == 0f && finalMechIndex == -1f && complementMech != null && complementMech.life / (float)complementMech?.lifeMax < ExoMechManagement.ComplementMechInvincibilityThreshold)
            {
                ExoMechManagement.SummonFinalMech(npc);
                npc.netUpdate = true;
            }

            // Become invincible if the complement mech is at high enough health or if in the middle of a death animation.
            npc.dontTakeDamage = ExoMechAIUtilities.PerformingDeathAnimation(npc);
            if (complementMechIndex >= 0 && Main.npc[(int)complementMechIndex].active && Main.npc[(int)complementMechIndex].life > Main.npc[(int)complementMechIndex].lifeMax * ExoMechManagement.ComplementMechInvincibilityThreshold)
                npc.dontTakeDamage = true;

            // Get a target.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Become more resistant to damage as necessary.
            npc.takenDamageMultiplier = 1f;
            if (ExoMechManagement.ShouldHaveSecondComboPhaseResistance(npc))
                npc.takenDamageMultiplier *= 0.5f;

            // Become invincible and disappear if necessary.
            npc.Calamity().newAI[1] = 0f;
            if (ExoMechAIUtilities.ShouldExoMechVanish(npc))
            {
                npc.Opacity = Clamp(npc.Opacity - 0.08f, 0f, 1f);
                if (npc.Opacity <= 0f)
                    npc.Center = target.Center - Vector2.UnitY * 2700f;

                attackTimer = 0f;
                attackState = (int)TwinsAttackType.BasicShots;
                npc.Calamity().newAI[1] = (int)Apollo.SecondaryPhase.PassiveAndImmune;
                npc.ModNPC<Apollo>().ChargeComboFlash = 0f;
                npc.Calamity().ShouldCloseHPBar = true;
                npc.dontTakeDamage = true;
            }
            else
                npc.Opacity = Clamp(npc.Opacity + 0.08f, 0f, 1f);

            // Despawn if the target is gone.
            if (!target.active || target.dead)
            {
                npc.TargetClosest(false);
                target = Main.player[npc.target];
                if (!target.active || target.dead)
                    npc.active = false;
            }

            // Handle the second phase transition.
            if (phaseTransitionAnimationTime < Phase2TransitionTime && lifeRatio < ExoMechManagement.Phase3LifeRatio)
            {
                if (phaseTransitionAnimationTime == 0f)
                {
                    SelectNextAttack(npc);
                    ExoMechManagement.ClearAwayTransitionProjectiles();
                }

                npc.ModNPC<Apollo>().ChargeComboFlash = 0f;
                attackState = (int)TwinsAttackType.BasicShots;
                phaseTransitionAnimationTime++;
                npc.dontTakeDamage = true;
                DoBehavior_DoPhaseTransition(npc, target, ref frame, hoverSide, phaseTransitionAnimationTime);
                return false;
            }

            // Handle the final phase transition.
            if (finalPhaseAnimationTime <= ExoMechManagement.FinalPhaseTransitionTime && ExoMechManagement.CurrentTwinsPhase >= 6 && !ExoMechManagement.ExoMechIsPerformingDeathAnimation)
            {
                if (finalPhaseAnimationTime == 1f)
                    ExoMechManagement.ClearAwayTransitionProjectiles();

                npc.ModNPC<Apollo>().ChargeComboFlash = 0f;
                attackState = (int)TwinsAttackType.BasicShots;
                finalPhaseAnimationTime++;
                npc.dontTakeDamage = true;
                DoBehavior_DoFinalPhaseTransition(npc, target, ref frame, hoverSide, finalPhaseAnimationTime);
                return false;
            }

            // Use combo attacks as necessary.
            if (ExoMechManagement.TotalMechs >= 2 && (int)attackState < 100)
            {
                attackTimer = 0f;

                if (initialMech.whoAmI == npc.whoAmI)
                    SelectNextAttack(npc);

                attackState = initialMech.ai[0];
                npc.netUpdate = true;
            }

            if ((finalMech != null && finalMech.Opacity > 0f || ExoMechManagement.CurrentTwinsPhase >= 6) && attackState >= 100f)
            {
                attackTimer = 0f;
                attackState = 0f;
                npc.netUpdate = true;
            }

            if (sideSwitchAttackDelay > 0f)
                sideSwitchAttackDelay--;

            // Perform specific attack behaviors.
            PerformSpecificAttackBehaviors(npc, target, performingDeathAnimation, attackState, hoverSide, ref enrageTimer, ref frame, ref attackTimer, ref deathAnimationTimer);

            attackTimer++;
            return false;
        }

        public static void PerformSpecificAttackBehaviors(NPC npc, Player target, bool performingDeathAnimation, float attackState, float hoverSide, ref float enrageTimer, ref float frame, ref float attackTimer, ref float deathAnimationTimer)
        {
            bool isApollo = npc.type == ModContent.NPCType<Apollo>();

            // Automatically transition to the ultimate attack if close to dying in the final phase.
            if (isApollo && ExoMechManagement.CurrentTwinsPhase >= 6 && npc.life < npc.lifeMax * 0.075f && attackState != (int)TwinsAttackType.ThermonuclearBlitz)
            {
                SelectNextAttack(npc);
                attackState = (int)TwinsAttackType.ThermonuclearBlitz;
            }

            if (!performingDeathAnimation)
            {
                switch ((TwinsAttackType)(int)attackState)
                {
                    case TwinsAttackType.BasicShots:
                        DoBehavior_BasicShots(npc, target, enrageTimer, ref frame, ref attackTimer);
                        break;
                    case TwinsAttackType.FireCharge:
                        DoBehavior_FireCharge(npc, target, hoverSide, enrageTimer, ref frame, ref attackTimer);
                        break;
                    case TwinsAttackType.ApolloPlasmaCharges:
                        DoBehavior_ApolloPlasmaCharges(npc, target, hoverSide, enrageTimer, ref frame, ref attackTimer);
                        break;
                    case TwinsAttackType.ArtemisLaserRay:
                        DoBehavior_ArtemisLaserRay(npc, target, ref frame, ref attackTimer);
                        break;
                    case TwinsAttackType.GatlingLaserAndPlasmaFlames:
                        DoBehavior_GatlingLaserAndPlasmaFlames(npc, target, hoverSide, enrageTimer, ref frame, ref attackTimer);
                        break;
                    case TwinsAttackType.SlowLaserRayAndPlasmaBlasts:
                        DoBehavior_SlowLaserRayAndPlasmaBlasts(npc, target, ref enrageTimer, ref frame, ref attackTimer);
                        break;
                    case TwinsAttackType.ThermonuclearBlitz:
                        DoBehavior_ThermonuclearBlitz(npc, target, ref frame, ref attackTimer);
                        break;
                }
            }

            // Perform the death animation.
            else
            {
                // Delete any lingering projectiles to ensure that the player doesn't recieve a surprise hit during the death animation.
                if (isApollo)
                    Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<ApolloFlamethrower>(), ModContent.ProjectileType<ArtemisSpinLaser>());

                npc.ai[0] = (int)TwinsAttackType.FireCharge;

                if (isApollo)
                    DoBehavior_DeathAnimation(npc, target, ref frame, ref npc.ModNPC<Apollo>().ChargeComboFlash, ref deathAnimationTimer);
                else
                    DoBehavior_DeathAnimation(npc, target, ref frame, ref npc.ModNPC<Artemis>().ChargeFlash, ref deathAnimationTimer);

                if (isApollo)
                    deathAnimationTimer++;
            }

            // Perform specific combo attack behaviors.
            if (ExoMechComboAttackContent.UseTwinsAresComboAttack(npc, hoverSide, ref attackTimer, ref frame) && isApollo)
                SelectNextAttack(npc);
            if (ExoMechComboAttackContent.UseTwinsThanatosComboAttack(npc, hoverSide, ref attackTimer, ref frame) && isApollo)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_DoPhaseTransition(NPC npc, Player target, ref float frame, float hoverSide, float phaseTransitionAnimationTime)
        {
            int startingFrame = 30;
            int endingFrame = 59;
            Vector2 hoverDestination = target.Center + Vector2.UnitX * hoverSide * 780f;

            // Determine rotation.
            npc.rotation = npc.AngleTo(target.Center) + PiOver2;

            // Disable contact damage.
            npc.damage = 0;

            // Move to the appropriate side of the target.
            ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 30f, 84f);

            // Determine frames.
            frame = (int)Math.Round(Lerp(startingFrame, endingFrame, phaseTransitionAnimationTime / Phase2TransitionTime));

            // Create the pupil gore thing.
            int pupilPopoffTime = (int)(Phase2TransitionTime * Utils.GetLerpValue(startingFrame, endingFrame, 37.5f));
            int chargeupSoundTime = (int)(Phase2TransitionTime * Utils.GetLerpValue(startingFrame, endingFrame, 46.5f));
            if (phaseTransitionAnimationTime == pupilPopoffTime)
            {
                SoundEngine.PlaySound(Artemis.LensSound with
                {
                    Volume = 2.5f
                }, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int lensType = ModContent.ProjectileType<BrokenApolloLens>();
                    Vector2 lensDirection = (npc.rotation - PiOver2).ToRotationVector2();
                    if (npc.type == ModContent.NPCType<Artemis>())
                        lensType = ModContent.ProjectileType<BrokenArtemisLens>();

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + lensDirection * 70f, lensDirection * 24f, lensType, 0, 0f);
                    npc.netUpdate = true;
                }
            }

            if (phaseTransitionAnimationTime >= chargeupSoundTime && phaseTransitionAnimationTime <= chargeupSoundTime + 40f && phaseTransitionAnimationTime % 16f == 15f)
                SoundEngine.PlaySound(GatlingLaser.FireSound, npc.Center);

            if (phaseTransitionAnimationTime == chargeupSoundTime + 75f)
                SoundEngine.PlaySound(GatlingLaser.FireEndSound, npc.Center);
        }

        public static void DoBehavior_DeathAnimation(NPC npc, Player target, ref float frame, ref float chargeInterpolant, ref float deathAnimationTimer)
        {
            int chargeupTime = 180;
            int reelBackTime = 45;
            int chargeTime = 90;
            float hoverSide = (npc.type == ModContent.NPCType<Apollo>()).ToDirectionInt();
            Vector2 hoverDestination = target.Center + new Vector2(hoverSide * 600f, -180f);

            // Use charge-up frames.
            npc.frameCounter++;
            frame = (int)Math.Round(Lerp(70f, 79f, (float)npc.frameCounter / 36f % 1f));

            // Clear away projectiles.
            ExoMechManagement.ClearAwayTransitionProjectiles();

            // Close the HP bar.
            npc.Calamity().ShouldCloseHPBar = true;

            // Use close to the minimum HP.
            npc.life = 50000;

            // Disable contact damage.
            npc.damage = 0;

            // Explode when colliding with the other mech.
            bool collidingWithMech = false;
            if (npc.type == ModContent.NPCType<Apollo>())
                collidingWithMech = npc.Hitbox.Intersects(Main.npc[NPC.FindFirstNPC(ModContent.NPCType<Artemis>())].Hitbox);
            if (npc.type == ModContent.NPCType<Artemis>())
                collidingWithMech = npc.Hitbox.Intersects(Main.npc[NPC.FindFirstNPC(ModContent.NPCType<Apollo>())].Hitbox);

            if (collidingWithMech && deathAnimationTimer > chargeupTime + reelBackTime)
                deathAnimationTimer = MathF.Max(chargeupTime + reelBackTime + chargeTime - 4f, deathAnimationTimer);

            // Hover to the sides of the target and charge energy.
            if (deathAnimationTimer < chargeupTime)
            {
                // Hover.
                ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 36f, 84f);

                // Determine rotation.
                npc.rotation = npc.AngleTo(target.Center) + PiOver2;

                // Determine the intensity of the charge visuals.
                chargeInterpolant = Utils.GetLerpValue(chargeupTime - 60f, chargeupTime - 1f, deathAnimationTimer, true);

                float particleCreationChance = Lerp(0.1f, 0.9f, Utils.GetLerpValue(0f, chargeupTime * 0.65f, deathAnimationTimer, true));

                for (int i = 0; i < 2; i++)
                {
                    if (Main.rand.NextFloat() > particleCreationChance)
                        continue;

                    float particleScale = Main.rand.NextFloat(0.4f, 0.55f);
                    Color particleColor;
                    Vector2 cannonEndPosition = npc.Center;
                    if (npc.type == ModContent.NPCType<Apollo>())
                    {
                        particleColor = Color.Lerp(Color.Lime, Color.Yellow, Main.rand.NextFloat(0.55f));
                        cannonEndPosition += (npc.rotation - PiOver2).ToRotationVector2() * 85f;
                    }
                    else
                    {
                        particleColor = Color.Lerp(Color.Orange, Color.Yellow, Main.rand.NextFloat(0.5f));
                        cannonEndPosition += (npc.rotation - PiOver2).ToRotationVector2() * 102f;
                    }

                    Vector2 particleSpawnPosition = cannonEndPosition + Main.rand.NextVector2Unit() * Main.rand.NextFloat(75f, 165f);
                    Vector2 particleVelocity = (cannonEndPosition - particleSpawnPosition + npc.velocity * 16f) * 0.06f;
                    GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(particleSpawnPosition, particleVelocity, particleScale, particleColor, 30));
                }
            }

            // Reel back.
            else if (deathAnimationTimer <= chargeupTime + reelBackTime)
            {
                float reelBackInterpolant = Utils.GetLerpValue(0f, reelBackTime * 0.65f, deathAnimationTimer - chargeupTime, true);
                hoverDestination -= npc.SafeDirectionTo(target.Center) * reelBackInterpolant * 200f;
                hoverDestination.X -= reelBackInterpolant * hoverSide * 200f;
                hoverDestination.Y -= reelBackInterpolant * 120f;

                // Hover.
                ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 36f, 84f);

                // Determine rotation.
                npc.rotation = npc.AngleTo(target.Center) + PiOver2;

                chargeInterpolant = 1f;

                // Charge.
                if (deathAnimationTimer == chargeupTime + reelBackTime)
                {
                    Vector2 tailEnd = npc.Center - (npc.rotation - PiOver2).ToRotationVector2() * 50f;

                    // Create a sonic boom at the tail end of the mech.
                    for (int i = 0; i < 2; i++)
                        GeneralParticleHandler.SpawnParticle(new PulseRing(tailEnd, Vector2.Zero, Color.Cyan, 0f, 8f, 40));

                    SoundEngine.PlaySound(ScorchedEarth.ShootSound, npc.Center);
                    npc.velocity = npc.SafeDirectionTo(target.Center) * 40f;
                    npc.rotation = npc.velocity.ToRotation() + PiOver2;
                }
            }

            // Explode.
            else if (deathAnimationTimer >= chargeupTime + reelBackTime + chargeTime)
            {
                // Slow down for the sake of preventing gores from flying at the speed of light.
                npc.velocity *= 0.5f;

                // Create a massive impact explosion and release sparks everywhere.
                if (npc.type == ModContent.NPCType<Apollo>())
                    PerformDeathAnimationExplosion(npc);
            }
        }

        public static void DoBehavior_DoFinalPhaseTransition(NPC npc, Player target, ref float frame, float hoverSide, float phaseTransitionAnimationTime)
        {
            // Clear away projectiles.
            ExoMechManagement.ClearAwayTransitionProjectiles();

            Vector2 hoverDestination = target.Center + Vector2.UnitX * hoverSide * 780f;

            // Heal HP.
            ExoMechAIUtilities.HealInFinalPhase(npc, phaseTransitionAnimationTime);

            // Determine rotation.
            npc.rotation = npc.AngleTo(target.Center) + PiOver2;

            // Move to the appropriate side of the target.
            ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 30f, 84f);

            // Determine frames.
            frame = (int)Math.Round(Lerp(70f, 79f, phaseTransitionAnimationTime / 45f % 1f));

            // Play the transition sound at the start.
            if (phaseTransitionAnimationTime == 3f && npc.type == ModContent.NPCType<Apollo>())
                SoundEngine.PlaySound(InfernumSoundRegistry.ExoMechFinalPhaseSound, target.Center);
        }

        public static void DoBehavior_BasicShots(NPC npc, Player target, float enrageTimer, ref float frame, ref float attackTimer)
        {
            // Artemis releases a set number of small laserbeam barrages before repositioning, while Apollo releases bursts of fireballs in a spread.
            // After Artemis completes its barrage set, it will swiftly reposition elsewhere before returning to a drift motion.
            int telegraphTime = 96;
            int artemisLaserbeamCountPerBurst = 8;
            int delayBetweenLaserbeams = 5;
            int artemisFastRedirectTime = 42;
            int totalLaserBurstCount = 3;
            int apolloFireballReleaseRate = 13;
            float apolloFireballExplosionRadius = 360f;
            float apolloPlasmaFireballSpeed = 18f;

            if (ExoMechManagement.CurrentTwinsPhase >= 3)
            {
                telegraphTime -= 18;
                artemisLaserbeamCountPerBurst += 8;
                artemisFastRedirectTime -= 8;
                apolloFireballReleaseRate -= 3;
                apolloFireballExplosionRadius += 80f;
            }
            if (ExoMechManagement.CurrentTwinsPhase >= 5)
            {
                telegraphTime -= 12;
                delayBetweenLaserbeams -= 2;
                artemisLaserbeamCountPerBurst += 3;
                totalLaserBurstCount--;
                apolloFireballReleaseRate -= 2;
                apolloFireballExplosionRadius += 50f;
                apolloPlasmaFireballSpeed += 3.85f;
            }
            if (ExoMechManagement.CurrentTwinsPhase >= 6)
            {
                delayBetweenLaserbeams = 0;
                artemisLaserbeamCountPerBurst += 12;
                apolloFireballReleaseRate -= 2;
                apolloFireballExplosionRadius += 75f;
            }
            if (enrageTimer > 0f)
            {
                delayBetweenLaserbeams = 0;
                apolloFireballReleaseRate = 1;
                apolloFireballExplosionRadius = 2400f;
            }

            int laserbeamAttackCycleTime = ArtemisBasicShotLaser.Lifetime / 5 + delayBetweenLaserbeams;
            Vector2 artemisAimDestination = target.Center;
            ref float artemisGleamTelegraphInterpolant = ref npc.Infernum().ExtraAI[0];
            ref float artemisLaserTimer = ref npc.Infernum().ExtraAI[1];
            ref float artemisLaserCounter = ref npc.Infernum().ExtraAI[2];
            ref float artemisHasPerformedTelegraph = ref npc.Infernum().ExtraAI[3];
            ref float laserBurstCounter = ref npc.Infernum().ExtraAI[4];
            ref float apolloFlameSpinDirection = ref npc.Infernum().ExtraAI[20];

            // Provide the target infinite flight time.
            if (npc.type == ModContent.NPCType<Apollo>())
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player player = Main.player[i];
                    if (player.dead || !player.active || !npc.WithinRange(player.Center, 10000f))
                        continue;

                    player.DoInfiniteFlightCheck(Color.ForestGreen);
                }
            }

            if (npc.type == ModContent.NPCType<Apollo>() && CalamityGlobalNPC.draedonExoMechTwinRed >= 0)
            {
                // Don't do damage.
                npc.damage = 0;

                NPC artemis = Main.npc[CalamityGlobalNPC.draedonExoMechTwinRed];

                // Rapidly approach the target before attacking, to ensure that they see Apollo and can be aware of the impending attack.
                if (attackTimer <= artemisFastRedirectTime)
                {
                    Vector2 hoverDestination = artemis.Center;

                    // Hover a bit offset to Artemis. This avoids moving in front of the target.
                    Vector2 hoverOffsetDirection = -npc.SafeDirectionTo(hoverDestination);
                    if (hoverOffsetDirection.AngleBetween(npc.SafeDirectionTo(target.Center)) < 0.96f)
                        hoverOffsetDirection *= -1f;

                    hoverDestination += hoverOffsetDirection * 180f;

                    npc.Center = Vector2.Lerp(npc.Center, hoverDestination, 0.1f);
                    npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(artemisAimDestination) + PiOver2, 0.25f);
                    npc.velocity = (npc.rotation - PiOver2).ToRotationVector2() * 0.1f;

                    // Decide which direction Apollo should release fireballs in.
                    apolloFlameSpinDirection = (WrapAngle(npc.AngleTo(target.Center) - npc.velocity.ToRotation()) > 0f).ToDirectionInt();
                }

                // Release a constant fan of fireballs if the telegraph has been completed.
                else
                {
                    npc.velocity = npc.velocity.RotatedBy(apolloFlameSpinDirection * TwoPi / 64f);
                    npc.rotation = npc.velocity.ToRotation() + PiOver2;

                    if (attackTimer % apolloFireballReleaseRate == apolloFireballReleaseRate - 1f)
                    {
                        if (Main.netMode != NetmodeID.Server)
                        {
                            SoundStyle plasmaSound = InfernumSoundRegistry.SafeLoadCalamitySound("Sounds/Custom/ExoMechs/ExoPlasmaShoot", PlasmaCaster.FireSound);
                            SoundEngine.PlaySound(plasmaSound with
                            {
                                Volume = 1.5f
                            }, npc.Center);
                        }

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(bomb =>
                            {
                                bomb.timeLeft = 60;
                            });
                            Vector2 aimDirection = npc.velocity.SafeNormalize(Vector2.UnitY);
                            Utilities.NewProjectileBetter(npc.Center + aimDirection * 72f, aimDirection.RotatedByRandom(0.2f) * apolloPlasmaFireballSpeed, ModContent.ProjectileType<ExoplasmaBomb>(), 0, 0f, -1, apolloFireballExplosionRadius);

                            npc.netUpdate = true;
                        }
                    }
                }
            }

            if (npc.type == ModContent.NPCType<Artemis>())
            {
                // Prepare the gleam telegraph interpolant.
                if (attackTimer <= telegraphTime && artemisHasPerformedTelegraph == 0f)
                {
                    artemisGleamTelegraphInterpolant = attackTimer / telegraphTime;

                    // Mark the telegraph as being done and reset the attack timer once complete.
                    if (attackTimer >= telegraphTime)
                    {
                        artemisHasPerformedTelegraph = 1f;
                        npc.netUpdate = true;
                    }
                }

                // Rapidly approach the target before attacking, to ensure that they see Artemis and can be aware of the impending attack.
                if (attackTimer <= artemisFastRedirectTime && artemisHasPerformedTelegraph == 0f)
                {
                    npc.Center = Vector2.Lerp(npc.Center, target.Center, 0.044f);
                    npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(artemisAimDestination) + PiOver2, 0.125f);
                }

                // Drift near the player.
                int laserID = ModContent.ProjectileType<ArtemisBasicShotLaser>();
                bool laserExists = Utilities.AnyProjectiles(laserID);
                bool aboutToFire = attackTimer >= telegraphTime - 30f && artemisHasPerformedTelegraph == 0f;
                float angularVelocity = 0.042f;
                float driftSpeed = 21f;
                if (laserExists || aboutToFire)
                    angularVelocity *= ExoMechManagement.CurrentTwinsPhase >= 6 ? 0.36f : 0.1f;
                if (enrageTimer > 0f)
                {
                    angularVelocity *= 3f;
                    driftSpeed *= 2f;
                }

                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center - Vector2.UnitY * 350f) * driftSpeed, 0.02f);
                if (angularVelocity > 0f)
                    npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(artemisAimDestination) + PiOver2, angularVelocity);

                // Everything beyond this point happens exclusively if the telegraph has been completed.
                if (artemisHasPerformedTelegraph == 0f)
                    return;

                // Fire the laserbeam on the first frame.
                if (artemisLaserTimer == 1f)
                {
                    if (Main.netMode != NetmodeID.Server)
                    {
                        SoundStyle laserSound = InfernumSoundRegistry.SafeLoadCalamitySound("Sounds/Custom/ExoMechs/ExoLaserShoot", CommonCalamitySounds.LaserCannonSound);
                        SoundEngine.PlaySound(laserSound with
                        {
                            Volume = 1.4f
                        }, npc.Center);
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(npc.Center, (npc.rotation - PiOver2).ToRotationVector2(), laserID, StrongerNormalShotDamage, 0f, -1, npc.whoAmI);
                }

                // Increment the laser timer.
                artemisLaserTimer++;

                // Once the laser timer has completed, reset it and increment the laser counter.
                if (artemisLaserTimer >= laserbeamAttackCycleTime)
                {
                    artemisLaserTimer = 0f;
                    artemisLaserCounter++;
                    if (artemisLaserCounter >= artemisLaserbeamCountPerBurst)
                    {
                        artemisLaserCounter = 0f;
                        attackTimer = 0f;
                        artemisHasPerformedTelegraph = 0f;
                        laserBurstCounter++;
                        Main.npc[CalamityGlobalNPC.draedonExoMechTwinGreen].ai[1] = 0f;

                        if (laserBurstCounter >= totalLaserBurstCount)
                        {
                            Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<ApolloPlasmaFireball>(), ModContent.ProjectileType<AresPlasmaBolt>(), ModContent.ProjectileType<ExoplasmaBomb>());
                            SelectNextAttack(npc);
                        }
                    }

                    npc.netUpdate = true;
                }
            }

            // Decide frames.
            npc.frameCounter++;
            frame = (int)Math.Round(Lerp(10f, 19f, (float)npc.frameCounter / 36f % 1f));
            if (ExoMechManagement.ExoTwinsAreInSecondPhase && frame <= 30f)
                frame += 60f;
        }

        public static void DoBehavior_FireCharge(NPC npc, Player target, float hoverSide, float enrageTimer, ref float frame, ref float attackTimer)
        {
            float artemisChargeSpeed = 25f;
            int artemisChargeTime = 78;
            int artemisLaserReleaseRate = 20;
            int artemisLaserBurstCount = 9;
            int flamethrowerHoverTime = 95;
            int attackTime = 540;
            float flamethrowerFlySpeed = 33f;

            if (ExoMechManagement.CurrentTwinsPhase >= 2)
                artemisChargeSpeed += 4f;
            if (ExoMechManagement.CurrentTwinsPhase == 3)
            {
                artemisLaserReleaseRate -= 3;
                artemisChargeTime += 5;
            }
            if (ExoMechManagement.CurrentTwinsPhase >= 5)
                artemisLaserReleaseRate -= 3;

            if (ExoMechManagement.CurrentTwinsPhase >= 6)
                artemisLaserReleaseRate -= 4;

            if (enrageTimer > 0f)
                artemisLaserReleaseRate = 6;

            // Apollo performs multiple flamethrower dashes in succession.
            if (npc.type == ModContent.NPCType<Apollo>())
            {
                npc.frameCounter++;
                frame = (int)Math.Round(Lerp(10f, 19f, (float)npc.frameCounter / 36f % 1f));

                float wrappedAttackTimer = attackTimer % (flamethrowerHoverTime + ApolloFlamethrower.Lifetime + 15f);

                // Look at the target and hover towards the top left/right of the target.
                if (wrappedAttackTimer < flamethrowerHoverTime + 15f)
                {
                    Vector2 mouthpiecePosition = npc.Center + (npc.rotation - PiOver2).ToRotationVector2() * 85f;
                    Vector2 hoverDestination = target.Center + new Vector2(hoverSide * 1020f, -375f);

                    npc.rotation = npc.AngleTo(target.Center) + PiOver2;
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination - npc.velocity) * 45f, 1.5f);

                    // Begin the delay if the destination is reached.
                    if (npc.WithinRange(hoverDestination, 50f) && wrappedAttackTimer < flamethrowerHoverTime - 2f)
                        attackTimer += flamethrowerHoverTime - wrappedAttackTimer - 1f;

                    // Release fire and smoke from the mouth as a telegraph.
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 dustSpawnPosition = mouthpiecePosition + Main.rand.NextVector2Circular(8f, 8f);
                        Vector2 dustVelocity = npc.SafeDirectionTo(dustSpawnPosition).RotatedByRandom(0.45f) * Main.rand.NextFloat(2f, 5f);
                        Dust hotStuff = Dust.NewDustPerfect(dustSpawnPosition, Main.rand.NextBool() ? 31 : 107);
                        hotStuff.velocity = dustVelocity + npc.velocity;
                        hotStuff.fadeIn = 0.8f;
                        hotStuff.scale = Main.rand.NextFloat(1f, 1.45f);
                        hotStuff.alpha = 200;
                    }
                }

                // Begin the charge and emit a flamethrower after a tiny delay.
                else if (wrappedAttackTimer == flamethrowerHoverTime + 15f)
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center) * flamethrowerFlySpeed;
                    Utilities.CreateShockwave(npc.Center - npc.velocity * 4f);
                    ScreenEffectSystem.SetBlurEffect(npc.Center, 0.3f, 45);
                    target.Infernum_Camera().CurrentScreenShakePower = 3f;

                    ExoMechsSky.CreateLightningBolt(25);

                    SoundEngine.PlaySound(SoundID.DD2_BetsyFlameBreath with
                    {
                        Volume = 1.5f
                    }, target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<ApolloFlamethrower>(), StrongerNormalShotDamage, 0f, -1, 0f, npc.whoAmI);

                    frame += 10f;
                }

                if (wrappedAttackTimer >= flamethrowerHoverTime + 15f)
                    npc.rotation = npc.velocity.ToRotation() + PiOver2;
            }


            // Have Artemis attempt to do a horizontal sweep while releasing lasers in bursts. This only happens after Ares has released the laserbeams.
            if (npc.type == ModContent.NPCType<Artemis>())
            {
                ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
                ref float generalAttackTimer = ref npc.Infernum().ExtraAI[1];

                // Don't do contact damage.
                npc.damage = 0;

                // Reset the flash effect.
                npc.ModNPC<Artemis>().ChargeFlash = 0f;

                // Simply hover in place if the laserbeams have not been fired.
                if (attackTimer < flamethrowerHoverTime && attackSubstate == 0f)
                {
                    Vector2 hoverDestination = target.Center + new Vector2(hoverSide * 600f, -400f);
                    ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 30f, 75f);

                    // Decide rotation.
                    npc.rotation = npc.AngleTo(target.Center) + PiOver2;
                }
                else
                {
                    switch ((int)attackSubstate)
                    {
                        // Hover into position.
                        case 0:
                        default:
                            Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 850f, -500f);
                            Vector2 chargeVelocity = Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X) * artemisChargeSpeed;
                            ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 29f, 60f);

                            // Determine rotation.
                            npc.rotation = chargeVelocity.ToRotation() + PiOver2;

                            // Prepare the charge.
                            if (generalAttackTimer > 45f && (npc.WithinRange(hoverDestination, 105f) || generalAttackTimer > 125f))
                            {
                                generalAttackTimer = 0f;
                                attackSubstate = 1f;
                                npc.velocity = chargeVelocity;
                                npc.netUpdate = true;
                            }
                            break;

                        // Swoop down slightly and release lasers.
                        case 1:
                            npc.velocity.Y = CalamityUtils.Convert01To010(generalAttackTimer / artemisChargeTime) * 13.5f;
                            npc.rotation = npc.velocity.ToRotation() + PiOver2;

                            if (generalAttackTimer % artemisLaserReleaseRate == artemisLaserReleaseRate - 1f && !npc.WithinRange(target.Center, 475f))
                            {
                                SoundEngine.PlaySound(CommonCalamitySounds.LaserCannonSound, npc.Center);

                                if (Main.netMode != NetmodeID.MultiplayerClient)
                                {
                                    float offsetAngle = Main.rand.NextFloat(Pi / artemisLaserBurstCount);
                                    for (int i = 0; i < artemisLaserBurstCount; i++)
                                    {
                                        Vector2 aimDestination = npc.Center + (TwoPi * i / artemisLaserBurstCount + offsetAngle).ToRotationVector2() * 1500f;
                                        Vector2 laserShootVelocity = npc.SafeDirectionTo(aimDestination) * 7.25f;

                                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(laser =>
                                        {
                                            laser.ModProjectile<ArtemisLaser>().InitialDestination = aimDestination + laserShootVelocity.SafeNormalize(Vector2.UnitY) * 1600f;
                                        });
                                        Utilities.NewProjectileBetter(npc.Center, laserShootVelocity, ModContent.ProjectileType<ArtemisLaser>(), NormalShotDamage, 0f, -1, 0f, npc.whoAmI);
                                    }
                                }
                            }

                            if (generalAttackTimer > artemisChargeTime)
                            {
                                generalAttackTimer = 0f;
                                attackSubstate = 0f;
                                npc.velocity *= 0.55f;
                                npc.netUpdate = true;
                            }
                            break;
                    }
                    generalAttackTimer++;
                }

                // Handle frames.
                npc.frameCounter++;
                frame = (int)Math.Round(Lerp(10f, 19f, (float)npc.frameCounter / 36f % 1f));
                if (attackSubstate >= 1f)
                    frame += 10f;
            }

            // Update frames for the second phase.
            if (ExoMechManagement.ExoTwinsAreInSecondPhase && frame <= 30f)
                frame += 60f;

            if (attackTimer >= attackTime)
            {
                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<ApolloFlamethrower>(), ModContent.ProjectileType<ApolloFallingPlasmaSpark>());
                SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_ApolloPlasmaCharges(NPC npc, Player target, float hoverSide, float enrageTimer, ref float frame, ref float attackTimer)
        {
            // Make Artemis go away so Apollo can do its attack without interference.
            if (npc.type == ModContent.NPCType<Artemis>())
            {
                npc.dontTakeDamage = true;
                npc.Opacity = Clamp(npc.Opacity - 0.16f, 0f, 1f);
                npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center - Vector2.UnitY * 450f) * 40f, 1.25f);
                return;
            }

            int waitTime = 8;
            int chargeTime = 45;
            int totalCharges = 5;
            int sparkCount = 17;
            float chargeSpeed = 54f;
            float chargePredictiveness = 10f;
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            Vector2 hoverDestination = target.Center - Vector2.UnitY * 300f;

            if (ExoMechManagement.CurrentTwinsPhase == 3)
                chargeSpeed += 2.5f;
            if (ExoMechManagement.CurrentTwinsPhase >= 5)
            {
                chargeTime -= 5;
                totalCharges--;
                chargeSpeed -= 4f;
            }
            if (ExoMechManagement.CurrentTwinsPhase >= 6)
                totalCharges--;
            if (!target.HasShieldBash())
                chargeSpeed *= 0.65f;

            if (enrageTimer > 0f)
            {
                chargeSpeed += 27f;
                chargeTime -= 13;
                sparkCount += 15;
            }

            ref float attackDelay = ref npc.Infernum().ExtraAI[1];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[2];

            if (chargeCounter == 0f)
                hoverDestination.X += hoverSide * 540f;
            else
                hoverDestination.X += (target.Center.X < npc.Center.X).ToDirectionInt() * 540f;

            switch ((int)attackSubstate)
            {
                // Hover into position.
                case 0:
                    npc.rotation = npc.AngleTo(target.Center) + PiOver2;

                    // Hover to the top left/right of the target.
                    ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 50f, 92f);

                    npc.frameCounter++;
                    frame = (int)Math.Round(Lerp(10f, 19f, (float)npc.frameCounter / 36f % 1f));

                    // Once sufficiently close, go to the next attack substate.
                    if (npc.WithinRange(hoverDestination, 50f))
                    {
                        npc.velocity = Vector2.Zero;
                        attackSubstate = 1f;
                        attackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;

                // Wait in place for a short period of time.
                case 1:
                    npc.rotation = npc.AngleTo(target.Center + target.velocity * chargePredictiveness) + PiOver2;

                    // Decide frames.
                    npc.frameCounter++;
                    frame = (int)Math.Round(Lerp(20f, 29f, (float)npc.frameCounter / 36f % 1f));

                    // Calculate the charge flash.
                    npc.ModNPC<Apollo>().ChargeComboFlash = Clamp(attackTimer / waitTime, 0f, 1f);

                    // Charge and release sparks.
                    if (attackTimer >= waitTime && attackDelay >= 64f)
                    {
                        // Create lightning bolts in the sky.
                        int lightningBoltCount = ExoMechManagement.CurrentTwinsPhase >= 6 ? 55 : 30;
                        if (Main.netMode != NetmodeID.Server)
                            ExoMechsSky.CreateLightningBolt(lightningBoltCount, true);

                        SoundEngine.PlaySound(CommonCalamitySounds.ELRFireSound, npc.Center);
                        Utilities.CreateShockwave(npc.Center, 4, 15, 192f);
                        ScreenEffectSystem.SetFlashEffect(npc.Center, 1f, 45);
                        target.Infernum_Camera().CurrentScreenShakePower = 3f;

                        npc.velocity = npc.SafeDirectionTo(target.Center + target.velocity * chargePredictiveness) * chargeSpeed;

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float offsetAngle = Main.rand.NextFloat(TwoPi);
                            for (int i = 0; i < sparkCount; i++)
                            {
                                Vector2 sparkShootVelocity = (TwoPi * i / sparkCount + offsetAngle).ToRotationVector2() * 16f;
                                Utilities.NewProjectileBetter(npc.Center + sparkShootVelocity * 10f, sparkShootVelocity, ModContent.ProjectileType<ApolloAcceleratingPlasmaSpark>(), StrongerNormalShotDamage, 0f);

                                sparkShootVelocity = (TwoPi * (i + 0.5f) / sparkCount + offsetAngle).ToRotationVector2() * 7f;
                                Utilities.NewProjectileBetter(npc.Center + sparkShootVelocity * 16f, sparkShootVelocity, ModContent.ProjectileType<ApolloAcceleratingPlasmaSpark>(), StrongerNormalShotDamage, 0f);
                            }
                        }

                        attackSubstate = 2f;
                        attackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;

                // Release fire.
                case 2:
                    npc.damage = npc.defDamage;

                    // Decide frames.
                    npc.frameCounter++;
                    frame = (int)Math.Round(Lerp(20f, 29f, (float)npc.frameCounter / 36f % 1f));

                    // Calculate the charge flash.
                    npc.ModNPC<Apollo>().ChargeComboFlash = Utils.GetLerpValue(chargeTime, chargeTime - 10f, attackTimer, true);

                    if (attackTimer >= chargeTime)
                    {
                        attackTimer = 0f;
                        attackSubstate = 0f;
                        chargeCounter++;
                        npc.netUpdate = true;

                        if (chargeCounter >= totalCharges)
                            SelectNextAttack(npc);
                    }
                    break;
            }

            if (ExoMechManagement.ExoTwinsAreInSecondPhase)
                frame += 60f;

            attackDelay++;
        }

        public static void DoBehavior_ArtemisLaserRay(NPC npc, Player target, ref float frame, ref float attackTimer)
        {
            // Make Apollo go away so Artemis can do its attack without interference.
            if (npc.type == ModContent.NPCType<Apollo>())
            {
                npc.dontTakeDamage = true;
                npc.Opacity = Clamp(npc.Opacity - 0.16f, 0f, 1f);
                npc.ai[1] = Main.npc[NPC.FindFirstNPC(ModContent.NPCType<Artemis>())].ai[1];
                npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center - Vector2.UnitY * 450f) * 40f, 1.25f);
                return;
            }

            int shootDelay = ArtemisLaserbeamTelegraph.TrueLifetime + 4;
            float spinRadius = 640f;
            float spinArc = Pi * 1.1f;

            npc.dontTakeDamage = false;

            if (ExoMechManagement.CurrentTwinsPhase >= 3)
                spinArc *= 1.1f;
            if (ExoMechManagement.CurrentTwinsPhase >= 5)
                spinArc *= 1.2f;

            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float spinDirection = ref npc.Infernum().ExtraAI[1];
            ref float spinningPointX = ref npc.Infernum().ExtraAI[2];
            ref float spinningPointY = ref npc.Infernum().ExtraAI[3];
            ref float hoverOffsetDirection = ref npc.Infernum().ExtraAI[4];

            Vector2 hoverDestination = target.Center + hoverOffsetDirection.ToRotationVector2() * new Vector2(1f, 0.65f) * spinRadius;

            switch ((int)attackSubstate)
            {
                // Hover into position.
                case 0:
                    // Determine which direction Artemis will spin in.
                    if (attackTimer == 1f)
                    {
                        SoundEngine.PlaySound(InfernumSoundRegistry.ExoMechImpendingDeathSound, npc.Center);
                        hoverOffsetDirection = Main.rand.Next(8) * TwoPi / 8f;
                        npc.netUpdate = true;
                    }

                    npc.frameCounter++;
                    frame = (int)Math.Round(Lerp(70f, 79f, (float)npc.frameCounter / 36f % 1f));

                    npc.rotation = npc.AngleTo(target.Center) + PiOver2;
                    ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 30f, 84f);

                    // Begin hovering in place once sufficiently close to the hover position.
                    if (npc.WithinRange(hoverDestination, 50f))
                    {
                        npc.velocity = Vector2.Zero;
                        npc.Center = hoverDestination;
                        npc.rotation = npc.AngleTo(target.Center) + PiOver2;
                        spinningPointX = target.Center.X;
                        spinningPointY = target.Center.Y;
                        attackTimer = 0f;
                        attackSubstate = 1f;
                        npc.netUpdate = true;
                    }
                    break;

                // Stay in place for a brief moment.
                case 1:
                    npc.frameCounter++;
                    frame = (int)Math.Round(Lerp(70f, 79f, (float)npc.frameCounter / 36f % 1f));

                    // Calculate the charge flash.
                    npc.ModNPC<Artemis>().ChargeFlash = Utils.GetLerpValue(0f, shootDelay * 0.8f, attackTimer, true);

                    if (attackTimer == 1f)
                        SoundEngine.PlaySound(InfernumSoundRegistry.ArtemisSpinLaserbeamSound with
                        {
                            Volume = 1.4f
                        }, new(spinningPointX, spinningPointY));

                    // Create a beam telegraph.
                    if (attackTimer == 4f)
                    {
                        SoundEngine.PlaySound(CommonCalamitySounds.LaserCannonSound, target.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 0; i < 2; i++)
                            {
                                float telegraphAngularOffset = Lerp(-0.62f, 0.62f, i);
                                Utilities.NewProjectileBetter(npc.Center, -hoverOffsetDirection.ToRotationVector2(), ModContent.ProjectileType<ArtemisLaserbeamTelegraph>(), 0, 0f, -1, npc.whoAmI, telegraphAngularOffset);
                            }
                        }
                    }

                    // Initialize Artemis' spin direction.
                    if (spinDirection == 0f)
                    {
                        float angularOffset = WrapAngle(npc.AngleTo(target.Center) - npc.rotation + PiOver2);

                        if (Math.Abs(angularOffset) > 0.01f)
                            spinDirection = Math.Sign(angularOffset);
                    }

                    // Fire the laser.
                    if (attackTimer >= shootDelay)
                    {
                        attackTimer = 0f;
                        attackSubstate = 2f;
                        if (spinDirection == 0f)
                            spinDirection = 1f;

                        npc.netUpdate = true;

                        // Create an incredibly violent screen shake effect.
                        Utilities.CreateShockwave(npc.Center, 4, 15, 192f, false);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int type = ModContent.ProjectileType<ArtemisSpinLaser>();
                            Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, type, 900, 0f, -1, npc.whoAmI, spinDirection);
                        }
                    }
                    break;

                // Spin 2 win.
                case 2:
                    npc.frameCounter++;
                    frame = (int)Math.Round(Lerp(80f, 89f, (float)npc.frameCounter / 32f % 1f));

                    float spinAngle = attackTimer / ArtemisSpinLaser.LaserLifetime * spinArc * -spinDirection + hoverOffsetDirection + PiOver2;
                    npc.velocity = spinAngle.ToRotationVector2() * Pi * spinRadius / ArtemisSpinLaser.LaserLifetime * -spinDirection * 1.8f;
                    npc.rotation = npc.AngleTo(new Vector2(spinningPointX, spinningPointY)) + PiOver2;

                    for (int i = 0; i < 2; i++)
                    {
                        ElectricSpark electricSpark = new(npc.Center + Main.rand.NextVector2Circular(5, 5), npc.SafeDirectionTo(new Vector2(spinningPointX, spinningPointY)).RotatedByRandom(TwoPi) * Main.rand.NextFloat(0.9f, 1.1f) * 60f, Color.Orange, Color.Gold, Main.rand.NextFloat(1.3f, 1.5f), 60);
                        GeneralParticleHandler.SpawnParticle(electricSpark);
                    }

                    // Calculate the charge flash.
                    npc.ModNPC<Artemis>().ChargeFlash = Utils.GetLerpValue(ArtemisSpinLaser.LaserLifetime - 20f, ArtemisSpinLaser.LaserLifetime - 32f, attackTimer, true);

                    if (attackTimer >= ArtemisSpinLaser.LaserLifetime - 16f)
                    {
                        foreach (Projectile laser in Utilities.AllProjectilesByID(ModContent.ProjectileType<ArtemisSpinLaser>()))
                            laser.Kill();

                        SelectNextAttack(npc);
                    }
                    break;
            }
            attackTimer++;
        }

        public static void DoBehavior_GatlingLaserAndPlasmaFlames(NPC npc, Player target, float hoverSide, float enrageTimer, ref float frame, ref float attackTimer)
        {
            int shootTime = 420;
            int attackTransitionDelay = 150;
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float hoverOffsetX = ref npc.Infernum().ExtraAI[1];
            ref float hoverOffsetY = ref npc.Infernum().ExtraAI[2];
            ref float hoverSideFlip = ref npc.Infernum().ExtraAI[3];
            ref float apolloShootCounter = ref npc.Infernum().ExtraAI[4];

            if (hoverSideFlip == 0f)
                hoverSideFlip = 1f;
            Vector2 hoverDestination = target.Center + new Vector2(hoverSide * hoverSideFlip * 820f, -100f);

            // Disable contact damage.
            npc.damage = 0;

            switch ((int)attackSubstate)
            {
                // Hover into position.
                case 0:
                    npc.frameCounter++;
                    frame = (int)Math.Round(Lerp(10f, 19f, (float)npc.frameCounter / 36f % 1f));

                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 40f, 1.15f);
                    npc.rotation = npc.AngleTo(target.Center) + PiOver2;

                    if (attackTimer >= 90f)
                    {
                        attackSubstate = 1f;
                        attackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;

                // Begin firing.
                case 1:
                    npc.frameCounter++;
                    frame = (int)Math.Round(Lerp(20f, 29f, (float)npc.frameCounter / 30f % 1f));

                    // Reset the hover offset periodically.
                    if (attackTimer % 90f == 89f)
                    {
                        hoverOffsetX = Main.rand.NextFloat(-50f, 50f);
                        hoverOffsetY = Main.rand.NextFloat(-520f, 520f);
                    }

                    // Fire a machine-gun of lasers.
                    if (npc.type == ModContent.NPCType<Artemis>())
                    {
                        int laserShootRate = 16;
                        float laserShootSpeed = 5.5f;
                        float predictivenessFactor = 18.5f;
                        Vector2 aimDestination = target.Center + target.velocity * new Vector2(predictivenessFactor, predictivenessFactor * 2.6f);
                        Vector2 aimDirection = npc.SafeDirectionTo(aimDestination);
                        npc.rotation = npc.AngleTo(target.Center) + PiOver2;

                        if (ExoMechManagement.CurrentTwinsPhase == 4)
                            laserShootRate += 7;
                        if (ExoMechManagement.CurrentTwinsPhase >= 5)
                            laserShootRate -= 2;
                        if (ExoMechManagement.CurrentTwinsPhase >= 6)
                            laserShootRate -= 4;
                        if (enrageTimer > 0f)
                            laserShootRate = 5;

                        // Do movement.
                        ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination + new Vector2(hoverOffsetX, hoverOffsetY), 25f, 84f);

                        // Play a laser preparation sound.
                        if (attackTimer == 15f)
                            SoundEngine.PlaySound(GatlingLaser.FireSound, target.Center);

                        // Play the laser fire loop.
                        if (attackTimer >= 15f && attackTimer % 70f == 20f && attackTimer < shootTime)
                            SoundEngine.PlaySound(GatlingLaser.FireLoopSound, target.Center);

                        bool shouldFire = attackTimer >= 15f && attackTimer % laserShootRate == laserShootRate - 1f && npc.WithinRange(hoverDestination + new Vector2(hoverOffsetX, hoverOffsetY), 90f) && attackTimer < shootTime;
                        if (shouldFire)
                            ExoMechsSky.CreateLightningBolt(4);
                        if (Main.netMode != NetmodeID.MultiplayerClient && shouldFire)
                        {
                            for (int i = -1; i <= 1; i++)
                            {
                                ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(laser =>
                                {
                                    laser.ModProjectile<ArtemisGatlingLaser>().InitialDestination = aimDestination;
                                    laser.ModProjectile<ArtemisGatlingLaser>().PositionOffsetVariant = i;
                                });
                                Utilities.NewProjectileBetter(npc.Center + aimDirection * 70f, aimDirection * laserShootSpeed, ModContent.ProjectileType<ArtemisGatlingLaser>(), StrongerNormalShotDamage, 0f, -1, 0f, npc.whoAmI);
                            }
                        }
                    }

                    // Release streams of plasma blasts rapid-fire.
                    else
                    {
                        int plasmaShootRate = 40;
                        float plasmaShootSpeed = 10f;
                        float predictivenessFactor = 25f;
                        Vector2 aimDestination = target.Center + target.velocity * predictivenessFactor;
                        Vector2 aimDirection = npc.SafeDirectionTo(aimDestination);
                        npc.rotation = npc.AngleTo(target.Center) + PiOver2;

                        if (ExoMechManagement.CurrentTwinsPhase == 4)
                            plasmaShootRate += 16;
                        if (ExoMechManagement.CurrentTwinsPhase >= 5)
                            plasmaShootRate -= 8;
                        if (enrageTimer > 0f)
                            plasmaShootRate = 16;

                        // Do movement.
                        ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination + new Vector2(hoverOffsetX, hoverOffsetY), 25f, 84f);

                        if (attackTimer >= 15f && attackTimer % plasmaShootRate == plasmaShootRate - 1f && npc.WithinRange(hoverDestination + new Vector2(hoverOffsetX, hoverOffsetY), 90f) && attackTimer < shootTime)
                        {
                            SoundEngine.PlaySound(PlasmaCaster.FireSound, npc.Center);

                            if (Main.netMode != NetmodeID.MultiplayerClient)
                                Utilities.NewProjectileBetter(npc.Center + aimDirection * 70f, aimDirection * plasmaShootSpeed, ModContent.ProjectileType<AresPlasmaFireballInfernum>(), StrongerNormalShotDamage, 0f);

                            apolloShootCounter++;
                            if (apolloShootCounter % 5f == 4f)
                            {
                                hoverSideFlip *= -1f;
                                foreach (Projectile laser in Utilities.AllProjectilesByID(ModContent.ProjectileType<ArtemisGatlingLaser>()).Where(p => p.ai[0] <= ArtemisGatlingLaser.TelegraphTotalTime))
                                    laser.Kill();

                                Main.npc[CalamityGlobalNPC.draedonExoMechTwinRed].Infernum().ExtraAI[3] = hoverSideFlip;
                                npc.netUpdate = true;
                            }
                        }

                        if (attackTimer >= shootTime + attackTransitionDelay)
                            SelectNextAttack(npc);
                    }
                    break;
            }

            if (ExoMechManagement.ExoTwinsAreInSecondPhase)
                frame += 60f;
        }

        public static void DoBehavior_SlowLaserRayAndPlasmaBlasts(NPC npc, Player target, ref float enrageTimer, ref float frame, ref float attackTimer)
        {
            int apolloShootRate = 70;
            int laserbeamTelegraphTime = 60;
            int laserbeamSweepTime = ArtemisSweepLaserbeam.LifetimeConst;
            int laserbeamAttackTime = laserbeamTelegraphTime + laserbeamSweepTime;
            int attackTransitionDelay = 30;
            float spinRadius = 600f;
            float spinArc = Pi * 5f;
            float plasmaBlastShootSpeed = 12f;

            if (ExoMechManagement.CurrentTwinsPhase >= 2)
            {
                apolloShootRate -= 9;
                plasmaBlastShootSpeed += 1.12f;
            }
            if (ExoMechManagement.CurrentTwinsPhase >= 3)
            {
                apolloShootRate -= 9;
                plasmaBlastShootSpeed += 0.84f;
            }
            if (ExoMechManagement.CurrentTwinsPhase >= 5)
                apolloShootRate -= 9;
            if (ExoMechManagement.CurrentTwinsPhase >= 6)
            {
                apolloShootRate -= 18;
                spinArc += Pi;
                plasmaBlastShootSpeed += 1.06f;
            }
            if (enrageTimer >= 1f)
            {
                apolloShootRate = 11;
                plasmaBlastShootSpeed += 10f;
            }

            NPC artemis = Main.npc[CalamityGlobalNPC.draedonExoMechTwinRed];
            NPC apollo = Main.npc[CalamityGlobalNPC.draedonExoMechTwinGreen];
            ref float artemisHasRepositioned = ref artemis.Infernum().ExtraAI[0];
            ref float generalTimer = ref npc.Infernum().ExtraAI[1];
            ref float apolloAngularHoverOffset = ref npc.Infernum().ExtraAI[2];
            ref float spinDirection = ref artemis.Infernum().ExtraAI[2];
            ref float spinningPointX = ref artemis.Infernum().ExtraAI[3];
            ref float spinningPointY = ref artemis.Infernum().ExtraAI[4];
            Vector2 artemisHoverDestination = new Vector2(spinningPointX, spinningPointY) + Vector2.UnitY * spinRadius;

            // Have Artemis cast a telegraph that indicates where the laserbeam will appear.
            if (npc.type == ModContent.NPCType<Artemis>())
            {
                // Disable contact damage.
                npc.damage = 0;

                // Hover into position before creating the telegraph.
                if (artemisHasRepositioned == 0f)
                {
                    spinningPointX = target.Center.X;
                    spinningPointY = target.Center.Y;

                    ExoMechAIUtilities.DoSnapHoverMovement(npc, artemisHoverDestination, 30f, 84f);
                    if (npc.WithinRange(artemisHoverDestination, 60f))
                    {
                        artemisHasRepositioned = 1f;
                        npc.netUpdate = true;
                    }
                    else
                        apollo.ai[1] = 0f;
                }

                if (attackTimer == 2f)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.ExoMechImpendingDeathSound, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Utilities.NewProjectileBetter(npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ArtemisDeathrayTelegraph>(), 0, 0f, -1, 0f, npc.whoAmI);

                        npc.velocity = Vector2.Zero;
                        npc.netUpdate = true;
                    }
                }

                // Create the laserbeam.
                if (attackTimer == laserbeamTelegraphTime)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Utilities.NewProjectileBetter(npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ArtemisSweepLaserbeam>(), PowerfulShotDamage, 0f, -1, npc.whoAmI);

                        spinDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        npc.velocity = Vector2.Zero;
                        npc.netUpdate = true;
                    }
                }

                // Handle frames.
                npc.frameCounter++;
                frame = (int)Math.Round(Lerp(10f, 19f, (float)npc.frameCounter / 36f % 1f));
                if (ExoMechManagement.ExoTwinsAreInSecondPhase)
                    frame += 60f;

                // Have Artemis sweep around.
                if (attackTimer >= laserbeamTelegraphTime)
                {
                    target.Infernum_Camera().CurrentScreenShakePower = 2f;
                    frame += 10f;
                    float spinAngle = (attackTimer - laserbeamTelegraphTime) / laserbeamSweepTime * spinArc * -spinDirection;
                    npc.velocity = Vector2.Zero;
                    npc.Center = new Vector2(spinningPointX, spinningPointY) + Vector2.UnitY.RotatedBy(spinAngle) * spinRadius;
                    npc.rotation = npc.AngleTo(new Vector2(spinningPointX, spinningPointY)) + PiOver2;
                }
                else if (artemisHasRepositioned == 1f)
                {
                    if (spinningPointX == 0f || Math.Abs(spinningPointX) > 100000f)
                    {
                        spinningPointX = target.Center.X;
                        spinningPointY = target.Center.Y;
                        if (spinningPointY < 1800f)
                            spinningPointY = 1800f;
                        npc.netUpdate = true;
                    }
                    npc.Center = artemisHoverDestination;
                    npc.rotation = 0f;
                    npc.velocity = Vector2.Zero;
                }
            }

            // Have Apollo hover to the side of the target and release plasma blasts.
            else
            {
                Vector2 hoverDestination = target.Center - Vector2.UnitY.RotatedBy(apolloAngularHoverOffset) * 675f;
                ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 30f, 84f);

                // Look at the target.
                npc.rotation = npc.AngleTo(target.Center) + PiOver2;

                // Handle frames.
                frame = (int)Math.Round(Lerp(20f, 29f, (float)attackTimer / apolloShootRate % 1f));
                if (ExoMechManagement.ExoTwinsAreInSecondPhase)
                    frame += 60f;

                // Fire plasma blasts.
                if (attackTimer % apolloShootRate == apolloShootRate - 1f && attackTimer < laserbeamAttackTime && artemisHasRepositioned == 1f)
                {
                    SoundEngine.PlaySound(PlasmaCaster.FireSound, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 plasmaShootCenter = npc.Center + npc.SafeDirectionTo(target.Center) * 70f;
                        Vector2 plasmaShootVelocity = npc.SafeDirectionTo(target.Center) * plasmaBlastShootSpeed;
                        Utilities.NewProjectileBetter(plasmaShootCenter, plasmaShootVelocity, ModContent.ProjectileType<ApolloPlasmaFireball>(), NormalShotDamage, 0f);

                        apolloAngularHoverOffset += TwoPi / 7f;
                        npc.netUpdate = true;
                    }
                }

                // Get REALLY pissed off if the player leaves the range of the laserbeam.
                if (enrageTimer <= 0f && !artemis.WithinRange(target.Center, ArtemisSweepLaserbeam.MaxLaserRayConst))
                {
                    enrageTimer = 900f;
                    npc.netUpdate = true;

                    // Play the impending death sound.
                    SoundEngine.PlaySound(InfernumSoundRegistry.ExoMechImpendingDeathSound, target.Center);

                    // Have Draedon comment on the player's attempts to escape.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.Status.Boss.DraedonAresEnrageText", DraedonNPC.TextColorEdgy);
                }

                if (attackTimer >= laserbeamAttackTime + attackTransitionDelay)
                {
                    Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<ApolloPlasmaFireball>(), ModContent.ProjectileType<AresPlasmaBolt>());
                    SelectNextAttack(npc);
                }
            }
        }

        public static void DoBehavior_ThermonuclearBlitz(NPC npc, Player target, ref float frame, ref float attackTimer)
        {
            int textSubstateTime = 172;
            int orbGrowTime = 95;
            int arcRedirectTime = 120;
            int desperationAttackTime = 1080;
            float orbMaxRadius = 1000f;
            float reelbackChargeSpeed = 33f;

            NPC apollo = Main.npc[CalamityGlobalNPC.draedonExoMechTwinGreen];
            NPC artemis = Main.npc[CalamityGlobalNPC.draedonExoMechTwinRed];
            bool isApollo = npc.type == ModContent.NPCType<Apollo>();
            ref float attackSubstate = ref apollo.Infernum().ExtraAI[0];
            ref float deathOrbIndex = ref apollo.Infernum().ExtraAI[1];
            ref float deathOrbRadius = ref apollo.Infernum().ExtraAI[2];
            ref float localAttackTimer = ref npc.Infernum().ExtraAI[3];
            attackTimer = ref apollo.ai[1];

            // Disable damage during this attack.
            npc.dontTakeDamage = false;
            npc.Calamity().DR = 0.9999999f;
            npc.Calamity().unbreakableDR = true;
            npc.Calamity().ShouldCloseHPBar = true;

            switch ((int)attackSubstate)
            {
                // Hover near the target.
                case 0:
                    // Handle frames.
                    npc.frameCounter++;
                    frame = (int)Math.Round(Lerp(10f, 19f, (float)npc.frameCounter / 45f % 1f));

                    // Have both twins stay close to each other when hovering.
                    Vector2 hoverDestination = target.Center + new Vector2(400f, -360f);
                    if (isApollo)
                        hoverDestination -= Vector2.One * 108f;
                    ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 36f, 84f);

                    // Play a charge sound on the first frame.
                    if (isApollo && attackTimer == 2f)
                        SoundEngine.PlaySound(InfernumSoundRegistry.ExoMechFinalPhaseSound);

                    // Give some warning text before attacking.
                    // What? What do you MEAN Artemis isn't actually saying this line?? I FEEL CHEATED!!!
                    if (isApollo && attackTimer == textSubstateTime / 2)
                        CalamityUtils.DisplayLocalizedText("Mods.InfernumMode.Status.ExoMechDesperationTwins1", ArtemisTextColor);

                    // Look at the target.
                    npc.rotation = npc.AngleTo(target.Center) + PiOver2;

                    if (attackTimer >= textSubstateTime && isApollo)
                    {
                        if (isApollo)
                            CalamityUtils.DisplayLocalizedText("Mods.InfernumMode.Status.ExoMechDesperationTwins2", ApolloTextColor);

                        attackSubstate = 1f;
                        attackTimer = 0f;
                        deathOrbRadius = 1f;
                        npc.netUpdate = true;

                        // Create the death orb.
                        if (Main.netMode != NetmodeID.MultiplayerClient && isApollo)
                        {
                            deathOrbIndex = Utilities.NewProjectileBetter(npc.Center + npc.SafeDirectionTo(target.Center) * 300f, Vector2.Zero, ModContent.ProjectileType<ThermonuclearDeathOrb>(), PowerfulShotDamage, 0f, -1, 0f, npc.whoAmI);
                            npc.netUpdate = true;
                        }
                    }

                    break;

                // Prepare the Thermonuclear Orb before firing it.
                case 1:
                    // Handle frames.
                    npc.frameCounter++;
                    frame = (int)Math.Round(Lerp(10f, 19f, (float)npc.frameCounter / 45f % 1f));

                    // Make the orb grow.
                    if (isApollo)
                    {
                        float orbGrowInterpolant = Pow(Utils.GetLerpValue(0f, orbGrowTime, attackTimer, true), 2.3f);
                        deathOrbRadius = Lerp(1f, orbMaxRadius, orbGrowInterpolant);
                    }

                    // Send energy towrds the orb.
                    Projectile energyOrb = Main.projectile[(int)deathOrbIndex];
                    bool energyOrbIsValid = energyOrb.type == ModContent.ProjectileType<ThermonuclearDeathOrb>();
                    if (energyOrbIsValid)
                    {
                        // Look at the orb.
                        npc.rotation = npc.AngleTo(energyOrb.Center) + PiOver2;
                        Vector2 currentDirection = npc.SafeDirectionTo(energyOrb.Center);

                        // Gradually move away from the orb.
                        npc.velocity = Vector2.Zero;
                        if (deathOrbRadius >= 300f)
                            npc.Center -= npc.SafeDirectionTo(energyOrb.Center) * (deathOrbRadius - 300f) / 20f;

                        if (isApollo)
                        {
                            Vector2 endOfFlamethrower = npc.Center + currentDirection * 84f;
                            Vector2 plasmaVelocity = currentDirection.RotatedByRandom(0.11f) * endOfFlamethrower.Distance(energyOrb.Center) * 0.1f;
                            MediumMistParticle plasma = new(endOfFlamethrower, plasmaVelocity, Color.Lime, Color.YellowGreen, 1.3f, 255f);
                            GeneralParticleHandler.SpawnParticle(plasma);
                        }
                        else
                        {
                            Vector2 fireSpawnPosition = npc.Center + currentDirection.RotatedByRandom(PiOver2) * 100f;
                            Dust fire = Dust.NewDustPerfect(fireSpawnPosition, 6);
                            fire.velocity = (energyOrb.Center - fire.position) * 0.1f;
                            fire.scale = 2f;
                            fire.noGravity = true;
                            fire.fadeIn = Main.rand.NextFloat(0.6f);
                        }
                    }

                    if (attackTimer >= orbGrowTime)
                    {
                        attackSubstate = 2f;
                        attackTimer = 0f;
                        apollo.velocity = apollo.SafeDirectionTo(energyOrb.Center) * -reelbackChargeSpeed;
                        artemis.velocity = artemis.SafeDirectionTo(energyOrb.Center) * -reelbackChargeSpeed;
                        if (energyOrbIsValid)
                        {
                            energyOrb.velocity = energyOrb.SafeDirectionTo(target.Center) * 3f;
                            energyOrb.netUpdate = true;
                        }

                        Utilities.CreateShockwave(npc.Center);

                        apollo.netUpdate = true;
                        npc.netUpdate = true;
                    }

                    break;

                // Arc back towards the target.
                case 2:
                    // Handle frames.
                    npc.frameCounter++;
                    frame = (int)Math.Round(Lerp(20f, 29f, (float)npc.frameCounter / 45f % 1f));

                    // Arc around for a bit, intending to redirect towards the target.
                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.06f);
                    npc.rotation = npc.velocity.ToRotation() + PiOver2;

                    if (attackTimer >= arcRedirectTime)
                    {
                        attackSubstate = 3f;
                        attackTimer = 0f;
                        artemis.Infernum().ExtraAI[3] = 16f;
                        npc.netUpdate = true;
                    }

                    break;

                // Perform increasingly rapid charges.
                case 3:
                    // Handle frames.
                    npc.frameCounter++;
                    frame = (int)Math.Round(Lerp(20f, 29f, (float)npc.frameCounter / 45f % 1f));

                    // Do damage.
                    npc.damage = npc.defDamage;

                    float desperationInterpolant = Utils.GetLerpValue(0f, desperationAttackTime * 0.475f, attackTimer, true);
                    int chargeRate = 56;
                    float chargeSpeed = 45f;

                    // Go a bit easier on the player if they don't have a dash.
                    if (!target.HasDash())
                    {
                        chargeRate += 23;
                        chargeSpeed *= 0.7f;
                    }

                    Vector2 directionToTarget = npc.SafeDirectionTo(target.Center);

                    // Crash into the other mech if the attack should conclude.
                    if (attackTimer >= desperationAttackTime)
                    {
                        // Stop doing damage for the final charge- The player has already won.
                        npc.damage = 0;

                        if (isApollo && npc.Hitbox.Intersects(artemis.Hitbox))
                            PerformDeathAnimationExplosion(npc);
                    }

                    if (localAttackTimer == 1f)
                    {
                        // Play a charge sound.
                        SoundStyle chargeSound = InfernumSoundRegistry.SafeLoadCalamitySound("Sounds/Custom/ExoMechs/ArtemisApolloDash", CommonCalamitySounds.ELRFireSound);
                        SoundEngine.PlaySound(chargeSound with
                        {
                            Volume = 1.5f
                        }, npc.Center);

                        // Charge at the target.
                        npc.velocity = directionToTarget * chargeSpeed;
                        npc.rotation = npc.velocity.ToRotation() + PiOver2;
                        npc.netUpdate = true;
                    }

                    // Turn to face the player again in anticipation of the next charge.
                    if (localAttackTimer >= chargeRate - 16f)
                    {
                        npc.velocity *= 0.7f;
                        npc.rotation = npc.rotation.AngleLerp(directionToTarget.ToRotation() + PiOver2, 0.24f);

                        npc.netUpdate = true;
                    }

                    else if (localAttackTimer >= 12f)
                        npc.rotation = npc.velocity.ToRotation() + PiOver2;

                    localAttackTimer++;
                    if (localAttackTimer >= chargeRate)
                    {
                        localAttackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;

                default:
                    attackSubstate = 0f;
                    attackTimer = 0f;
                    Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<ThermonuclearDeathOrb>());
                    break;
            }

            if (ExoMechManagement.ExoTwinsAreInSecondPhase)
                frame += 60f;
        }

        public static void SelectNextAttack(NPC npc)
        {
            // Reset the frame counter, in case it was used in the previous attack.
            npc.frameCounter = 0f;

            TwinsAttackType oldAttackType = (TwinsAttackType)(int)npc.ai[0];
            ref float previousSpecialAttack = ref npc.Infernum().ExtraAI[17];

            if (ExoMechComboAttackContent.ShouldSelectComboAttack(npc, out ExoMechComboAttackContent.ExoMechComboAttackType newAttack))
            {
                npc.ai[0] = (int)newAttack;
                if (npc.type == ModContent.NPCType<Apollo>())
                {
                    NPC artemis = Main.npc[NPC.FindFirstNPC(ModContent.NPCType<Artemis>())];
                    artemis.ai[0] = npc.ai[0];
                }
            }
            else
            {
                npc.ai[0] = (int)TwinsAttackType.BasicShots;
                if (oldAttackType == TwinsAttackType.BasicShots)
                {
                    int tries = 0;
                    do
                    {
                        npc.ai[0] = (int)TwinsAttackType.FireCharge;
                        if (Main.rand.NextBool(3))
                            npc.ai[0] = (int)TwinsAttackType.GatlingLaserAndPlasmaFlames;
                        if (ExoMechManagement.CurrentTwinsPhase >= 2 && Main.rand.NextBool())
                            npc.ai[0] = (int)(Main.rand.NextBool() ? TwinsAttackType.ArtemisLaserRay : TwinsAttackType.ApolloPlasmaCharges);
                        if (ExoMechManagement.CurrentTwinsPhase >= 5 && Main.rand.NextBool())
                            npc.ai[0] = (int)TwinsAttackType.SlowLaserRayAndPlasmaBlasts;
                        tries++;

                        if (tries >= 1000)
                            break;
                    }
                    while (previousSpecialAttack == npc.ai[0]);
                    previousSpecialAttack = npc.ai[0];

                    // Perform the ultimate attack if in the final phase.
                    if (ExoMechManagement.CurrentTwinsPhase >= 6)
                        npc.ai[0] = (int)TwinsAttackType.ThermonuclearBlitz;
                }
            }

            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            // Increment the attack counter. It is used when determining if the mechs should swap sides.
            npc.Infernum().ExtraAI[6]++;

            // Reset flame tails.
            if (npc.type == ModContent.NPCType<Artemis>())
                npc.ModNPC<Artemis>().ChargeFlash = 0f;
            if (npc.type == ModContent.NPCType<Apollo>())
                npc.ModNPC<Apollo>().ChargeComboFlash = 0f;

            // Inform Apollo of the attack state change if Artemis calls this method, as Artemis' state relies on Apollo's.
            if (npc.type == ModContent.NPCType<Artemis>() && Main.npc.IndexInRange(npc.realLife) && Main.npc[npc.realLife].active && Main.npc[npc.realLife].type != ModContent.NPCType<Artemis>())
                SelectNextAttack(Main.npc[npc.realLife]);
            else
            {
                NPC artemis = Main.npc[NPC.FindFirstNPC(ModContent.NPCType<Artemis>())];
                artemis.ai[0] = npc.ai[0];
                artemis.ai[1] = 0f;
                for (int i = 0; i < 5; i++)
                    artemis.Infernum().ExtraAI[i] = 0f;

                // Switch sides.
                if (npc.Infernum().ExtraAI[6] % 5f == 2f)
                {
                    npc.Infernum().ExtraAI[18] = 90f;
                    npc.ai[2] *= -1f;
                }
                artemis.netUpdate = true;
            }

            // Delete leftover Artemis lasers.
            foreach (Projectile laser in Utilities.AllProjectilesByID(ModContent.ProjectileType<ArtemisLaser>()))
                laser.Kill();

            npc.netUpdate = true;
        }

        public static void PerformDeathAnimationExplosion(NPC npc)
        {
            SoundEngine.PlaySound(InfernumSoundRegistry.WyrmChargeSound with
            {
                Volume = 1.75f
            }, npc.Center);

            GeneralParticleHandler.SpawnParticle(new ElectricExplosionRing(npc.Center, Vector2.Zero, CalamityUtils.ExoPalette, 3f, 90));
            for (int i = 0; i < 40; i++)
            {
                Vector2 sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f, 24f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(npc.Center, sparkVelocity, Main.rand.NextBool(4), 60, 2f, Color.Gold));

                sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f, 24f);
                Color arcColor = Color.Lerp(Color.Yellow, Color.Cyan, Main.rand.NextFloat());
                GeneralParticleHandler.SpawnParticle(new ElectricArc(npc.Center, sparkVelocity, arcColor, 0.84f, 60));
            }

            for (int i = 0; i < 32; i++)
            {
                Color smokeColor = Color.Lerp(Color.Yellow, Color.Cyan, Main.rand.NextFloat());
                Vector2 smokeVelocity = (TwoPi * i / 32f).ToRotationVector2() * Main.rand.NextFloat(7f, 11.5f) + Main.rand.NextVector2Circular(4f, 4f);
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(npc.Center, smokeVelocity, smokeColor, 56, 2.4f, 1f));

                smokeVelocity *= 2f;
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(npc.Center, smokeVelocity, smokeColor, 56, 3f, 1f));
            }
            Utilities.CreateShockwave(npc.Center);

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
            npc.active = false;
        }

        #endregion AI

        #region Frames and Drawcode
        public override void FindFrame(NPC npc, int frameHeight)
        {
            int frameX = (int)npc.localAI[0] / 9;
            int frameY = (int)npc.localAI[0] % 9;
            npc.frame = new Rectangle(npc.width * frameX, npc.height * frameY, npc.width, npc.height);
        }

        public static float FlameTrailWidthFunction(NPC npc, float completionRatio) => SmoothStep(21f, 8f, completionRatio) * npc.ModNPC<Apollo>().ChargeComboFlash * npc.Opacity;

        public static float FlameTrailWidthFunctionBig(NPC npc, float completionRatio) => SmoothStep(34f, 12f, completionRatio) * npc.ModNPC<Apollo>().ChargeComboFlash * npc.Opacity;

        public static float RibbonTrailWidthFunction(float completionRatio)
        {
            float baseWidth = Utils.GetLerpValue(1f, 0.54f, completionRatio, true) * 5f;
            float endTipWidth = CalamityUtils.Convert01To010(Utils.GetLerpValue(0.96f, 0.89f, completionRatio, true)) * 2.4f;
            return baseWidth + endTipWidth;
        }

        public static Color FlameTrailColorFunction(NPC npc, float completionRatio)
        {
            float trailOpacity = Utils.GetLerpValue(0.8f, 0.27f, completionRatio, true) * Utils.GetLerpValue(0f, 0.067f, completionRatio, true) * npc.Opacity;
            Color startingColor = Color.Lerp(Color.White, Color.Cyan, 0.27f);
            Color middleColor = Color.Lerp(Color.Orange, Color.ForestGreen, 0.74f);
            Color endColor = Color.Lime;
            return CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * npc.ModNPC<Apollo>().ChargeComboFlash * trailOpacity;
        }

        public static Color FlameTrailColorFunctionBig(NPC npc, float completionRatio)
        {
            float trailOpacity = Utils.GetLerpValue(0.8f, 0.27f, completionRatio, true) * Utils.GetLerpValue(0f, 0.067f, completionRatio, true) * 0.56f;
            Color startingColor = Color.Lerp(Color.White, Color.Cyan, 0.25f);
            Color middleColor = Color.Lerp(Color.Blue, Color.White, 0.35f);
            Color endColor = Color.Lerp(Color.DarkBlue, Color.White, 0.47f);
            Color color = CalamityUtils.MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * npc.ModNPC<Apollo>().ChargeComboFlash * trailOpacity;
            color.A = 0;
            return color;
        }

        public static Color RibbonTrailColorFunction(NPC npc, float completionRatio)
        {
            Color startingColor = new(34, 40, 48);
            Color endColor = new(40, 160, 32);
            return Color.Lerp(startingColor, endColor, Pow(completionRatio, 1.5f)) * npc.Opacity;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            // Declare the trail drawers if they have yet to be defined.
            if (npc.ModNPC<Apollo>().ChargeFlameTrail is null)
                npc.ModNPC<Apollo>().ChargeFlameTrail = new PrimitiveTrail(c => FlameTrailWidthFunction(npc, c), c => FlameTrailColorFunction(npc, c), null, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);

            if (npc.ModNPC<Apollo>().ChargeFlameTrailBig is null)
                npc.ModNPC<Apollo>().ChargeFlameTrailBig = new PrimitiveTrail(c => FlameTrailWidthFunctionBig(npc, c), c => FlameTrailColorFunctionBig(npc, c), null, GameShaders.Misc["CalamityMod:ImpFlameTrail"]);

            if (npc.ModNPC<Apollo>().RibbonTrail is null)
                npc.ModNPC<Apollo>().RibbonTrail = new PrimitiveTrail(RibbonTrailWidthFunction, c => RibbonTrailColorFunction(npc, c));

            // Prepare the flame trail shader with its map texture.
            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(InfernumTextureRegistry.StreakFaded);
            DrawExoTwin(npc, lightColor, npc.ModNPC<Apollo>().ChargeComboFlash, npc.ModNPC<Apollo>().RibbonTrail, npc.ModNPC<Apollo>().ChargeFlameTrail, npc.ModNPC<Apollo>().ChargeFlameTrailBig);
            return false;
        }

        public static float TelegraphWidthFunction(float _) => 96f;

        public static Color TelegraphColorFunction(NPC npc, float completionRatio)
        {
            float attackTimer = npc.ai[1];
            float telegraphOpacity = Utils.GetLerpValue(0f, 16f, attackTimer, true) * Utils.GetLerpValue(ArtemisLaserbeamTelegraph.TrueLifetime, ArtemisLaserbeamTelegraph.TrueLifetime - 8f, attackTimer, true);
            float endFadeOpacity = Utils.GetLerpValue(0f, 0.15f, completionRatio, true) * Utils.GetLerpValue(1f, 0.8f, completionRatio, true);
            return Color.Orange * endFadeOpacity * telegraphOpacity * 0.2f;
        }

        public static void DrawExoTwin(NPC npc, Color lightColor, float flashInterpolant, PrimitiveTrail ribbonTrail, PrimitiveTrail chargeFlameTrail, PrimitiveTrail chargeFlameTrailBig)
        {
            int numAfterimages = flashInterpolant > 0f ? 0 : 5;
            bool isArtemis = npc.type == ModContent.NPCType<Artemis>();
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Texture2D glowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Apollo/ApolloGlow").Value;
            if (isArtemis)
                glowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Artemis/ArtemisGlow").Value;

            if (!Main.npc.IndexInRange(CalamityGlobalNPC.draedonExoMechTwinGreen) || !Main.npc[CalamityGlobalNPC.draedonExoMechTwinGreen].active)
                return;

            NPC apollo = Main.npc[CalamityGlobalNPC.draedonExoMechTwinGreen];

            Rectangle frame = npc.frame;
            Vector2 origin = npc.Size * 0.5f;
            Vector2 center = npc.Center - Main.screenPosition;
            Color afterimageBaseColor = ExoMechComboAttackContent.EnrageTimer > 0f || apollo.Infernum().ExtraAI[ExoMechManagement.Twins_ComplementMechEnrageTimerIndex] > 0f ? Color.Red : Color.White;

            // Artemis telegraphs which direction it will spin when performing the alone Ohio beam attack.
            if (apollo.ai[0] == (int)TwinsAttackType.ArtemisLaserRay && isArtemis && npc.Infernum().ExtraAI[0] == 1f)
            {
                Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/TrientCircularSmear").Value;

                float spinningPointX = npc.Infernum().ExtraAI[2];
                float spinningPointY = npc.Infernum().ExtraAI[3];
                Vector2 offset = npc.Center - new Vector2(spinningPointX, spinningPointY);
                float telegraphDirection = npc.Infernum().ExtraAI[1];
                if (telegraphDirection == 0)
                    telegraphDirection = 1;

                // Get the correct opacity and rotation.
                float attackTimer = npc.ai[1];
                float telegraphOpacity = Utils.GetLerpValue(0f, 16f, attackTimer, true) * Utils.GetLerpValue(ArtemisLaserbeamTelegraph.TrueLifetime, ArtemisLaserbeamTelegraph.TrueLifetime - 8f, attackTimer, true);
                Color smearColor = Color.Lerp(Color.Gold, Color.Orange, 0.75f);
                smearColor *= telegraphOpacity;
                float smearRotation = npc.rotation + (telegraphDirection == 1 ? Pi : 0);

                Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
                Main.EntitySpriteDraw(smear, npc.Center - offset * 0.32f - Main.screenPosition, null, smearColor, smearRotation, smear.Size() * 0.5f, npc.scale * 3f, 0, 0);
                Main.spriteBatch.ExitShaderRegion();
            }

            // Draws a single instance of a regular, non-glowmask based Apollo.
            // This is created to allow easy duplication of them when drawing the charge.
            void drawInstance(Vector2 drawOffset, Color baseColor)
            {
                if (npc.Opacity < 0.02f)
                    return;

                if (CalamityConfig.Instance.Afterimages)
                {
                    for (int i = 1; i < numAfterimages; i += 2)
                    {
                        float afterimageOpacity = (numAfterimages - i) / 15f;
                        Color afterimageColor = npc.GetAlpha(Color.Lerp(baseColor, afterimageBaseColor, 0.75f)) * afterimageOpacity;
                        afterimageColor.A /= 8;

                        Vector2 afterimageCenter = npc.oldPos[i] + frame.Size() * 0.5f - Main.screenPosition;
                        Main.spriteBatch.Draw(texture, afterimageCenter, npc.frame, afterimageColor, npc.rotation, origin, npc.scale, 0, 0f);
                        Main.spriteBatch.Draw(glowmask, afterimageCenter, npc.frame, afterimageBaseColor * afterimageOpacity, npc.rotation, origin, npc.scale, 0, 0f);
                    }
                }

                Main.spriteBatch.Draw(texture, center + drawOffset, frame, npc.GetAlpha(baseColor), npc.rotation, origin, npc.scale, 0, 0f);
                Main.spriteBatch.Draw(glowmask, center + drawOffset, frame, npc.GetAlpha(afterimageBaseColor), npc.rotation, origin, npc.scale, 0, 0f);
            }

            // Draw ribbons near the main thruster.
            for (int direction = -1; direction <= 1; direction += 2)
            {
                Vector2 ribbonOffset = -Vector2.UnitY.RotatedBy(npc.rotation) * 14f;
                ribbonOffset += Vector2.UnitX.RotatedBy(npc.rotation) * direction * 26f;

                float currentSegmentRotation = npc.rotation;
                List<Vector2> ribbonDrawPositions = new();
                for (int i = 0; i < 12; i++)
                {
                    float ribbonCompletionRatio = i / 12f;
                    float wrappedAngularOffset = WrapAngle(npc.oldRot[i + 1] - currentSegmentRotation) * 0.3f;
                    float segmentRotationOffset = Clamp(wrappedAngularOffset, -0.12f, 0.12f);

                    // Add a sinusoidal offset that goes based on time and completion ratio to create a waving-flag-like effect.
                    // This is dampened for the first few points to prevent weird offsets. It is also dampened by high velocity.
                    float sinusoidalRotationOffset = Sin(ribbonCompletionRatio * 2.22f + Main.GlobalTimeWrappedHourly * 3.4f) * 1.36f;
                    float sinusoidalRotationOffsetFactor = Utils.GetLerpValue(0f, 0.37f, ribbonCompletionRatio, true) * direction * 24f;
                    sinusoidalRotationOffsetFactor *= Utils.GetLerpValue(24f, 16f, npc.velocity.Length(), true);

                    Vector2 sinusoidalOffset = Vector2.UnitY.RotatedBy(npc.rotation + sinusoidalRotationOffset) * sinusoidalRotationOffsetFactor;
                    Vector2 ribbonSegmentOffset = Vector2.UnitY.RotatedBy(currentSegmentRotation) * ribbonCompletionRatio * 540f + sinusoidalOffset;
                    ribbonDrawPositions.Add(npc.Center + ribbonSegmentOffset + ribbonOffset);

                    currentSegmentRotation += segmentRotationOffset;
                }
                ribbonTrail.Draw(ribbonDrawPositions, -Main.screenPosition, 66);
            }

            int instanceCount = (int)Lerp(1f, 15f, flashInterpolant);
            Color baseInstanceColor = Color.Lerp(lightColor, Color.White, flashInterpolant);
            baseInstanceColor.A = (byte)(int)(255f - flashInterpolant * 255f);

            Main.spriteBatch.EnterShaderRegion();

            ExoMechAIUtilities.DrawFinalPhaseGlow(npc, texture, center, frame, origin);
            drawInstance(Vector2.Zero, baseInstanceColor);

            if (instanceCount > 1)
            {
                baseInstanceColor *= 0.04f;
                float backAfterimageOffset = SmoothStep(0f, 2f, flashInterpolant);
                for (int i = 0; i < instanceCount; i++)
                {
                    Vector2 drawOffset = (TwoPi * i / instanceCount + Main.GlobalTimeWrappedHourly * 0.8f).ToRotationVector2() * backAfterimageOffset;
                    drawInstance(drawOffset, baseInstanceColor);
                }
            }
            Main.spriteBatch.ExitShaderRegion();

            // Draw a flame trail on the thrusters if needed. This happens during charges.
            if (flashInterpolant > 0f)
            {
                for (int direction = -1; direction <= 1; direction++)
                {
                    Vector2 baseDrawOffset = new Vector2(0f, direction == 0f ? 18f : 60f).RotatedBy(npc.rotation);
                    baseDrawOffset += new Vector2(direction * 64f, 0f).RotatedBy(npc.rotation);

                    float backFlameLength = direction == 0f ? 700f : 190f;
                    Vector2 drawStart = npc.Center + baseDrawOffset;
                    Vector2 drawEnd = drawStart - (npc.rotation - PiOver2).ToRotationVector2() * flashInterpolant * backFlameLength;
                    Vector2[] drawPositions = new Vector2[]
                    {
                        drawStart,
                        drawEnd
                    };

                    if (direction == 0)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            Vector2 drawOffset = (TwoPi * i / 4f).ToRotationVector2() * 8f;
                            chargeFlameTrailBig.Draw(drawPositions, drawOffset - Main.screenPosition, 70);
                        }
                    }
                    else
                        chargeFlameTrail.Draw(drawPositions, -Main.screenPosition, 70);
                }
            }

            // Artemis telegraphs that it will perform basic shots with a gleam and line telegraph.
            if (apollo.ai[0] == (int)TwinsAttackType.BasicShots && isArtemis)
            {
                Main.spriteBatch.SetBlendState(BlendState.Additive);

                float telegraphInterpolant = npc.Infernum().ExtraAI[0];

                // Draw the pupil gleam.
                float pupilOffset = ExoMechManagement.ExoTwinsAreInSecondPhase ? 102f : 70f;
                Vector2 pupilPosition = npc.Center - Vector2.UnitY.RotatedBy(npc.rotation) * pupilOffset - Main.screenPosition;
                Texture2D pupilStarTexture = InfernumTextureRegistry.LargeStar.Value;
                Vector2 pupilOrigin = pupilStarTexture.Size() * 0.5f;

                float gleamFadeFactor = Utils.GetLerpValue(0f, 0.24f, telegraphInterpolant, true) * Utils.GetLerpValue(1f, 0.76f, telegraphInterpolant, true);
                Vector2 pupilScale = new Vector2(0.1f, 0.3f) * gleamFadeFactor;

                Main.spriteBatch.Draw(pupilStarTexture, pupilPosition, null, Color.Yellow * gleamFadeFactor, npc.rotation, pupilOrigin, pupilScale, 0, 0f);
                pupilScale = new Vector2(0.15f, 0.55f) * gleamFadeFactor;
                Main.spriteBatch.Draw(pupilStarTexture, pupilPosition, null, Color.OrangeRed * gleamFadeFactor, npc.rotation + PiOver2, pupilOrigin, pupilScale, 0, 0f);

                // Draw the telegraph line.
                Texture2D line = InfernumTextureRegistry.BloomLineSmall.Value;

                float telegraphWidth = telegraphInterpolant * 1.1f;
                Color telegraphColor = Color.Orange * telegraphInterpolant * gleamFadeFactor;
                Vector2 beamOrigin = new(line.Width / 2f, line.Height);
                Vector2 beamScale = new(telegraphWidth, ArtemisBasicShotLaser.LaserLength / line.Height);
                Main.spriteBatch.Draw(line, pupilPosition, null, telegraphColor, npc.rotation, beamOrigin, beamScale, 0, 0f);

                Main.spriteBatch.ResetBlendState();
            }
        }
        #endregion Frames and Drawcode

        #region Death Effects
        public override bool CheckDead(NPC npc)
        {
            if (npc.ai[0] == (int)TwinsAttackType.ThermonuclearBlitz && ExoMechManagement.TotalMechs <= 1)
                return true;
            return ExoMechManagement.HandleDeathEffects(npc);
        }
        #endregion Death Effects

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Mods.InfernumMode.PetDialog.ExoTwinsTip1";
        }
        #endregion Tips
    }
}
