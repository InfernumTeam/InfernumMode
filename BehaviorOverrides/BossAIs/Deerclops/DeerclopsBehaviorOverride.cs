using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Sounds;
using InfernumMode.OverridingSystem;
using InfernumMode.Sounds;
using InfernumMode.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Deerclops
{
    public class DeerclopsBehaviorOverride : NPCBehaviorOverride
    {
        public enum DeerclopsAttackState
        {
            DecideArena,
            WalkToTarget,
            TallIcicles,
            WideIcicles,
            BidirectionalIcicleSlam,
            UpwardDebrisLaunch,

            TransitionToNextPhase,
            FeastclopsEyeLaserbeam,
            AimedAheadShadowHands,

            DyingBeaconOfLight
        }

        public enum DeerclopsFrameType
        {
            FrontFacingRoar,
            DigIntoGround,
            Walking,
            RaiseArmsUp
        }

        public const float Phase1ArenaWidth = 2000f;

        public const float Phase2LifeRatio = 0.66667f;

        public const float Phase3LifeRatio = 0.3f;

        public override int NPCOverrideType => NPCID.Deerclops;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame | NPCOverrideContext.NPCPreDraw;

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio,
            Phase3LifeRatio
        };

        public override bool PreAI(NPC npc)
        {
            // Select a target as necessary.
            npc.TargetClosestIfTargetIsInvalid();

            Player target = Main.player[npc.target];
            ref float attackTimer = ref npc.ai[1];
            ref float frameType = ref npc.localAI[0];
            ref float shadowFormInterpolant = ref npc.localAI[2];
            ref float shadowFormP3Interpolant = ref npc.Infernum().ExtraAI[8];
            ref float currentPhase = ref npc.Infernum().ExtraAI[6];
            ref float radiusDecreaseInterpolant = ref npc.Infernum().ExtraAI[7];

            // Reset things.
            radiusDecreaseInterpolant = 0f;
            npc.Opacity = 1f;
            npc.noGravity = false;
            npc.noTileCollide = false;
            npc.dontTakeDamage = false;
            npc.chaseable = true;
            npc.Calamity().DR = 0.15f;

            // Transition to the second phase.
            if ((currentPhase == 0f && npc.life < npc.lifeMax * Phase2LifeRatio) ||
                (currentPhase == 1f && npc.life < npc.lifeMax * Phase3LifeRatio))
            {
                // Reset the attack cycle.
                npc.ai[2] = 0f;

                npc.ai[0] = (int)DeerclopsAttackState.TransitionToNextPhase;
                attackTimer = 0f;
                currentPhase++;
                npc.netUpdate = true;
            }

            // Disappear if the player is really far away or dead.
            if (!npc.WithinRange(target.Center, 5600f) || target.dead)
            {
                npc.TargetClosest();
                target = Main.player[npc.target];
                if (!npc.WithinRange(target.Center, 5600f) || target.dead)
                {
                    npc.active = false;
                    return false;
                }
            }

            // Create shadow form dust.
            bool inPhase2 = currentPhase >= 1f;
            bool inPhase3 = currentPhase == 2f;
            if (npc.life > npc.lifeMax * Phase3LifeRatio)
                shadowFormP3Interpolant = 0f;

            float dustInterpolant = Utils.Remap(shadowFormInterpolant, 0f, 0.8333f, 0f, 1f);
            if (dustInterpolant > 0f)
            {
                float dustCount = Main.rand.NextFloat() * dustInterpolant * 3f;
                while (dustCount > 0f)
                {
                    dustCount -= 1f;
                    Dust.NewDustDirect(npc.position, npc.width, npc.height, 109, 0f, -3f, 0, default, 1.4f).noGravity = true;
                }
            }

            // Become invincible in phase 1 if the player leaves the spike area.
            if (!inPhase2)
            {
                float arenaCenterX = npc.Infernum().ExtraAI[9];
                if (target.Center.X < arenaCenterX - Phase1ArenaWidth * 0.5f - 36f)
                    npc.dontTakeDamage = true;
                if (target.Center.X > arenaCenterX + Phase1ArenaWidth * 0.5f + 36f)
                    npc.dontTakeDamage = true;
            }

            switch ((DeerclopsAttackState)npc.ai[0])
            {
                case DeerclopsAttackState.DecideArena:
                    DoBehavior_DecideArena(npc, target, ref attackTimer, ref frameType);
                    break;
                case DeerclopsAttackState.WalkToTarget:
                    DoBehavior_WalkToTarget(npc, target, inPhase3, ref attackTimer, ref frameType);
                    break;
                case DeerclopsAttackState.TallIcicles:
                    DoBehavior_CreateIcicles(npc, target, false, inPhase2, ref attackTimer, ref frameType);
                    break;
                case DeerclopsAttackState.WideIcicles:
                    DoBehavior_CreateIcicles(npc, target, true, inPhase2, ref attackTimer, ref frameType);
                    break;
                case DeerclopsAttackState.BidirectionalIcicleSlam:
                    DoBehavior_BidirectionalIcicleSlam(npc, target, ref attackTimer, ref frameType);
                    break;
                case DeerclopsAttackState.UpwardDebrisLaunch:
                    DoBehavior_UpwardDebrisLaunch(npc, target, inPhase3, ref attackTimer, ref frameType);
                    break;
                case DeerclopsAttackState.TransitionToNextPhase:
                    if (inPhase2)
                        DoBehavior_TransitionToNextPhase(npc, target, ref attackTimer, ref frameType, ref shadowFormInterpolant);
                    else
                        DoBehavior_TransitionToNextPhase(npc, target, ref attackTimer, ref frameType, ref shadowFormP3Interpolant);
                    break;
                case DeerclopsAttackState.FeastclopsEyeLaserbeam:
                    DoBehavior_FeastclopsEyeLaserbeam(npc, target, inPhase3, ref attackTimer, ref frameType);
                    break;
                case DeerclopsAttackState.AimedAheadShadowHands:
                    DoBehavior_AimedAheadShadowHands(npc, target, ref attackTimer, ref frameType);
                    break;
                case DeerclopsAttackState.DyingBeaconOfLight:
                    DoBehavior_DyingBeaconOfLight(npc, target, ref attackTimer, ref frameType, ref radiusDecreaseInterpolant);
                    break;
            }
            attackTimer++;
            return false;
        }

        public static Vector2 GetEyePosition(NPC npc)
        {
            Vector2 result = npc.Center + new Vector2(npc.spriteDirection * 12f, -85f);
            if (npc.frame.Y < 12)
            {
                result.X += npc.spriteDirection * 25f;
                result.Y += 25f;
            }

            return result;
        }

        public static void DoBehavior_DecideArena(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            // Slow down and roar.
            npc.velocity.X *= 0.9f;
            frameType = (int)DeerclopsFrameType.FrontFacingRoar;

            if (attackTimer == 38f)
            {
                npc.Infernum().ExtraAI[9] = target.Center.X;
                npc.netUpdate = true;
                CreateIcicles(target);
            }

            if (attackTimer >= 54f)
                SelectNextAttack(npc);
        }

        public static void DoDefaultWalk(NPC npc, Player target, float walkSpeed, bool haltMovement)
        {
            if (haltMovement)
                npc.velocity.X *= 0.9f;
            else
            {
                npc.velocity.X = MathHelper.Lerp(npc.velocity.X, Math.Sign(target.Center.X - npc.Center.X) * walkSpeed, 0.2f);
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
            }

            Rectangle hitbox = target.Hitbox;
            int horizontalArea = 40;
            int verticalArea = 20;
            Vector2 checkTopLeft = new(npc.Center.X - horizontalArea / 2, npc.position.Y + npc.height - verticalArea);
            bool inHorizontalBounds = checkTopLeft.X < hitbox.X && checkTopLeft.X + npc.width > hitbox.X + hitbox.Width;
            bool inVerticalBounds = checkTopLeft.Y + verticalArea < hitbox.Y + hitbox.Height - 16;
            bool acceptTopSurfaces = npc.Bottom.Y >= hitbox.Top;
            bool riseUpward = Collision.SolidCollision(checkTopLeft, horizontalArea, verticalArea, acceptTopSurfaces);
            bool canCeaseVerticalMovement = Collision.SolidCollision(checkTopLeft, horizontalArea, verticalArea - 4, acceptTopSurfaces);
            bool shouldJump = !Collision.SolidCollision(checkTopLeft + new Vector2(horizontalArea * npc.spriteDirection, 0f), 16, 80, acceptTopSurfaces);
            if ((inHorizontalBounds || haltMovement) & inVerticalBounds)
            {
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + 0.8f, 0.001f, 16f);
                return;
            }
            if (riseUpward && !canCeaseVerticalMovement)
            {
                npc.velocity.Y = 0f;
                return;
            }
            if (riseUpward)
            {
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y - 0.4f, -8f, 0f);
                return;
            }
            if (npc.velocity.Y == 0f & shouldJump)
            {
                npc.velocity.Y = -8f;
                return;
            }
            npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + 0.4f, -8f, 16f);
        }

        public static void DoBehavior_WalkToTarget(NPC npc, Player target, bool inPhase3, ref float attackTimer, ref float frameType)
        {
            int maxWalkTime = 210;
            float walkSpeed = MathHelper.Lerp(4.56f, 7f, 1f - npc.life / (float)npc.lifeMax);
            bool haltMovement = MathHelper.Distance(npc.Center.X, target.Center.X) < 100f;
            if (inPhase3)
            {
                maxWalkTime -= 60;
                walkSpeed += 2f;
            }

            // Slow down and make the attack go by quicker if really close to the target.
            if (haltMovement)
                attackTimer += 5f;

            // Use walking frames.
            frameType = (int)DeerclopsFrameType.Walking;

            // Rest tile collision things.
            npc.noTileCollide = true;

            if (attackTimer >= maxWalkTime)
                SelectNextAttack(npc);
            DoDefaultWalk(npc, target, walkSpeed, haltMovement);
        }

        public static void DoBehavior_CreateIcicles(NPC npc, Player target, bool wideIcicles, bool inPhase2, ref float attackTimer, ref float frameType)
        {
            int spikeShootRate = 2;
            int spikeShootTime = 64;
            int handCreationRate = 0;
            float offsetPerSpike = 35f;
            float minSpikeScale = 0.5f;
            float maxSpikeScale = 1.84f;
            if (wideIcicles)
            {
                offsetPerSpike = 56f;
                minSpikeScale = 0.5f;
                maxSpikeScale = minSpikeScale + 0.01f;
            }

            if (inPhase2)
            {
                offsetPerSpike *= 0.6f;
                spikeShootTime += 8;
                handCreationRate = 24;
            }

            int spikeCount = spikeShootTime / spikeShootRate;
            ref float sendSpikesForward = ref npc.Infernum().ExtraAI[0];

            // Slow down and choose frames.
            npc.velocity.X *= 0.9f;
            frameType = (int)DeerclopsFrameType.DigIntoGround;

            // Choose the current direction.
            if (sendSpikesForward == 0f)
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            if (npc.localAI[1] == 1f && sendSpikesForward == 0f)
            {
                SoundEngine.PlaySound(SoundID.DeerclopsIceAttack with { Volume = 1.9f }, npc.Center);
                sendSpikesForward = 1f;
                npc.netUpdate = true;
            }

            // Don't increment the attack timer until the dig effect has happened.
            bool hitGround = npc.collideY || npc.velocity.Y == 0f;
            if (sendSpikesForward == 0f || !hitGround)
                attackTimer = -1f;
            if (!hitGround)
                frameType = (int)DeerclopsFrameType.RaiseArmsUp;

            Point point = npc.Bottom.ToTileCoordinates();
            point.X += npc.spriteDirection * 3;

            // Create a screen shake on the first frame when ready to shoot.
            if (attackTimer == 1f)
            {
                PunchCameraModifier modifier = new(npc.Center, Vector2.UnitY, 20f, 6f, 30, 1000f, "Deerclops");
                Main.instance.CameraModifiers.Add(modifier);
            }

            // Create spikes.
            int spikeIndex = (int)attackTimer / spikeShootRate;
            if (spikeShootRate <= 1f || attackTimer % spikeShootRate == spikeShootRate - 1f && attackTimer < spikeShootTime)
            {
                float horizontalOffset = spikeIndex * offsetPerSpike;
                float scale = Utils.Remap(attackTimer / spikeShootTime, 0f, 0.75f, minSpikeScale, maxSpikeScale);
                TryMakingSpike(target, ref point, 105, inPhase2, npc.spriteDirection, spikeCount, spikeIndex, horizontalOffset, scale);
            }

            // Summon shadow hands.
            if (Main.netMode != NetmodeID.MultiplayerClient && handCreationRate > 0 && attackTimer % handCreationRate == handCreationRate - 1f)
            {
                float handDirection = Main.rand.NextBool().ToDirectionInt();
                Vector2 handSpawnPosition = target.Center + Vector2.UnitY * handDirection * 640f;
                Utilities.NewProjectileBetter(handSpawnPosition, Vector2.UnitY * handDirection * -7.5f, ModContent.ProjectileType<AcceleratingShadowHand>(), 105, 0f);
            }

            if (attackTimer >= spikeShootTime + 30f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_BidirectionalIcicleSlam(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            int jumpDelay = 42;
            int spikeShootRate = 2;
            int spikeShootTime = 44;
            float offsetPerSpike = 25f;
            float minSpikeScale = 0.8f;
            float maxSpikeScale = 1f;
            bool hitGround = npc.collideY || npc.velocity.Y == 0f;
            ref float jumpState = ref npc.Infernum().ExtraAI[0];

            // Sit in place briefly before jumping.
            if (attackTimer < jumpDelay && jumpState < 2f)
            {
                frameType = (int)DeerclopsFrameType.DigIntoGround;
                npc.velocity.X *= 0.9f;
                return;
            }

            frameType = (int)DeerclopsFrameType.RaiseArmsUp;

            // Jump once ready.
            if (hitGround && jumpState == 0f)
            {
                npc.velocity = Vector2.UnitY * -10f;
                npc.position.Y -= 8f;
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                jumpState = 1f;
                npc.netUpdate = true;
                return;
            }

            // Create hit effects the ground has been hit again.
            if (hitGround && jumpState == 1f)
            {
                SoundEngine.PlaySound(SoundID.DeerclopsIceAttack with { Volume = 1.9f }, npc.Bottom);
                SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact, npc.Bottom);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Utilities.NewProjectileBetter(npc.Bottom, Vector2.UnitX * npc.spriteDirection * 6f, ProjectileID.DD2OgreSmash, 0, 0f);
                    jumpState = 2f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Release spikes once ready again.
            if (jumpState == 2f)
            {
                frameType = (int)DeerclopsFrameType.FrontFacingRoar;

                Point point = npc.Bottom.ToTileCoordinates();
                point.X += npc.spriteDirection * 3;

                // Create spikes.
                int spikeIndex = (int)attackTimer / spikeShootRate;
                int spikeCount = spikeShootTime / spikeShootRate;
                if (spikeShootRate <= 1f || attackTimer % spikeShootRate == spikeShootRate - 1f && attackTimer < spikeShootTime)
                {
                    float horizontalOffset = spikeIndex * offsetPerSpike;
                    float scale = Utils.Remap(attackTimer / spikeShootTime, 0f, 0.75f, minSpikeScale, maxSpikeScale);
                    TryMakingSpike(target, ref point, 105, false, -npc.spriteDirection, spikeCount, spikeIndex, horizontalOffset, scale);
                    TryMakingSpike(target, ref point, 105, false, npc.spriteDirection, spikeCount, spikeIndex, horizontalOffset, scale);
                }

                if (attackTimer >= spikeShootTime + 30f)
                    SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_UpwardDebrisLaunch(NPC npc, Player target, bool inPhase3, ref float attackTimer, ref float frameType)
        {
            int debrisCount = 18;
            int shadowHandCount = 0;
            int attackTransitionDelay = 175;
            float debrisShootSpeed = 13.75f;
            if (inPhase3)
            {
                debrisCount = 0;
                shadowHandCount = 7;
                attackTransitionDelay -= 30;
            }

            ref float readyToShoot = ref npc.Infernum().ExtraAI[0];

            // Slow down and choose frames.
            npc.velocity.X *= 0.9f;
            frameType = (int)DeerclopsFrameType.DigIntoGround;

            // Choose the current direction.
            if (readyToShoot == 0f)
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            if (npc.localAI[1] == 1f && readyToShoot == 0f)
            {
                var sound = shadowHandCount > 0 ? InfernumSoundRegistry.DeerclopsRubbleAttackDistortedSound : SoundID.DeerclopsRubbleAttack;
                SoundEngine.PlaySound(sound, npc.Center);
                readyToShoot = 1f;
                npc.netUpdate = true;
            }

            // Don't increment the attack timer until the dig effect has happened.
            bool hitGround = npc.collideY || npc.velocity.Y == 0f;
            if (readyToShoot == 0f || !hitGround)
                attackTimer = -1f;
            if (!hitGround)
                frameType = (int)DeerclopsFrameType.RaiseArmsUp;

            Point point = npc.Bottom.ToTileCoordinates();
            point.X += npc.spriteDirection * 3;

            // Create a screen shake on the first frame when ready to shoot.
            if (attackTimer == 1f)
            {
                PunchCameraModifier modifier = new(npc.Center, Vector2.UnitY, 20f, 6f, 30, 1000f, "Deerclops");
                Main.instance.CameraModifiers.Add(modifier);
            }

            // Create debris.
            // Shadow hands are launched upwards instead in the third phase.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 2f)
            {
                // Handle debris creation.
                for (int i = 0; i < debrisCount; i++)
                {
                    Vector2 shootDestination = target.Center + Vector2.UnitX * (MathHelper.Lerp(-450f, 450f, i / (float)(debrisCount - 1f)) + Main.rand.NextFloat(10f));
                    Vector2 shootVelocity = Utilities.GetProjectilePhysicsFiringVelocity(npc.Bottom, shootDestination, 0.15f, debrisShootSpeed, out _).RotatedByRandom(0.08f);
                    int debris = Utilities.NewProjectileBetter(npc.Bottom, shootVelocity, ProjectileID.DeerclopsRangedProjectile, 105, 0f);
                    if (Main.projectile.IndexInRange(debris))
                        Main.projectile[debris].ai[1] = Main.rand.Next(6, 12);

                    shootVelocity = Utilities.GetProjectilePhysicsFiringVelocity(npc.Bottom, shootDestination, 0.15f, debrisShootSpeed * 1.45f, out _).RotatedByRandom(0.06f);
                    debris = Utilities.NewProjectileBetter(npc.Bottom, shootVelocity, ProjectileID.DeerclopsRangedProjectile, 105, 0f);
                    if (Main.projectile.IndexInRange(debris))
                        Main.projectile[debris].ai[1] = Main.rand.Next(6, 12);
                }

                // Handle shadow hand creation.
                for (int i = 0; i < shadowHandCount; i++)
                {
                    float shootOffsetAngle = MathHelper.Lerp(-0.71f, 0.71f, i / (float)(shadowHandCount - 1f));
                    Vector2 shootVelocity = -Vector2.UnitY.RotatedBy(shootOffsetAngle) * Main.rand.NextFloat(12.5f, 16f);
                    int hand = Utilities.NewProjectileBetter(npc.Center - shootVelocity * 4f, shootVelocity, ModContent.ProjectileType<SpinningShadowHand>(), 105, 0f);
                    if (Main.projectile.IndexInRange(hand))
                        Main.projectile[hand].ai[1] = 47f;
                }
            }

            if (attackTimer >= attackTransitionDelay)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_TransitionToNextPhase(NPC npc, Player target, ref float attackTimer, ref float frameType, ref float shadowFormInterpolant)
        {
            int fadeToShadowTime = 36;
            int roarTime = 56;

            // Slow down.
            if (attackTimer <= fadeToShadowTime)
            {
                npc.velocity.X *= 0.97f;
                frameType = (int)DeerclopsFrameType.Walking;
                shadowFormInterpolant = attackTimer / fadeToShadowTime;
                return;
            }

            // Roar and create an arena of shadow hands.
            frameType = (int)DeerclopsFrameType.FrontFacingRoar;
            
            if (npc.life > npc.lifeMax * Phase3LifeRatio)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == fadeToShadowTime + roarTime - 27f)
                {
                    ShatterIcicleArena(target);
                    Utilities.NewProjectileBetter(target.Center, Vector2.Zero, ModContent.ProjectileType<ShadowHandArena>(), 100, 0f);
                    Utilities.NewProjectileBetter(npc.Center - Vector2.UnitY * 30f, Vector2.Zero, ModContent.ProjectileType<DeerclopsP2Wave>(), 0, 0f);
                }
            }

            if (attackTimer >= fadeToShadowTime + roarTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_FeastclopsEyeLaserbeam(NPC npc, Player target, bool inPhase3, ref float attackTimer, ref float frameType)
        {
            int eyeChargeTelegraphTime = 48;
            float laserOffsetAngle = npc.spriteDirection * -0.32f;
            Vector2 initialDirection = Vector2.UnitY.RotatedBy(laserOffsetAngle);
            float maxLaserAngle = MathHelper.PiOver2 * 1.34f;
            if (inPhase3)
                maxLaserAngle *= 1.3f;

            float laserSweepSpeed = (maxLaserAngle - Math.Abs(laserOffsetAngle)) * -npc.spriteDirection / DeerclopsEyeLaserbeam.LaserLifetime;

            // Slow down horizontally.
            npc.velocity *= 0.95f;

            // Create telegraph particles at the eye prior to firing.
            if (attackTimer < eyeChargeTelegraphTime)
            {
                Vector2 eyePosition = GetEyePosition(npc);
                Vector2 lightSpawnPosition = eyePosition + Main.rand.NextVector2Circular(64f, 128f);
                Vector2 lightSpawnVelocity = (eyePosition - lightSpawnPosition) * 0.1f;
                SquishyLightParticle light = new(lightSpawnPosition, lightSpawnVelocity, 1.25f, Color.Red, 20, 1f, 4f);
                GeneralParticleHandler.SpawnParticle(light);
                frameType = (int)DeerclopsFrameType.Walking;

                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                return;
            }

            frameType = (int)DeerclopsFrameType.RaiseArmsUp;

            if (attackTimer == eyeChargeTelegraphTime + 24f)
            {
                SoundEngine.PlaySound(SoundID.DeerclopsScream, npc.Center);
                SoundEngine.PlaySound(InfernumSoundRegistry.CalThunderStrikeSound, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 eyePosition = GetEyePosition(npc);
                    int laser = Utilities.NewProjectileBetter(eyePosition, initialDirection, ModContent.ProjectileType<DeerclopsEyeLaserbeam>(), 125, 0f);
                    if (Main.projectile.IndexInRange(laser))
                    {
                        Main.projectile[laser].ai[0] = npc.whoAmI;
                        Main.projectile[laser].ai[1] = laserSweepSpeed;
                    }
                }
            }

            if (attackTimer >= eyeChargeTelegraphTime + DeerclopsEyeLaserbeam.LaserLifetime + 45f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_AimedAheadShadowHands(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            int handSummonRate = 75;
            int handSummonCycleCount = 2;
            int totalHandsToSummon = 5;
            float handSpawnOffset = 450f;
            bool haltMovement = MathHelper.Distance(npc.Center.X, target.Center.X) < 100f;

            // Use walking frames.
            frameType = (int)DeerclopsFrameType.Walking;

            // Rest tile collision things.
            npc.noTileCollide = true;

            // Create hands.
            if (attackTimer % handSummonRate == (int)(handSummonRate / 3f))
            {
                SoundEngine.PlaySound(SoundID.DeerclopsScream, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 handSpawnOffsetDirection = -(target.velocity * new Vector2(0.2f, 1f)).SafeNormalize(Main.rand.NextVector2Unit());
                    handSpawnOffsetDirection = (handSpawnOffsetDirection - Vector2.UnitY * 0.52f).SafeNormalize(Vector2.UnitY);

                    for (int i = 0; i < totalHandsToSummon; i++)
                    {
                        float spawnOffsetAngle = MathHelper.Lerp(-1.26f, 1.26f, i / (float)(totalHandsToSummon - 1f));
                        Vector2 handSpawnPosition = target.Center + handSpawnOffsetDirection.RotatedBy(spawnOffsetAngle) * handSpawnOffset;
                        Vector2 handSpawnVelocity = (target.Center - handSpawnPosition).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.3f) * -Main.rand.NextFloat(7f, 10f);
                        Utilities.NewProjectileBetter(handSpawnPosition, handSpawnVelocity, ModContent.ProjectileType<SpinningShadowHand>(), 105, 0f);
                    }
                }
            }
            
            if (attackTimer >= (handSummonCycleCount + 0.24f) * handSummonRate)
                SelectNextAttack(npc);
            DoDefaultWalk(npc, target, 5f, haltMovement);
        }

        public static void DoBehavior_DyingBeaconOfLight(NPC npc, Player target, ref float attackTimer, ref float frameType, ref float radiusDecreaseInterpolant)
        {
            int darknessFadeTime = 125;
            int handSummonRate = 120;
            int maxHandCount = 6;
            int attackTime = 660;
            float maxNaturalRadiusDecreaseInterpolant = 0.25f;
            float maxRadiusDecreaseInterpolant = 0.8f;
            ref float smoothDistance = ref npc.Infernum().ExtraAI[0];

            npc.velocity.X *= 0.9f;
            frameType = (int)DeerclopsFrameType.RaiseArmsUp;

            // Make the darkness grow.
            if (attackTimer <= darknessFadeTime)
            {
                radiusDecreaseInterpolant = MathHelper.SmoothStep(0f, maxNaturalRadiusDecreaseInterpolant, attackTimer / darknessFadeTime);
                return;
            }

            npc.chaseable = false;
            npc.Calamity().DR = 0.6f;

            // Make the radius decrease as more hands congregate near the eye.
            // To make the attack better than a simple DPS check the hands will target nearby players if close to deerclops.
            int handID = ModContent.NPCType<LightSnuffingHand>();
            float inverseCoveredDistance = 0f;
            NPC[] nearbyHands = Main.npc.Take(Main.maxNPCs).Where(n => n.active && n.type == handID && Vector2.Distance(GetEyePosition(npc), n.Center) < 240f).ToArray();
            inverseCoveredDistance = nearbyHands.Sum(n => 240f - Vector2.Distance(GetEyePosition(npc), n.Center));
            smoothDistance = MathHelper.Lerp(smoothDistance, inverseCoveredDistance, 0.08f);

            // Fade in and out as necessary.
            float fadeInterpolant = Utils.GetLerpValue(180f, 850f, smoothDistance, true);
            radiusDecreaseInterpolant = MathHelper.Lerp(maxNaturalRadiusDecreaseInterpolant, maxRadiusDecreaseInterpolant, fadeInterpolant);
            npc.Opacity = MathHelper.Lerp(1f, 0.36f, fadeInterpolant);

            bool summonInterval = attackTimer % handSummonRate == handSummonRate - 1f || attackTimer == darknessFadeTime + 1f;
            if (NPC.CountNPCS(handID) < maxHandCount && summonInterval && attackTimer < attackTime)
            {
                Vector2 handSummonPosition = target.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(335f, 400f);
                SoundEngine.PlaySound(SoundID.DD2_EtherianPortalSpawnEnemy, handSummonPosition);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NPC.NewNPC(npc.GetSource_FromAI(), (int)handSummonPosition.X, (int)handSummonPosition.Y, handID, npc.whoAmI);
            }

            if (attackTimer >= attackTime && !NPC.AnyNPCs(handID))
                SelectNextAttack(npc);
        }

        public static void TryMakingSpike(Player target, ref Point sourceTileCoords, int damage, bool shadow, int dir, int spikeCount, int spikeIndex, float horizontalOffset, float scale)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int spikeID = ModContent.ProjectileType<GroundIcicleSpike>();
            int x = sourceTileCoords.X + (int)(horizontalOffset / 16f * dir);
            int y = TryMakingSpike_FindBestY(target, ref sourceTileCoords, x);
            if (!WorldGen.ActiveAndWalkableTile(x, y))
                return;

            Vector2 position = new(x * 16 + 8, y * 16 - scale * 80f);
            Vector2 velocity = -Vector2.UnitY.RotatedBy(spikeIndex / (float)spikeCount * dir * MathHelper.Pi * 0.175f);
            int spike = Utilities.NewProjectileBetter(position, velocity, spikeID, damage, 0f, Main.myPlayer, 0f);
            if (Main.projectile.IndexInRange(spike))
            {
                Main.projectile[spike].ai[1] = scale;
                Main.projectile[spike].localAI[1] = shadow.ToInt();
            }
        }

        public static int TryMakingSpike_FindBestY(Player target, ref Point sourceTileCoords, int x)
        {
            int bestY = sourceTileCoords.Y;
            if (!target.dead && target.active)
            {
                int targetTileBottom = (int)(target.Bottom.Y / 16f);
                int bestYPlayerDistance = Math.Sign((int)(target.Bottom.Y / 16f) - bestY);
                int soughtY = targetTileBottom + bestYPlayerDistance * 15;
                int? result = null;
                float bestScore = float.PositiveInfinity;
                for (int y = bestY; y != soughtY; y += bestYPlayerDistance)
                {
                    if (WorldGen.ActiveAndWalkableTile(x, y))
                    {
                        float score = new Point(x, y).ToWorldCoordinates().Distance(target.Bottom);
                        if (!result.HasValue || score < bestScore)
                        {
                            result = y;
                            bestScore = score;
                        }
                    }
                }
                if (result.HasValue)
                    bestY = result.Value;
            }
            int tries = 0;
            while (tries < 20 && bestY >= 10 && WorldGen.SolidTile(x, bestY))
            {
                bestY--;
                tries++;
            }
            tries = 0;
            while (tries < 20 && bestY <= Main.maxTilesY - 10 && !WorldGen.ActiveAndWalkableTile(x, bestY))
            {
                bestY++;
                tries++;
            }
            return bestY;
        }

        public static void SelectNextAttack(NPC npc)
        {
            DeerclopsAttackState[] phase1Pattern = new DeerclopsAttackState[]
            {
                DeerclopsAttackState.TallIcicles,
                DeerclopsAttackState.WalkToTarget,
                DeerclopsAttackState.WideIcicles,
                DeerclopsAttackState.WalkToTarget,
                DeerclopsAttackState.BidirectionalIcicleSlam,
                DeerclopsAttackState.UpwardDebrisLaunch,
                DeerclopsAttackState.WalkToTarget,
            };
            DeerclopsAttackState[] phase2Pattern = new DeerclopsAttackState[]
            {
                DeerclopsAttackState.FeastclopsEyeLaserbeam,
                DeerclopsAttackState.AimedAheadShadowHands,
                DeerclopsAttackState.WideIcicles,
                DeerclopsAttackState.WalkToTarget,
                DeerclopsAttackState.WalkToTarget,
                DeerclopsAttackState.WideIcicles,
            };
            DeerclopsAttackState[] phase3Pattern = new DeerclopsAttackState[]
            {
                DeerclopsAttackState.DyingBeaconOfLight,
                DeerclopsAttackState.FeastclopsEyeLaserbeam,
                DeerclopsAttackState.AimedAheadShadowHands,
                DeerclopsAttackState.WideIcicles,
                DeerclopsAttackState.WalkToTarget,
                DeerclopsAttackState.DyingBeaconOfLight,
                DeerclopsAttackState.WalkToTarget,
                DeerclopsAttackState.BidirectionalIcicleSlam,
                DeerclopsAttackState.UpwardDebrisLaunch,
                DeerclopsAttackState.WalkToTarget,
                DeerclopsAttackState.WideIcicles,
            };
            var patternToUse = phase1Pattern;
            if (npc.life < npc.lifeMax * Phase2LifeRatio)
                patternToUse = phase2Pattern;
            if (npc.life < npc.lifeMax * Phase3LifeRatio)
                patternToUse = phase3Pattern;

            npc.ai[0] = (int)patternToUse[(int)npc.ai[2] % patternToUse.Length];
            npc.localAI[0] = 0f;
            npc.localAI[1] = 0f;
            npc.ai[1] = 0f;
            npc.ai[2]++;

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.netUpdate = true;
        }

        public static void ShatterIcicleArena(Player target)
        {
            SoundEngine.PlaySound(SoundID.DD2_WitherBeastDeath, target.Center);
            Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<ArenaIcicle>());
        }

        public static void CreateIcicles(Player target)
        {
            SoundEngine.PlaySound(CommonCalamitySounds.MeatySlashSound, target.Center);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 leftIciclePosition = Utilities.GetGroundPositionFrom(target.Center - Vector2.UnitX * Phase1ArenaWidth * 0.5f) + Vector2.UnitY * 24f;
                Vector2 rightIciclePosition = Utilities.GetGroundPositionFrom(target.Center + Vector2.UnitX * Phase1ArenaWidth * 0.5f) + Vector2.UnitY * 24f;
                Utilities.NewProjectileBetter(leftIciclePosition, Vector2.Zero, ModContent.ProjectileType<ArenaIcicle>(), 160, 0f);
                Utilities.NewProjectileBetter(rightIciclePosition, Vector2.Zero, ModContent.ProjectileType<ArenaIcicle>(), 160, 0f);
            }
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frame.Width = 218;
            npc.frame.Height = 240;
            int frame = npc.frame.Y;

            npc.localAI[1] = 0f;

            switch ((DeerclopsFrameType)npc.localAI[0])
            {
                case DeerclopsFrameType.FrontFacingRoar:
                    if (frame < 19)
                        frame = 19;
                    if (npc.frameCounter >= 5D)
                    {
                        frame++;
                        // Roar if the last frame has been reached before the frame loop.
                        if (frame == 24)
                            SoundEngine.PlaySound(SoundID.DeerclopsScream, npc.Center);

                        npc.frameCounter = 0D;
                    }

                    if (frame >= 25)
                        frame = 24;
                    break;
                case DeerclopsFrameType.DigIntoGround:
                    if (frame is < 12 or >= 19)
                        frame = 12;
                    if (npc.frameCounter >= 7D && frame < 18)
                    {
                        frame++;
                        if (frame == 17)
                            npc.localAI[1] = 1f;

                        npc.frameCounter = 0D;
                    }
                    break;
                case DeerclopsFrameType.Walking:
                    if (frame is >= 12 or < 2)
                        frame = 2;
                    if (npc.frameCounter >= 6D)
                    {
                        frame++;
                        if (frame >= 12)
                            frame = 2;

                        npc.frameCounter = 0D;
                    }

                    npc.frameCounter += Math.Abs(npc.velocity.X) * 0.1333f - 0.8f;
                    break;
                case DeerclopsFrameType.RaiseArmsUp:
                    frame = 13;
                    break;
            }

            npc.frame.Y = frame;
            npc.frameCounter++;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D tex = TextureAssets.Npc[npc.type].Value;
            SpriteEffects direction = npc.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Vector2 baseDrawPosition = npc.Bottom - Main.screenPosition;
            Rectangle frame = tex.Frame(5, 5, npc.frame.Y / 5, npc.frame.Y % 5, 2, 2);
            Vector2 origin = frame.Size() * new Vector2(0.5f, 1f);
            origin.Y -= 4f;
            if (npc.spriteDirection == 1)
                origin.X = 106;
            else
                origin.X = frame.Width - 106;

            Color shadowColor = Color.White;
            int maxTrailCount = 12;
            float opacityAffectedFadeToShadow = 0f;
            float forcedFadeToShadow = 0f;
            int shadowBackglowCount = 0;
            float offsetInterpolant = 0f;
            float shadowOffset = 0f;
            float shadowFormInterpolant = npc.localAI[2];
            float strongerShadowsInterpolant = npc.Infernum().ExtraAI[8];
            int trailCount = (int)(strongerShadowsInterpolant * maxTrailCount);
            Color baseColor = lightColor;
            if (shadowFormInterpolant > 0f)
            {
                shadowBackglowCount = 2;
                offsetInterpolant = (float)Math.Pow(shadowFormInterpolant, 2D);
                shadowOffset = 20f;
                shadowColor = new Color(80, 0, 0, 255) * npc.Opacity * 0.5f;
                forcedFadeToShadow = 1f;
                baseColor = Color.Lerp(Color.Transparent, baseColor, 1f - offsetInterpolant);
            }
            for (int i = 0; i < shadowBackglowCount; i++)
            {
                Color c = npc.GetAlpha(Color.Lerp(lightColor, shadowColor, opacityAffectedFadeToShadow));
                c = Color.Lerp(c, shadowColor, forcedFadeToShadow) * (1f - offsetInterpolant * 0.5f);
                Vector2 shadowDrawOffset = Vector2.UnitY.RotatedBy(i * MathHelper.TwoPi / shadowBackglowCount + Main.GlobalTimeWrappedHourly * 10f) * offsetInterpolant * shadowOffset;
                Main.spriteBatch.Draw(tex, baseDrawPosition + shadowDrawOffset, frame, c, npc.rotation, origin, npc.scale, direction, 0f);
            }
            Color opacityAffectedColor = npc.GetAlpha(baseColor);
            if (shadowFormInterpolant > 0f)
            {
                Color result = new Color(50, 0, 160) * npc.Opacity;
                result.A = (byte)((1f - strongerShadowsInterpolant) * 75 + 180);
                opacityAffectedColor = Color.Lerp(opacityAffectedColor, result, Utils.Remap(shadowFormInterpolant, 0f, 0.5555f, 0f, 1f));
            }

            // Redefine trails.
            NPCID.Sets.TrailCacheLength[npc.type] = maxTrailCount;
            NPCID.Sets.TrailingMode[npc.type] = 0;

            if (trailCount > 1)
            {
                for (int i = 1; i < trailCount; i++)
                {
                    float shadowFade = 1f - i / (float)(trailCount - 1f);
                    Vector2 drawPosition = npc.oldPos[i] + new Vector2(npc.width * 0.5f, npc.height) - Main.screenPosition;
                    Color fadeColor = opacityAffectedColor * shadowFade;
                    fadeColor.A = (byte)((1f - strongerShadowsInterpolant) * 255);
                    Main.spriteBatch.Draw(tex, drawPosition, frame, fadeColor, npc.rotation, origin, npc.scale, direction, 0f);
                }
            }
            Main.spriteBatch.Draw(tex, baseDrawPosition, frame, opacityAffectedColor, npc.rotation, origin, npc.scale, direction, 0f);

            if (shadowFormInterpolant > 0f)
            {
                Texture2D eyeTexture = TextureAssets.Extra[245].Value;
                float scale = Utils.Remap(shadowFormInterpolant, 0f, 0.5555f, 0f, 1f, true);
                Color eyeColor = new Color(255, 30, 30, 66) * npc.Opacity * scale * 0.25f;
                for (int j = 0; j < shadowBackglowCount; j++)
                {
                    Vector2 eyeDrawPosition = baseDrawPosition + Vector2.UnitY.RotatedBy(j * MathHelper.TwoPi / shadowBackglowCount + Main.GlobalTimeWrappedHourly * 10f) * offsetInterpolant * 4f;
                    Main.spriteBatch.Draw(eyeTexture, eyeDrawPosition, frame, eyeColor, npc.rotation, origin, npc.scale, direction, 0f);
                }
            }
            return false;
        }

        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Deerclops' are Myopic, so they will force you to stay close, dont let them corner you!";
            yield return n => "The Deerclops will follow a set pattern, learn it to gain the upper hand!";
            yield return n =>
            {
                if (HatGirlTipsManager.ShouldUseJokeText)
                    return "Deer god...";
                return string.Empty;
            };
        }
    }
}
