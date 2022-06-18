using CalamityMod;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.Ogre
{
    public class OgreBehaviorOverride : NPCBehaviorOverride
    {
        public enum OgreAttackType
        {
            SlowWalk,
            LostKinSlams,
            ChargeRam,
            BouncingSpitballs,
            BouncingSpitballs2
        }

        public override int NPCOverrideType => NPCID.DD2OgreT2;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCFindFrame;

        public override bool PreAI(NPC npc) => DoAI(npc);

        public static bool OnGround(NPC npc, out Vector2 groundPosition)
        {
            bool onGround = false;

            groundPosition = npc.Bottom + Vector2.UnitX * 8f;
            for (int x = (int)(npc.Left.X / 16f); x < (int)(npc.Right.X / 16f); x++)
            {
                Tile tile = CalamityUtils.ParanoidTileRetrieval(x, (int)(groundPosition.Y / 16f));
                if (tile.nactive() && (Main.tileSolid[tile.type] || Main.tileSolidTop[tile.type]))
                    onGround = true;
            }
            return onGround;
        }

        public static bool DoAI(NPC npc)
        {
            // Select a target.
            OldOnesArmyMinibossChanges.TargetClosestMiniboss(npc, true, npc.Infernum().ExtraAI[7] == 1f);
            NPCAimedTarget target = npc.GetTargetData();

            bool isBuffed = npc.type == NPCID.DD2OgreT3;
            bool wasSpawnedInValidContext = npc.Infernum().ExtraAI[5] == 1f || !DD2Event.Ongoing;
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float hasFadedIn = ref npc.ai[2];
            ref float hadGravityDisabled = ref npc.ai[3];
            ref float currentFrame = ref npc.localAI[0];
            ref float shouldUseAfterimages = ref npc.localAI[1];
            ref float fadeInTimer = ref npc.localAI[3];

            // Reset things.
            hadGravityDisabled = npc.noGravity.ToInt();
            npc.damage = npc.defDamage;
            npc.dontTakeDamage = false;
            npc.noGravity = false;
            npc.noTileCollide = false;

            // Despawn if spawned in an incorrect context.
            if (Main.netMode != NetmodeID.MultiplayerClient && !wasSpawnedInValidContext)
                npc.active = false;
            else
            {
                // Clear pickoff enemies.
                OldOnesArmyMinibossChanges.ClearPickoffOOAEnemies();
            }

            // Fade in after appearing from the portal.
            if (fadeInTimer < 60f)
            {
                npc.damage = 0;
                npc.Opacity = Utils.InverseLerp(0f, 48f, fadeInTimer, true);
                npc.dontTakeDamage = npc.Opacity < 0.7f;

                // Create magic dust while fading.
                int dustCount = (int)MathHelper.Lerp(15f, 1f, npc.Opacity);
                for (int i = 0; i < dustCount; i++)
                {
                    if (!Main.rand.NextBool(3))
                        continue;

                    Dust magic = Dust.NewDustDirect(npc.position, npc.width, npc.height, 27, npc.velocity.X * 1f, 0f, 100, default, 1f);
                    magic.scale = 0.55f;
                    magic.fadeIn = 0.7f;
                    magic.velocity *= npc.Size.Length() / 400f;
                    magic.velocity += npc.velocity;
                }
                fadeInTimer++;
                if (fadeInTimer >= 60f)
                    hasFadedIn = 1f;

                if (hasFadedIn == 0f)
                    return false;
            }

            // Move towards the target if very far away from it.
            if (!npc.WithinRange(target.Center, 2400f))
                npc.Center = npc.Center.MoveTowards(target.Center, 15f);

            // Float through walls to reach the target if they cannot be reached or the ogre is currently stuck.
            bool targetOutOfSight = !Collision.CanHit(npc.Center, 0, 0, target.Center, 0, 0) && !npc.WithinRange(target.Center, 200f);
            if ((Collision.SolidCollision(npc.position, npc.width, npc.height) || targetOutOfSight) && hadGravityDisabled == 0f)
            {
                fadeInTimer = 32f;
                shouldUseAfterimages = 0f;
                npc.noGravity = true;
                npc.noTileCollide = true;
                npc.Opacity = MathHelper.Lerp(npc.Opacity, 0.75f, 0f);
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center - Vector2.UnitY * 500f) * 12f, 0.1f);

                // Decide frames.
                npc.frameCounter++;
                if (currentFrame < 43f)
                {
                    npc.frameCounter = 0f;
                    currentFrame = 43f;
                }
                currentFrame = MathHelper.Lerp(43f, 46f, MathHelper.Clamp((float)npc.frameCounter / 36f, 0f, 1f));
                return false;
            }

            shouldUseAfterimages = 0f;
            switch ((OgreAttackType)(int)attackState)
            {
                case OgreAttackType.SlowWalk:
                    DoBehavior_SlowWalk(npc, target, isBuffed, ref attackTimer, ref currentFrame);
                    break;
                case OgreAttackType.LostKinSlams:
                    DoBehavior_LostKinSlams(npc, target, isBuffed, ref attackTimer, ref currentFrame);
                    break;
                case OgreAttackType.ChargeRam:
                    DoBehavior_ChargeRams(npc, target, isBuffed, ref attackTimer, ref shouldUseAfterimages, ref currentFrame);
                    break;
                case OgreAttackType.BouncingSpitballs:
                    DoBehavior_BouncingSpitballs(npc, target, isBuffed, ref attackTimer, ref currentFrame);
                    break;
                case OgreAttackType.BouncingSpitballs2:
                    DoBehavior_BouncingSpitballs2(npc, target, isBuffed, ref attackTimer, ref currentFrame);
                    break;
            }

            // Reset afterimages if they aren't being used.
            if (shouldUseAfterimages == 0f)
                npc.oldPos = new Vector2[npc.oldPos.Length];

            attackTimer++;
            return false;
        }

        public static void DoBehavior_SlowWalk(NPC npc, NPCAimedTarget target, bool isBuffed, ref float attackTimer, ref float currentFrame)
        {
            npc.Infernum().ExtraAI[7] = 0f;

            int walkTime = 270;
            int walkSlowdownTime = 45;
            int jumpPreparationDelay = 24;
            int jumpAnimationTime = 45;
            int jumpSitTime = 15;
            float walkSpeed = 8f;

            if (isBuffed)
            {
                walkTime -= 60;
                walkSpeed += 2.5f;
            }

            bool onGround = OnGround(npc, out Vector2 groundPosition);
            bool shouldSlowDown = attackTimer >= walkTime - walkSlowdownTime;
            bool shouldStopMoving = attackTimer >= walkTime - walkSlowdownTime * 0.5f;
            ref float jumpTimer = ref npc.Infernum().ExtraAI[0];
            ref float hasMadeGroundPound = ref npc.Infernum().ExtraAI[1];

            int closestJumpOffset = -1;
            float jumpAheadSpeedFactor = 0f;
            for (int i = 0; i < 20; i++)
            {
                int x = (int)((npc.spriteDirection == 1 ? npc.Right : npc.Left).X / 16f) + (i + 1) * npc.spriteDirection;
                Tile tile = CalamityUtils.ParanoidTileRetrieval(x, (int)(groundPosition.Y / 16f));
                bool isTileSolid = Main.tileSolid[tile.type] || Main.tileSolidTop[tile.type];
                if (!tile.nactive() || !isTileSolid)
                {
                    if (closestJumpOffset == -1)
                        closestJumpOffset = i;

                    jumpAheadSpeedFactor = i / 9f;
                    break;
                }
            }

            // Handle jumps.
            if (jumpTimer > 0f)
            {
                jumpTimer++;
                if (jumpTimer < jumpPreparationDelay)
                {
                    npc.velocity.X *= 0.92f;
                    currentFrame = MathHelper.Lerp(39f, 42f, jumpTimer / jumpPreparationDelay);
                }

                if (jumpTimer == jumpPreparationDelay)
                {
                    float jumpSpeed = MathHelper.Lerp(6f, 11f, jumpAheadSpeedFactor);
                    npc.velocity.X = npc.spriteDirection * jumpSpeed;
                    npc.velocity.Y = -9f;
                    npc.netUpdate = true;
                }

                if (jumpTimer > jumpPreparationDelay)
                {
                    currentFrame = MathHelper.Lerp(43f, 46f, MathHelper.Clamp((jumpTimer - jumpPreparationDelay) / jumpAnimationTime, 0f, 1f));
                    if (!onGround)
                        npc.position.Y += 2f;
                }
                if (jumpTimer > jumpPreparationDelay + jumpAnimationTime)
                {
                    npc.velocity.X *= 0.84f;

                    // Slam into the ground.
                    if (onGround)
                    {
                        if (hasMadeGroundPound == 0f)
                        {
                            int groundPoundDamage = isBuffed ? 200 : 130;
                            Main.PlaySound(SoundID.DD2_OgreGroundPound, target.Center);
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                                Utilities.NewProjectileBetter(npc.Bottom, Vector2.UnitX * npc.spriteDirection * 6f, ProjectileID.DD2OgreSmash, groundPoundDamage, 0f);
                            hasMadeGroundPound = 1f;
                        }
                        currentFrame = 47f;
                    }
                }
                if (jumpTimer > jumpPreparationDelay + jumpAnimationTime + jumpSitTime)
                {
                    jumpTimer = 0f;
                    npc.netUpdate = true;
                }
                attackTimer--;
                return;
            }
            hasMadeGroundPound = 0f;

            // Walk forward.
            if (onGround && MathHelper.Distance(target.Center.X, npc.Center.X) > 90f && !shouldSlowDown)
            {
                npc.velocity.X = MathHelper.Lerp(npc.velocity.X, npc.SafeDirectionTo(target.Center).X * walkSpeed, 0.05f);
                npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
            }

            // Slow down and stop after walking for long enough.
            if (shouldSlowDown)
                npc.velocity.X *= 0.95f;
            if (shouldStopMoving)
                npc.velocity.X = 0f;
            if (attackTimer >= walkTime)
            {
                npc.Infernum().ExtraAI[6]++;
                npc.Infernum().ExtraAI[7] = npc.Infernum().ExtraAI[7] == 0f ? Main.rand.NextBool(4).ToInt() : 0f;
                switch ((int)npc.Infernum().ExtraAI[6] % 4)
                {
                    case 0:
                        npc.ai[0] = (int)OgreAttackType.LostKinSlams;
                        npc.Infernum().ExtraAI[7] = 0f;
                        break;
                    case 1:
                        npc.ai[0] = (int)OgreAttackType.BouncingSpitballs;
                        break;
                    case 2:
                        npc.ai[0] = (int)OgreAttackType.ChargeRam;
                        if (!isBuffed)
                            npc.ai[0] = (int)OgreAttackType.LostKinSlams;
                        npc.Infernum().ExtraAI[7] = 0f;
                        break;
                    case 3:
                        npc.ai[0] = (int)OgreAttackType.BouncingSpitballs2;
                        break;
                }
                jumpTimer = 0f;
                hasMadeGroundPound = 0f;
                attackTimer = 0f;
                npc.netUpdate = true;
            }

            // Jump to avoid obstacles.
            if (onGround && closestJumpOffset < 3 && jumpAheadSpeedFactor > 0f)
            {
                jumpTimer = 1f;
                npc.netUpdate = true;
            }

            npc.frameCounter += MathHelper.Max(0.5f, Math.Abs(npc.velocity.X) / walkSpeed);
            if (npc.frameCounter >= 4.5f)
            {
                currentFrame++;
                if (currentFrame < 1f || currentFrame >= 11f)
                    currentFrame = 1f;
                npc.frameCounter = 0;
            }
        }

        public static void DoBehavior_LostKinSlams(NPC npc, NPCAimedTarget target, bool isBuffed, ref float attackTimer, ref float currentFrame)
        {
            int jumpPreparationDelay = 32;
            int riseTime = 180;
            int hoverTime = 28;
            int slamTime = 55;
            int slamCount = 3;
            float maxSlamSpeed = 25f;

            if (isBuffed)
            {
                hoverTime -= 5;
                slamTime -= 10;
                maxSlamSpeed += 5f;
            }

            bool canCollideWithThings = OnGround(npc, out _) && npc.Bottom.Y > target.Position.Y;
            ref float hasMadeGroundPound = ref npc.Infernum().ExtraAI[0];
            ref float totalSlams = ref npc.Infernum().ExtraAI[1];

            if (attackTimer < jumpPreparationDelay)
            {
                npc.velocity.X *= 0.92f;
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                currentFrame = MathHelper.Lerp(39f, 42f, attackTimer / jumpPreparationDelay);
            }

            // Grunt before jumping.
            if (attackTimer == jumpPreparationDelay - 15f)
                Main.PlaySound(SoundID.DD2_OgreHurt, npc.Center);

            // Jump.
            if (attackTimer == jumpPreparationDelay)
            {
                npc.velocity = new Vector2(npc.spriteDirection * 9f, -14.5f);
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                npc.netUpdate = true;
            }

            if (attackTimer > jumpPreparationDelay && attackTimer < jumpPreparationDelay + riseTime)
            {
                // Disable tile collision and gravity.
                npc.noGravity = true;
                npc.noTileCollide = true;

                // Disable contact damage.
                npc.damage = 0;

                // Hover into position.
                Vector2 slamHoverDestination = target.Center - Vector2.UnitY * 430f;
                Vector2 idealHoverVelocity = npc.SafeDirectionTo(slamHoverDestination) * 37f;
                if (attackTimer < jumpPreparationDelay + riseTime - hoverTime)
                {
                    currentFrame = 43f;
                    npc.velocity = Vector2.Lerp(npc.velocity, idealHoverVelocity, 0.05f);
                    npc.velocity = npc.velocity.MoveTowards(idealHoverVelocity, 2f);
                    npc.Center = npc.Center.MoveTowards(idealHoverVelocity, 5f);
                }
                else
                {
                    if (MathHelper.Distance(npc.Center.X, target.Center.X) > 30f)
                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                    npc.velocity = npc.velocity.MoveTowards(Vector2.Zero, 2f).ClampMagnitude(0f, 18f) * 0.85f;
                    npc.Center = npc.Center.MoveTowards(slamHoverDestination, 10f);
                    currentFrame = MathHelper.Lerp(43f, 46f, (attackTimer - (jumpPreparationDelay + riseTime - hoverTime)) / hoverTime);
                }

                // Stop in place prior to slamming.
                if (npc.WithinRange(slamHoverDestination, 45f) && attackTimer < jumpPreparationDelay + riseTime - hoverTime)
                {
                    attackTimer = jumpPreparationDelay + riseTime - hoverTime;
                    npc.netUpdate = true;
                }
            }

            if (attackTimer > jumpPreparationDelay + riseTime)
            {
                currentFrame = 47f;
                if (!canCollideWithThings)
                {
                    if (hasMadeGroundPound == 0f)
                        npc.position.Y += MathHelper.Lerp(2f, maxSlamSpeed, Utils.InverseLerp(jumpPreparationDelay + riseTime, jumpPreparationDelay + riseTime + 25f, attackTimer, true));
                    else
                        npc.velocity.Y = 0f;

                    while (Collision.SolidCollision(npc.position, npc.width, npc.height))
                        npc.position.Y--;
                }
                else if (hasMadeGroundPound == 0f)
                {
                    int groundPoundDamage = isBuffed ? 200 : 130;
                    int fireballDamage = groundPoundDamage - 15;
                    int fireballCount = isBuffed ? 10 : 6;
                    Main.PlaySound(SoundID.DD2_OgreGroundPound, target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Utilities.NewProjectileBetter(npc.Bottom, Vector2.UnitX * npc.spriteDirection * 6f, ProjectileID.DD2OgreSmash, groundPoundDamage, 0f);
                        for (int i = -fireballCount; i < fireballCount; i++)
                        {
                            if (Math.Abs(i) <= 1)
                                continue;

                            Vector2 fireballSpawnPosition = npc.Bottom + Vector2.UnitX * i * 5f;
                            Vector2 fireballShootVelocity = new Vector2(i * 2.5f + Main.rand.NextFloatDirection() * 0.8f, Math.Abs(i / (float)fireballCount) * -3f + 13f);
                            Utilities.NewProjectileBetter(fireballSpawnPosition, fireballShootVelocity, ModContent.ProjectileType<RisingFireball>(), fireballDamage, 0f);
                        }
                    }
                    npc.position.Y -= 16f;
                    hasMadeGroundPound = 1f;
                    npc.netUpdate = true;
                }
            }

            if (attackTimer > jumpPreparationDelay + riseTime + slamTime)
            {
                totalSlams++;
                hasMadeGroundPound = 0f;
                attackTimer = 0f;
                if (totalSlams >= slamCount)
                {
                    totalSlams = 0f;
                    npc.ai[0] = (int)OgreAttackType.SlowWalk;
                }

                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_ChargeRams(NPC npc, NPCAimedTarget target, bool isBuffed, ref float attackTimer, ref float shouldUseAfterimages, ref float currentFrame)
        {
            int chargePreparationTime = 42;
            int jumpTime = 25;
            int chargeTime = 32;
            int chargeCount = 4;
            float chargeSpeed = 31f;

            if (isBuffed)
                chargeSpeed += 5f;

            ref float chargeDirection = ref npc.Infernum().ExtraAI[0];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[1];
            ref float hasRoaredAtTarget = ref npc.Infernum().ExtraAI[2];

            // Sit in place and prepare to swing the club.  
            if (attackTimer < chargePreparationTime)
            {
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                if (!OnGround(npc, out _))
                    attackTimer = 0f;
                else
                {
                    npc.velocity.X *= 0.85f;
                    currentFrame = MathHelper.Lerp(11f, 13f, MathHelper.Lerp(attackTimer / chargePreparationTime * 1.3f, 0f, 1f));
                }
            }

            // Jump into the air.
            else if (attackTimer < chargePreparationTime + jumpTime)
            {
                if (attackTimer == chargePreparationTime + (int)(jumpTime * 0.5f))
                    Main.PlaySound(SoundID.DD2_OgreAttack, npc.Center);

                float jumpSpeed = MathHelper.Lerp(-3f, -30f, (attackTimer - chargePreparationTime) / jumpTime);
                jumpSpeed *= Utils.InverseLerp(chargePreparationTime + jumpTime - 5f, chargePreparationTime + jumpTime, attackTimer, true);
                npc.velocity = Vector2.UnitY * jumpSpeed;
                npc.noTileCollide = true;
                npc.noGravity = true;

                currentFrame = 14f;
            }

            // Charge at the target.
            else if (attackTimer < chargePreparationTime + jumpTime + chargeTime)
            {
                if (chargeDirection == 0f)
                {
                    chargeDirection = npc.AngleTo(target.Center);
                    npc.netUpdate = true;
                }
                shouldUseAfterimages = 1f;
                currentFrame = MathHelper.Lerp(15f, 20f, Utils.InverseLerp(chargePreparationTime + jumpTime, chargePreparationTime + jumpTime + chargeTime, attackTimer, true));

                Vector2 idealVelocity = chargeDirection.ToRotationVector2() * chargeSpeed;
                if (idealVelocity.Y < -22f)
                    idealVelocity.Y = -22f;

                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.15f);
                npc.velocity = npc.velocity.MoveTowards(idealVelocity, 1.5f);
                npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
                npc.noTileCollide = true;
                npc.noGravity = true;

                // Play a sound if the target is about to be hit.
                if (npc.WithinRange(target.Center, 120f) && hasRoaredAtTarget == 0f)
                {
                    Main.PlaySound(SoundID.DD2_OgreGroundPound, target.Center);
                    hasRoaredAtTarget = 1f;
                }

                if (Collision.SolidCollision(npc.position, npc.width, npc.height))
                {
                    attackTimer = chargePreparationTime + jumpTime + chargeTime;
                    npc.netUpdate = true;
                }
            }
            else
            {
                npc.velocity.X *= 0.97f;
                npc.position.Y += attackTimer >= chargePreparationTime + jumpTime + chargeTime + 30f ? 24f : 15f;
                while (Collision.SolidCollision(npc.position, npc.width, npc.height))
                    npc.position.Y--;

                if (currentFrame < 43f)
                    currentFrame = 43f;
                currentFrame = MathHelper.Lerp(currentFrame, 46f, 0.18f);
                shouldUseAfterimages = 1f;
                if (attackTimer >= chargePreparationTime + jumpTime + chargeTime + 125f || OnGround(npc, out _))
                {
                    // Create a smash effect.
                    int groundPoundDamage = isBuffed ? 200 : 130;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(npc.Bottom, Vector2.UnitX * npc.spriteDirection * 6f, ProjectileID.DD2OgreSmash, groundPoundDamage, 0f);

                    chargeCounter++;
                    chargeDirection = 0f;
                    attackTimer = 0f;
                    hasRoaredAtTarget = 0f;

                    Main.PlaySound(SoundID.DD2_OgreGroundPound, npc.Center);
                    if (chargeCounter >= chargeCount)
                        npc.ai[0] = (int)OgreAttackType.SlowWalk;
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_BouncingSpitballs(NPC npc, NPCAimedTarget target, bool isBuffed, ref float attackTimer, ref float currentFrame)
        {
            int animationTime = 120;
            int frameToShootOn = 32;
            int totalSpitBalls = 11;
            currentFrame = MathHelper.Lerp(21f, 36f, attackTimer / animationTime);

            // Slow down and look at the target.
            npc.velocity.X *= 0.93f;
            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

            if (attackTimer >= animationTime)
            {
                npc.ai[0] = (int)OgreAttackType.SlowWalk;
                attackTimer = 0f;
                npc.netUpdate = true;
            }

            // Fling the spit balls.
            if (attackTimer % 6f == 1f && (int)currentFrame == frameToShootOn)
            {
                Vector2 spitSpawnPosition = npc.Center + new Vector2(npc.spriteDirection * 30f, -70f);
                Main.PlaySound(SoundID.DD2_OgreSpit, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int spitBallDamage = isBuffed ? 205 : 135;
                    for (int i = 0; i < totalSpitBalls; i++)
                    {
                        Vector2 spitBallShootVelocity = new Vector2((i * 1.34f + 3f) * npc.spriteDirection, -5f) + Main.rand.NextVector2Circular(0.9f, 0.9f);
                        spitBallShootVelocity += npc.SafeDirectionTo(target.Center) * new Vector2(3f, 13f);

                        // Shoot at the target if they're really close.
                        float aimAtTargetInterpolant = Utils.InverseLerp(215f, 175f, npc.Distance(target.Center), true);
                        Vector2 aimedVelocity = (target.Center - spitSpawnPosition).SafeNormalize(Vector2.UnitY) * spitBallShootVelocity.Length() + Main.rand.NextVector2Circular(1.6f, 1.6f);
                        spitBallShootVelocity = Vector2.Lerp(spitBallShootVelocity, aimedVelocity, aimAtTargetInterpolant);

                        Utilities.NewProjectileBetter(spitSpawnPosition, spitBallShootVelocity, ModContent.ProjectileType<BouncingSpitBall>(), spitBallDamage, 0f);
                    }
                }
            }
        }

        public static void DoBehavior_BouncingSpitballs2(NPC npc, NPCAimedTarget target, bool isBuffed, ref float attackTimer, ref float currentFrame)
        {
            int animationTime = 120;
            int startOfAnimationFrame = 32;
            int endOfAnimationFrame = 34;
            currentFrame = MathHelper.Lerp(21f, 36f, attackTimer / animationTime);

            // Slow down and look at the target.
            npc.velocity.X *= 0.93f;
            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

            if (attackTimer >= animationTime)
            {
                npc.ai[0] = (int)OgreAttackType.SlowWalk;
                attackTimer = 0f;
                npc.netUpdate = true;
            }

            // Fling the spit balls.
            float startOfAnimation = Utils.InverseLerp(21f, 36f, startOfAnimationFrame, true) * animationTime;
            float frameBasedTimer = attackTimer % 6f;
            if ((frameBasedTimer == 1f || frameBasedTimer == 5f) && (int)currentFrame >= startOfAnimationFrame && (int)currentFrame <= endOfAnimationFrame)
            {
                float shootOffsetAngle = MathHelper.Lerp(0.84f, 0f, Utils.InverseLerp(startOfAnimation, animationTime, attackTimer, true));
                Vector2 spitSpawnPosition = npc.Center + new Vector2(npc.spriteDirection * 30f, -60f);
                spitSpawnPosition.X += MathHelper.Lerp(0f, 46f, 1f - shootOffsetAngle / 0.84f) * npc.spriteDirection;
                spitSpawnPosition.Y += MathHelper.Lerp(0f, 44f, 1f - shootOffsetAngle / 0.84f);
                Main.PlaySound(SoundID.DD2_OgreSpit, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int spitBallDamage = isBuffed ? 205 : 135;
                    Vector2 spitBallShootVelocity = -Vector2.UnitY.RotatedBy(shootOffsetAngle * npc.spriteDirection) * 10f + Main.rand.NextVector2Circular(0.9f, 0.9f);
                    spitBallShootVelocity += npc.SafeDirectionTo(target.Center) * new Vector2(3f, 11f);

                    // Shoot at the target if they're really close.
                    float aimAtTargetInterpolant = Utils.InverseLerp(215f, 175f, npc.Distance(target.Center), true);
                    Vector2 aimedVelocity = (target.Center - spitSpawnPosition).SafeNormalize(Vector2.UnitY) * spitBallShootVelocity.Length() + Main.rand.NextVector2Circular(1.6f, 1.6f);
                    spitBallShootVelocity = Vector2.Lerp(spitBallShootVelocity, aimedVelocity, aimAtTargetInterpolant);

                    if (isBuffed)
                        spitBallShootVelocity *= 1.25f;

                    Utilities.NewProjectileBetter(spitSpawnPosition, spitBallShootVelocity, ModContent.ProjectileType<BouncingSpitBall>(), spitBallDamage, 0f);
                }
            }
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frame.Width = 80;
            npc.frame.Y = (int)Math.Round(npc.localAI[0]);
        }

        public static bool DoDrawing(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            NPCID.Sets.TrailingMode[npc.type] = 1;
            NPCID.Sets.TrailCacheLength[npc.type] = 4;
            if (npc.oldPos.Length != NPCID.Sets.TrailCacheLength[npc.type])
                npc.oldPos = new Vector2[NPCID.Sets.TrailCacheLength[npc.type]];

            Texture2D texture = Main.npcTexture[npc.type];
            Vector2 baseDrawPosition = npc.Bottom - Main.screenPosition + Vector2.UnitY * npc.gfxOffY;
            Rectangle frame = texture.Frame(5, 10, npc.frame.Y / 10, npc.frame.Y % 10);
            Vector2 origin = frame.Size() * new Vector2(0.5f, 1f);
            SpriteEffects direction = npc.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            origin.Y -= 4f;

            int width = 94;
            if (npc.spriteDirection == 1)
                origin.X = width;
            else
                origin.X = frame.Width - width;

            Color light = Color.White;
            float lightInterpolant = 0f;
            int afterimageCount = 0;
            float afterimageOpacityFactor = 0f;
            Color baseColor = lightColor;

            // Draw afterimages.
            if (npc.localAI[1] == 1f)
            {
                for (int i = 0; i < npc.oldPos.Length; i++)
                {
                    Vector2 drawPosition = Vector2.Lerp(npc.oldPos[i], npc.oldPos[0], 0.5f);
                    drawPosition += npc.Size * new Vector2(0.5f, 1f) - Main.screenPosition + Vector2.UnitY * npc.gfxOffY;
                    Color afterimageColor = new Color(127, 0, 255, 0);
                    spriteBatch.Draw(texture, drawPosition, frame, npc.GetAlpha(afterimageColor), npc.rotation, origin, npc.scale, direction, 0f);
                }
            }

            if (npc.localAI[3] < 60f)
            {
                float fadeToPurple = npc.localAI[3] / 60f;
                afterimageCount = 3;
                afterimageOpacityFactor = 1f - fadeToPurple * fadeToPurple;
                light = new Color(127, 0, 255, 0);
                lightInterpolant = 1f;
                baseColor = Color.Lerp(Color.Transparent, baseColor, fadeToPurple * fadeToPurple);
            }
            for (int i = 0; i < afterimageCount; i++)
            {
                Color afterimageColor = Color.Lerp(lightColor, light, lightInterpolant);
                afterimageColor *= 1f - afterimageOpacityFactor;
                spriteBatch.Draw(texture, baseDrawPosition, frame, afterimageColor, npc.rotation, origin, npc.scale, direction, 0f);
            }
            spriteBatch.Draw(texture, baseDrawPosition, frame, npc.GetAlpha(baseColor), npc.rotation, origin, npc.scale, direction, 0f);
            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor) => DoDrawing(npc, spriteBatch, lightColor);
    }
}
