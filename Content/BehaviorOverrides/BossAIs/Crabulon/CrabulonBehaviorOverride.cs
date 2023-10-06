using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs.Crabulon;
using CalamityMod.Projectiles.Boss;
using InfernumMode.Assets.Effects;
using InfernumMode.Common.Graphics.Primitives;
using InfernumMode.Common.Worldgen;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using CrabulonNPC = CalamityMod.NPCs.Crabulon.Crabulon;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Crabulon
{
    public class CrabulonBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<CrabulonNPC>();

        #region Enumerations
        public enum CrabulonAttackState
        {
            SpawnWait,
            JumpToTarget,
            WalkToTarget,
            CreateGroundMushrooms,
            ClawSlamMushroomWaves
        }
        #endregion

        #region AI

        public static int MushroomBombDamage => 70;

        public static int SporeCloudDamage => 75;

        public static int MushroomPillarDamage => 80;

        public const int MushroomStompBarrageInterval = 3;

        public const int UsingDetachedHandsFlagIndex = 5;

        public const int DetachedHandOffsetXIndex = 6;

        public const int DetachedHandOffsetYIndex = 7;

        public const float Phase2LifeRatio = 0.85f;

        public const float Phase3LifeRatio = 0.45f;

        public static CrabulonAttackState[] Phase1AttackCycle => new[]
        {
            CrabulonAttackState.WalkToTarget,
            CrabulonAttackState.JumpToTarget
        };

        public static CrabulonAttackState[] Phase2AttackCycle => new[]
        {
            CrabulonAttackState.WalkToTarget,
            CrabulonAttackState.JumpToTarget,
            CrabulonAttackState.WalkToTarget,
            CrabulonAttackState.CreateGroundMushrooms,
            CrabulonAttackState.JumpToTarget,
        };

        public static CrabulonAttackState[] Phase3AttackCycle => new[]
        {
            CrabulonAttackState.WalkToTarget,
            CrabulonAttackState.JumpToTarget,
            CrabulonAttackState.ClawSlamMushroomWaves,
            CrabulonAttackState.WalkToTarget,
            CrabulonAttackState.CreateGroundMushrooms,
            CrabulonAttackState.JumpToTarget,
            CrabulonAttackState.ClawSlamMushroomWaves,
        };

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio,
            Phase3LifeRatio
        };

        public override void SetDefaults(NPC npc)
        {
            // Set defaults that, if were to be changed by Calamity, would cause significant issues to the fight.
            npc.width = 196;
            npc.height = 196;
            npc.scale = 1f;
            npc.Opacity = 1f;
            npc.defense = 8;
            npc.DR_NERD(0.5f);
        }

        public override bool PreAI(NPC npc)
        {
            // Give a visual offset to the boss.
            npc.gfxOffY = 4;

            // Emit a deep blue light idly.
            Lighting.AddLight(npc.Center, 0f, 0.3f, 0.7f);

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();

            // Reset things.
            npc.defDamage = 84;
            npc.damage = npc.alpha > 40 ? 0 : npc.defDamage;
            npc.noTileCollide = false;
            npc.defense = 11;

            // If none was found or it was too far away, despawn.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead ||
                !Main.player[npc.target].active || !npc.WithinRange(Main.player[npc.target].Center, 3400f))
            {
                DoDespawnEffects(npc);
                return false;
            }

            Player target = Main.player[npc.target];

            ref float attackType = ref npc.ai[2];
            ref float attackTimer = ref npc.ai[1];
            ref float jumpCount = ref npc.Infernum().ExtraAI[6];
            ref float currentFrame = ref npc.Infernum().ExtraAI[7];

            // Use a temporary variable to store the current frame. This is necessary to ensure that claw offset calculations are correct server-side, since the server does not have access to
            // frame information.
            if (Main.netMode != NetmodeID.Server)
            {
                int f = npc.frame.Y / npc.frame.Height;
                if (currentFrame != f)
                {
                    currentFrame = f;
                    npc.netUpdate = true;
                }
            }

            bool usingClaws = false;
            bool enraged = !target.ZoneGlowshroom && npc.Top.Y / 16 < Main.worldSurface && !BossRushEvent.BossRushActive;
            npc.Calamity().CurrentlyEnraged = enraged;
            npc.alpha = Utils.Clamp(npc.alpha - 12, 0, 255);

            switch ((CrabulonAttackState)(int)attackType)
            {
                case CrabulonAttackState.SpawnWait:
                    DoBehavior_SpawnWait(npc, attackTimer);
                    npc.ai[0] = 1f;
                    break;
                case CrabulonAttackState.JumpToTarget:
                    DoBehavior_JumpToTarget(npc, target, attackTimer, enraged, ref jumpCount);
                    break;
                case CrabulonAttackState.WalkToTarget:
                    DoBehavior_WalkToTarget(npc, target, attackTimer, enraged);
                    npc.ai[0] = 1f;
                    break;
                case CrabulonAttackState.CreateGroundMushrooms:
                    DoBehavior_CreateGroundMushrooms(npc, target, ref attackTimer, enraged);
                    npc.ai[0] = 1f;
                    break;
                case CrabulonAttackState.ClawSlamMushroomWaves:
                    DoBehavior_ClawSlamMushroomWaves(npc, target, ref attackTimer, ref usingClaws, enraged);
                    npc.ai[0] = 1f;
                    break;
            }
            npc.Infernum().ExtraAI[UsingDetachedHandsFlagIndex] = usingClaws.ToInt();
            attackTimer++;
            return false;
        }

        #region Specific Attacks
        internal static void DoDespawnEffects(NPC npc)
        {
            npc.noTileCollide = true;
            npc.noGravity = false;
            npc.alpha = Utils.Clamp(npc.alpha + 20, 0, 255);
            npc.damage = 0;
            if (npc.timeLeft > 45)
                npc.timeLeft = 45;
        }

        internal static void DoBehavior_SpawnWait(NPC npc, float attackTimer)
        {
            if (attackTimer == 0f)
                npc.alpha = 255;
            npc.damage = 0;

            // Idly emit mushroom dust off of Crabulon.
            Dust spore = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.BlueFairy);
            spore.velocity = -Vector2.UnitY * Main.rand.NextFloat(0.4f, 2.7f);
            spore.noGravity = true;
            spore.scale = Lerp(0.75f, 1.45f, Utils.GetLerpValue(npc.Top.Y, npc.Bottom.Y, spore.position.Y));

            if (attackTimer >= 210f || npc.justHit)
                SelectNextAttack(npc);
        }

        internal static void DoBehavior_JumpToTarget(NPC npc, Player target, float attackTimer, bool enraged, ref float jumpCount)
        {
            // Rapidly decelerate for the first half second or so prior to the jump.
            if (attackTimer < 30f)
            {
                npc.velocity.X *= 0.9f;
                return;
            }

            int sporeCloudCount = 22;
            int pillarMushroomSpawnRate = 28;
            float sporeCloudSpeed = 9f;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float jumpSpeed = Lerp(13.5f, 18.75f, 1f - lifeRatio);
            float extraGravity = Lerp(0f, 0.45f, 1f - lifeRatio);
            float jumpAngularImprecision = Lerp(0.1f, 0f, Utils.GetLerpValue(0f, 0.7f, 1f - lifeRatio));

            jumpSpeed += Clamp((npc.Top.Y - target.Top.Y) * 0.02f, 0f, 12f);
            if (BossRushEvent.BossRushActive)
            {
                sporeCloudSpeed = 18f;
                sporeCloudCount = 70;
                jumpSpeed *= 1.4f;
                extraGravity += 0.25f;
            }

            if (enraged)
            {
                extraGravity += 0.18f;
                jumpSpeed += 2.8f;
                jumpAngularImprecision *= 0.25f;
            }

            if (Utilities.AnyProjectiles(ModContent.ProjectileType<MushroomPillar>()))
            {
                jumpSpeed *= 0.85f;
                extraGravity = Clamp(extraGravity - 0.1f, 0f, 10f);

                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % pillarMushroomSpawnRate == pillarMushroomSpawnRate - 1f)
                    Utilities.NewProjectileBetter(target.Center - Vector2.UnitY * 600f, Vector2.UnitY * 6f, ModContent.ProjectileType<MushBomb>(), MushroomBombDamage, 0f, -1, 0f, target.Bottom.Y);
            }

            ref float hasJumpedFlag = ref npc.Infernum().ExtraAI[0];
            ref float hasHitGroundFlag = ref npc.Infernum().ExtraAI[1];
            ref float jumpTimer = ref npc.Infernum().ExtraAI[2];
            if (Main.netMode != NetmodeID.MultiplayerClient && npc.velocity.Y == 0f && hasJumpedFlag == 0f)
            {
                npc.position.Y -= 16f;
                npc.velocity = Utilities.GetProjectilePhysicsFiringVelocity(npc.Center, target.Center + target.velocity * 20f, extraGravity + 0.3f, jumpSpeed, out _);
                npc.velocity = npc.velocity.RotatedByRandom(jumpAngularImprecision);
                hasJumpedFlag = 1f;

                npc.netUpdate = true;
            }

            if (npc.velocity.Y != 0f)
                npc.velocity.Y += extraGravity + 0.3f;

            if (hasJumpedFlag == 1f)
            {
                // Don't interact with any obstacles in the way if above the target.
                npc.noTileCollide = npc.Bottom.Y < target.Top.Y && hasHitGroundFlag == 0f;
                if (jumpTimer < 8f)
                    npc.noTileCollide = true;

                // Do gravity manually.
                npc.noGravity = true;

                // Do more damage since Crabulon is essentially trying to squish the target.
                npc.damage = npc.defDamage + 36;

                if (npc.velocity.Y == 0f)
                {
                    // Make some visual and auditory effects when hitting the ground.
                    if (hasHitGroundFlag == 0f)
                    {
                        SoundEngine.PlaySound(SoundID.Item14, npc.Center);
                        for (int i = 0; i < 36; i++)
                        {
                            Vector2 dustSpawnPosition = Vector2.Lerp(npc.BottomLeft, npc.BottomRight, i / 36f);
                            Dust stompMushroomDust = Dust.NewDustDirect(dustSpawnPosition, 4, 4, DustID.BlueFairy);
                            stompMushroomDust.velocity = Vector2.UnitY * Main.rand.NextFloatDirection() * npc.velocity.Length() * 0.5f;
                            stompMushroomDust.scale = 1.8f;
                            stompMushroomDust.fadeIn = 1.2f;
                            stompMushroomDust.noGravity = true;
                        }

                        // Optionally, if below a certain life ratio or enraged, release mushrooms into the air.
                        bool tooManyShrooms = NPC.CountNPCS(ModContent.NPCType<CrabShroom>()) > 10;
                        if (Main.netMode != NetmodeID.MultiplayerClient && (lifeRatio < Phase2LifeRatio || enraged) && lifeRatio >= Phase3LifeRatio)
                        {
                            for (int i = 0; i < 2; i++)
                            {
                                if (tooManyShrooms)
                                    break;

                                Vector2 spawnPosition = npc.Center + Main.rand.NextVector2Circular(npc.width, npc.height) * 0.5f;
                                int shroom = NPC.NewNPC(npc.GetSource_FromAI(), (int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<CrabShroom>());
                                if (Main.npc.IndexInRange(shroom))
                                {
                                    Main.npc[shroom].velocity = -Vector2.UnitY.RotatedByRandom(0.36f) * Main.rand.NextFloat(3f, 6f);
                                    Main.npc[shroom].netUpdate = true;
                                }
                            }
                        }
                        jumpCount++;

                        if (Main.netMode != NetmodeID.MultiplayerClient && jumpCount % MushroomStompBarrageInterval == MushroomStompBarrageInterval - 1f)
                        {
                            for (int i = 0; i < sporeCloudCount; i++)
                            {
                                Vector2 spawnPosition = npc.Center + Main.rand.NextVector2Circular(npc.width, npc.height) * 0.45f;
                                Vector2 sporeShootVelocity = Main.rand.NextVector2Unit() * sporeCloudSpeed * Main.rand.NextFloat(1f, 2f);
                                Utilities.NewProjectileBetter(spawnPosition, sporeShootVelocity, ModContent.ProjectileType<SporeCloud>(), SporeCloudDamage, 0f, -1, Main.rand.Next(3));
                            }
                        }

                        hasHitGroundFlag = 1f;
                        npc.netUpdate = true;
                    }

                    npc.velocity.X *= 0.9f;
                    if (Math.Abs(npc.velocity.X) < 0.2f)
                        SelectNextAttack(npc);
                }
                jumpTimer++;
            }
            else
                jumpTimer = 0f;
            npc.ai[0] = hasJumpedFlag == 1f ? 4f : 3f;
            if (hasHitGroundFlag == 1f)
                npc.ai[0] = 0f;
        }

        internal static void DoBehavior_WalkToTarget(NPC npc, Player target, float attackTimer, bool enraged)
        {
            npc.direction = (target.Center.X > npc.Center.X).ToDirectionInt();

            float horizontalDistanceFromTarget = Distance(target.Center.X, npc.Center.X);
            bool shouldSlowDown = horizontalDistanceFromTarget < 50f || Utilities.AnyProjectiles(ModContent.ProjectileType<MushroomPillar>());
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float walkSpeed = Lerp(2.4f, 5.6f, 1f - lifeRatio);
            if (enraged)
                walkSpeed += 1.5f;
            if (BossRushEvent.BossRushActive)
                walkSpeed *= 5f;
            walkSpeed += horizontalDistanceFromTarget * 0.004f;
            walkSpeed *= npc.SafeDirectionTo(target.Center).X;

            // Release spores into the air after a specific life ratio is passed.
            if (lifeRatio < Phase2LifeRatio)
            {
                bool canShoot = attackTimer % 120f >= 80f && attackTimer % 8f == 7f;
                shouldSlowDown = attackTimer % 120f >= 60f;
                float shootPower = Lerp(5f, 10f, Utils.GetLerpValue(80f, 120f, attackTimer % 120f, true));
                if (Main.netMode != NetmodeID.MultiplayerClient && canShoot)
                {
                    Vector2 shootVelocity = npc.SafeDirectionTo(target.Center) * shootPower;
                    shootVelocity.X += npc.SafeDirectionTo(target.Center).X * shootPower * 0.4f;
                    shootVelocity.Y -= shootPower * 0.6f;
                    Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<MushBomb>(), MushroomBombDamage, 0f);
                }
            }

            if (shouldSlowDown)
            {
                npc.velocity.X *= 0.8f;
                if (Math.Abs(npc.velocity.X) < 0.1f)
                    npc.velocity.X = 0f;
            }
            else
                npc.velocity.X = (npc.velocity.X * 20f + walkSpeed) / 21f;

            npc.noGravity = true;
            npc.noTileCollide = true;

            // Check if tile collision ignoral is necessary.
            PerformGravityCheck(npc, target);

            if (attackTimer >= 132f || npc.collideX || target.Center.Y < npc.Top.Y - 200f || target.Center.Y > npc.Bottom.Y + 80f)
            {
                SelectNextAttack(npc);
                if (target.Center.Y > npc.Bottom.Y + 80f)
                    npc.ai[2] = (int)CrabulonAttackState.JumpToTarget;
            }
        }

        internal static void DoBehavior_CreateGroundMushrooms(NPC npc, Player target, ref float attackTimer, bool enraged)
        {
            // Rapidly decelerate for the first second or so prior to the summon.
            if (attackTimer < 45f)
            {
                npc.velocity.X *= 0.9f;
                return;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 75f)
            {
                for (float dx = -1000f; dx < 1000f; dx += enraged ? 250f : 360f)
                {
                    Vector2 spawnPosition = target.Bottom + Vector2.UnitX * dx;
                    WorldUtils.Find(spawnPosition.ToTileCoordinates(), Searches.Chain(new Searches.Down(6000), new GenCondition[]
                    {
                        new Conditions.IsSolid(),
                        new CustomTileConditions.ActiveAndNotActuated(),
                        new CustomTileConditions.NotPlatform()
                    }), out Point newBottom);
                    Utilities.NewProjectileBetter(newBottom.ToWorldCoordinates(8, 0), Vector2.Zero, ModContent.ProjectileType<MushroomPillar>(), MushroomPillarDamage, 0f);
                }

                // Release spores into the air.
                for (int i = 0; i < 3; i++)
                {
                    int x = (int)(npc.position.X + Main.rand.Next(npc.width - 32));
                    int y = (int)(npc.position.Y + Main.rand.Next(npc.height - 32));
                    int fuck = NPC.NewNPC(npc.GetSource_FromAI(), x, y, ModContent.NPCType<CrabShroom>());
                    Main.npc[fuck].SetDefaults(ModContent.NPCType<CrabShroom>());
                    Main.npc[fuck].velocity.X = Main.rand.NextFloat(-5f, 5f);
                    Main.npc[fuck].velocity.Y = Main.rand.NextFloat(-9f, -6f);
                    if (Main.netMode == NetmodeID.Server && fuck < 200)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, fuck, 0f, 0f, 0f, 0, 0, 0);
                }
            }

            if (attackTimer >= 120f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_ClawSlamMushroomWaves(NPC npc, Player target, ref float attackTimer, ref bool usingClaws, bool enraged)
        {
            int clawMoveTime = 72;
            int clawPressTime = 32;
            int clawSlamTime = 28;
            int clawSlamWaitTime = 64;
            int slamCount = 3;
            Vector2 clawOffset = new(npc.Infernum().ExtraAI[DetachedHandOffsetXIndex], npc.Infernum().ExtraAI[DetachedHandOffsetYIndex]);

            ref float slamCounter = ref npc.Infernum().ExtraAI[0];

            // Extend the claws outward.
            if (attackTimer <= clawMoveTime)
            {
                float moveInterpolant = Pow(attackTimer / clawMoveTime, 4.81f);
                Vector2 idealClawOffset = new(Lerp(0f, 168f, moveInterpolant), Lerp(0f, -92f, moveInterpolant));
                clawOffset = Vector2.Lerp(clawOffset, idealClawOffset, 0.16f);

                // Walk towards the target.
                if (slamCounter >= slamCount)
                {
                    if (attackTimer >= 10f)
                        SelectNextAttack(npc);
                }
                else
                {
                    Vector2 walkDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * 180f;
                    npc.velocity.X = (npc.velocity.X * 20f + npc.SafeDirectionTo(walkDestination).X * 8f) / 21f;
                    PerformGravityCheck(npc, target);
                }
            }

            // Press the claws together.
            else if (attackTimer <= clawMoveTime + clawPressTime)
            {
                float moveInterpolant = Pow(Utils.GetLerpValue(clawMoveTime, clawMoveTime + clawPressTime, attackTimer, true), 2.9f);
                clawOffset.X = Lerp(168f, 30f, moveInterpolant);
                clawOffset.Y = Lerp(-92f, -138f, moveInterpolant);

                // Slow down.
                npc.velocity.X *= 0.85f;
            }

            // Make the claws slam into the ground.
            else
            {
                float moveInterpolant = Pow(Utils.GetLerpValue(clawMoveTime + clawPressTime, clawMoveTime + clawPressTime + clawSlamTime, attackTimer, true), 8.3f);
                clawOffset.X = Lerp(30f, 42f, moveInterpolant);
                clawOffset.Y = Lerp(-138f, 56f, moveInterpolant);

                if (attackTimer == clawMoveTime + clawPressTime + clawSlamTime - 15f)
                    npc.netUpdate = true;

                // Create a slam effect at the position where the claw slammed.
                if (attackTimer == clawMoveTime + clawPressTime + clawSlamTime)
                {
                    SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact with { Volume = 1.8f }, npc.Center);
                    for (int i = -1; i <= 1; i += 2)
                    {
                        Vector2 clawCenter = npc.Center + GetBaseClawOffset(npc, i == 1) + clawOffset * new Vector2(-i, 1f);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            Utilities.NewProjectileBetter(clawCenter, Vector2.UnitX * i * -6f, ProjectileID.DD2OgreSmash, 0, 0f);

                        if (target.WithinRange(clawCenter, 100f))
                            target.Hurt(PlayerDeathReason.ByNPC(npc.whoAmI), npc.damage, i);

                        // Release a bunch of falling crab shrooms into the air from both arms.
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int j = 0; j < (enraged ? 28 : 16); j++)
                            {
                                float pointToCrabulonInterpolant = Utils.GetLerpValue(5f, 0f, j, true);
                                Vector2 shroomVelocity = new Vector2(-i * (j * 0.85f + 1f), -8f - Sqrt(j) * 0.5f) + Main.rand.NextVector2Circular(0.2f, 0.2f);
                                shroomVelocity.X = Lerp(shroomVelocity.X, (npc.Center - clawCenter).SafeNormalize(Vector2.Zero).X * 4f, pointToCrabulonInterpolant);

                                // Make mushrooms go higher up if the target is quite a bit above Crabulon.
                                if (target.Center.Y < npc.Center.Y - 400f)
                                    shroomVelocity.Y *= 1.5f;

                                Utilities.NewProjectileBetter(clawCenter, shroomVelocity, ModContent.ProjectileType<MushBombInfernum>(), MushroomBombDamage, 0f, -1, 0f, npc.Bottom.Y);
                            }
                        }
                    }
                }
            }

            // Use the claw animation.
            usingClaws = true;

            // Save the new claw offset value.
            npc.Infernum().ExtraAI[DetachedHandOffsetXIndex] = clawOffset.X;
            npc.Infernum().ExtraAI[DetachedHandOffsetYIndex] = clawOffset.Y;

            if (attackTimer >= clawMoveTime + clawPressTime + clawSlamTime + clawSlamWaitTime)
            {
                attackTimer = 0f;
                slamCounter++;
                npc.netUpdate = true;
            }
        }

        public static void PerformGravityCheck(NPC npc, Player target)
        {
            int horizontalCheckArea = 80;
            int verticalCheckArea = 20;
            Vector2 checkPosition = new(npc.Center.X - horizontalCheckArea * 0.5f, npc.Bottom.Y - verticalCheckArea);
            if (Collision.SolidCollision(checkPosition, horizontalCheckArea, verticalCheckArea))
            {
                if (npc.velocity.Y > 0f)
                    npc.velocity.Y = 0f;

                if (npc.velocity.Y > -0.2)
                    npc.velocity.Y -= 0.025f;
                else
                    npc.velocity.Y -= 0.2f;

                if (npc.velocity.Y < -4f)
                    npc.velocity.Y = -4f;

                // Walk upwards to reach the target if below them.
                if (npc.Center.Y > target.Bottom.Y && npc.velocity.Y > -14f)
                    npc.velocity.Y -= 0.15f;

            }
            else
            {
                if (npc.velocity.Y < 0f)
                    npc.velocity.Y = 0f;

                if (npc.velocity.Y < 0.1)
                    npc.velocity.Y += 0.025f;
                else
                    npc.velocity.Y += 0.5f;
            }
        }
        #endregion Specific Attacks

        #region AI Utility Methods

        internal static void SelectNextAttack(NPC npc)
        {
            npc.TargetClosest();

            CrabulonAttackState newAttackState;
            float lifeRatio = npc.life / (float)npc.lifeMax;

            npc.ai[3]++;
            if (lifeRatio < Phase3LifeRatio)
                newAttackState = Phase3AttackCycle[(int)npc.ai[3] % Phase3AttackCycle.Length];
            else if (lifeRatio < Phase2LifeRatio)
                newAttackState = Phase2AttackCycle[(int)npc.ai[3] % Phase2AttackCycle.Length];
            else
                newAttackState = Phase1AttackCycle[(int)npc.ai[3] % Phase1AttackCycle.Length];

            npc.ai[2] = (int)newAttackState;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netSpam = 0;
            npc.netUpdate = true;
        }
        #endregion AI Utility Methods

        #endregion AI

        #region Frames and Drawcode

        public override void FindFrame(NPC npc, int frameHeight)
        {
            ref float stomping = ref npc.localAI[0];
            if (npc.ai[0] > 1f)
            {
                if (npc.ai[0] == 2f) // Idle just before jump
                {
                    if (stomping == 1f)
                        stomping = 0f;

                    npc.frameCounter += 0.15;
                    npc.frameCounter %= Main.npcFrameCount[npc.type];
                    int frame = (int)npc.frameCounter;
                    npc.frame.Y = frame * frameHeight;
                }
                else if (npc.ai[0] == 3f) // Prepare to jump and then jump
                {
                    npc.frameCounter += 1D;
                    if (npc.frameCounter > 12D)
                    {
                        npc.frame.Y += frameHeight;
                        npc.frameCounter = 0D;
                    }
                    if (npc.frame.Y >= frameHeight)
                        npc.frame.Y = frameHeight;
                }
                else // Stomping
                {
                    if (stomping == 0f)
                    {
                        stomping = 1f;
                        npc.frameCounter = 0D;
                    }

                    npc.frameCounter += 1D;
                    if (npc.frameCounter > 8D)
                    {
                        npc.frame.Y += frameHeight;
                        npc.frameCounter = 0D;
                    }
                    if (npc.frame.Y >= frameHeight * 5)
                        npc.frame.Y = frameHeight * 5;
                }
            }

            // Walking.
            else
            {
                if (stomping == 1f)
                    stomping = 0f;

                npc.frameCounter += Math.Abs(npc.velocity.X) * 0.05f;
                if (Math.Abs(npc.velocity.X) < 0.1f)
                    npc.frameCounter = 0D;

                npc.frameCounter %= Main.npcFrameCount[npc.type];
                int frame = (int)npc.frameCounter;
                npc.frame.Y = frame * frameHeight;
            }
        }

        public static Color ClawArmColorFunction(NPC npc, float completionRatio)
        {
            Color endColors = new(116, 108, 166);
            Color middleColor = new(90, 167, 209);
            Color baseColor = Color.Lerp(endColors, middleColor, Math.Abs(Sin(completionRatio * Pi * 0.7f)));
            return baseColor * Utils.GetLerpValue(0f, 0.07f, completionRatio, true) * npc.Opacity;
        }

        public static float ClawArmWidthFunction(float _) => 18f;

        public static Vector2 GetBaseClawOffset(NPC npc, bool right)
        {
            int frame = (int)npc.Infernum().ExtraAI[7];
            Vector2 defaultArmOffset = new Vector2(130f, 6f) * npc.scale;
            Vector2 frameBasedOffset = Vector2.Zero;

            if (frame == 1)
                frameBasedOffset.X += npc.scale * 2f;
            if (frame == 2)
                frameBasedOffset.X += npc.scale * -2f;
            if (frame == 4)
                frameBasedOffset.X += npc.scale * 2f;
            return defaultArmOffset * new Vector2(-right.ToDirectionInt(), 1f) + frameBasedOffset;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/NPCs/Crabulon/CrabulonGlow").Value;
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/Crabulon/CrabulonAlt").Value;
            Texture2D textureGlow = ModContent.Request<Texture2D>("CalamityMod/NPCs/Crabulon/CrabulonAltGlow").Value;
            Texture2D textureAttack = ModContent.Request<Texture2D>("CalamityMod/NPCs/Crabulon/CrabulonAttack").Value;
            Texture2D textureAttackGlow = ModContent.Request<Texture2D>("CalamityMod/NPCs/Crabulon/CrabulonAttackGlow").Value;
            Texture2D textureArmless = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Crabulon/CrabulonArmless").Value;
            Texture2D textureArmlessGlow = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Crabulon/CrabulonArmlessGlow").Value;

            Texture2D leftArm = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Crabulon/CrabulonClawLeft").Value;
            Texture2D leftArmGlow = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Crabulon/CrabulonClawLeftGlow").Value;
            Texture2D rightArm = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Crabulon/CrabulonClawRight").Value;
            Texture2D rightArmGlow = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Crabulon/CrabulonClawRightGlow").Value;

            Vector2 origin = npc.frame.Size() * 0.5f;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Color glowColor = Color.Lerp(Color.White, Color.Cyan, 0.5f) * npc.Opacity;
            SpriteEffects direction = npc.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            // Jumping.
            if (npc.ai[0] > 1f)
            {
                if (npc.velocity.Y == 0f && npc.ai[1] >= 0f && npc.ai[0] == 2f)
                {
                    spriteBatch.Draw(TextureAssets.Npc[npc.type].Value, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, direction, 0f);
                    spriteBatch.Draw(glow, drawPosition, npc.frame, glowColor, npc.rotation, origin, npc.scale, direction, 0f);
                }
                else
                {
                    spriteBatch.Draw(textureAttack, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, direction, 0f);
                    spriteBatch.Draw(textureAttackGlow, drawPosition, npc.frame, glowColor, npc.rotation, origin, npc.scale, direction, 0f);
                }
            }

            // Detached arms.
            else if (npc.Infernum().ExtraAI[UsingDetachedHandsFlagIndex] == 1f)
            {
                // Initialize the claw drawer.
                npc.Infernum().OptionalPrimitiveDrawer ??= new PrimitiveTrailCopy(ClawArmWidthFunction, c => ClawArmColorFunction(npc, c), null, true, InfernumEffectsRegistry.WoFTentacleVertexShader);

                InfernumEffectsRegistry.WoFTentacleVertexShader.UseColor(new Color(70, 90, 166));
                InfernumEffectsRegistry.WoFTentacleVertexShader.UseSecondaryColor(new Color(113, 255, 233));
                InfernumEffectsRegistry.WoFTentacleVertexShader.SetShaderTexture(ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin"));

                Main.spriteBatch.EnterShaderRegion();
                spriteBatch.Draw(textureArmless, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, direction, 0f);
                spriteBatch.Draw(textureArmlessGlow, drawPosition, npc.frame, glowColor, npc.rotation, origin, npc.scale, direction, 0f);

                // Draw the left arm.
                Vector2 clawDrawPosition = drawPosition + new Vector2(npc.Infernum().ExtraAI[DetachedHandOffsetXIndex] + npc.scale * 0f, npc.Infernum().ExtraAI[DetachedHandOffsetYIndex]) + GetBaseClawOffset(npc, false);
                float leftClawRotation = (clawDrawPosition - drawPosition).ToRotation() - 0.33f;

                drawPosition.X += npc.scale * 84f;
                npc.Infernum().OptionalPrimitiveDrawer.Draw(new List<Vector2>()
                {
                    drawPosition,
                    Vector2.Lerp(drawPosition, clawDrawPosition, 0.25f),
                    Vector2.Lerp(drawPosition, clawDrawPosition, 0.5f),
                    Vector2.Lerp(drawPosition, clawDrawPosition, 0.75f),
                    clawDrawPosition
                }, Vector2.Zero, 50);
                drawPosition.X -= npc.scale * 84f;

                spriteBatch.Draw(leftArm, clawDrawPosition, null, npc.GetAlpha(lightColor), npc.rotation + leftClawRotation, leftArm.Size() * 0.5f, npc.scale, 0, 0f);
                spriteBatch.Draw(leftArmGlow, clawDrawPosition, null, glowColor, npc.rotation + leftClawRotation, leftArm.Size() * 0.5f, npc.scale, 0, 0f);

                // Draw the right arm.
                clawDrawPosition = drawPosition + new Vector2(-npc.Infernum().ExtraAI[DetachedHandOffsetXIndex], npc.Infernum().ExtraAI[DetachedHandOffsetYIndex]) + GetBaseClawOffset(npc, true);
                float rightClawRotation = (clawDrawPosition - drawPosition).ToRotation() + Pi + 0.33f;

                drawPosition.X -= npc.scale * 84f;
                npc.Infernum().OptionalPrimitiveDrawer.Draw(new List<Vector2>()
                {
                    drawPosition,
                    Vector2.Lerp(drawPosition, clawDrawPosition, 0.25f),
                    Vector2.Lerp(drawPosition, clawDrawPosition, 0.5f),
                    Vector2.Lerp(drawPosition, clawDrawPosition, 0.75f),
                    clawDrawPosition
                }, Vector2.Zero, 50);
                drawPosition.X += npc.scale * 84f;

                spriteBatch.Draw(rightArm, clawDrawPosition, null, npc.GetAlpha(lightColor), npc.rotation + rightClawRotation, rightArm.Size() * 0.5f, npc.scale, 0, 0f);
                spriteBatch.Draw(rightArmGlow, clawDrawPosition, null, glowColor, npc.rotation + rightClawRotation, rightArm.Size() * 0.5f, npc.scale, 0, 0f);
                Main.spriteBatch.ExitShaderRegion();
            }

            // Walking.
            else if (npc.ai[0] == 1f)
            {
                spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, direction, 0f);
                spriteBatch.Draw(textureGlow, drawPosition, npc.frame, glowColor, npc.rotation, origin, npc.scale, direction, 0f);
            }

            // Standing still.
            else
            {
                spriteBatch.Draw(TextureAssets.Npc[npc.type].Value, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, direction, 0f);
                spriteBatch.Draw(glow, drawPosition, npc.frame, glowColor, npc.rotation, origin, npc.scale, direction, 0f);
            }
            return false;
        }
        #endregion Frames and Drawcode

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Mods.InfernumMode.PetDialog.CrabulonTip1";
            yield return n => "Mods.InfernumMode.PetDialog.CrabulonTip2";
            yield return n => "Mods.InfernumMode.PetDialog.CrabulonTip3";
            yield return n => "Mods.InfernumMode.PetDialog.CrabulonTip4";
            yield return n =>
            {
                if(n.life < n.lifeMax * Phase3LifeRatio)
                {
                    return "Mods.InfernumMode.PetDialog.CrabulonClawTip";
                }
                return string.Empty;
            };

            yield return n =>
            {
                if (TipsManager.ShouldUseJokeText)
                    return "Mods.InfernumMode.PetDialog.CrabulonJokeTip1";
                return string.Empty;
            };
        }
        #endregion Tips
    }
}
