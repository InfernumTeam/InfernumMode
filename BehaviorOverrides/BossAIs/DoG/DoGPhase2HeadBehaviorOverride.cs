using CalamityMod;
using CalamityMod.Events;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.Items.Weapons.Typeless;
using CalamityMod.NPCs;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Sounds;
using InfernumMode.Achievements;
using InfernumMode.BehaviorOverrides.BossAIs.CeaselessVoid;
using InfernumMode.GlobalInstances.Players;
using InfernumMode.Projectiles;
using InfernumMode.Skies;
using InfernumMode.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.BehaviorOverrides.BossAIs.DoG.DoGPhase1HeadBehaviorOverride;

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public static class DoGPhase2HeadBehaviorOverride
    {
        public enum SpecialAttackType
        {
            LaserWalls = 1,
            CircularLaserBurst,
            ChargeGates
        }

        public enum PerpendicularPortalAttackState
        {
            NotPerformingAttack,
            EnteringPortal,
            Waiting,
            AttackEndDelay
        }

        public enum BodySegmentFadeType
        {
            InhertHeadOpacity,
            EnteringPortal,
            ApproachAheadSegmentOpacity
        }

        public static bool InPhase2
        {
            get
            {
                if (CalamityGlobalNPC.DoGHead < 0)
                    return false;

                return Main.npc[CalamityGlobalNPC.DoGHead].Infernum().ExtraAI[InPhase2FlagIndex] == 1f;
            }
            set
            {
                if (CalamityGlobalNPC.DoGHead < 0)
                    return;

                Main.npc[CalamityGlobalNPC.DoGHead].Infernum().ExtraAI[InPhase2FlagIndex] = value.ToInt();
            }
        }

        public static float FadeToAntimatterForm
        {
            get
            {
                if (CalamityGlobalNPC.DoGHead < 0)
                    return 0f;

                return Main.npc[CalamityGlobalNPC.DoGHead].Infernum().ExtraAI[AntimatterFormInterpolantIndex];
            }
            set
            {
                if (CalamityGlobalNPC.DoGHead < 0)
                    return;

                Main.npc[CalamityGlobalNPC.DoGHead].Infernum().ExtraAI[AntimatterFormInterpolantIndex] = value;
            }
        }

        public static PerpendicularPortalAttackState SurprisePortalAttackState
        {
            get
            {
                if (CalamityGlobalNPC.DoGHead < 0)
                    return 0f;

                return (PerpendicularPortalAttackState)Main.npc[CalamityGlobalNPC.DoGHead].Infernum().ExtraAI[PerpendicularPortalAttackStateIndex];
            }
            set
            {
                if (CalamityGlobalNPC.DoGHead < 0)
                    return;

                Main.npc[CalamityGlobalNPC.DoGHead].Infernum().ExtraAI[PerpendicularPortalAttackStateIndex] = (int)value;
            }
        }

        public const int PostP2AnimationMoveDelay = 45;

        public const int SpecialAttackDuration = 675;

        public const int SpecialAttackPortalCreationDelay = 25;

        public const int SpecialAttackPortalSnapDelay = 65;

        public const int PassiveMovementTimeP2 = 360;

        public const int AggressiveMovementTimeP2 = 720;

        public const int SpecialAttackDelay = 1335;

        public const int SpecialAttackTransitionPreparationTime = 135;

        public const float CanUseSpecialAttacksLifeRatio = 0.8f;
        
        public const float CanUseSignusSentinelAttackLifeRatio = 0.7f;

        public const float FinalPhaseLifeRatio = 0.2f;

        #region AI
        public static bool Phase2AI(NPC npc, ref float phaseCycleTimer, ref float passiveAttackDelay, ref float portalIndex, ref float segmentFadeType, ref float universalFightTimer)
        {
            ref float performingSpecialAttack = ref npc.Infernum().ExtraAI[PerformingSpecialAttackFlagIndex];
            ref float specialAttackTimer = ref npc.Infernum().ExtraAI[SpecialAttackTimerIndex];
            ref float hasEnteredFinalPhaseFlag = ref npc.Infernum().ExtraAI[HasEnteredFinalPhaseFlagIndex];
            ref float sentinelAttackTimer = ref npc.Infernum().ExtraAI[SentinelAttackTimerIndex];
            ref float jawRotation = ref npc.Infernum().ExtraAI[JawRotationIndex];
            ref float chompEffectsCountdown = ref npc.Infernum().ExtraAI[ChompEffectsCountdownIndex];
            ref float flyAcceleration = ref npc.Infernum().ExtraAI[CurrentFlyAccelerationIndex];
            ref float postAnimationMoveDelay = ref npc.Infernum().ExtraAI[AnimationMoveDelayIndex];
            ref float hasPerformedSpecialAttackBefore = ref npc.Infernum().ExtraAI[HasPerformedSpecialAttackYetFlagIndex];
            ref float phase2IntroductionAnimationTimer = ref npc.Infernum().ExtraAI[Phase2IntroductionAnimationTimerIndex];
            ref float deathAnimationTimer = ref npc.Infernum().ExtraAI[DeathAnimationTimerIndex];
            ref float destroyedSegmentsCount = ref npc.Infernum().ExtraAI[DestroyedSegmentsCountIndex];
            ref float aggressiveChargeCycleCounter = ref npc.Infernum().ExtraAI[Phase2AggressiveChargeCycleCounterIndex];
            ref float perpendicularPortalAttackTimer = ref npc.Infernum().ExtraAI[PerpendicularPortalAttackTimerIndex];
            ref float perpendicularPortalAngle = ref npc.Infernum().ExtraAI[PerpendicularPortalAngleIndex];
            ref float previousSnapAngle = ref npc.Infernum().ExtraAI[PreviousSnapAngleIndex];
            ref float damageImmunityCountdown = ref npc.Infernum().ExtraAI[DamageImmunityCountdownIndex];

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Get rid of the dumb edgy on-hit text.
            target.Calamity().dogTextCooldown = 20;

            // Disable teleportations.
            target.Calamity().normalityRelocator = false;
            target.Calamity().spectralVeil = false;

            // Take more damage than usual.
            npc.takenDamageMultiplier = 2f;

            // Declare the global whoAmI index.
            CalamityGlobalNPC.DoGHead = npc.whoAmI;

            // Handle fade-in logic when the boss is summoned.
            if (phase2IntroductionAnimationTimer < DoGPhase2IntroPortalGate.Phase2AnimationTime)
            {
                npc.TargetClosest();
                target = Main.player[npc.target];
                if (!Utilities.AnyProjectiles(ModContent.ProjectileType<DoGPhase2IntroPortalGate>()))
                {
                    npc.Center = target.Center - Vector2.UnitY * 600f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        portalIndex = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<DoGPhase2IntroPortalGate>(), 0, 0f);
                }

                npc.Opacity = 0f;
                npc.dontTakeDamage = true;
                segmentFadeType = (int)BodySegmentFadeType.InhertHeadOpacity;

                // Stay far above the player, but get increasing close as the animation goes on.
                // This is a trick to make the background fade from violet/cyan to black as the animation goes on.
                // This probably is fucked in multiplayer but whatever lmao.
                npc.Center = target.Center - Vector2.UnitY * MathHelper.Lerp(6000f, 3000f, phase2IntroductionAnimationTimer / DoGPhase2IntroPortalGate.Phase2AnimationTime);
                phase2IntroductionAnimationTimer++;
                passiveAttackDelay = 0f;
                phaseCycleTimer = 0f;

                // Teleport to the position of the portal and charge at the target after the animation concludes.
                if (phase2IntroductionAnimationTimer >= DoGPhase2IntroPortalGate.Phase2AnimationTime)
                {
                    npc.Opacity = 1f;
                    npc.Center = Main.projectile[(int)portalIndex].Center;
                    npc.velocity = npc.SafeDirectionTo(target.Center) * 36f;
                    npc.netUpdate = true;
                    
                    // Reset the special attack portal index to -1 and re-intiialize the sentinel attack timer.
                    portalIndex = -1f;
                    sentinelAttackTimer = 0f;
                }
                return false;
            }

            // Percent life remaining
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Variables
            int wrappedPhaseCycleTimer = (int)phaseCycleTimer % (PassiveMovementTimeP2 + AggressiveMovementTimeP2);
            bool canPerformSpecialAttacks = lifeRatio < CanUseSpecialAttacksLifeRatio;
            bool finalPhase = lifeRatio < FinalPhaseLifeRatio;
            bool doPassiveMovement = wrappedPhaseCycleTimer >= AggressiveMovementTimeP2 && !finalPhase;

            // Instantly transition to charge combos when entering the final phase.
            // Also say something about gods and death because this worm has a serious ego problem.
            if (hasEnteredFinalPhaseFlag == 0f && finalPhase)
            {
                hasEnteredFinalPhaseFlag = 1f;
                doPassiveMovement = false;
                specialAttackTimer = SpecialAttackDelay - SpecialAttackTransitionPreparationTime - 1f;
                Utilities.DisplayText("A GOD DOES NOT FEAR DEATH!", Color.Cyan);
                npc.netUpdate = true;
            }

            // Don't take damage when fading out or in the middle of a damage cooldown.
            npc.dontTakeDamage = npc.Opacity < 0.5f;
            npc.damage = npc.dontTakeDamage ? 0 : 885;
            npc.Calamity().DR = 0.2f;
            if (damageImmunityCountdown > 0f)
            {
                npc.Calamity().DR = 0.9999999f;
                npc.Calamity().unbreakableDR = true;
                damageImmunityCountdown--;
            }
            
            // Stay in the world.
            if (SurprisePortalAttackState == PerpendicularPortalAttackState.NotPerformingAttack)
                npc.position.Y = MathHelper.Clamp(npc.position.Y, 180f, Main.maxTilesY * 16f - 180f);

            // Do the death animation once dead.
            if (deathAnimationTimer > 0f)
            {
                // Mark DoG as defeated for the achievement.
                AchievementPlayer.DoGDefeated = true;
                DoDeathEffects(npc, deathAnimationTimer, ref destroyedSegmentsCount);
                jawRotation = jawRotation.AngleTowards(0f, 0.07f);
                segmentFadeType = (int)BodySegmentFadeType.ApproachAheadSegmentOpacity;
                npc.Opacity = 1f;
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                deathAnimationTimer++;
                return false;
            }

            // Have segments by default inherit the opacity of the head.
            segmentFadeType = (int)BodySegmentFadeType.InhertHeadOpacity;

            // Handle the surprise portal attack.
            if (SurprisePortalAttackState != PerpendicularPortalAttackState.NotPerformingAttack)
            {
                PerformPerpendicularPortalAttack(npc, target, ref portalIndex, ref segmentFadeType, ref perpendicularPortalAttackTimer, ref perpendicularPortalAngle, ref damageImmunityCountdown);
                phaseCycleTimer--;
                perpendicularPortalAttackTimer++;
                return false;
            }

            // Reset the perpendicular portal timer.
            perpendicularPortalAttackTimer = 0f;

            // Handle special attacks.
            if (canPerformSpecialAttacks)
            {
                // Handle special attack transition.
                if (performingSpecialAttack == 0f)
                {
                    // Disappear immediately if the target is gone.
                    if (!target.active || target.dead)
                        npc.active = false;

                    // The charge gate attack happens much more frequently when DoG is close to death.
                    specialAttackTimer += finalPhase && specialAttackTimer < SpecialAttackDelay - SpecialAttackTransitionPreparationTime - 5f ? 2f : 1f;

                    // Enter a portal before performing a special attack.
                    if (Main.netMode != NetmodeID.MultiplayerClient && specialAttackTimer == SpecialAttackDelay - SpecialAttackTransitionPreparationTime)
                    {
                        portalIndex = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + npc.velocity * 75f, Vector2.Zero, ModContent.ProjectileType<DoGChargeGate>(), 0, 0f);
                        Main.projectile[(int)portalIndex].localAI[0] = 1f;
                        npc.netUpdate = true;
                    }

                    // Ensure that DoG only performs a special attack when in his passive phase, to prevent strange cutoffs.
                    // This doesn't apply if DoG is in his final phase.
                    bool shouldWaitBeforeDoingSpecialAttack = !doPassiveMovement && !finalPhase;
                    if (shouldWaitBeforeDoingSpecialAttack && specialAttackTimer == SpecialAttackDelay - SpecialAttackTransitionPreparationTime - 1f)
                        specialAttackTimer--;

                    if (specialAttackTimer >= SpecialAttackDelay)
                    {
                        specialAttackTimer = 0f;
                        performingSpecialAttack = 1f;
                        portalIndex = -1f;

                        // Select a special attack type.
                        do
                            npc.Infernum().ExtraAI[SpecialAttackTypeIndex] = (int)Utils.SelectRandom(Main.rand, SpecialAttackType.LaserWalls, SpecialAttackType.CircularLaserBurst, SpecialAttackType.ChargeGates);
                        while (npc.Infernum().ExtraAI[SpecialAttackTypeIndex] == npc.Infernum().ExtraAI[PreviousSpecialAttackTypeIndex]);
                        npc.netUpdate = true;
                    }

                    // Do nothing and drift into the portal.
                    if (specialAttackTimer >= SpecialAttackDelay - SpecialAttackTransitionPreparationTime)
                    {
                        // Laugh if this is the first time DoG has performed a special attack in the fight.
                        if (hasPerformedSpecialAttackBefore == 0f && specialAttackTimer == SpecialAttackDelay - SpecialAttackTransitionPreparationTime)
                        {
                            SoundEngine.PlaySound(InfernumSoundRegistry.DoGLaughSound with { Volume = 3f }, target.Center);
                            hasPerformedSpecialAttackBefore = 1f;
                        }

                        // Disable damage.
                        npc.damage = 0;
                        npc.dontTakeDamage = true;

                        // Go very, very quickly into the portal.
                        npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitX) * MathHelper.Lerp(npc.velocity.Length(), 105f, 0.15f);

                        // Disappear when touching the portal.
                        // This same logic applies to body/tail segments.
                        if (Main.projectile.IndexInRange((int)portalIndex) && npc.Hitbox.Intersects(Main.projectile[(int)portalIndex].Hitbox))
                            npc.Opacity = 0f;

                        segmentFadeType = (int)BodySegmentFadeType.EnteringPortal;

                        // Clear away various misc projectiles.
                        int[] projectilesToDelete = new int[]
                        {
                            ProjectileID.CultistBossLightningOrbArc,
                            ModContent.ProjectileType<AcceleratingDoGBurst>()
                        };
                        for (int i = 0; i < Main.maxProjectiles; i++)
                        {
                            if (projectilesToDelete.Contains(Main.projectile[i].type) && Main.projectile[i].active)
                                Main.projectile[i].active = false;
                        }
                        return false;
                    }
                }

                if (performingSpecialAttack == 1f)
                {
                    // Disappear immediately if the target is gone.
                    if (!target.active || target.dead)
                        npc.active = false;
                    
                    bool doingSpecialAttacks = DoSpecialAttacks(npc, target, finalPhase, ref performingSpecialAttack, ref specialAttackTimer, ref portalIndex, ref segmentFadeType, ref damageImmunityCountdown);
                    if (doingSpecialAttacks)
                        phaseCycleTimer = 0f;

                    if (!doingSpecialAttacks)
                        return false;
                }
            }
            else
            {
                // Fade in.
                npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.023f, 0f, 1f);

                // Reset the special attack state, just in case.
                if (performingSpecialAttack != 0f)
                    performingSpecialAttack = 0f;
            }
            portalIndex = -1f;

            // Reset the attack type selection once the special attacks are cleared.
            if (performingSpecialAttack == 0f)
            {
                npc.Infernum().ExtraAI[SpecialAttackTypeIndex] = 0f;
                npc.netUpdate = true;
            }

            // Calculate the amount of sentinel attacks that can be used in the passive phase.
            // This does not apply in the aggressive phase.
            int totalSentinelAttacks = 1;
            if (lifeRatio < CanUseSignusSentinelAttackLifeRatio)
                totalSentinelAttacks++;

            // Despawn if no tail is present for some reason.
            if (!NPC.AnyNPCs(ModContent.NPCType<DevourerofGodsTail>()))
                npc.active = false;

            // Chomping after attempting to eat the player.
            bool chomping = performingSpecialAttack == 0f && DoChomp(npc, ref chompEffectsCountdown, ref jawRotation);

            // Despawn if no valid target exists.
            if (target.dead || !target.active)
                Despawn(npc);

            // Don't do any movement yet if the move animation delay isn't ready yet.
            else if (postAnimationMoveDelay < PostP2AnimationMoveDelay)
                postAnimationMoveDelay++;

            // Do passive movement along with sentinel attacks.
            else if (doPassiveMovement)
            {
                if (wrappedPhaseCycleTimer == AggressiveMovementTimeP2 + 1f)
                {
                    DoGSkyInfernum.CreateLightningBolt(Color.White, 16, true);
                    aggressiveChargeCycleCounter++;
                    npc.netUpdate = true;
                }

                bool aboutToFinishAttacking = wrappedPhaseCycleTimer > AggressiveMovementTimeP2 + PassiveMovementTimeP2 - 96f;
                if (passiveAttackDelay >= 300f && !aboutToFinishAttacking)
                {
                    // Increment the sentinal attack timer if DoG is completely visible.
                    if (totalSentinelAttacks >= 1 && npc.Opacity >= 1f)
                        sentinelAttackTimer++;
                    if (sentinelAttackTimer >= totalSentinelAttacks * 450f)
                        sentinelAttackTimer = 0f;

                    DoSentinelAttacks(npc, target, phaseCycleTimer, ref sentinelAttackTimer);
                }
                DoPassiveFlyMovement(npc, ref jawRotation, ref chompEffectsCountdown, aboutToFinishAttacking);
            }

            // Do aggressive fly movement, snapping at the target ruthlessly.
            else
            {
                bool dontChompYet = wrappedPhaseCycleTimer < 90f;
                if (wrappedPhaseCycleTimer == 2f)
                    DoGSkyInfernum.CreateLightningBolt(new Color(1f, 0f, 0f, 0.2f), 16, true);

                // Every second chomp cycle, DoG performs his perpendicular portal surprise attack.
                if (aggressiveChargeCycleCounter % 2f == 1f && !finalPhase && wrappedPhaseCycleTimer == AggressiveMovementTimeP2 / 2)
                {
                    SurprisePortalAttackState = PerpendicularPortalAttackState.EnteringPortal;
                    npc.netUpdate = true;
                }

                DoAggressiveFlyMovement(npc, target, dontChompYet, chomping, ref jawRotation, ref chompEffectsCountdown, ref universalFightTimer, ref flyAcceleration);
            }

            // Define the rotation and sprite direction. This only applies for non-special attacks.
            npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            return false;
        }

        public static void Despawn(NPC npc)
        {
            // Despawn all segments once SCal has reached space.
            int headID = ModContent.NPCType<DevourerofGodsHead>();
            int bodyID = ModContent.NPCType<DevourerofGodsBody>();
            int tailID = ModContent.NPCType<DevourerofGodsTail>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].type != headID && Main.npc[i].type != bodyID && Main.npc[i].type != tailID)
                    break;

                Main.npc[i].active = false;
                Main.npc[i].netUpdate = true;
            }
            npc.active = false;
            npc.netUpdate = true;
        }

        public static void DoSentinelAttacks(NPC npc, Player target, float attackTimer, ref float sentinelAttackTimer)
        {
            // Storm Weaver Effect (Lightning Storm).
            int attackTime = 450;
            bool nearEndOfAttack = sentinelAttackTimer % attackTime >= attackTime - 105f;
            if (sentinelAttackTimer > 0f && sentinelAttackTimer <= attackTime * 2f && npc.Opacity >= 0.5f)
            {
                if (attackTimer % 120f == 0f && !nearEndOfAttack)
                {
                    SoundEngine.PlaySound(SoundID.Item12, target.position);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 24; i++)
                        {
                            Vector2 spawnOffset = (MathHelper.TwoPi * i / 24f).ToRotationVector2() * 1580f + Main.rand.NextVector2Circular(105f, 105f);
                            Vector2 laserShootVelocity = spawnOffset.SafeNormalize(Vector2.UnitY) * -Main.rand.NextFloat(20f, 24f) + Main.rand.NextVector2Circular(2f, 2f);
                            int laser = Utilities.NewProjectileBetter(target.Center + spawnOffset, laserShootVelocity, ModContent.ProjectileType<DoGDeath>(), 455, 0f);
                            if (Main.projectile.IndexInRange(laser))
                                Main.projectile[laser].MaxUpdates = 3;
                        }
                    }
                }

                sentinelAttackTimer = attackTime;
            }
        }

        public static void DoDeathEffects(NPC npc, float deathAnimationTimer, ref float destroyedSegmentsCount)
        {
            // Ensure DoG fades in and do/take damage.
            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.1f, 0f, 1f);
            npc.dontTakeDamage = true;
            npc.damage = 0;

            // Revoke DoG's health bar privileges since he got killed.
            npc.Calamity().CanHaveBossHealthBar = false;

            // Play a sound as the death animation starts.
            if (deathAnimationTimer == 1f)
                SoundEngine.PlaySound(DevourerofGodsHead.SpawnSound, Main.player[npc.target].Center);

            void DestroySegment(int index, ref float destroyedSegments)
            {
                // Randomly play an electric pop sound to accompany the destruction of a segment.
                if (Main.rand.NextBool(5))
                    SoundEngine.PlaySound(SoundID.Item94, npc.Center);

                List<int> segments = new()
                {
                    ModContent.NPCType<DevourerofGodsBody>(),
                    ModContent.NPCType<DevourerofGodsTail>()
                };

                // Start at some number, n, at 0. This number is used as a counter for the nth segment to destroy, from the tail to the head.
                // This works by looping through all NPCs and identifying if it's a DoG segment with that segment ID
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (segments.Contains(Main.npc[i].type) && Main.npc[i].active && Main.npc[i].Infernum().ExtraAI[SegmentNumberIndex] == index)
                    {
                        // Create some dust at the segment's position to indicate a small cosmic explosion puff.
                        for (int j = 0; j < 20; j++)
                        {
                            Dust cosmicBurst = Dust.NewDustPerfect(Main.npc[i].Center + Main.rand.NextVector2Circular(25f, 25f), 234);
                            cosmicBurst.scale = 1.7f;
                            cosmicBurst.velocity = Main.rand.NextVector2Circular(9f, 9f);
                            cosmicBurst.noGravity = true;
                        }

                        Main.npc[i].life = 0;
                        Main.npc[i].HitEffect();
                        Main.npc[i].active = false;
                        Main.npc[i].netUpdate = true;
                        destroyedSegments++;
                        break;
                    }
                }
            }

            int textDelay = 120;
            int segmentDestructionTime = 260;
            int lastSegmentsDestructionDelay = 30;
            int lastSegmentsDestructionTime = 60;
            float idealSpeed = MathHelper.Lerp(9f, 4.75f, Utils.GetLerpValue(15f, 210f, deathAnimationTimer, true));
            if (npc.velocity.Length() != idealSpeed)
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(npc.velocity.Length(), idealSpeed, 0.08f);

            // Say edgy things.
            if (deathAnimationTimer == textDelay)
                Utilities.DisplayText("I WILL NOT BE DESTROYED!!!", Color.Cyan);
            if (deathAnimationTimer == textDelay + 50f)
                Utilities.DisplayText("I WILL NOT BE DESTROYED!!!", Color.Cyan);
            if (deathAnimationTimer == textDelay + 100f)
                Utilities.DisplayText("I WILL NOT BE DESTROYED!!!!", Color.Cyan);

            if (deathAnimationTimer == deathAnimationTimer + segmentDestructionTime - 50f)
                Utilities.DisplayText("I WILL NOT...", Color.Cyan);

            if (deathAnimationTimer == deathAnimationTimer + segmentDestructionTime + 40f)
                Utilities.DisplayText("I...", Color.Cyan);

            // Destroy most of DoG's first segments.
            if (deathAnimationTimer >= textDelay && deathAnimationTimer < deathAnimationTimer + segmentDestructionTime && deathAnimationTimer % 4f == 0f)
            {
                int segmentToDestroy = (int)(Utils.GetLerpValue(0f, segmentDestructionTime, deathAnimationTimer - textDelay, true) * 60f);
                DestroySegment(segmentToDestroy, ref destroyedSegmentsCount);
            }

            if (deathAnimationTimer == deathAnimationTimer + segmentDestructionTime + 42f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<DoGSpawnBoom>(), 0, 0f);

                if (Main.netMode != NetmodeID.Server)
                {
                    SoundEngine.PlaySound(DevourerofGodsHead.SpawnSound with { Volume = 1.6f }, npc.Center);

                    for (int i = 0; i < 3; i++)
                    {
                        float pitch = -MathHelper.Lerp(0.1f, 0.4f, i / 3f);
                        SoundEngine.PlaySound(TeslaCannon.FireSound with { Pitch = pitch, Volume = 1.8f }, npc.Center);
                    }
                }
            }

            // Destroy the last segments DoG has.
            float finalSegmentDestructionTimer = deathAnimationTimer - textDelay - segmentDestructionTime - lastSegmentsDestructionDelay;
            bool destroyingLastSegments = finalSegmentDestructionTimer >= 0f && finalSegmentDestructionTimer <= lastSegmentsDestructionTime;
            if (destroyingLastSegments && deathAnimationTimer % 2f == 0f)
            {
                int segmentToDestroy = (int)(Utils.GetLerpValue(0f, lastSegmentsDestructionTime, finalSegmentDestructionTimer, true) * 10f) + 60;
                DestroySegment(segmentToDestroy, ref destroyedSegmentsCount);
            }

            // Cover the screen in a white light before DoG's head explodes.
            float light = Utils.GetLerpValue(20f, 55f, finalSegmentDestructionTimer, true);
            MoonlordDeathDrama.RequestLight(light, Main.LocalPlayer.Center);

            if (finalSegmentDestructionTimer >= 75f)
            {
                npc.life = 0;
                npc.HitEffect();
                npc.NPCLoot();
                npc.active = false;
                npc.netUpdate = true;
            }
        }

        public static bool DoChomp(NPC npc, ref float chompEffectsCountdown, ref float jawRotation)
        {
            bool chomping = chompEffectsCountdown > 0f;
            int dustCount = 40;
            float idealChompAngle = MathHelper.ToRadians(-18f);
            float dustScale = 2.6f;
            if (!InPhase2)
            {
                dustCount = 25;
                idealChompAngle *= 0.5f;
                dustScale = 1.8f;
            }

            if (chomping)
            {
                chompEffectsCountdown--;

                // Make the jaw move to its desired rotation.
                // Once sufficiently close, a puff of electric dust is released as a way of indicating impact.
                if (jawRotation != idealChompAngle)
                {
                    jawRotation = jawRotation.AngleTowards(idealChompAngle, 0.12f);
                    if (Math.Abs(jawRotation - idealChompAngle) < 0.001f)
                    {
                        for (int i = 0; i < dustCount; i++)
                        {
                            Dust electricity = Dust.NewDustPerfect(npc.Center - Vector2.UnitY.RotatedBy(npc.rotation) * 52f, 229);
                            electricity.velocity = ((MathHelper.TwoPi * i / dustCount).ToRotationVector2() * new Vector2(7f, 4f)).RotatedBy(npc.rotation) + npc.velocity * 1.5f;
                            electricity.noGravity = true;
                            electricity.scale = dustScale;
                        }
                        jawRotation = idealChompAngle;
                    }
                }
            }
            return chomping;
        }

        public static void DoPassiveFlyMovement(NPC npc, ref float jawRotation, ref float chompEffectsCountdown, bool flyHigherUp)
        {
            chompEffectsCountdown = 0f;
            jawRotation = jawRotation.AngleTowards(0f, 0.08f);

            // Move towards the target.
            Vector2 destination = Main.player[npc.target].Center - Vector2.UnitY * 660f;
            if (flyHigherUp)
                destination.Y -= 950f;

            if (!npc.WithinRange(destination, 125f))
            {
                float flySpeed = MathHelper.Lerp(27f, 38f, 1f - npc.life / (float)npc.lifeMax);
                if (flyHigherUp)
                    flySpeed *= 1.5f;

                Vector2 idealVelocity = npc.SafeDirectionTo(destination) * flySpeed;
                npc.velocity = npc.velocity.MoveTowards(idealVelocity, 2f).RotateTowards(idealVelocity.ToRotation(), 0.032f);
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(npc.velocity.Length(), idealVelocity.Length(), 0.1f);
                if (npc.velocity.Y > -1f && MathHelper.Distance(destination.X, npc.Center.X) < 1050f)
                    npc.velocity.Y -= 1.75f;
            }
        }

        public static void DoAggressiveFlyMovement(NPC npc, Player target, bool dontChompYet, bool chomping, ref float jawRotation, ref float chompEffectsCountdown, ref float universalFightTimer, ref float flyAcceleration)
        {
            npc.Center = npc.Center.MoveTowards(target.Center, InPhase2 ? 1.8f : 2.4f);
            bool targetHasDash = target.dash > 0 || target.Calamity().HasCustomDash;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float idealFlyAcceleration = MathHelper.Lerp(0.046f, 0.036f, lifeRatio);
            float idealFlySpeed = MathHelper.Lerp(21f, 15.3f, lifeRatio);
            float idealMouthOpeningAngle = MathHelper.ToRadians(32f);
            float flySpeedFactor = 1.45f + (1f - lifeRatio) * 0.4f;
            float snakeMovementDistanceThreshold = 650f;
            float chompDistance = 330f;
            float chompSpeedFactor = 1.75f;
            ref float timeSinceLastChomp = ref npc.Infernum().ExtraAI[TimeSinceLastSnapIndex];

            if (InPhase2)
            {
                idealFlyAcceleration += 0.005f;
                idealMouthOpeningAngle = MathHelper.ToRadians(34f);
                flySpeedFactor += lifeRatio * 0.05f;
                snakeMovementDistanceThreshold -= 125f;
                if (BossRushEvent.BossRushActive)
                    idealFlySpeed *= 1.4f;
            }
            else
                idealFlySpeed *= 0.9f;

            // Increment the time since last snap value.
            timeSinceLastChomp++;

            if (!targetHasDash)
                flyAcceleration *= 0.885f;

            Vector2 destination = target.Center;

            // Swerve around in a snake-like movement if sufficiently far away from the target.
            float distanceFromBaseDestination = npc.Distance(destination);
            float distanceFromTarget = npc.Distance(target.Center);
            if (npc.Distance(destination) > snakeMovementDistanceThreshold)
            {
                destination += (universalFightTimer % 60f / 60f * MathHelper.TwoPi).ToRotationVector2() * 145f;
                distanceFromBaseDestination = npc.Distance(destination);
                idealFlyAcceleration *= 1.8f;
                flySpeedFactor = 1.55f;
            }

            if (!target.HasShieldBash())
            {
                flySpeedFactor *= 0.62f;
                chompSpeedFactor *= 0.75f;
                chompDistance += 160f;
            }
            else
                HatGirl.SayThingWhileOwnerIsAlive(target, "Don't feel intimidated, face fear in the eyes and dash directly into the Devourer's maw!");

            float swimOffsetAngle = (float)Math.Sin(MathHelper.TwoPi * universalFightTimer / 160f) * Utils.GetLerpValue(400f, 540f, distanceFromBaseDestination, true) * 0.41f;

            // Charge if the player is far away.
            // Don't do this at the start of the fight though. Doing so might lead to an unfair
            // charge.
            if (distanceFromBaseDestination > 1500f && universalFightTimer > 120f)
            {
                idealFlyAcceleration = MathHelper.Min(6f, flyAcceleration + 1f);
                idealFlySpeed *= 2f;
            }

            flyAcceleration = MathHelper.Lerp(flyAcceleration, idealFlyAcceleration, 0.3f);

            // Degrees are used here for ease of readability in the calculations below.
            // This used to rely on raw dot normalized dot products, but this has since been changed for the sake of clarity.
            float targetDirectionAngleDiscrepancy = MathHelper.ToDegrees(npc.velocity.AngleBetween(npc.SafeDirectionTo(destination)));

            // Adjust the speed based on how the direction towards the target compares to the direction of the
            // current velocity. This check is unnecessary once close to the target, which would prompt a snap/charge.
            if (npc.Distance(destination) > 200f)
            {
                float speed = npc.velocity.Length();

                // Try to stay within a general speed.
                if (speed < 15f)
                    speed += 0.08f;
                if (speed > 23.65f)
                    speed -= 0.08f;

                // Speed up if close to aiming at the target, but not too close (within a margin of 32-60 degrees).
                if (targetDirectionAngleDiscrepancy is > 32f and < 60f)
                    speed += 0.24f;

                // Slow down if farther to aiming at the target, for the sake of allowing DoG to get back on track again (within a margin of 60-135 degrees).
                if (targetDirectionAngleDiscrepancy is > 60f and < 135f)
                    speed -= 0.1f;

                // Clamp the speed.
                speed = MathHelper.Clamp(speed, flySpeedFactor * 14.333f, flySpeedFactor * 32f);

                // And handle movement.
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(destination) + swimOffsetAngle, flyAcceleration, true) * speed;
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(destination) * speed, flyAcceleration * 25f);
            }

            // Jaw opening when near player.
            if (!chomping)
            {
                if ((distanceFromTarget < chompDistance && targetDirectionAngleDiscrepancy < 38f) || (distanceFromTarget < chompDistance + 200f && targetDirectionAngleDiscrepancy < 30f))
                {
                    jawRotation = jawRotation.AngleTowards(idealMouthOpeningAngle, 0.028f);

                    // Chomp at the player if they're close enough.
                    if (distanceFromBaseDestination < 112f && chompEffectsCountdown == 0f)
                    {
                        SoundEngine.PlaySound(CommonCalamitySounds.OtherwordlyHitSound, npc.Center);
                        chompEffectsCountdown = 18f;
                    }
                }
                else
                    jawRotation = jawRotation.AngleTowards(0f, 0.08f);
            }

            // Lunge if near the player and aiming sufficiently close to them in preparation of a chomp.
            bool shouldChomp = (distanceFromBaseDestination < 360f && targetDirectionAngleDiscrepancy < 64f && npc.velocity.Length() < idealFlySpeed * 2f) || timeSinceLastChomp >= 360f;
            if (shouldChomp && !dontChompYet)
            {
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * npc.velocity.Length() * chompSpeedFactor;
                npc.Infernum().ExtraAI[PreviousSnapAngleIndex] = npc.velocity.ToRotation();
                jawRotation = jawRotation.AngleLerp(idealMouthOpeningAngle, 0.55f);

                if (chompEffectsCountdown == 0f)
                {
                    SoundEngine.PlaySound(CommonCalamitySounds.OtherwordlyHitSound, npc.Center);
                    timeSinceLastChomp = 0f;
                    chompEffectsCountdown = 26f;
                }
            }
        }

        public static void DoSpecialAttack_LaserWalls(Player target, ref float attackTimer, ref float segmentFadeType)
        {
            // Body segments should be invisible along with the head.
            segmentFadeType = (int)BodySegmentFadeType.InhertHeadOpacity;

            float offsetPerLaser = 88f;
            float laserWallSpeed = 16f;
            if (attackTimer % 80f == 79f && attackTimer < SpecialAttackDuration - 90f)
            {
                SoundEngine.PlaySound(CommonCalamitySounds.LaserCannonSound, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int shootType = ModContent.ProjectileType<DoGDeath>();
                    float targetY = target.position.Y + (Main.rand.NextBool(2) ? 50f : 0f);

                    // Sometimes use diagonal laser walls.
                    float laserOffsetAngle = Main.rand.NextBool(3) ? MathHelper.PiOver4 : 0f;

                    // Side walls.
                    for (int x = -10; x < 10; x++)
                    {
                        Vector2 laserSpawnPositionLeft = new Vector2(target.Center.X, targetY) + new Vector2(-1000f, x * offsetPerLaser).RotatedBy(laserOffsetAngle);
                        Vector2 laserSpawnPositionRight = new Vector2(target.Center.X, targetY) + new Vector2(1000f, x * offsetPerLaser).RotatedBy(laserOffsetAngle);
                        Vector2 laserVelocityLeft = Vector2.UnitX.RotatedBy(laserOffsetAngle) * laserWallSpeed;
                        Vector2 laserVelocityRight = -Vector2.UnitX.RotatedBy(laserOffsetAngle) * laserWallSpeed;

                        int laser = Utilities.NewProjectileBetter(laserSpawnPositionRight, laserVelocityRight, shootType, 400, 0f);
                        if (Main.projectile.IndexInRange(laser))
                            Main.projectile[laser].MaxUpdates = 2;
                        laser = Utilities.NewProjectileBetter(laserSpawnPositionLeft, laserVelocityLeft, shootType, 400, 0f);
                        if (Main.projectile.IndexInRange(laser))
                            Main.projectile[laser].MaxUpdates = 2;
                    }

                    // Lower wall.
                    for (int x = -12; x <= 12; x++)
                    {
                        Vector2 laserSpawnPosition = new Vector2(target.Center.X, targetY) + new Vector2(x * offsetPerLaser, 1000f).RotatedBy(laserOffsetAngle);
                        Vector2 laserVelocity = -Vector2.UnitY.RotatedBy(laserOffsetAngle) * laserWallSpeed;
                        int laser = Utilities.NewProjectileBetter(laserSpawnPosition, laserVelocity, shootType, 400, 0f);
                        if (Main.projectile.IndexInRange(laser))
                            Main.projectile[laser].MaxUpdates = 2;
                    }

                    // Upper wall.
                    for (int x = -20; x < 20; x++)
                    {
                        Vector2 laserSpawnPosition = new Vector2(target.Center.X, targetY) + new Vector2(x * offsetPerLaser, -1000f).RotatedBy(laserOffsetAngle);
                        Vector2 laserVelocity = Vector2.UnitY.RotatedBy(laserOffsetAngle) * laserWallSpeed;
                        int laser = Utilities.NewProjectileBetter(laserSpawnPosition, laserVelocity, shootType, 400, 0f);
                        if (Main.projectile.IndexInRange(laser))
                            Main.projectile[laser].MaxUpdates = 2;
                    }
                }
            }
        }

        public static void DoSpecialAttack_CircularLaserBurst(NPC npc, Player target, ref float attackTimer, ref float segmentFadeType)
        {
            // Body segments should be invisible along with the head.
            segmentFadeType = (int)BodySegmentFadeType.InhertHeadOpacity;

            float radius = MathHelper.Lerp(700f, 550f, 1f - npc.life / (float)npc.lifeMax);
            if (attackTimer % 70f == 69f && attackTimer < SpecialAttackDuration - 120f)
            {
                float spawnOffsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                for (int i = 0; i < 6; i++)
                {
                    Vector2 spawnOffset = (spawnOffsetAngle + MathHelper.TwoPi * i / 6f).ToRotationVector2() * radius;
                    Vector2 spawnPosition = target.Center + spawnOffset;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), spawnPosition, Vector2.Zero, ModContent.ProjectileType<RealityBreakPortalLaserWall>(), 0, 0f);
                }
            }
        }

        public static void DoSpecialAttack_ChargeGates(NPC npc, Player target, bool finalPhase, ref float attackTimer, ref float portalIndex, ref float segmentFadeType)
        {
            // Transform into the antimatter form.
            FadeToAntimatterForm = MathHelper.Clamp(FadeToAntimatterForm + 0.05f, 0f, 1f);

            int fireballCount = 17;
            int idealPortalTelegraphTime = 48;
            float wrappedAttackTimer = attackTimer % 135f;
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Charges become increasingly powerful as DoG gets closer to death, once past the final phase.
            // This is done as a means of indicating desparation, and acts as a sort of final stand.
            if (finalPhase)
            {
                idealPortalTelegraphTime -= 10;
                fireballCount += 4;
            }

            if (lifeRatio < 0.15f)
            {
                fireballCount -= 3;
                idealPortalTelegraphTime -= 7;
            }
            if (lifeRatio < 0.1f)
            {
                fireballCount -= 2;
                idealPortalTelegraphTime -= 10;
            }
            if (lifeRatio < 0.05f)
            {
                fireballCount -= 2;
                idealPortalTelegraphTime -= 5;
            }

            float chargeSpeed = finalPhase ? 85f : 60f;
            ref float chargeGatePortalIndex = ref npc.Infernum().ExtraAI[ChargeGatePortalIndexIndex];
            ref float portalTelegraphTime = ref npc.Infernum().ExtraAI[ChargeGatePortalTelegraphTimeIndex];

            // Define the portal telegraph time if it is uninitialized.
            if (portalTelegraphTime == 0f)
            {
                portalTelegraphTime = idealPortalTelegraphTime;
                npc.netUpdate = true;
            }

            // Disappear if the target is dead and DoG is invisible.
            if (target.dead && npc.Opacity <= 0f)
            {
                attackTimer = 5f;
                npc.Center = Vector2.One * -10000f;

                // Bring segments along with.
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && (Main.npc[i].type == ModContent.NPCType<DevourerofGodsBody>() || Main.npc[i].type == ModContent.NPCType<DevourerofGodsTail>()))
                    {
                        Main.npc[i].Center = npc.Center;
                        Main.npc[i].Opacity = 0f;
                        Main.npc[i].netUpdate = true;
                    }
                }
                npc.active = false;
            }

            // Fade in.
            segmentFadeType = (int)BodySegmentFadeType.ApproachAheadSegmentOpacity;

            // Create a portal to teleport from.
            if (Main.netMode != NetmodeID.MultiplayerClient && wrappedAttackTimer == 20f)
            {
                portalIndex = -1f;
                Vector2 portalSpawnPosition = target.Center + Main.rand.NextVector2CircularEdge(600f, 600f);
                chargeGatePortalIndex = Projectile.NewProjectile(npc.GetSource_FromAI(), portalSpawnPosition, Vector2.Zero, ModContent.ProjectileType<DoGChargeGate>(), 0, 0f, 0, 0f, portalTelegraphTime);
                npc.netUpdate = true;
            }

            // Teleport and charge.
            if (wrappedAttackTimer == portalTelegraphTime + 20f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.Center = Main.projectile[(int)chargeGatePortalIndex].Center;

                    int segmentCount = 0;
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (Main.npc[i].active && (Main.npc[i].type == ModContent.NPCType<DevourerofGodsBody>() || Main.npc[i].type == ModContent.NPCType<DevourerofGodsTail>()))
                        {
                            Main.npc[i].Center = npc.Center;
                            Main.npc[i].Opacity = Utils.GetLerpValue(15f, 0f, segmentCount, true);
                            Main.npc[i].netUpdate = true;
                            segmentCount++;
                        }
                    }
                    npc.velocity = npc.SafeDirectionTo(Main.projectile[(int)chargeGatePortalIndex].ModProjectile<DoGChargeGate>().Destination) * (chargeSpeed + npc.Distance(target.Center) * 0.0127f);
                    npc.Opacity = 1f;
                    npc.netUpdate = true;

                    // Create a burst of homing flames.
                    float flameBurstOffsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                    for (int i = 0; i < fireballCount; i++)
                    {
                        Vector2 flameShootVelocity = (MathHelper.TwoPi * i / fireballCount + flameBurstOffsetAngle).ToRotationVector2() * 13f;
                        Utilities.NewProjectileBetter(npc.Center + flameShootVelocity * 3f, flameShootVelocity, ModContent.ProjectileType<AcceleratingDoGBurst>(), 380, 0f);

                        flameShootVelocity = flameShootVelocity.RotatedBy(MathHelper.Pi / fireballCount) * 0.5f;
                        Utilities.NewProjectileBetter(npc.Center + flameShootVelocity * 3f, flameShootVelocity, ModContent.ProjectileType<AcceleratingDoGBurst>(), 380, 0f);
                    }

                    // Create the portal to go through.
                    if (!target.dead)
                    {
                        Vector2 portalSpawnPosition = npc.Center + npc.velocity.SafeNormalize(Vector2.UnitY) * 1900f;
                        portalIndex = Projectile.NewProjectile(npc.GetSource_FromAI(), portalSpawnPosition, Vector2.Zero, ModContent.ProjectileType<DoGChargeGate>(), 0, 0f, 0, 0f, portalTelegraphTime);
                        Main.projectile[(int)portalIndex].localAI[0] = 1f;
                    }
                }
                SoundEngine.PlaySound(DevourerofGodsHead.AttackSound, target.Center);
            }
            if (wrappedAttackTimer > portalTelegraphTime)
            {
                // Disappear when touching the portal.
                // This same logic applies to body/tail segments.
                if (Main.projectile.IndexInRange((int)portalIndex) && npc.Hitbox.Intersects(Main.projectile[(int)portalIndex].Hitbox))
                    npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.2f, 0f, 1f);

                if (wrappedAttackTimer > portalTelegraphTime + 20f)
                    segmentFadeType = (int)BodySegmentFadeType.EnteringPortal;
            }

            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public static void PerformPerpendicularPortalAttack(NPC npc, Player target, ref float portalIndex, ref float segmentFadeType, ref float perpendicularPortalAttackTimer, ref float perpendicularPortalAngle, ref float damageImmunityCountdown)
        {
            int portalTelegraphTime = 55;
            int waitBeforeSnappingAgain = 16;
            float chargeSpeed = 67f;
            
            switch (SurprisePortalAttackState)
            {
                // Do nothing and drift into the portal.
                case PerpendicularPortalAttackState.EnteringPortal:
                    // Disable contact damage.
                    npc.damage = 0;
                    npc.dontTakeDamage = true;

                    // Create the portal and define the charge angle if it does not exist yet.
                    if (portalIndex == -1f)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float scaleFactor = 1.6f;
                            perpendicularPortalAngle = MathHelper.PiOver2 * (float)Math.Round(npc.Infernum().ExtraAI[PreviousSnapAngleIndex] / MathHelper.PiOver2);

                            portalIndex = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + npc.velocity * 64f, Vector2.Zero, ModContent.ProjectileType<DoGChargeGate>(), 0, 0f);
                            Main.projectile[(int)portalIndex].localAI[0] = 1f;
                            Main.projectile[(int)portalIndex].scale *= scaleFactor;
                            Main.projectile[(int)portalIndex].Size *= scaleFactor;
                            npc.netUpdate = true;
                        }
                        return;
                    }

                    // Accelerate extremely quickly.
                    float flySpeed = MathHelper.Clamp(npc.velocity.Length() * 1.045f, 40f, 180f);
                    if (npc.Opacity <= 0f)
                        npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * flySpeed;
                    else
                        npc.velocity = npc.SafeDirectionTo(Main.projectile[(int)portalIndex].Center) * flySpeed;

                    // Dissapear once entering the portal.
                    if (Main.projectile.IndexInRange((int)portalIndex) && npc.Hitbox.Intersects(Main.projectile[(int)portalIndex].Hitbox))
                        npc.Opacity = 0f;

                    perpendicularPortalAttackTimer = 0f;
                    segmentFadeType = (int)BodySegmentFadeType.EnteringPortal;
                    break;

                case PerpendicularPortalAttackState.Waiting:
                    // Disable damage.
                    npc.damage = 0;
                    npc.dontTakeDamage = true;

                    if (Main.netMode != NetmodeID.MultiplayerClient && perpendicularPortalAttackTimer == 1f)
                    {
                        Vector2 portalSpawnPosition = target.Center + (perpendicularPortalAngle + MathHelper.PiOver2).ToRotationVector2() * 700f;
                        
                        portalIndex = Projectile.NewProjectile(npc.GetSource_FromAI(), portalSpawnPosition, Vector2.Zero, ModContent.ProjectileType<DoGChargeGate>(), 0, 0f, 0, 0f, portalTelegraphTime);
                        Main.projectile[(int)portalIndex].ModProjectile<DoGChargeGate>().Destination = target.Center;
                        Main.projectile[(int)portalIndex].ModProjectile<DoGChargeGate>().TelegraphShouldAim = false;
                        Main.projectile[(int)portalIndex].netUpdate = true;

                        npc.netUpdate = true;
                    }

                    // Play some sounds to accompany the reality slice effect.
                    if (perpendicularPortalAttackTimer is <= 10f and >= 1f)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 portalPosition = Main.projectile[(int)portalIndex].Center;
                            int cosmicTear = Utilities.NewProjectileBetter(portalPosition, Vector2.Zero, ModContent.ProjectileType<RealitySlice>(), 0, 0f);
                            Vector2 offset = -Vector2.UnitY.RotatedBy(MathHelper.TwoPi * (perpendicularPortalAttackTimer - 1f) / 9f) * 950f;
                            if (Main.projectile.IndexInRange(cosmicTear))
                            {
                                Main.projectile[cosmicTear].ModProjectile<RealitySlice>().Start = portalPosition - offset;
                                Main.projectile[cosmicTear].ModProjectile<RealitySlice>().End = portalPosition + offset;
                                Main.projectile[cosmicTear].ModProjectile<RealitySlice>().Cosmilite = true;
                                Main.projectile[cosmicTear].timeLeft = Main.projectile[cosmicTear].ModProjectile<RealitySlice>().Lifetime;
                            }
                        }

                        if (perpendicularPortalAttackTimer == 10f)
                        {
                            SoundEngine.PlaySound(YanmeisKnife.HitSound with { Volume = 1.7f }, target.Center);
                            SoundEngine.PlaySound(TeslaCannon.FireSound with { Volume = 1.7f }, target.Center);
                            target.Calamity().GeneralScreenShakePower = 10f;
                        }
                    }

                    // Snap at the target.
                    if (perpendicularPortalAttackTimer >= portalTelegraphTime)
                    {
                        npc.Center = target.Center - Vector2.UnitY * 1200f;
                        Vector2 aimDirection = npc.SafeDirectionTo(target.Center);
                        if (Main.projectile.IndexInRange((int)portalIndex))
                        {
                            npc.Center = Main.projectile[(int)portalIndex].Center;
                            aimDirection = npc.SafeDirectionTo(Main.projectile[(int)portalIndex].ModProjectile<DoGChargeGate>().Destination);
                        }

                        // Bring all segments along with.
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            if (Main.npc[i].active && (Main.npc[i].type == ModContent.NPCType<DevourerofGodsBody>() || Main.npc[i].type == ModContent.NPCType<DevourerofGodsTail>()))
                            {
                                Main.npc[i].Center = npc.Center - npc.velocity.SafeNormalize(Vector2.UnitY) * i * 2f;
                                Main.npc[i].netUpdate = true;
                            }
                        }

                        // Charge and roar.
                        npc.velocity = aimDirection * chargeSpeed;
                        SoundEngine.PlaySound(DevourerofGodsHead.AttackSound, target.Center);

                        // Go to the next state.
                        perpendicularPortalAttackTimer = 0f;
                        damageImmunityCountdown = 90f;
                        SurprisePortalAttackState = PerpendicularPortalAttackState.AttackEndDelay;
                    }

                    segmentFadeType = (int)BodySegmentFadeType.ApproachAheadSegmentOpacity;
                    break;
                case PerpendicularPortalAttackState.AttackEndDelay:
                    npc.Opacity = 1f;
                    segmentFadeType = (int)BodySegmentFadeType.ApproachAheadSegmentOpacity;

                    if (perpendicularPortalAttackTimer >= waitBeforeSnappingAgain)
                    {
                        SurprisePortalAttackState = PerpendicularPortalAttackState.NotPerformingAttack;
                        perpendicularPortalAttackTimer = 0f;
                        npc.netUpdate = true;
                    }
                    break;
            }

            // Determine rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public static bool DoSpecialAttacks(NPC npc, Player target, bool finalPhase, ref float performingSpecialAttack, ref float specialAttackTimer, ref float specialAttackPortalIndex, ref float segmentFadeType, ref float damageImmunityCountdown)
        {
            SpecialAttackType specialAttackType = (SpecialAttackType)npc.Infernum().ExtraAI[SpecialAttackTypeIndex];

            // Always use charge gates when in the final phase.
            if (finalPhase)
                specialAttackType = SpecialAttackType.ChargeGates;

            if (npc.Opacity <= 0f || specialAttackType == SpecialAttackType.ChargeGates)
            {
                specialAttackTimer++;

                // Create the portal to teleport out of.
                if (specialAttackTimer == SpecialAttackDuration + SpecialAttackPortalCreationDelay)
                {
                    Vector2 portalSpawnPosition = target.Center - Vector2.UnitY * 500f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        specialAttackPortalIndex = Utilities.NewProjectileBetter(portalSpawnPosition, Vector2.Zero, ModContent.ProjectileType<DoGChargeGate>(), 0, 0f, -1, 0f, (int)(SpecialAttackPortalSnapDelay * 1.25f));

                    // Delete lingering laser wall things.
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        if (Main.projectile[i].type == ModContent.ProjectileType<DoGDeath>())
                            Main.projectile[i].Kill();
                    }
                }

                // Move to the portal and snap at the target after a brief period of time.
                if (specialAttackTimer >= SpecialAttackDuration + SpecialAttackPortalCreationDelay + SpecialAttackPortalSnapDelay)
                {
                    if (specialAttackPortalIndex >= 0f)
                    {
                        SoundEngine.PlaySound(DevourerofGodsHead.AttackSound, target.Center);

                        npc.Center = Main.projectile[(int)specialAttackPortalIndex].Center;
                        npc.velocity = npc.SafeDirectionTo(Main.projectile[(int)specialAttackPortalIndex].ModProjectile<DoGChargeGate>().Destination) * 45f;
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            if (Main.npc[i].active && (Main.npc[i].type == ModContent.NPCType<DevourerofGodsBody>() || Main.npc[i].type == ModContent.NPCType<DevourerofGodsTail>()))
                            {
                                Main.npc[i].Center = npc.Center - npc.velocity.SafeNormalize(Vector2.UnitY) * i * 0.5f;
                                Main.npc[i].netUpdate = true;
                            }
                        }
                        Main.projectile[(int)specialAttackPortalIndex].Kill();
                    }

                    FadeToAntimatterForm = 0f;
                    npc.Opacity = 1f;
                    npc.dontTakeDamage = false;
                    npc.netUpdate = true;

                    if (!finalPhase)
                        npc.Infernum().ExtraAI[AnimationMoveDelayIndex] = 0f;

                    specialAttackPortalIndex = -1f;
                    performingSpecialAttack = 0f;
                    specialAttackTimer = 0f;
                    damageImmunityCountdown = 60f;
                    npc.Infernum().ExtraAI[PreviousSpecialAttackTypeIndex] = npc.Infernum().ExtraAI[SpecialAttackTypeIndex];
                    DoGSkyInfernum.CreateLightningBolt(Color.White, 16, true);
                }

                // Make sure to return early to inform the AI that it should not execute any code beyond the special attack section.
                // This is necessary to ensure that the portal index clearing code in the AI method does not happen, which would screw up the teleportion step above.
                if (specialAttackTimer >= SpecialAttackDuration + SpecialAttackPortalCreationDelay)
                {
                    FadeToAntimatterForm = MathHelper.Clamp(FadeToAntimatterForm - 0.05f, 0f, 1f);
                    return false;
                }

                if (specialAttackTimer < SpecialAttackDuration)
                {
                    // Ensure that DoG does not move too far away from the target if they're invisible.
                    // This is done to prevent him from moving so far away that the background color changes for a few seconds until he teleports.
                    if (npc.Opacity <= 0f && !npc.WithinRange(target.Center, 2500f))
                    {
                        npc.Center = target.Center + target.SafeDirectionTo(npc.Center) * 2490f;
                        npc.velocity *= 0.7f;
                    }

                    switch (specialAttackType)
                    {
                        case SpecialAttackType.LaserWalls:
                            DoSpecialAttack_LaserWalls(target, ref specialAttackTimer, ref segmentFadeType);
                            break;
                        case SpecialAttackType.CircularLaserBurst:
                            DoSpecialAttack_CircularLaserBurst(npc, target, ref specialAttackTimer, ref segmentFadeType);
                            break;
                        case SpecialAttackType.ChargeGates:
                            DoSpecialAttack_ChargeGates(npc, target, finalPhase, ref specialAttackTimer, ref specialAttackPortalIndex, ref segmentFadeType);
                            return false;
                    }

                    // Provide the target infinite flight time during laser grid attacks.
                    if (specialAttackType != SpecialAttackType.ChargeGates)
                        target.wingTime = target.wingTimeMax;

                    if (specialAttackType is SpecialAttackType.LaserWalls or SpecialAttackType.CircularLaserBurst)
                        HatGirl.SayThingWhileOwnerIsAlive(target, "Oh man, theres so many lasers! Slow and precise movements seem like your best bet here...");
                }

                // Be completely invisible after the special attacks conclude.
                else
                {
                    segmentFadeType = (int)BodySegmentFadeType.InhertHeadOpacity;
                    npc.Infernum().ExtraAI[ChargeGatePortalTelegraphTimeIndex] = 0f;
                    npc.Opacity = 0f;
                }
            }
            else
                npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.02f, 0f, 1f);

            return true;
        }

        #endregion AI

        #region Drawing
        public static bool PreDraw(NPC npc, Color lightColor)
        {
            npc.scale = 1f;
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            float jawRotation = npc.Infernum().ExtraAI[JawRotationIndex];

            Texture2D headTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2Head").Value;
            Texture2D glowTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2HeadGlow").Value;
            Texture2D jawTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2Jaw").Value;
            Texture2D headTextureAntimatter = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2HeadAntimatter").Value;
            Texture2D glowTextureAntimatter = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2HeadGlowAntimatter").Value;
            Texture2D jawTextureAntimatter = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2JawAntimatter").Value;

            npc.frame = new Rectangle(0, 0, headTexture.Width, headTexture.Height);
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 headTextureOrigin = headTexture.Size() * 0.5f;
            drawPosition -= headTexture.Size() * npc.scale * 0.5f;
            drawPosition += headTextureOrigin * npc.scale + new Vector2(0f, 4f + npc.gfxOffY);
            Vector2 jawOrigin = jawTexture.Size() * 0.5f;

            // Draw each jaw.
            for (int i = -1; i <= 1; i += 2)
            {
                float jawBaseOffset = 42f;
                SpriteEffects jawSpriteEffect = SpriteEffects.None;
                if (i == 1)
                {
                    jawSpriteEffect |= SpriteEffects.FlipHorizontally;
                }
                Vector2 jawPosition = drawPosition;
                jawPosition += Vector2.UnitX.RotatedBy(npc.rotation + jawRotation * i) * i * (jawBaseOffset + (float)Math.Sin(jawRotation) * 24f);
                jawPosition -= Vector2.UnitY.RotatedBy(npc.rotation) * (58f + (float)Math.Sin(jawRotation) * 30f);
                Main.spriteBatch.Draw(jawTexture, jawPosition, null, npc.GetAlpha(lightColor) * (1f - FadeToAntimatterForm), npc.rotation + jawRotation * i, jawOrigin, npc.scale, jawSpriteEffect, 0f);
                Main.spriteBatch.Draw(jawTextureAntimatter, jawPosition, null, npc.GetAlpha(lightColor) * FadeToAntimatterForm, npc.rotation + jawRotation * i, jawOrigin, npc.scale, jawSpriteEffect, 0f);
            }

            Main.spriteBatch.Draw(headTexture, drawPosition, npc.frame, npc.GetAlpha(lightColor) * (1f - FadeToAntimatterForm), npc.rotation, headTextureOrigin, npc.scale, spriteEffects, 0f);
            Main.spriteBatch.Draw(glowTexture, drawPosition, npc.frame, npc.GetAlpha(Color.White) * (1f - FadeToAntimatterForm), npc.rotation, headTextureOrigin, npc.scale, spriteEffects, 0f);
            Main.spriteBatch.Draw(headTextureAntimatter, drawPosition, npc.frame, npc.GetAlpha(lightColor) * FadeToAntimatterForm, npc.rotation, headTextureOrigin, npc.scale, spriteEffects, 0f);
            Main.spriteBatch.Draw(glowTextureAntimatter, drawPosition, npc.frame, npc.GetAlpha(Color.White) * FadeToAntimatterForm, npc.rotation, headTextureOrigin, npc.scale, spriteEffects, 0f);
            return false;
        }
        #endregion Drawing
    }
}
