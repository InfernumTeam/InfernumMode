using CalamityMod;
using CalamityMod.Events;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.NPCs;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Sounds;
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

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public static class DoGPhase2HeadBehaviorOverride
    {
        public enum SpecialAttackType
        {
            LaserWalls,
            CircularLaserBurst,
            ChargeGates
        }

        public enum BodySegmentFadeType
        {
            InhertHeadOpacity,
            EnterPortal,
            ApproachAheadSegmentOpacity
        }

        public static bool InPhase2
        {
            get
            {
                if (CalamityGlobalNPC.DoGHead < 0)
                    return false;

                return Main.npc[CalamityGlobalNPC.DoGHead].Infernum().ExtraAI[33] == 1f;
            }
            set
            {
                if (CalamityGlobalNPC.DoGHead < 0)
                    return;

                Main.npc[CalamityGlobalNPC.DoGHead].Infernum().ExtraAI[33] = value.ToInt();
            }
        }

        public static float FadeToAntimatterForm
        {
            get
            {
                if (CalamityGlobalNPC.DoGHead < 0)
                    return 0f;

                return Main.npc[CalamityGlobalNPC.DoGHead].Infernum().ExtraAI[39];
            }
            set
            {
                if (CalamityGlobalNPC.DoGHead < 0)
                    return;

                Main.npc[CalamityGlobalNPC.DoGHead].Infernum().ExtraAI[39] = value;
            }
        }

        public const int PostP2AnimationMoveDelay = 45;

        public const int SpecialAttackDuration = 675;

        public const int SpecialAttackPortalCreationDelay = 25;

        public const int SpecialAttackPortalSnapDelay = 65;

        public const int PassiveMovementTimeP2 = 360;

        public const int AggressiveMovementTimeP2 = 720;

        public const float CanUseSpecialAttacksLifeRatio = 0.7f;

        public const float CanUseCeaselessVoidSentinelAttackLifeRatio = 0.6f;

        public const float CanUseSignusSentinelAttackLifeRatio = 0.5f;

        public const float FinalPhaseLifeRatio = 0.2f;

        public static readonly Color PassiveFadeColor = Color.DeepSkyBlue;

        public static readonly Color AggressiveFadeColor = Color.Red;

        #region AI
        public static bool Phase2AI(NPC npc, ref float phaseCycleTimer, ref float passiveAttackDelay, ref float portalIndex, ref float segmentFadeType)
        {
            // Set music.
            npc.ModNPC.Music = (InfernumMode.CalamityMod as CalamityMod.CalamityMod).GetMusicFromMusicMod("DevourerOfGodsP2") ?? MusicID.LunarBoss;

            ref float performingSpecialAttack = ref npc.Infernum().ExtraAI[14];
            ref float specialAttackTimer = ref npc.Infernum().ExtraAI[15];
            ref float nearDeathFlag = ref npc.Infernum().ExtraAI[16];
            ref float spawnedSegmentsFlag = ref npc.Infernum().ExtraAI[17];
            ref float sentinelAttackTimer = ref npc.Infernum().ExtraAI[18];
            ref float signusAttackState = ref npc.Infernum().ExtraAI[19];
            ref float jawRotation = ref npc.Infernum().ExtraAI[20];
            ref float chompEffectsCountdown = ref npc.Infernum().ExtraAI[21];
            ref float time = ref npc.Infernum().ExtraAI[22];
            ref float flyAcceleration = ref npc.Infernum().ExtraAI[23];
            ref float postSpawnAnimationMoveDelay = ref npc.Infernum().ExtraAI[24];
            ref float hasPerformedSpecialAttackBefore = ref npc.Infernum().ExtraAI[25];
            ref float totalCharges = ref npc.Infernum().ExtraAI[27];
            ref float fadeinTimer = ref npc.Infernum().ExtraAI[28];
            ref float fireballShootTimer = ref npc.Infernum().ExtraAI[31];
            ref float deathTimer = ref npc.Infernum().ExtraAI[32];

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            target.Calamity().normalityRelocator = false;
            target.Calamity().spectralVeil = false;

            npc.takenDamageMultiplier = 2f;

            // Declare the global whoAmI index.
            CalamityGlobalNPC.DoGHead = npc.whoAmI;

            // Handle fade-in logic when the boss is summoned.
            if (fadeinTimer < DoGPhase2IntroPortalGate.Phase2AnimationTime)
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
                segmentFadeType = (int)BodySegmentFadeType.InhertHeadOpacity;

                // Stay far above the player, but get increasing close as the animation goes on.
                // This is a trick to make the background fade from violet/cyan to black as the animation goes on.
                // This probably is fucked in multiplayer but whatever lmao.
                npc.Center = target.Center - Vector2.UnitY * MathHelper.Lerp(6000f, 3000f, fadeinTimer / DoGPhase2IntroPortalGate.Phase2AnimationTime);
                fadeinTimer++;
                passiveAttackDelay = 0f;
                phaseCycleTimer = 0f;

                // Teleport to the position of the portal and charge at the target after the animation concludes.
                if (fadeinTimer >= DoGPhase2IntroPortalGate.Phase2AnimationTime)
                {
                    npc.Opacity = 1f;
                    npc.Center = Main.projectile[(int)portalIndex].Center;
                    npc.velocity = npc.SafeDirectionTo(target.Center) * 36f;
                    npc.netUpdate = true;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<DoGSpawnBoom>(), 0, 0f);

                    // Reset the special attack portal index to -1.
                    portalIndex = -1f;
                }
                return false;
            }

            // Percent life remaining
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Variables
            bool canPerformSpecialAttacks = lifeRatio < CanUseSpecialAttacksLifeRatio;
            bool nearDeath = lifeRatio < FinalPhaseLifeRatio;
            bool doPassiveMovement = phaseCycleTimer % (PassiveMovementTimeP2 + AggressiveMovementTimeP2) >= AggressiveMovementTimeP2 && !nearDeath;

            // Don't take damage when fading out.
            npc.dontTakeDamage = npc.Opacity < 0.5f;
            npc.damage = npc.dontTakeDamage ? 0 : 3000;
            npc.Calamity().DR = 0.3f;

            // Stay in the world.
            npc.position.Y = MathHelper.Clamp(npc.position.Y, 180f, Main.maxTilesY * 16f - 180f);

            // Do the death animation once dead.
            if (deathTimer > 0f)
            {
                DoDeathEffects(npc, deathTimer);
                jawRotation = jawRotation.AngleTowards(0f, 0.07f);
                segmentFadeType = (int)BodySegmentFadeType.ApproachAheadSegmentOpacity;
                npc.Opacity = 1f;
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                deathTimer++;
                return false;
            }

            // Have segments by default inherit the opacity of the head.
            segmentFadeType = (int)BodySegmentFadeType.InhertHeadOpacity;

            // Handle special attacks.
            if (canPerformSpecialAttacks)
            {
                // Handle special attack transition.
                int specialAttackDelay = 1335;
                int specialAttackTransitionPreparationTime = 135;
                if (performingSpecialAttack == 0f)
                {
                    // Disappear immediately if the target is gone.
                    if (!target.active || target.dead)
                        npc.active = false;

                    // The charge gate attack happens much more frequently when DoG is close to death.
                    specialAttackTimer += nearDeath && specialAttackTimer < specialAttackDelay - specialAttackTransitionPreparationTime - 5f ? 2f : 1f;

                    // Enter a portal before performing a special attack.
                    if (Main.netMode != NetmodeID.MultiplayerClient && specialAttackTimer == specialAttackDelay - specialAttackTransitionPreparationTime)
                    {
                        portalIndex = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + npc.velocity * 75f, Vector2.Zero, ModContent.ProjectileType<DoGChargeGate>(), 0, 0f);
                        Main.projectile[(int)portalIndex].localAI[0] = 1f;
                        npc.netUpdate = true;
                    }

                    // Ensure that DoG only performs a special attack when in his passive phase, to prevent strange cutoffs.
                    // This doesn't apply if DoG is in his final phase.
                    bool shouldWaitBeforeDoingSpecialAttack = !doPassiveMovement && !nearDeath;
                    if (shouldWaitBeforeDoingSpecialAttack && specialAttackTimer == specialAttackDelay - specialAttackTransitionPreparationTime - 1f)
                        specialAttackTimer--;

                    if (specialAttackTimer >= specialAttackDelay)
                    {
                        specialAttackTimer = 0f;
                        performingSpecialAttack = 1f;
                        portalIndex = -1f;

                        // Select a special attack type.
                        npc.Infernum().ExtraAI[2] = Main.rand.Next(10);
                        npc.netUpdate = true;
                    }

                    // Do nothing and drift into the portal.
                    if (specialAttackTimer >= specialAttackDelay - specialAttackTransitionPreparationTime)
                    {
                        // Laugh if this is the first time DoG has performed a special attack in the fight.
                        if (hasPerformedSpecialAttackBefore == 0f && specialAttackTimer == specialAttackDelay - specialAttackTransitionPreparationTime)
                        {
                            SoundEngine.PlaySound(InfernumSoundRegistry.DoGLaughSound with { Volume = 3f }, target.Center);
                            hasPerformedSpecialAttackBefore = 1f;
                        }

                        npc.damage = 0;
                        npc.dontTakeDamage = true;
                        npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitX) * MathHelper.Lerp(npc.velocity.Length(), 105f, 0.15f);

                        // Disappear when touching the portal.
                        // This same logic applies to body/tail segments.
                        if (Main.projectile.IndexInRange((int)portalIndex) && npc.Hitbox.Intersects(Main.projectile[(int)portalIndex].Hitbox))
                            npc.Opacity = 0f;

                        segmentFadeType = (int)BodySegmentFadeType.EnterPortal;

                        // Clear away various misc projectiles.
                        int[] projectilesToDelete = new int[]
                        {
                            ProjectileID.CultistBossLightningOrbArc,
                            ModContent.ProjectileType<HomingDoGBurst>(),
                            ModContent.ProjectileType<EssenceSliceTelegraphLine>()
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
                    
                    bool doingSpecialAttacks = DoSpecialAttacks(npc, target, nearDeath, ref performingSpecialAttack, ref specialAttackTimer, ref portalIndex, ref segmentFadeType);
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
                npc.Infernum().ExtraAI[2] = 0f;

            // Say some edgy things if close to death as an indicator that the final phase has been entered.
            if (nearDeath && nearDeathFlag == 0f)
            {
                Utilities.DisplayText("A GOD DOES NOT FEAR DEATH!", Color.Cyan);
                nearDeathFlag = 1f;
            }

            // Increment the universal attack timer.
            time++;

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
            else if (postSpawnAnimationMoveDelay < PostP2AnimationMoveDelay)
                postSpawnAnimationMoveDelay++;

            // Do passive movement along with sentinel attacks.
            else if (doPassiveMovement)
            {
                DoPassiveFlyMovement(npc, ref jawRotation, ref chompEffectsCountdown);
                if (phaseCycleTimer % (PassiveMovementTimeP2 + AggressiveMovementTimeP2) == AggressiveMovementTimeP2 + 1f)
                    DoGSkyInfernum.CreateLightningBolt(Color.White, 16, true);

                if (passiveAttackDelay >= 300f)
                {
                    // Increment the sentinal attack timer if DoG is completely visible.
                    if (totalSentinelAttacks >= 1 && npc.Opacity >= 1f)
                        sentinelAttackTimer++;
                    if (sentinelAttackTimer >= totalSentinelAttacks * 450f)
                        sentinelAttackTimer = 0f;

                    DoSentinelAttacks(npc, target, phaseCycleTimer, ref sentinelAttackTimer, ref signusAttackState);
                }
            }

            // Do aggressive fly movement, snapping at the target ruthlessly.
            else
            {
                bool dontChompYet = phaseCycleTimer % (PassiveMovementTimeP2 + AggressiveMovementTimeP2) < 90f;
                if (phaseCycleTimer % (PassiveMovementTimeP2 + AggressiveMovementTimeP2) == 2f)
                    DoGSkyInfernum.CreateLightningBolt(new Color(1f, 0f, 0f, 0.2f), 16, true);

                DoAggressiveFlyMovement(npc, target, dontChompYet, chomping, ref jawRotation, ref chompEffectsCountdown, ref time, ref flyAcceleration);
            }

            // Define the rotation and sprite direction. This only applies for non-special attacks.
            npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            return false;
        }

        public static void Despawn(NPC npc)
        {
            npc.velocity.Y -= 3f;
            if (npc.position.Y < Main.topWorld + 16f)
                npc.velocity.Y -= 3f;

            if (npc.position.Y < 200f)
            {
                for (int a = 0; a < Main.maxNPCs; a++)
                {
                    if (Main.npc[a].type == InfernumMode.CalamityMod.Find<ModNPC>("DevourerofGodsHead").Type ||
                        Main.npc[a].type == InfernumMode.CalamityMod.Find<ModNPC>("DevourerofGodsBody").Type ||
                        Main.npc[a].type == InfernumMode.CalamityMod.Find<ModNPC>("DevourerofGodsTail").Type)
                    {
                        Main.npc[a].active = false;
                        Main.npc[a].netUpdate = true;
                    }
                }
            }
        }

        public static void DoSentinelAttacks(NPC npc, Player target, float attackTimer, ref float sentinelAttackTimer, ref float signusAttackState)
        {
            // Storm Weaver Effect (Lightning Storm).
            int attackTime = 450;
            bool nearEndOfAttack = sentinelAttackTimer % attackTime >= attackTime - 105f;
            if (sentinelAttackTimer > 0f && sentinelAttackTimer <= attackTime && npc.Opacity >= 0.5f)
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
                            int laser = Utilities.NewProjectileBetter(target.Center + spawnOffset, laserShootVelocity, ModContent.ProjectileType<DoGDeath>(), 415, 0f);
                            if (Main.projectile.IndexInRange(laser))
                                Main.projectile[laser].MaxUpdates = 3;
                        }
                    }
                }


                sentinelAttackTimer = attackTime;
            }

            // Signus Effect (Essence Cleave).
            if (sentinelAttackTimer > attackTime && sentinelAttackTimer <= attackTime * 2f && npc.Opacity >= 0.5f)
            {
                float wrappedAttackTimer = attackTimer % attackTime;
                if (wrappedAttackTimer % 45f == 0f)
                {
                    int cleaveCount = 16;
                    float offsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                    for (int i = 0; i < cleaveCount; i++)
                    {
                        int cleave = Utilities.NewProjectileBetter(target.Center, Vector2.Zero, ModContent.ProjectileType<EssenceSliceTelegraphLine>(), 0, 0f);
                        if (Main.projectile.IndexInRange(cleave))
                            Main.projectile[cleave].ai[0] = MathHelper.TwoPi * i / cleaveCount + offsetAngle;
                    }
                }

                if (wrappedAttackTimer % 45f == 20f)
                    SoundEngine.PlaySound(SoundID.Item122, target.Center);
            }
        }

        public static void DoDeathEffects(NPC npc, float deathTimer)
        {
            npc.Calamity().CanHaveBossHealthBar = false;
            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.1f, 0f, 1f);
            npc.dontTakeDamage = true;
            npc.damage = 0;

            void destroySegment(int index, ref float destroyedSegments)
            {
                if (Main.rand.NextBool(5))
                    SoundEngine.PlaySound(SoundID.Item94, npc.Center);

                List<int> segments = new()
                {
                    ModContent.NPCType<DevourerofGodsBody>(),
                    ModContent.NPCType<DevourerofGodsTail>()
                };
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (segments.Contains(Main.npc[i].type) && Main.npc[i].active && Main.npc[i].Infernum().ExtraAI[34] == index)
                    {
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

            float idealSpeed = MathHelper.Lerp(9f, 4.75f, Utils.GetLerpValue(15f, 210f, deathTimer, true));
            ref float destroyedSegmentsCounts = ref npc.Infernum().ExtraAI[34];
            if (npc.velocity.Length() != idealSpeed)
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(npc.velocity.Length(), idealSpeed, 0.08f);

            if (deathTimer == 120f)
                Utilities.DisplayText("I WILL NOT BE DESTROYED!!!", Color.Cyan);
            if (deathTimer == 170f)
                Utilities.DisplayText("I WILL NOT BE DESTROYED!!!", Color.Cyan);
            if (deathTimer == 220f)
                Utilities.DisplayText("I WILL NOT BE DESTROYED!!!!", Color.Cyan);

            if (deathTimer >= 120f && deathTimer < 380f && deathTimer % 4f == 0f)
            {
                int segmentToDestroy = (int)(Utils.GetLerpValue(120f, 380f, deathTimer, true) * 60f);
                destroySegment(segmentToDestroy, ref destroyedSegmentsCounts);
            }

            if (deathTimer == 330f)
                Utilities.DisplayText("I WILL NOT...", Color.Cyan);

            if (deathTimer == 420f)
                Utilities.DisplayText("I...", Color.Cyan);

            if (deathTimer == 442f)
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

            if (deathTimer >= 410f && deathTimer < 470f && deathTimer % 2f == 0f)
            {
                int segmentToDestroy = (int)(Utils.GetLerpValue(410f, 470f, deathTimer, true) * 10f) + 60;
                destroySegment(segmentToDestroy, ref destroyedSegmentsCounts);
            }

            float light = Utils.GetLerpValue(430f, 465f, deathTimer, true);
            MoonlordDeathDrama.RequestLight(light, Main.LocalPlayer.Center);

            if (deathTimer >= 485f)
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

        public static void DoPassiveFlyMovement(NPC npc, ref float jawRotation, ref float chompEffectsCountdown)
        {
            chompEffectsCountdown = 0f;
            jawRotation = jawRotation.AngleTowards(0f, 0.08f);

            // Move towards the target.
            Vector2 destination = Main.player[npc.target].Center - Vector2.UnitY * 660f;
            if (!npc.WithinRange(destination, 125f))
            {
                float flySpeed = MathHelper.Lerp(27f, 38f, 1f - npc.life / (float)npc.lifeMax);
                Vector2 idealVelocity = npc.SafeDirectionTo(destination) * flySpeed;
                npc.velocity = npc.velocity.MoveTowards(idealVelocity, 2f).RotateTowards(idealVelocity.ToRotation(), 0.032f);
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(npc.velocity.Length(), idealVelocity.Length(), 0.1f);
                if (npc.velocity.Y > -1f && MathHelper.Distance(destination.X, npc.Center.X) < 1050f)
                    npc.velocity.Y -= 1.75f;
            }
        }

        public static void DoAggressiveFlyMovement(NPC npc, Player target, bool dontChompYet, bool chomping, ref float jawRotation, ref float chompEffectsCountdown, ref float time, ref float flyAcceleration)
        {
            npc.Center = npc.Center.MoveTowards(target.Center, InPhase2 ? 1.8f : 2.4f);
            bool targetHasDash = target.dash > 0 || target.Calamity().HasCustomDash;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float idealFlyAcceleration = MathHelper.Lerp(0.045f, 0.032f, lifeRatio);
            float idealFlySpeed = MathHelper.Lerp(20.5f, 15f, lifeRatio);
            float idealMouthOpeningAngle = MathHelper.ToRadians(32f);
            float flySpeedFactor = 1.45f + (1f - lifeRatio) * 0.4f;
            float snakeMovementDistanceThreshold = 650f;
            if (InPhase2)
            {
                idealFlyAcceleration += 0.005f;
                idealMouthOpeningAngle = MathHelper.ToRadians(34f);
                flySpeedFactor += lifeRatio * 0.05f;
                snakeMovementDistanceThreshold -= 125f;
                if (BossRushEvent.BossRushActive)
                    idealFlySpeed *= 1.4f;
            }

            if (!targetHasDash)
                flyAcceleration *= 0.84f;

            Vector2 destination = target.Center;

            // Swerve around in a snake-like movement if sufficiently far away from the target.
            float distanceFromBaseDestination = npc.Distance(destination);
            float distanceFromTarget = npc.Distance(target.Center);
            if (npc.Distance(destination) > snakeMovementDistanceThreshold)
            {
                destination += (time % 60f / 60f * MathHelper.TwoPi).ToRotationVector2() * 145f;
                distanceFromBaseDestination = npc.Distance(destination);
                idealFlyAcceleration *= 1.8f;
                flySpeedFactor = 1.55f;
            }

            float swimOffsetAngle = (float)Math.Sin(MathHelper.TwoPi * time / 160f) * Utils.GetLerpValue(400f, 540f, distanceFromBaseDestination, true) * 0.41f;

            // Charge if the player is far away.
            // Don't do this at the start of the fight though. Doing so might lead to an unfair
            // charge.
            if (distanceFromBaseDestination > 1500f && time > 120f)
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
                if (speed < 18.5f)
                    speed += 0.08f;
                if (speed > 26f)
                    speed -= 0.08f;

                // Speed up if close to aiming at the target, but not too close (within a margin of 32-60 degrees).
                if (targetDirectionAngleDiscrepancy is > 32f and < 60f)
                    speed += 0.24f;

                // Slow down if farther to aiming at the target, for the sake of allowing DoG to get back on track again (within a margin of 60-135 degrees).
                if (targetDirectionAngleDiscrepancy is > 60f and < 135f)
                    speed -= 0.1f;

                // Clamp the speed.
                speed = MathHelper.Clamp(speed, flySpeedFactor * 15f, flySpeedFactor * 35f);

                // And handle movement.
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(destination) + swimOffsetAngle, flyAcceleration, true) * speed;
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(destination) * speed, flyAcceleration * 15f);
            }

            // Jaw opening when near player.
            if (!chomping)
            {
                if ((distanceFromTarget < 330f && targetDirectionAngleDiscrepancy < 38f) || (distanceFromTarget < 550f && targetDirectionAngleDiscrepancy < 30f))
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
            if (distanceFromBaseDestination < 360f && targetDirectionAngleDiscrepancy < 64f && npc.velocity.Length() < idealFlySpeed * 2f && !dontChompYet)
            {
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * npc.velocity.Length() * 1.75f;
                jawRotation = jawRotation.AngleLerp(idealMouthOpeningAngle, 0.55f);

                if (chompEffectsCountdown == 0f)
                {
                    SoundEngine.PlaySound(CommonCalamitySounds.OtherwordlyHitSound, npc.Center);
                    chompEffectsCountdown = 26f;
                }
            }
        }

        public static void DoSpecialAttack_LaserWalls(Player target, ref float attackTimer, ref float segmentFadeType)
        {
            // Body segments should be invisible along with the head.
            segmentFadeType = (int)BodySegmentFadeType.InhertHeadOpacity;

            float offsetPerLaser = 80f;
            float laserWallSpeed = 16f;
            if (attackTimer % 90f == 89f)
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

                        int laser = Utilities.NewProjectileBetter(laserSpawnPositionRight, laserVelocityRight, shootType, 415, 0f);
                        if (Main.projectile.IndexInRange(laser))
                            Main.projectile[laser].MaxUpdates = 2;
                        laser = Utilities.NewProjectileBetter(laserSpawnPositionLeft, laserVelocityLeft, shootType, 415, 0f);
                        if (Main.projectile.IndexInRange(laser))
                            Main.projectile[laser].MaxUpdates = 2;
                    }

                    // Lower wall.
                    for (int x = -12; x <= 12; x++)
                    {
                        Vector2 laserSpawnPosition = new Vector2(target.Center.X, targetY) + new Vector2(x * offsetPerLaser, 1000f).RotatedBy(laserOffsetAngle);
                        Vector2 laserVelocity = -Vector2.UnitY.RotatedBy(laserOffsetAngle) * laserWallSpeed;
                        int laser = Utilities.NewProjectileBetter(laserSpawnPosition, laserVelocity, shootType, 415, 0f);
                        if (Main.projectile.IndexInRange(laser))
                            Main.projectile[laser].MaxUpdates = 2;
                    }

                    // Upper wall.
                    for (int x = -20; x < 20; x++)
                    {
                        Vector2 laserSpawnPosition = new Vector2(target.Center.X, targetY) + new Vector2(x * offsetPerLaser, -1000f).RotatedBy(laserOffsetAngle);
                        Vector2 laserVelocity = Vector2.UnitY.RotatedBy(laserOffsetAngle) * laserWallSpeed;
                        int laser = Utilities.NewProjectileBetter(laserSpawnPosition, laserVelocity, shootType, 415, 0f);
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
            if (attackTimer % 80f == 79f)
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

        public static void DoSpecialAttack_ChargeGates(NPC npc, Player target, bool nearDeath, ref float attackTimer, ref float portalIndex, ref float segmentFadeType)
        {
            // Transform into the antimatter form.
            FadeToAntimatterForm = MathHelper.Clamp(FadeToAntimatterForm + 0.05f, 0f, 1f);

            int fireballCount = 8;
            int idealPortalTelegraphTime = 52;
            float wrappedAttackTimer = attackTimer % 135f;
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Charges become increasingly powerful as DoG gets closer to death, once past the final phase.
            // This is done as a means of indicating desparation, and acts as a sort of final stand.
            if (lifeRatio < 0.15f)
            {
                fireballCount -= 2;
                idealPortalTelegraphTime -= 12;
            }
            if (lifeRatio < 0.1f)
            {
                fireballCount -= 2;
                idealPortalTelegraphTime -= 10;
            }
            if (lifeRatio < 0.05f)
            {
                fireballCount -= 2;
                idealPortalTelegraphTime -= 10;
            }

            float chargeSpeed = nearDeath ? 85f : 60f;
            ref float chargeGatePortalIndex = ref npc.Infernum().ExtraAI[36];
            ref float portalTelegraphTime = ref npc.Infernum().ExtraAI[38];

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
                chargeGatePortalIndex = Projectile.NewProjectile(npc.GetSource_FromAI(), portalSpawnPosition, Vector2.Zero, ModContent.ProjectileType<DoGChargeGate>(), 0, 0f);
                Main.projectile[(int)chargeGatePortalIndex].ai[1] = portalTelegraphTime;
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
                    npc.velocity = npc.SafeDirectionTo(Main.projectile[(int)chargeGatePortalIndex].ModProjectile<DoGChargeGate>().Destination) * chargeSpeed;
                    npc.Opacity = 1f;
                    npc.netUpdate = true;

                    // Create a burst of homing flames.
                    float flameBurstOffsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                    for (int i = 0; i < fireballCount; i++)
                    {
                        Vector2 flameShootVelocity = (MathHelper.TwoPi * i / fireballCount + flameBurstOffsetAngle).ToRotationVector2() * 15f;
                        Utilities.NewProjectileBetter(npc.Center + flameShootVelocity * 3f, flameShootVelocity, ModContent.ProjectileType<HomingDoGBurst>(), 415, 0f);
                    }

                    // Create the portal to go through.
                    if (!target.dead)
                    {
                        Vector2 portalSpawnPosition = npc.Center + npc.velocity.SafeNormalize(Vector2.UnitY) * 1900f;
                        portalIndex = Projectile.NewProjectile(npc.GetSource_FromAI(), portalSpawnPosition, Vector2.Zero, ModContent.ProjectileType<DoGChargeGate>(), 0, 0f);
                        Main.projectile[(int)portalIndex].localAI[0] = 1f;
                        Main.projectile[(int)portalIndex].ai[1] = portalTelegraphTime;
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
                    segmentFadeType = (int)BodySegmentFadeType.EnterPortal;
            }

            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public static bool DoSpecialAttacks(NPC npc, Player target, bool nearDeath, ref float performingSpecialAttack, ref float specialAttackTimer, ref float specialAttackPortalIndex, ref float segmentFadeType)
        {
            SpecialAttackType specialAttackType;

            // Always use charge gates when in the final phase.
            if (nearDeath)
                npc.Infernum().ExtraAI[2] = 9f;

            // Otherwise, select the associated special attack.
            if (npc.Infernum().ExtraAI[2] <= 3f)
                specialAttackType = SpecialAttackType.LaserWalls;
            else if (npc.Infernum().ExtraAI[2] <= 5f)
                specialAttackType = SpecialAttackType.CircularLaserBurst;
            else
                specialAttackType = SpecialAttackType.ChargeGates;

            if (npc.Opacity <= 0f || specialAttackType == SpecialAttackType.ChargeGates)
            {
                specialAttackTimer++;

                // Create the portal to teleport out of.
                if (specialAttackTimer == SpecialAttackDuration + SpecialAttackPortalCreationDelay)
                {
                    Vector2 portalSpawnPosition = target.Center + target.velocity.SafeNormalize(Main.rand.NextVector2Unit()) * 450f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        specialAttackPortalIndex = Projectile.NewProjectile(npc.GetSource_FromAI(), portalSpawnPosition, Vector2.Zero, ModContent.ProjectileType<DoGChargeGate>(), 0, 0f);
                        Main.projectile[(int)specialAttackPortalIndex].ai[1] = (int)(SpecialAttackPortalSnapDelay * 1.25f);
                    }

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
                        npc.velocity = npc.SafeDirectionTo(target.Center) * 45f;
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

                    if (!nearDeath)
                        npc.Infernum().ExtraAI[24] = 0f;
                    specialAttackPortalIndex = -1f;
                    performingSpecialAttack = 0f;
                    specialAttackTimer = 0f;
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
                            DoSpecialAttack_ChargeGates(npc, target, nearDeath, ref specialAttackTimer, ref specialAttackPortalIndex, ref segmentFadeType);
                            return false;
                    }
                }

                // Be completely invisible after the special attacks conclude.
                else
                {
                    segmentFadeType = (int)BodySegmentFadeType.InhertHeadOpacity;
                    npc.Infernum().ExtraAI[38] = 0f;
                    npc.Opacity = 0f;
                }
            }
            else
                npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.02f, 0f, 1f);

            return true;
        }

        #endregion AI

        #region Drawing
        public static bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            npc.scale = 1f;
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            float jawRotation = npc.Infernum().ExtraAI[20];

            Texture2D headTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2Head").Value;
            Texture2D glowTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2HeadGlow").Value;
            Texture2D jawTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2Jaw").Value;
            Texture2D headTextureAntimatter = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2HeadAntimatter").Value;
            Texture2D glowTextureAntimatter = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2HeadGlowAntimatter").Value;
            Texture2D jawTextureAntimatter = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2JawAntimatter").Value;

            npc.frame = new Rectangle(0, 0, headTexture.Width, headTexture.Height);
            if (npc.Size != headTexture.Size())
                npc.Size = headTexture.Size();

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
