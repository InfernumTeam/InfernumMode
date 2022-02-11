using CalamityMod;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.Ogre
{
    public class OgreBehaviorOverride : NPCBehaviorOverride
    {
        public enum OgreAttackType
        {
            SlowWalk,
            LostKinSlams
        }

        public override int NPCOverrideType => NPCID.DD2OgreT2;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame;

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
            OldOnesArmyMinibossChanges.TargetClosestMiniboss(npc);
            NPCAimedTarget target = npc.GetTargetData();

            bool isBuffed = npc.type == NPCID.DD2OgreT3;
            bool wasSpawnedInValidContext = npc.Infernum().ExtraAI[5] == 1f;
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float hasFadedIn = ref npc.ai[2];
            ref float currentFrame = ref npc.localAI[0];
            ref float fadeInTimer = ref npc.localAI[3];

            // Reset things.
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

            // Float through walls to reach the target if they cannot be reached or the ogre is currently stuck.
            bool targetOutOfSight = !Collision.CanHit(npc.Center, 0, 0, target.Center, 0, 0) && !npc.WithinRange(target.Center, 300f);
            if (Collision.SolidCollision(npc.position, npc.width, npc.height) || targetOutOfSight)
            {
                fadeInTimer = 32f;
                npc.noGravity = true;
                npc.noTileCollide = true;
                npc.Opacity = MathHelper.Lerp(npc.Opacity, 0.75f, 0f);
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 12f, 0.02f);

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

            switch ((OgreAttackType)(int)attackState)
            {
                case OgreAttackType.SlowWalk:
                    DoBehavior_SlowWalk(npc, target, isBuffed, ref attackTimer, ref currentFrame);
                    break;
                case OgreAttackType.LostKinSlams:
                    DoBehavior_LostKinSlams(npc, target, isBuffed, ref attackTimer, ref currentFrame);
                    break;
            }

            attackTimer++;
            return false;
        }

        public static void DoBehavior_SlowWalk(NPC npc, NPCAimedTarget target, bool isBuffed, ref float attackTimer, ref float currentFrame)
        {
            int walkTime = 270;
            int walkSlowdownTime = 45;
            int jumpPreparationDelay = 24;
            int jumpAnimationTime = 45;
            int jumpSitTime = 15;
            float walkSpeed = 8f;
            bool shouldSlowDown = attackTimer >= walkTime - walkSlowdownTime;
            bool shouldStopMoving = attackTimer >= walkTime - walkSlowdownTime * 0.5f;
            ref float jumpTimer = ref npc.Infernum().ExtraAI[0];
            ref float hasMadeGroundPound = ref npc.Infernum().ExtraAI[1];

            bool onGround = OnGround(npc, out Vector2 groundPosition);
            float jumpAheadSpeedFactor = 0f;
            int closestJumpOffset = -1;
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
                npc.ai[0] = (int)OgreAttackType.LostKinSlams;
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
            int hoverTime = 23;
            int slamTime = 55;
            bool canCollideWithThings = OnGround(npc, out _) && npc.Bottom.Y > target.Position.Y;
            ref float hasMadeGroundPound = ref npc.Infernum().ExtraAI[0];

            if (attackTimer < jumpPreparationDelay)
            {
                npc.velocity.X *= 0.92f;
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                currentFrame = MathHelper.Lerp(39f, 42f, attackTimer / jumpPreparationDelay);
            }

            // Grunt before jumping.
            if (attackTimer == jumpPreparationDelay - 15f)
                Main.PlaySound(SoundID.DD2_OgreHurt, target.Center);

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
                        npc.position.Y += MathHelper.Lerp(2f, 25f, Utils.InverseLerp(jumpPreparationDelay + riseTime, jumpPreparationDelay + riseTime + 25f, attackTimer, true));
                    else
                        npc.velocity.Y = 0f;

                    while (Collision.SolidCollision(npc.position, npc.width, npc.height))
                        npc.position.Y--;
                }
                else if (hasMadeGroundPound == 0f)
                {
                    int groundPoundDamage = isBuffed ? 200 : 130;
                    int fireballDamage = groundPoundDamage - 15;
                    Main.PlaySound(SoundID.DD2_OgreGroundPound, target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Utilities.NewProjectileBetter(npc.Bottom, Vector2.UnitX * npc.spriteDirection * 6f, ProjectileID.DD2OgreSmash, groundPoundDamage, 0f);
                        for (int i = -10; i < 10; i++)
                        {
                            if (Math.Abs(i) <= 1)
                                continue;

                            Vector2 fireballSpawnPosition = npc.Bottom + Vector2.UnitX * i * 5f;
                            Vector2 fireballShootVelocity = new Vector2(i * 2.5f + Main.rand.NextFloatDirection() * 0.8f, Math.Abs(i / 10f) * -3f + 13f);
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
                hasMadeGroundPound = 0f;
                attackTimer = 0f;
            }
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frame.Width = 80;
            npc.frame.Y = (int)Math.Round(npc.localAI[0]);
        }
    }
}
