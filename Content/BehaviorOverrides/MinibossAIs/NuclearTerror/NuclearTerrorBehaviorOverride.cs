using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs.OldDuke;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Enemy;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Common.Graphics.ScreenEffects;
using InfernumMode.Common.Worldgen;
using InfernumMode.Core.GlobalInstances;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using NuclearTerrorNPC = CalamityMod.NPCs.AcidRain.NuclearTerror;

namespace InfernumMode.Content.BehaviorOverrides.MinibossAIs.NuclearTerror
{
    public class NuclearTerrorBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<NuclearTerrorNPC>();

        public enum NuclearTerrorAttackType
        {
            // Spawn animation state.
            SpawnAnimation,

            // Phase 1 attacks.
            DownwardSlams,
            GammaRain,

            // Phase 2 transition animation state.
            TransitionToPhase2,

            // Phase 2 attacks.
            InwardGammaBursts,
            NuclearSuperDeathray,

            // Death animation state.
            DeathAnimation
        }

        public enum NuclearTerrorFrameType
        {
            Idle,
            Walking,
            Flashing,
            Dying,
        }

        #region Loading
        public override void Load()
        {
            GlobalNPCOverrides.BossHeadSlotEvent += DisableIconWhenInvisible;
        }

        private void DisableIconWhenInvisible(NPC npc, ref int index)
        {
            if (npc.type == ModContent.NPCType<NuclearTerrorNPC>() && npc.Opacity <= 0.7f)
                index = -1;
        }
        #endregion Loading

        #region AI

        public const float Phase2LifeRatio = 0.5f;

        public const int AttackCycleIndexIndex = 5;

        public override float[] PhaseLifeRatioThresholds => new[]
        {
            Phase2LifeRatio
        };

        public static NuclearTerrorAttackType[] Phase1AttackCycle => new[]
        {
            NuclearTerrorAttackType.DownwardSlams,
            NuclearTerrorAttackType.GammaRain
        };

        public static NuclearTerrorAttackType[] Phase2AttackCycle => new[]
        {
            NuclearTerrorAttackType.InwardGammaBursts,
            NuclearTerrorAttackType.DownwardSlams,
            NuclearTerrorAttackType.NuclearSuperDeathray,
            NuclearTerrorAttackType.DownwardSlams
        };

        public static int GammaEnergyDamage => 300;

        public static int GammaRainDamage => 300;

        public static int GammaDeathrayDamage => 540;

        public override bool PreAI(NPC npc)
        {
            // Pick a target if necessary.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Set variables.
            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float currentAttack = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float currentPhase = ref npc.ai[2];
            ref float acidOverlayInterpolant = ref npc.ai[3];
            ref float frameType = ref npc.localAI[0];
            ref float attackCycleIndex = ref npc.Infernum().ExtraAI[AttackCycleIndexIndex];

            // Set things back to their defaults every frame.
            npc.damage = npc.defDamage;
            npc.dontTakeDamage = false;
            npc.noGravity = false;
            npc.noTileCollide = false;

            // Disable natural despawning.
            npc.timeLeft = 3600;
            npc.Infernum().DisableNaturalDespawning = true;

            // Handle phase transitions.
            if (currentPhase == 0f && lifeRatio <= Phase2LifeRatio)
            {
                SelectNextAttack(npc);
                currentAttack = (int)NuclearTerrorAttackType.TransitionToPhase2;
                currentPhase = 1f;
                attackCycleIndex = 0f;
                npc.netUpdate = true;
            }

            // Die of the Old Duke is present.
            if (currentAttack != (int)NuclearTerrorAttackType.DeathAnimation && NPC.AnyNPCs(ModContent.NPCType<OldDuke>()))
            {
                npc.life = 0;
                npc.checkDead();
            }

            // Perform the current attack.
            switch ((NuclearTerrorAttackType)currentAttack)
            {
                case NuclearTerrorAttackType.SpawnAnimation:
                    DoBehavior_SpawnAnimation(npc, target, ref attackTimer, ref frameType);
                    break;

                case NuclearTerrorAttackType.DownwardSlams:
                    DoBehavior_DownwardSlams(npc, target, currentPhase, ref attackTimer, ref frameType);
                    break;
                case NuclearTerrorAttackType.GammaRain:
                    DoBehavior_GammaRain(npc, target, ref attackTimer, ref frameType);
                    break;

                case NuclearTerrorAttackType.TransitionToPhase2:
                    DoBehavior_TransitionToPhase2(npc, target, ref attackTimer, ref frameType, ref acidOverlayInterpolant);
                    break;

                case NuclearTerrorAttackType.InwardGammaBursts:
                    DoBehavior_InwardGammaBursts(npc, target, ref attackTimer, ref frameType);
                    break;
                case NuclearTerrorAttackType.NuclearSuperDeathray:
                    DoBehavior_NuclearSuperDeathray(npc, target, ref attackTimer, ref frameType);
                    break;

                case NuclearTerrorAttackType.DeathAnimation:
                    DoBehavior_DeathAnimation(npc, ref attackTimer, ref frameType);
                    break;
            }

            // Constantly emit radioactive light.
            if (npc.Opacity >= 0.5f)
            {
                DelegateMethods.v3_1 = Color.Lime.ToVector3();
                Utils.PlotTileLine(npc.Top, npc.Bottom, npc.width, DelegateMethods.CastLight);
            }

            // Increment the attack timer and return.
            attackTimer++;
            return false;
        }

        public static void DoBehavior_SpawnAnimation(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            int waterEmergeDelay = 150;
            int emergeTime = 45;

            // Use idle frames.
            frameType = (int)NuclearTerrorFrameType.Idle;

            // Kill any and all old acid rain enemies. Also use a really large NPC slot value, to prevent new ones from spawning.
            npc.npcSlots = 1000f;
            ClearAcidRainEntities();

            // Disable gravity for this attack.
            npc.noGravity = true;

            // Disable water slowdown physics.
            // This has a several frame buffer to ensure that all clients are informed of this change throughout the duration of the Nuclear Terror's lifetime, as it is not
            // synced by default.
            if (attackTimer <= 10f)
                npc.waterMovementSpeed = 0f;

            // Determine opacity in accordance to the emergence timer.
            npc.Opacity = Sqrt(Utils.GetLerpValue(0f, emergeTime, attackTimer - waterEmergeDelay, true));
            npc.dontTakeDamage = npc.Opacity <= 0.7f;
            npc.Calamity().ShouldCloseHPBar = npc.Opacity <= 0.7f;

            // Find a large pool of water to rise out of on the first frame.
            // If nothing is found, despawn immediately.
            if (attackTimer <= 1f)
            {
                var waterSearchCriterion = Searches.Chain(new Searches.Down(3200), new CustomTileConditions.IsWater());
                for (int i = 0; i < 12000; i++)
                {
                    if (!WorldUtils.Find(new Point((int)target.Top.X / 16 + Main.rand.Next(-100, 100), (int)target.Top.Y / 16), waterSearchCriterion, out Point candidatePosition))
                        break;

                    candidatePosition.Y++;

                    // Teleport to the given teleport position if it fits the water condition.
                    if (IsInLargeWaterPool(candidatePosition) && Distance(candidatePosition.Y * 16f, target.Center.Y) <= 960f)
                    {
                        if (Main.LocalPlayer.WithinRange(candidatePosition.ToWorldCoordinates(), 4800f))
                            SoundEngine.PlaySound(InfernumSoundRegistry.LeviathanRumbleSound with { Volume = 3f });
                        npc.Bottom = candidatePosition.ToWorldCoordinates() + Vector2.UnitY * 250f;
                        npc.velocity = Vector2.Zero;
                        npc.netUpdate = true;
                        return;
                    }
                }

                // The loop was exited without triggering the return statement. That means that no valid teleport position could be located, and as such it is time to despawn.
                npc.active = false;
                return;
            }

            // Create bubble and and acid particles above where the nuclear terror is.
            var surfaceSearchCriterion = Searches.Chain(new Searches.Up(400), new CustomTileConditions.IsAir());
            float particleCreationRateInterpolant = Pow(Utils.GetLerpValue(0f, waterEmergeDelay * 0.8f, attackTimer, true), 0.93f);
            if (!WorldUtils.Find(new Point((int)npc.Center.X / 16, (int)npc.Center.Y / 16), surfaceSearchCriterion, out Point waterSurfacePoint))
            {
                npc.active = false;
                return;
            }

            waterSurfacePoint.Y++;
            Vector2 waterSurface = waterSurfacePoint.ToWorldCoordinates();
            if (particleCreationRateInterpolant > 0.01f)
            {
                // Create acid clouds.
                for (int i = 0; i < 3; i++)
                {
                    if (Main.rand.NextFloat() > particleCreationRateInterpolant * 0.4f)
                        continue;

                    float acidOpacity = 0.56f;
                    Vector2 acidSpawnPosition = waterSurface + new Vector2(Main.rand.NextFloatDirection() * particleCreationRateInterpolant * 132f, Main.rand.NextFloat(8f, 64f));
                    Vector2 acidCloudVelocity = -Vector2.UnitY.RotatedByRandom(0.73f - particleCreationRateInterpolant * 0.65f) * Main.rand.NextFloat(1f, 16f) * particleCreationRateInterpolant;
                    CloudParticle acidCloud = new(acidSpawnPosition, acidCloudVelocity, Color.YellowGreen * acidOpacity, Color.Olive * acidOpacity * 0.7f, 120, Main.rand.NextFloat(1.9f, 2.12f) * particleCreationRateInterpolant);
                    GeneralParticleHandler.SpawnParticle(acidCloud);
                }

                // Create acid bubbles.
                Vector2 bubbleSpawnPosition = waterSurface + Main.rand.NextVector2Circular(particleCreationRateInterpolant * 200f, 240f) + Vector2.UnitY * 160f;
                if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(3) && particleCreationRateInterpolant < 1f)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Gore bubble = Gore.NewGorePerfect(npc.GetSource_FromAI(), bubbleSpawnPosition, -Vector2.UnitY.RotatedByRandom(0.6f) * Main.rand.NextFloat(2f, 13f) + Main.rand.NextVector2Circular(1f, 1f) * 0.75f, 411);
                        bubble.timeLeft = Main.rand.Next(8, 14);
                        bubble.scale = Main.rand.NextFloat(0.5f, 0.5f);
                        bubble.type = Main.rand.NextBool(3) ? 422 : 421;
                    }
                }
            }

            // Play a sound and fly upward as the nuclear terror emerges.
            if (attackTimer == waterEmergeDelay)
            {
                // Play a scary geiger counter spawn sound and water emerge sound.
                if (Main.LocalPlayer.WithinRange(npc.Center, 4800f))
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.AquaticScourgeAppearSound);
                    SoundEngine.PlaySound(NuclearTerrorNPC.SpawnSound);
                }

                // Jump up and look at the target.
                npc.velocity = -Vector2.UnitY.RotatedByRandom(0.23f) * 8f;
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                npc.netUpdate = true;

                // Apply screen effects.
                ScreenEffectSystem.SetBlurEffect(npc.Center, 0.8f, 20);
                target.Infernum_Camera().CurrentScreenShakePower = 9f;

                // Create acid clouds.
                for (int i = 0; i < 20; i++)
                {
                    float acidOpacity = 0.6f;
                    Vector2 acidSpawnPosition = waterSurface + new Vector2(Main.rand.NextFloatDirection() * 132f, Main.rand.NextFloat(8f, 64f));
                    Vector2 acidCloudVelocity = -Vector2.UnitY.RotatedByRandom(0.73f) * Main.rand.NextFloat(1f, 27f);
                    CloudParticle acidCloud = new(acidSpawnPosition, acidCloudVelocity, Color.YellowGreen * acidOpacity, Color.Olive * acidOpacity * 0.7f, 120, Main.rand.NextFloat(1.9f, 2.12f));
                    GeneralParticleHandler.SpawnParticle(acidCloud);
                }
            }

            // Create some manual lightning shortly after emerging.
            if (attackTimer == waterEmergeDelay + 15f)
                Main.NewLightning();

            // Make the camera focus on the water surface.
            float cameraMoveTowardsInterpolant = Utils.GetLerpValue(waterEmergeDelay * 0.5f - 8f, waterEmergeDelay * 0.5f, attackTimer, true);
            float cameraMoveAwayInterpolant = Utils.GetLerpValue(waterEmergeDelay + 12f, waterEmergeDelay + 4f, attackTimer, true);
            float cameraFocusInterpolant = cameraMoveTowardsInterpolant * cameraMoveAwayInterpolant;
            target.Infernum_Camera().ScreenFocusInterpolant = cameraFocusInterpolant;
            target.Infernum_Camera().ScreenFocusPosition = waterSurface;

            // Accelerate. This only applies after the Nuclear Terror has left the water.
            npc.velocity *= 1.022f;

            if (npc.Opacity >= 1f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_DownwardSlams(NPC npc, Player target, float currentPhase, ref float attackTimer, ref float frameType)
        {
            int restTime = 45;
            int slamCount = 3;
            float jumpGravity = 0.38f;
            float minJumpSpeed = 24f;
            float startingSlamSpeed = 3f;
            float maxSlamSpeed = 40f;
            float slamSpeedAcceleration = 2.04f;
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float slamCounter = ref npc.Infernum().ExtraAI[1];

            if (currentPhase >= 1f)
            {
                restTime -= 24;
                slamCount--;
                startingSlamSpeed += 2.8f;
                slamSpeedAcceleration += 0.67f;
            }

            // Use idle frames.
            frameType = (int)NuclearTerrorFrameType.Idle;

            // Disable gravity and natural tile collision by default for the duration of the attack.
            // They will both be applied manually.
            npc.noGravity = true;
            npc.noTileCollide = true;

            switch ((int)attackSubstate)
            {
                // Wait for an opportunity to jump.
                case 0:
                    // Re-activate natural gravity and tile collision.
                    npc.noGravity = false;
                    npc.noTileCollide = false;

                    // Approach the target if far away.
                    float verticalApproachInterpolant = Utils.Remap(target.Center.Y - npc.Center.Y, 950f, 200f, 0.13f, 0.004f);
                    if (npc.Center.Y <= target.Center.Y)
                        npc.Center = Vector2.Lerp(npc.Center, new(npc.Center.X, target.Center.Y), verticalApproachInterpolant);

                    // Check to see if the nuclear terror is on top of tiles. If it is, the jump can begin.
                    // As a failsafe, this also happens if enough time has passed.
                    if (Utilities.ActualSolidCollisionTop(npc.TopLeft, npc.width, npc.height + 32) || attackTimer >= 210f)
                    {
                        attackTimer = 0f;
                        attackSubstate = 1f;

                        // Use a jump sound.
                        SoundEngine.PlaySound(InfernumSoundRegistry.NuclearTerrorJumpSound, target.Center);

                        // Jump up and look at the target.
                        float jumpSpeed = npc.Distance(target.Center) * 0.043f;
                        if (jumpSpeed < minJumpSpeed)
                            jumpSpeed = minJumpSpeed;

                        npc.noGravity = true;
                        npc.noTileCollide = true;

                        npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                        npc.velocity = Utilities.GetProjectilePhysicsFiringVelocity(npc.Center, target.Center - Vector2.UnitY * 200f, jumpGravity, jumpSpeed, out _);
                    }

                    break;

                // Jump and wait until above the target for the slam.
                case 1:
                    // Disable contact damage during the jump, since it's going to be very, very fast and thusly unreasonable to expect the player to react to it.
                    npc.damage = 0;

                    // Horizontally decelerate if approaching the target on the X axis.
                    if (Distance(npc.Center.X, target.Center.X) <= 450f)
                        npc.velocity.X *= 0.987f;

                    // Approach the target.
                    verticalApproachInterpolant = Utils.Remap(target.Center.Y - npc.Center.Y, 950f, 200f, 0.13f, 0.004f);
                    npc.Center = Vector2.Lerp(npc.Center, new(target.Center.X + target.velocity.X * 10f, npc.Center.Y), 0.07f);
                    if (npc.Center.Y <= target.Center.Y)
                        npc.Center = Vector2.Lerp(npc.Center, new(npc.Center.X, target.Center.Y), verticalApproachInterpolant);

                    // Obey gravity.
                    npc.velocity.Y += jumpGravity;

                    // Look at the target.
                    npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

                    // Check to see if the nuclear terror is above the target and slamming down would hit them. If so, the slam can begin.
                    // As a failsafe, this also happens if enough time has passed.
                    bool shouldSlam = (Distance(npc.Center.X, target.Center.X) <= npc.scale * 148f && npc.Bottom.Y < target.Top.Y - 120f) || npc.velocity.Y > 0f;
                    if ((shouldSlam && attackTimer >= 45f) || attackTimer >= 540f)
                    {
                        SoundEngine.PlaySound(InfernumSoundRegistry.NuclearTerrorJumpSound with { Pitch = 0.8f }, target.Center);
                        attackTimer = 0f;
                        attackSubstate = 2f;

                        npc.velocity.Y = startingSlamSpeed;
                        npc.netUpdate = true;
                    }

                    break;

                // Slam downward.
                case 2:
                    // Release energy puffs.
                    if (attackTimer == 5f)
                    {
                        Vector2 puffSpawnPosition = npc.Center + npc.velocity * 5f;
                        CreateAcidPuffInDirection(puffSpawnPosition, new(-0.707f, -0.707f), 13f);
                        CreateAcidPuffInDirection(puffSpawnPosition, new(0.707f, -0.707f), 13f);
                        CreateAcidPuffInDirection(puffSpawnPosition, -Vector2.UnitY, 28f);

                        CreateAcidPuffInDirection(puffSpawnPosition, new(-0.707f, 0.707f), 13f);
                        CreateAcidPuffInDirection(puffSpawnPosition, new(0.707f, 0.707f), 13f);
                    }

                    // Perform acceleration.
                    npc.velocity = (npc.velocity + npc.velocity.SafeNormalize(Vector2.UnitY) * slamSpeedAcceleration).ClampMagnitude(startingSlamSpeed, maxSlamSpeed);

                    // Make horizontal movement exponentially diminish.
                    npc.velocity.X *= 0.85f;

                    // Check if ground was hit. If it was, the slam can end. This only applies if the Nuclear Terror is below the target.
                    // The slam will also naturally end if enough time has passed.
                    bool canInteractWithGround = npc.Bottom.Y >= target.Top.Y + 2f;
                    bool hasHitGround = canInteractWithGround && Utilities.ActualSolidCollisionTop(npc.TopLeft, npc.width, npc.height + 16);
                    if (hasHitGround || attackTimer >= 90f)
                    {
                        SoundEngine.PlaySound(InfernumSoundRegistry.NuclearTerrorGroundSlamSound, target.Center);
                        CreateAcidPuffInDirection(npc.Bottom - Vector2.UnitY * 12f, -Vector2.UnitX, 16f);
                        CreateAcidPuffInDirection(npc.Bottom - Vector2.UnitY * 12f, Vector2.UnitX, 16f);

                        // Create screen shake effects.
                        target.Infernum_Camera().CurrentScreenShakePower = 7.5f;

                        attackTimer = 0f;
                        attackSubstate = 3f;
                        npc.netUpdate = true;
                    }

                    break;

                // Sit in place and wait for a short period of time.
                case 3:
                    // Look at the target.
                    npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

                    // Cease horizontal movement, if there is any.
                    npc.velocity.X *= 0.9f;

                    // Re-activate natural gravity and tile collision.
                    npc.noGravity = false;
                    npc.noTileCollide = false;

                    if (attackTimer >= restTime)
                    {
                        attackTimer = 0f;
                        attackSubstate = 0f;
                        slamCounter++;
                        if (slamCounter >= slamCount)
                            SelectNextAttack(npc);

                        npc.netUpdate = true;
                    }

                    break;
            }
        }

        public static void DoBehavior_GammaRain(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            int gammaRainTime = 90;
            int restTime = 90;
            int gammaRainReleaseRate = 3;
            int energyChargeupTime = GammaBurstLineTelegraph.Lifetime;
            float rainStartingFallSpeed = 7.5f;
            float jumpSpeed = 56f;
            float jumpGravity = 1.256f;
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];

            // Use idle frames.
            frameType = (int)NuclearTerrorFrameType.Idle;

            switch ((int)attackSubstate)
            {
                // Wait in anticipation of the jump.
                case 0:
                    // Teleport below the player horizontally if far enough away from the target that the effect wouldn't be noticed.
                    if (attackTimer == 2f && !npc.WithinRange(target.Center, 840f))
                    {
                        Vector2 groundPosition = Utilities.GetGroundPositionFrom(target.Top);
                        if (!target.WithinRange(groundPosition, 500f))
                        {
                            SoundEngine.PlaySound(InfernumSoundRegistry.NuclearTerrorTeleportSound, target.Center);
                            CreateCircularAcidPuff(npc.Center, 16f);
                            npc.Bottom = groundPosition;
                            npc.netUpdate = true;
                        }
                    }

                    // Create the telegraph on the first frame.
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 2f)
                        Utilities.NewProjectileBetter(npc.Bottom, -Vector2.UnitY, ModContent.ProjectileType<GammaBurstLineTelegraph>(), 0, 0f);

                    // Release energy puffs before the charge.
                    if (Main.rand.NextBool(16))
                        CreateAcidPuffInDirection(npc.Center, -Vector2.UnitY.RotatedByRandom(PiOver2 * 0.8f), 15f);

                    // Jump into the air.
                    if (attackTimer >= energyChargeupTime)
                    {
                        // Use a jump sound.
                        SoundEngine.PlaySound(InfernumSoundRegistry.NuclearTerrorJumpSound, target.Center);

                        attackTimer = 0f;
                        attackSubstate = 1f;
                        npc.velocity = -Vector2.UnitY * jumpSpeed;
                        npc.noGravity = true;
                        npc.noTileCollide = true;
                        npc.netUpdate = true;
                    }

                    break;

                // Jump into the air and slam downward due to gravity.
                case 1:
                    // Disable natural tile collision and gravity effects. Both will be handled manually.
                    npc.noTileCollide = true;
                    npc.noGravity = true;

                    // Enforce gravity.
                    npc.velocity.X *= 0.9f;
                    npc.velocity.Y = Clamp(npc.velocity.Y + jumpGravity, -jumpSpeed, jumpSpeed * 0.67f);

                    // Check if ground was hit. If it was, the slam can end. This only applies if the Nuclear Terror is below the target.
                    // The slam will also naturally end if enough time has passed.
                    bool canInteractWithGround = npc.Bottom.Y >= target.Top.Y + 2f;
                    bool hasHitGround = canInteractWithGround && Utilities.ActualSolidCollisionTop(npc.TopLeft, npc.width, npc.height + 16) && attackTimer >= 4f;
                    if (hasHitGround || attackTimer >= 180f)
                    {
                        SoundEngine.PlaySound(InfernumSoundRegistry.NuclearTerrorGroundSlamSound, target.Center);
                        CreateAcidPuffInDirection(npc.Bottom - Vector2.UnitY * 12f, -Vector2.UnitX, 16f);
                        CreateAcidPuffInDirection(npc.Bottom - Vector2.UnitY * 12f, Vector2.UnitX, 16f);

                        // Create screen shake effects.
                        target.Infernum_Camera().CurrentScreenShakePower = 12f;

                        attackTimer = 0f;
                        attackSubstate = 2f;
                        npc.netUpdate = true;
                    }

                    break;

                // Sit in place and create gamma rain from above, due to the impactful slam.
                case 2:
                    // Cease all horizontal movement.
                    npc.velocity.X = 0f;

                    // Create gamma rain above the player.
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % gammaRainReleaseRate == gammaRainReleaseRate - 1f && attackTimer < gammaRainTime)
                    {
                        Vector2 rainSpawnPosition = target.Center + new Vector2(Main.rand.NextFloatDirection() * 840f, -720f);
                        Utilities.NewProjectileBetter(rainSpawnPosition, Vector2.UnitY * rainStartingFallSpeed, ModContent.ProjectileType<GammaRain>(), GammaRainDamage, 0f);
                    }

                    if (attackTimer >= gammaRainTime + restTime)
                        SelectNextAttack(npc);
                    break;
            }
        }

        public static void DoBehavior_TransitionToPhase2(NPC npc, Player target, ref float attackTimer, ref float frameType, ref float acidOverlayInterpolant)
        {
            int flashTime = 120;

            // Use flashing frames.
            frameType = (int)NuclearTerrorFrameType.Flashing;

            // Disable damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            // Cease all horizontal movement.
            npc.velocity.X *= 0.85f;

            // Make the camera focus on the Nuclear Terror.
            float cameraMoveTowardsInterpolant = Utils.GetLerpValue(0f, 8f, attackTimer, true);
            float cameraMoveAwayInterpolant = Utils.GetLerpValue(flashTime + 36f, flashTime - 28f, attackTimer, true);
            float cameraFocusInterpolant = cameraMoveTowardsInterpolant * cameraMoveAwayInterpolant;
            target.Infernum_Camera().ScreenFocusInterpolant = cameraFocusInterpolant;
            target.Infernum_Camera().ScreenFocusPosition = npc.Center;

            // Roar and use idle frames after done flashing.
            if (attackTimer == flashTime)
            {
                SoundEngine.PlaySound(NuclearTerrorNPC.SpawnSound);
                ScreenEffectSystem.SetBlurEffect(npc.Center, 1f, 60);
                target.Infernum_Camera().CurrentScreenShakePower = 8f;
            }

            // Use idle frames after the flash concludes.
            if (attackTimer >= flashTime)
            {
                frameType = (int)NuclearTerrorFrameType.Idle;
                acidOverlayInterpolant = Clamp(acidOverlayInterpolant + 0.05f, 0f, 1f);

                // Look at the target.
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
            }

            if (attackTimer >= flashTime + 54f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_InwardGammaBursts(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            int inwardProjectileShootTime = 300;
            int chargeUpTime = 120;
            int inwardProjectileShootRate = 6;

            // Use idle frames.
            frameType = (int)NuclearTerrorFrameType.Idle;

            // Cease all horizontal movement and look at the target.
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
            npc.velocity.X *= 0.9f;

            // Create gamma projectiles around the target that converge in on the nuclear terror.
            if (attackTimer <= inwardProjectileShootTime && Main.netMode != NetmodeID.MultiplayerClient && attackTimer % inwardProjectileShootRate == 0f)
            {
                Vector2 energySpawnPosition = target.Center + Main.rand.NextVector2CircularEdge(150f, 150f) * Main.rand.NextFloat(0.9f, 1f) + target.velocity * 90f;
                energySpawnPosition += npc.SafeDirectionTo(target.Center) * 1050f;

                float fireballShootSpeed = npc.Distance(energySpawnPosition) * 0.0043f + 4f;
                float minSpeed = 13f;
                if (fireballShootSpeed < minSpeed)
                    fireballShootSpeed = minSpeed;

                Vector2 energyVelocity = (npc.Center - energySpawnPosition).SafeNormalize(Vector2.UnitY) * fireballShootSpeed;
                while (target.WithinRange(energySpawnPosition, 750f))
                    energySpawnPosition -= energyVelocity;

                Utilities.NewProjectileBetter(energySpawnPosition, energyVelocity, ModContent.ProjectileType<ConvergingGammaEnergy>(), GammaEnergyDamage, 0f, -1, 0f, 1f);

                // The frequency of these projectile firing conditions may be enough to trigger the anti NPC packet spam system that Terraria uses.
                // Consequently, that system is ignored for this specific sync.
                npc.netSpam = 0;
                npc.netUpdate = true;
            }

            // Charge up energy.
            if (attackTimer >= inwardProjectileShootTime && attackTimer <= inwardProjectileShootTime + chargeUpTime - 30f)
            {
                // Create pulse rungs and bloom periodically.
                if (attackTimer % 15f == 0f)
                {
                    SoundEngine.PlaySound(NuclearTerrorNPC.HitSound, target.Center);

                    Color energyColor = Color.Lerp(Color.DarkOliveGreen, Color.YellowGreen, Main.rand.NextFloat(0.6f));
                    PulseRing ring = new(npc.Center, Vector2.Zero, energyColor, 3.6f, 0f, 45);
                    GeneralParticleHandler.SpawnParticle(ring);

                    StrongBloom bloom = new(npc.Center, Vector2.Zero, energyColor, 1f, 15);
                    GeneralParticleHandler.SpawnParticle(bloom);
                }
            }

            // Shoot a barrage of energy at the target.
            if (attackTimer == inwardProjectileShootTime + chargeUpTime)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.NuclearTerrorTeleportSound, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 20; i++)
                    {
                        Vector2 energyVelocity = npc.SafeDirectionTo(target.Center).RotatedByRandom(0.4f) * 13f + Main.rand.NextVector2CircularEdge(4f, 4f);
                        Utilities.NewProjectileBetter(npc.Center + energyVelocity * 3f, energyVelocity, ModContent.ProjectileType<ConvergingGammaEnergy>(), GammaEnergyDamage, 0f, -1);
                    }
                }
            }

            if (attackTimer >= inwardProjectileShootTime + chargeUpTime + 60f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_NuclearSuperDeathray(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            int chargeUpTime = 120;
            int deathrayLifetime = 90;
            ref float deathrayDirection = ref npc.Infernum().ExtraAI[0];
            ref float telegraphInterpolant = ref npc.Infernum().ExtraAI[1];

            // Use flashing frames.
            frameType = (int)NuclearTerrorFrameType.Flashing;

            // Roar on the first frame.
            if (attackTimer == 1f)
                SoundEngine.PlaySound(NuclearTerrorNPC.DeathSound with { Pitch = 0.4f }, target.Center);

            // Perform charge-up behaviors.
            if (attackTimer <= chargeUpTime)
            {
                // Disable damage while charging up.
                npc.damage = 0;
                npc.dontTakeDamage = true;

                // Cease all horizontal movement and look at the target.
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                npc.velocity.X *= 0.9f;

                // Create pulse rungs and bloom periodically.
                if (attackTimer % 15f == 0f)
                {
                    SoundEngine.PlaySound(NuclearTerrorNPC.HitSound, target.Center);

                    Color energyColor = Color.Lerp(Color.DarkOliveGreen, Color.YellowGreen, Main.rand.NextFloat(0.6f));
                    PulseRing ring = new(npc.Center, Vector2.Zero, energyColor, 3.6f, 0f, 45);
                    GeneralParticleHandler.SpawnParticle(ring);

                    StrongBloom bloom = new(npc.Center, Vector2.Zero, energyColor, 1f, 15);
                    GeneralParticleHandler.SpawnParticle(bloom);
                }

                // Define the deathray direction for telegraph purposes.
                if (attackTimer == chargeUpTime - 60f)
                {
                    deathrayDirection = (npc.SafeDirectionTo(target.Center) + Vector2.UnitY * 0.7f).SafeNormalize(Vector2.UnitY).ToRotation();
                    npc.netUpdate = true;
                }

                telegraphInterpolant = Utils.GetLerpValue(chargeUpTime - 60f, chargeUpTime - 5f, attackTimer, true);
            }

            // Fire the deathray and some energy projectiles.
            if (attackTimer == chargeUpTime)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.NuclearTerrorTeleportSound with { Pitch = -0.35f, Volume = 2f }, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Utilities.NewProjectileBetter(npc.Center, deathrayDirection.ToRotationVector2(), ModContent.ProjectileType<GammaSuperDeathray>(), GammaDeathrayDamage, 0f, -1, 0f, deathrayLifetime);

                    for (int i = 0; i < 18; i++)
                    {
                        Vector2 energyVelocity = (TwoPi * i / 18f).ToRotationVector2() * 8f;
                        Utilities.NewProjectileBetter(npc.Center + energyVelocity * 3f, energyVelocity, ModContent.ProjectileType<ConvergingGammaEnergy>(), GammaEnergyDamage, 0f, -1);
                    }

                    // Reset the telegraph interpolant.
                    telegraphInterpolant = 0f;

                    // Look at the target one last time.
                    npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                    npc.netUpdate = true;
                }
            }

            if (attackTimer >= chargeUpTime + deathrayLifetime + 8f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_DeathAnimation(NPC npc, ref float attackTimer, ref float frameType)
        {
            // Cease all horizontal movement.
            npc.velocity.X *= 0.8f;

            // Use death animation frames.
            frameType = (int)NuclearTerrorFrameType.Dying;

            // Disable damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            if (attackTimer >= 30f)
            {
                NuclearTerrorSpawnSystem.WaitingForNuclearTerrorSpawn = false;
                NuclearTerrorSpawnSystem.TotalNuclearTerrorsKilledInEvent++;

                npc.life = 0;
                npc.HitEffect();

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    npc.StrikeInstantKill();

                npc.NPCLoot();
                npc.active = false;
            }
        }

        public static void CreateAcidPuffInDirection(Vector2 puffSpawnPosition, Vector2 puffDirection, float puffMaxSpeed)
        {
            // Create the acid cloud puff.
            for (int i = 0; i < 100; i++)
            {
                float acidOpacity = 0.6f;
                Vector2 acidSpawnPosition = puffSpawnPosition + Main.rand.NextVector2Circular(20f, 20f);
                Vector2 acidCloudVelocity = puffDirection.RotatedByRandom(0.13f) * Main.rand.NextFloat(1f, puffMaxSpeed);
                CloudParticle acidCloud = new(acidSpawnPosition, acidCloudVelocity, Color.YellowGreen * acidOpacity, Color.Olive * acidOpacity * 0.7f, 60, Main.rand.NextFloat(0.9f, 1.22f));
                GeneralParticleHandler.SpawnParticle(acidCloud);
            }
        }

        public static void CreateCircularAcidPuff(Vector2 puffSpawnPosition, float puffMaxSpeed)
        {
            // Create the acid cloud puff.
            for (int i = 0; i < 40; i++)
            {
                float acidOpacity = 0.6f;
                Vector2 acidSpawnPosition = puffSpawnPosition + Main.rand.NextVector2Circular(20f, 20f);
                Vector2 acidCloudVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(puffMaxSpeed * 0.67f, puffMaxSpeed);
                CloudParticle acidCloud = new(acidSpawnPosition, acidCloudVelocity, Color.YellowGreen * acidOpacity, Color.Olive * acidOpacity * 0.7f, 60, Main.rand.NextFloat(1.2f, 1.56f));
                GeneralParticleHandler.SpawnParticle(acidCloud);
            }
        }

        public static bool IsInLargeWaterPool(Point p)
        {
            // Check if a 5x20 rectangle of solely water exists at and below the inputted point.
            // If any of the tiles are not watery enough, this return false immediately.
            for (int dx = -2; dx < 2; dx++)
            {
                for (int dy = 1; dy < 20; dy++)
                {
                    Tile t = CalamityUtils.ParanoidTileRetrieval(p.X + dx, p.Y + dy);
                    if (t.LiquidAmount <= 192)
                        return false;
                }
            }

            return true;
        }

        public static void ClearAcidRainEntities()
        {
            // Kill all enemies.
            List<int> enemyTypes = AcidRainEvent.PossibleEnemiesPolter.Select(e => e.Key).ToList();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (!n.active || !enemyTypes.Contains(n.type))
                    continue;

                n.life = 0;
                n.HitEffect();

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    n.StrikeInstantKill();

                n.active = false;
            }

            // Kill all projectiles.
            int[] projectileTypes = new[]
            {
                ModContent.ProjectileType<CragmawBeam>(),
                ModContent.ProjectileType<CragmawSpike>(),
                ModContent.ProjectileType<CragmawVibeCheckChain>(),
                ModContent.ProjectileType<FlakAcid>(),
                ModContent.ProjectileType<GammaAcid>(),
                ModContent.ProjectileType<GammaBeam>(),
                ModContent.ProjectileType<MaulerAcidBubble>(),
                ModContent.ProjectileType<MaulerAcidDrop>(),
                ModContent.ProjectileType<NuclearToadGoo>(),
                ModContent.ProjectileType<OrthoceraStream>(),
                ModContent.ProjectileType<TrilobiteSpike>()
            };
            for (int i = 0; i < 3; i++)
                Utilities.DeleteAllProjectiles(false, projectileTypes);
        }

        public static void SelectNextAttack(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackCycleIndex = ref npc.Infernum().ExtraAI[AttackCycleIndexIndex];

            if (lifeRatio < Phase2LifeRatio)
                npc.ai[0] = (int)Phase2AttackCycle[(int)attackCycleIndex % Phase2AttackCycle.Length];
            else
                npc.ai[0] = (int)Phase1AttackCycle[(int)attackCycleIndex % Phase1AttackCycle.Length];
            attackCycleIndex++;

            npc.ai[1] = 1f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.netUpdate = true;
        }

        #endregion

        #region Frames and Drawcode

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frameCounter++;
            int currentFrame = npc.frame.Y / frameHeight;

            switch ((NuclearTerrorFrameType)npc.localAI[0])
            {
                case NuclearTerrorFrameType.Walking:
                    int frameChangeRate = 8 - (int)Math.Ceiling(Math.Abs(npc.velocity.X) / 5f);
                    if (npc.frameCounter >= frameChangeRate)
                    {
                        currentFrame++;
                        npc.frame.Y += frameHeight;
                        npc.frameCounter = 0;
                    }

                    if (currentFrame < 4)
                        npc.frame.Y = frameHeight * 4;
                    if (currentFrame >= 8)
                        npc.frame.Y = frameHeight * 4;
                    break;

                case NuclearTerrorFrameType.Idle:
                    if (npc.frameCounter >= 5)
                    {
                        currentFrame++;
                        npc.frame.Y += frameHeight;
                        npc.frameCounter = 0;
                    }

                    if (currentFrame >= 4)
                        npc.frame.Y = 0;
                    break;

                case NuclearTerrorFrameType.Flashing:
                    if (npc.frameCounter >= 7)
                    {
                        currentFrame++;
                        npc.frame.Y += frameHeight;
                        npc.frameCounter = 0;
                    }

                    if (currentFrame < 8)
                        npc.frame.Y = frameHeight * 8;
                    if (currentFrame >= 10)
                        npc.frame.Y = frameHeight * 8;
                    break;

                case NuclearTerrorFrameType.Dying:
                    if (npc.frameCounter >= 7)
                    {
                        currentFrame++;
                        npc.frame.Y += frameHeight;
                        npc.frameCounter = 0;
                    }

                    if (currentFrame < 10)
                        npc.frame.Y = frameHeight * 10;
                    if (currentFrame >= Main.npcFrameCount[npc.type])
                        npc.hide = true;
                    break;
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(npc.ModNPC.Texture).Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            SpriteEffects direction = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            for (int i = npc.oldPos.Length - 1; i >= 0; i--)
            {
                Vector2 localDrawPosition = Vector2.Lerp(npc.oldPos[i] + npc.Size * 0.5f, npc.Center, 0.6f) - Main.screenPosition;
                Color afterimageDrawPosition = npc.GetAlpha(Color.White) * (1f - i / (float)npc.oldPos.Length);
                afterimageDrawPosition.A /= 4;

                Main.spriteBatch.Draw(texture, localDrawPosition, npc.frame, afterimageDrawPosition, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);
            }
            Main.spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);

            // Draw an acid overlay if necessary.
            float acidOverlayInterpolant = npc.ai[3];
            if (acidOverlayInterpolant > 0f)
            {
                Color acidOverlayColor = CalamityUtils.ColorSwap(Color.Yellow, Color.HotPink, 1f) with { A = 0 } * acidOverlayInterpolant * 0.5f;
                Main.spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(acidOverlayColor), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);
            }

            // Draw the laser telegraph if necessary.
            if (npc.ai[0] == (int)NuclearTerrorAttackType.NuclearSuperDeathray && npc.Infernum().ExtraAI[1] > 0.01f)
            {
                Vector2 telegraphDirection = npc.Infernum().ExtraAI[0].ToRotationVector2();

                Main.spriteBatch.SetBlendState(BlendState.Additive);

                float telegraphStrength = npc.Infernum().ExtraAI[1];
                float pulse = Cos(Main.GlobalTimeWrappedHourly * 36f);
                Vector2 laserStart = npc.Center + new Vector2(npc.spriteDirection * -60f, -32f).RotatedBy(npc.rotation);
                Texture2D backglowTexture = ModContent.Request<Texture2D>("CalamityMod/Skies/XerocLight").Value;
                Vector2 origin = backglowTexture.Size() * 0.5f;
                Vector2 glowPosition = laserStart - Main.screenPosition;
                Vector2 baseScale = new Vector2(1f + pulse * 0.05f, 1f) * npc.scale * 0.7f;
                Main.spriteBatch.Draw(backglowTexture, glowPosition, null, Color.White * npc.scale * telegraphStrength, 0f, origin, baseScale * 0.7f, 0, 0f);
                Main.spriteBatch.Draw(backglowTexture, glowPosition, null, Color.Yellow * npc.scale * telegraphStrength * 0.4f, 0f, origin, baseScale * 1.2f, 0, 0f);
                Main.spriteBatch.Draw(backglowTexture, glowPosition, null, Color.Lime * npc.scale * telegraphStrength * 0.3f, 0f, origin, baseScale * 1.7f, 0, 0f);

                Main.spriteBatch.DrawBloomLine(laserStart, laserStart + telegraphDirection * 2400f, Color.DarkOliveGreen * telegraphStrength, telegraphStrength * 120f);
                Main.spriteBatch.DrawBloomLine(laserStart, laserStart + telegraphDirection * 2400f, Color.YellowGreen * telegraphStrength, telegraphStrength * 95f);
                Main.spriteBatch.ResetBlendState();
            }

            return false;
        }
        #endregion Frames and Drawcode

        #region Death Effects
        public override bool CheckDead(NPC npc)
        {
            Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<GammaSuperDeathray>());

            npc.ai[0] = (int)NuclearTerrorAttackType.DeathAnimation;
            npc.ai[1] = 0f;
            npc.netUpdate = true;
            npc.life = npc.lifeMax;
            npc.dontTakeDamage = true;
            SoundEngine.PlaySound(NuclearTerrorNPC.DeathSound);
            return false;
        }
        #endregion Death Effects
    }
}
