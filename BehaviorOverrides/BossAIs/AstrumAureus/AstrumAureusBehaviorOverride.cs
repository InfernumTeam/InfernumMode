using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.NPCs.AstrumAureus;
using CalamityMod.NPCs.AstrumDeus;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

using AureusBoss = CalamityMod.NPCs.AstrumAureus.AstrumAureus;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumAureus
{
    public class AstrumAureusBehaviorOverride : NPCBehaviorOverride
    {
        public enum AureusAttackType
        {
            SpawnActivation,
            WalkAndShootLasers,
            LeapAtTarget,
            RocketBarrage,
            CreateAureusSpawnRing,
            CelestialRain,
            AstralDrillLaser,
            Recharge
        }

        public enum AureusFrameType
        {
            Idle,
            SitAndRecharge,
            Walk,
            Jump,
            Stomp
        }

        public override int NPCOverrideType => ModContent.NPCType<AureusBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame | NPCOverrideContext.NPCPreDraw;

        public static readonly List<Vector2> LaserbeamSpawnOffsets = new List<Vector2>()
        {
            new Vector2(0f, -52f),
            new Vector2(110f, 24f),
            new Vector2(-110f, -24f),
            new Vector2(184f, -20f),
            new Vector2(-184f, -20f),
        };

        public const float Phase2LifeRatio = 0.6f;
        public const float Phase3LifeRatio = 0.3f;

        public override bool PreAI(NPC npc)
        {
            // Do targeting.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            // Manually offset the boss graphically.
            npc.gfxOffY = -46;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float frameType = ref npc.localAI[0];

            // Reset things every frame.
            npc.damage = npc.defDamage;
            npc.defense = npc.defDefense;

            // Reset gravity affection and tile collision.
            npc.noGravity = false;
            npc.noTileCollide = false;

            // Despawn if necessary
            if (!target.active || target.dead || !npc.WithinRange(target.Center, 7200f))
            {
                DoBehavior_Despawn(npc);
                return false;
            }

            switch ((AureusAttackType)(int)attackState)
            {
                case AureusAttackType.SpawnActivation:
                    DoAttack_SpawnActivation(npc, attackTimer, ref frameType);
                    break;
                case AureusAttackType.WalkAndShootLasers:
                    DoAttack_WalkAndShootLasers(npc, target, lifeRatio, ref attackTimer, ref frameType);
                    break;
                case AureusAttackType.LeapAtTarget:
                    DoAttack_LeapAtTarget(npc, target, lifeRatio, ref attackTimer, ref frameType);
                    break;
                case AureusAttackType.RocketBarrage:
                    DoAttack_RocketBarrage(npc, target, lifeRatio, ref attackTimer, ref frameType);
                    break;
                case AureusAttackType.CreateAureusSpawnRing:
                    DoAttack_CreateAureusSpawnRing(npc, lifeRatio, ref attackTimer, ref frameType);
                    break;
                case AureusAttackType.CelestialRain:
                    DoAttack_CelestialRain(npc, target, lifeRatio, ref attackTimer, ref frameType);
                    break;
                case AureusAttackType.AstralDrillLaser:
                    DoAttack_AstralDrillLaser(npc, target, lifeRatio, ref attackTimer, ref frameType);
                    break;
                case AureusAttackType.Recharge:
                    DoAttack_Recharge(npc, attackTimer, ref frameType);
                    break;
            }

            attackTimer++;
            return false;
        }

        #region Custom Behaviors

        public static void DoBehavior_Despawn(NPC npc)
        {
            // Fall and cease horizontal movement.
            npc.velocity.X *= 0.9f;
            npc.noGravity = false;
            npc.noTileCollide = false;

            // Fade away.
            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.05f, 0f, 1f);

            // Despawn once invisible.
            if (npc.Opacity <= 0)
            {
                npc.life = 0;
                npc.active = false;
                npc.netUpdate = true;
            }
        }

        public static void DoAttack_SpawnActivation(NPC npc, float attackTimer, ref float frameType)
        {
            // Fall and cease horizontal movement.
            npc.velocity.X *= 0.9f;
            npc.noGravity = false;
            npc.noTileCollide = false;

            // Do no damage.
            npc.damage = 0;

            frameType = (int)AureusFrameType.SitAndRecharge;

            // Go to the next attack after 1.5 seconds, or if hit before then.
            if (attackTimer > 90f || npc.justHit)
                GotoNextAttackState(npc);
        }

        public static void DoAttack_Recharge(NPC npc, float attackTimer, ref float frameType)
        {
            // Fall and cease horizontal movement.
            npc.velocity.X *= 0.9f;
            npc.noGravity = false;
            npc.noTileCollide = false;

            // Do no damage.
            npc.damage = 0;

            // Use less defense.
            npc.defense = npc.defDefense / 2;

            frameType = (int)AureusFrameType.SitAndRecharge;

            // Go to the next attack after 1.5 seconds, or if hit before then.
            if (attackTimer > 150f)
                GotoNextAttackState(npc);
        }

        public static void DoAttack_WalkAndShootLasers(NPC npc, Player target, float lifeRatio, ref float attackTimer, ref float frameType)
        {
            // Adjust directioning.
            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

            // And frames.
            frameType = (int)AureusFrameType.Walk;

            npc.noGravity = true;
            npc.noTileCollide = true;

            float horizontalDistanceFromTarget = MathHelper.Distance(target.Center.X, npc.Center.X);
            bool shouldSlowDown = horizontalDistanceFromTarget < 50f;

            int laserShootRate = (int)MathHelper.Lerp(110f, 60f, 1f - lifeRatio);
            float walkSpeed = MathHelper.Lerp(7f, 11f, 1f - lifeRatio);
            walkSpeed += horizontalDistanceFromTarget * 0.0075f;
            walkSpeed *= npc.SafeDirectionTo(target.Center).X;

            ref float laserShootCounter = ref npc.Infernum().ExtraAI[0];

            if (shouldSlowDown)
            {
                // Make the attack go by more quickly if close to the target horizontally.
                attackTimer++;

                npc.velocity.X *= 0.8f;
                if (Math.Abs(npc.velocity.X) < 0.1f)
                    npc.velocity.X = 0f;
            }
            else
                npc.velocity.X = (npc.velocity.X * 15f + walkSpeed) / 16f;

            // Shoot bursts of lasers periodically.
            laserShootCounter++;
            if (laserShootCounter >= laserShootRate)
            {
                Main.PlaySound(SoundID.Item33, npc.Center);

                int laserCount = 8;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < laserCount; i++)
                    {
                        Vector2 laserShootVelocity = npc.SafeDirectionTo(target.Center + target.velocity * 20f).RotatedByRandom(0.59f) * Main.rand.NextFloat(15f, 23f);
                        Utilities.NewProjectileBetter(npc.Center + laserShootVelocity * 2f, laserShootVelocity, ModContent.ProjectileType<AstralLaser>(), 165, 0f);
                    }

                    laserShootCounter = 0f;
                    npc.netUpdate = true;
                }
            }

            // Check if tile collision ignoral is necessary.
            int horizontalCheckArea = 80;
            int verticalCheckArea = 20;
            Vector2 checkPosition = new Vector2(npc.Center.X - horizontalCheckArea * 0.5f, npc.Bottom.Y - verticalCheckArea);
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

            if (attackTimer >= 540f)
                GotoNextAttackState(npc);
        }

        public static void DoAttack_LeapAtTarget(NPC npc, Player target, float lifeRatio, ref float attackTimer, ref float frameType)
        {
            // Reset tile collision.
            npc.noTileCollide = false;

            // Reset the frame type. It will be changed below as needed.
            frameType = (int)AureusFrameType.Idle;

            // Reset frames if necessary.
            if (attackTimer == 1f)
                npc.frame.Y = 0;

            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float stompCounter = ref npc.Infernum().ExtraAI[1];
            ref float jumpIntensity = ref npc.Infernum().ExtraAI[2];

            switch ((int)attackState)
            {
                // Wait in anticipation of being able to jump.
                // Everything here only executes of on solid ground.
                case 0:
                    if (npc.velocity.Y == 0f)
                    {
                        frameType = (int)AureusFrameType.Jump;

                        // Slow down.
                        npc.velocity.X *= 0.8f;

                        // Jump after a delay..
                        if (attackTimer >= 50f)
                        {
                            float velocityX = 21f;
                            npc.spriteDirection = (npc.Center.X < target.Center.X).ToDirectionInt();
                            npc.velocity.X = velocityX * npc.spriteDirection;

                            jumpIntensity = 1f;
                            float distanceBelowTarget = npc.Top.Y - (target.Top.Y + 80f);

                            if (distanceBelowTarget > 0f)
                                jumpIntensity = 1f + distanceBelowTarget * 0.0015f;

                            if (jumpIntensity > 3.6f)
                                jumpIntensity = 3.6f;

                            // Fly upward.
                            npc.velocity.Y = -17.5f;

                            npc.noTileCollide = true;

                            if (jumpIntensity > 1f)
                                npc.velocity.Y *= jumpIntensity;

                            attackState++;
                            attackTimer = 0f;
                            npc.netUpdate = true;
                        }
                    }
                    else
                        attackTimer = 0f;
                    break;

                // Attempt to stomp on the target.
                case 1:
                    if (npc.velocity.Y == 0f)
                    {
                        frameType = (int)AureusFrameType.Stomp;

                        // Play a stomp sound.
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/LegStomp"), npc.Center);

                        // Reset the jump intensity.
                        jumpIntensity = 0f;

                        // Determine whether the attack should be repeated.
                        stompCounter++;
                        if (stompCounter >= 4f)
                        {
                            npc.noTileCollide = false;
                            npc.noGravity = false;
                            GotoNextAttackState(npc);
                        }
                        else
                        {
                            npc.spriteDirection = (npc.Center.X < target.Center.X).ToDirectionInt();

                            attackTimer = 0f;
                            attackState = 0f;
                            npc.netUpdate = true;
                        }

                        // Spawn dust on the ground.
                        for (int i = (int)npc.position.X - 20; i < (int)npc.position.X + npc.width + 40; i += 20)
                        {
                            for (int j = 0; j < 4; j++)
                            {
                                Dust fire = Dust.NewDustDirect(new Vector2(npc.position.X - 20f, npc.position.Y + npc.height), npc.width + 20, 4, ModContent.DustType<AstralOrange>(), 0f, 0f, 100, default, 1.5f);
                                fire.velocity *= 0.2f;
                            }
                        }
                    }
                    else
                    {
                        // Set velocities while falling. This happens before the stomp.
                        // Fall through tiles if necessary.
                        if (!target.dead)
                        {
                            if ((target.position.Y > npc.Bottom.Y && npc.velocity.Y > 0f) || (target.position.Y < npc.Bottom.Y && npc.velocity.Y < 0f))
                                npc.noTileCollide = true;
                            else if ((npc.velocity.Y > 0f && npc.Bottom.Y > Main.player[npc.target].Top.Y) || (Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].Center, 1, 1) && !Collision.SolidCollision(npc.position, npc.width, npc.height)))
                                npc.noTileCollide = false;
                        }

                        if (npc.position.X < target.position.X && npc.position.X + npc.width > target.position.X + target.width)
                        {
                            npc.velocity.X *= 0.9f;

                            if (npc.Bottom.Y < target.position.Y)
                            {
                                float fallSpeed = MathHelper.Lerp(0.87f, 1.35f, 1f - lifeRatio);

                                if (jumpIntensity > 1f)
                                    fallSpeed *= jumpIntensity;

                                npc.velocity.Y += fallSpeed;
                            }
                        }
                        else
                        {
                            float horizontalMovementSpeed = Math.Abs(npc.Center.X - target.Center.X) * 0.0001f + 0.12f;

                            if (npc.spriteDirection < 0)
                                npc.velocity.X -= horizontalMovementSpeed;
                            else if (npc.spriteDirection > 0)
                                npc.velocity.X += horizontalMovementSpeed;

                            npc.velocity.X = MathHelper.Clamp(npc.velocity.X, -25f, 25f);
                        }

                        EnforceCustomGravity(npc);
                        npc.noGravity = true;
                    }
                    break;
            }
        }

        public static void DoAttack_RocketBarrage(NPC npc, Player target, float lifeRatio, ref float attackTimer, ref float frameType)
        {
            frameType = (int)AureusFrameType.Idle;

            // Sit in place and periodically release rockets.
            // They shoot more quickly the farther away the target is from the boss.
            int rocketReleaseRate = (int)MathHelper.Lerp(10f, 5f, 1f - lifeRatio);
            ref float rocketShootTimer = ref npc.Infernum().ExtraAI[0];

            // Slow down.
            npc.velocity.X *= 0.9f;

            rocketShootTimer++;
            if (rocketShootTimer >= rocketReleaseRate && attackTimer > 45f && attackTimer < 210f)
            {
                Main.PlaySound(SoundID.Item11, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 rocketShootVelocity = npc.SafeDirectionTo(target.Center).RotatedByRandom(0.66f) * Main.rand.NextFloat(14f, 18.5f);
                    Utilities.NewProjectileBetter(npc.Center + rocketShootVelocity * 3f, rocketShootVelocity, ModContent.ProjectileType<AstralMissile>(), 165, 0f);

                    rocketShootTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            if (attackTimer >= 270f)
                GotoNextAttackState(npc);
        }

        public static void DoAttack_CreateAureusSpawnRing(NPC npc, float lifeRatio, ref float attackTimer, ref float frameType)
        {
            frameType = (int)AureusFrameType.Idle;

            int totalPlanetsToSpawn = lifeRatio < Phase3LifeRatio ? 5 : 4;
            ref float planetsSpawnedCounter = ref npc.Infernum().ExtraAI[0];

            // Slow down horziontally.
            npc.velocity.X *= 0.9f;

            // Create planets that orbit Aureus and act as explosive meat-shields.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= 10f && planetsSpawnedCounter < totalPlanetsToSpawn)
            {
                attackTimer = 0f;
                planetsSpawnedCounter++;

                float ringAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                float ringRadius = npc.Size.Length() * 0.2f + Main.rand.NextFloat(75f, 150f);
                float ringIrregularity = Main.rand.NextFloat(0.5f);

                if (NPC.CountNPCS(ModContent.NPCType<AureusSpawn>()) < 7)
                    NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<AureusSpawn>(), npc.whoAmI, ringAngle, ringRadius, ringIrregularity);
                npc.netUpdate = true;
            }

            // After 30 frames after the above stuff go to the next attack.
            if (attackTimer >= 30f)
                GotoNextAttackState(npc);
        }

        public static void DoAttack_CelestialRain(NPC npc, Player target, float lifeRatio, ref float attackTimer, ref float frameType)
        {
            frameType = (int)AureusFrameType.Idle;

            int cometShootRate = lifeRatio < Phase3LifeRatio ? 4 : 7;
            ref float rainAngle = ref npc.Infernum().ExtraAI[0];

            // Slow down horziontally.
            npc.velocity.X *= 0.9f;

            if (rainAngle == 0f)
                rainAngle = Main.rand.NextFloat(-MathHelper.PiOver2 / 8f, MathHelper.PiOver2 / 8f);

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % cometShootRate == cometShootRate - 1f && attackTimer > 60f)
            {
                Vector2 cometSpawnPosition = target.Center + new Vector2(Main.rand.NextFloat(-1050, 1050f), -780f);
                Vector2 shootDirection = Vector2.UnitY.RotatedBy(rainAngle);
                Vector2 shootVelocity = shootDirection * 16f;

                int cometType = Main.rand.NextBool(12) ? ModContent.ProjectileType<AstralFlame>() : ModContent.ProjectileType<AstralBlueComet>();
                Utilities.NewProjectileBetter(cometSpawnPosition, shootVelocity, cometType, 170, 0f);
            }

            if (attackTimer > 450f)
                GotoNextAttackState(npc);
        }

        public static void DoAttack_AstralDrillLaser(NPC npc, Player target, float lifeRatio, ref float attackTimer, ref float frameType)
        {
            // Adjust directioning.
            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

            npc.noGravity = true;
            npc.noTileCollide = true;

            float horizontalDistanceFromTarget = MathHelper.Distance(target.Center.X, npc.Center.X);
            bool shouldSlowDown = horizontalDistanceFromTarget < 50f;

            int laserShootDelay = 390;
            float walkSpeed = MathHelper.Lerp(5f, 8.65f, 1f - lifeRatio);
            walkSpeed += horizontalDistanceFromTarget * 0.0075f;
            walkSpeed *= Utils.InverseLerp(laserShootDelay - 90f, 210f, attackTimer, true) * npc.SafeDirectionTo(target.Center).X;

            if (shouldSlowDown)
            {
                // Make the attack go by more quickly if close to the target horizontally.
                attackTimer++;

                npc.velocity.X *= 0.8f;
                if (Math.Abs(npc.velocity.X) < 0.1f)
                    npc.velocity.X = 0f;
            }
            else
                npc.velocity.X = (npc.velocity.X * 15f + walkSpeed) / 16f;

            // Adjust frames.
            frameType = Math.Abs(walkSpeed) > 0f ? (int)AureusFrameType.Walk : (int)AureusFrameType.Idle;

            // Release multiple lasers downward that arc upwards over time.
            if (attackTimer == laserShootDelay - 5f)
            {
                // Create a laserbeam fire sound effect.
                Main.PlaySound(SoundID.Zombie, npc.Center, 104);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < LaserbeamSpawnOffsets.Count; i++)
                    {
                        int laserbeamType = i == 0 ? ModContent.ProjectileType<OrangeLaserbeam>() : ModContent.ProjectileType<BlueLaserbeam>();
                        int laser = Utilities.NewProjectileBetter(npc.Center, Vector2.UnitY, laserbeamType, 280, 0f);
                        if (Main.projectile.IndexInRange(laser))
                        {
                            int laserDirection = -1;
                            if (LaserbeamSpawnOffsets[i].X > 0f)
                                laserDirection = 1;
                            Main.projectile[i].Infernum().ExtraAI[0] = i;
                            Main.projectile[i].ai[0] = MathHelper.Pi / 180f * laserDirection * 0.84f;
                        }
                    }
                }
            }

            // Release slow spreads of lasers after the beams have been released.
            if (attackTimer > laserShootDelay && attackTimer % 45f == 44f)
            {
                Main.PlaySound(SoundID.Item33, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        Vector2 laserShootVelocity = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 7.5f;
                        Utilities.NewProjectileBetter(npc.Center + laserShootVelocity * 2f, laserShootVelocity, ModContent.ProjectileType<AstralLaser>(), 165, 0f);
                    }

                    npc.netUpdate = true;
                }
            }

            // Check if tile collision ignoral is necessary.
            int horizontalCheckArea = 80;
            int verticalCheckArea = 20;
            Vector2 checkPosition = new Vector2(npc.Center.X - horizontalCheckArea * 0.5f, npc.Bottom.Y - verticalCheckArea);
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

            if (attackTimer >= laserShootDelay + 180f)
                GotoNextAttackState(npc);
        }
        #endregion Custom Behaviors

        #region Misc AI Operations
        public static void GotoNextAttackState(NPC npc)
        {
            Player target = Main.player[npc.target];

            // Increment the attack counter.
            npc.ai[2]++;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            AureusAttackType oldAttackState = (AureusAttackType)(int)npc.ai[0];
            AureusAttackType newAttackState;
            WeightedRandom<AureusAttackType> attackSelector = new WeightedRandom<AureusAttackType>();
            attackSelector.Add(AureusAttackType.WalkAndShootLasers, 1.5);
            attackSelector.Add(AureusAttackType.LeapAtTarget, 1D + Utils.InverseLerp(540f, 1360f, npc.Center.Y - target.Center.Y) * 1.75f);
            attackSelector.Add(AureusAttackType.RocketBarrage);

            if (lifeRatio < Phase2LifeRatio)
            {
                attackSelector.Add(AureusAttackType.CreateAureusSpawnRing, 0.7);
                attackSelector.Add(AureusAttackType.CelestialRain);
            }

            if (lifeRatio < Phase3LifeRatio)
                attackSelector.Add(AureusAttackType.AstralDrillLaser, 2D);

            do
                newAttackState = attackSelector.Get();
            while (newAttackState == oldAttackState);

            // Recharge once the attack counter every few attacks.
            if (npc.ai[2] >= 4f)
            {
                newAttackState = AureusAttackType.Recharge;
                npc.ai[2] = 0f;
            }

            npc.ai[0] = (int)newAttackState;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }

        public static void EnforceCustomGravity(NPC npc)
        {
            float gravity = 0.6f;
            float maxFallSpeed = 18f;
            float jumpIntensity = npc.Infernum().ExtraAI[2];

            if (npc.wet)
            {
                if (npc.honeyWet)
                {
                    gravity *= 0.33f;
                    maxFallSpeed *= 0.4f;
                }
                else
                {
                    gravity *= 0.66f;
                    maxFallSpeed *= 0.7f;
                }
            }

            if (jumpIntensity > 1f)
                maxFallSpeed *= jumpIntensity;

            npc.velocity.Y += gravity;
            if (npc.velocity.Y > maxFallSpeed)
                npc.velocity.Y = maxFallSpeed;
        }

        #endregion Misc AI Operations

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frameCounter++;

            int frameUpdateRate = 12;
            AureusFrameType frameType = (AureusFrameType)(int)npc.localAI[0];

            if (frameType == AureusFrameType.Walk)
                frameUpdateRate = Utils.Clamp((int)(10 - Math.Abs(npc.velocity.X) * 0.72f), 1, 10);

            if (npc.frameCounter > frameUpdateRate)
            {
                npc.frame.Y += frameHeight;
                if (npc.frame.Y >= frameHeight * Main.npcFrameCount[npc.type])
                    npc.frame.Y = 0;

                npc.frameCounter = 0D;
            }
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.npcTexture[npc.type];
            Texture2D glowmaskTexture = Main.npcTexture[npc.type];
            SpriteEffects spriteEffects = npc.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            AureusFrameType frameType = (AureusFrameType)(int)npc.localAI[0];

            switch (frameType)
            {
                case AureusFrameType.Idle:
                    glowmaskTexture = ModContent.GetTexture("CalamityMod/NPCs/AstrumAureus/AstrumAureusGlow");
                    break;
                case AureusFrameType.SitAndRecharge:
                    texture = ModContent.GetTexture("CalamityMod/NPCs/AstrumAureus/AstrumAureusRecharge");
                    break;
                case AureusFrameType.Walk:
                    texture = ModContent.GetTexture("CalamityMod/NPCs/AstrumAureus/AstrumAureusWalk");
                    glowmaskTexture = ModContent.GetTexture("CalamityMod/NPCs/AstrumAureus/AstrumAureusWalkGlow");
                    break;
                case AureusFrameType.Jump:
                    texture = ModContent.GetTexture("CalamityMod/NPCs/AstrumAureus/AstrumAureusJump");
                    glowmaskTexture = ModContent.GetTexture("CalamityMod/NPCs/AstrumAureus/AstrumAureusJumpGlow");
                    break;
                case AureusFrameType.Stomp:
                    texture = ModContent.GetTexture("CalamityMod/NPCs/AstrumAureus/AstrumAureusStomp");
                    glowmaskTexture = ModContent.GetTexture("CalamityMod/NPCs/AstrumAureus/AstrumAureusStompGlow");
                    break;
            }

            float scale = npc.scale;
            float rotation = npc.rotation;
            int afterimageCount = 8;
            Vector2 origin = npc.frame.Size() * 0.5f;

            // Draw normal afterimages.
            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < afterimageCount; i += 2)
                {
                    Color afterimageColor = npc.GetAlpha(Color.Lerp(lightColor, Color.White, 0.5f)) * ((afterimageCount - i) / 15f);
                    Vector2 afterimageDrawPosition = npc.oldPos[i] + npc.Size * 0.5f - Main.screenPosition + Vector2.UnitY * npc.gfxOffY;
                    spriteBatch.Draw(texture, afterimageDrawPosition, npc.frame, afterimageColor, rotation, origin, scale, spriteEffects, 0f);
                }
            }

            // Draw the normal texture.
            Vector2 drawPosition = npc.Center - Main.screenPosition + Vector2.UnitY * npc.gfxOffY;
            spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(lightColor), rotation, origin, scale, spriteEffects, 0f);

            // And draw glowmasks (and their afterimages) if not recharging.
            if ((AureusAttackType)(int)npc.ai[0] != AureusAttackType.Recharge && glowmaskTexture != Main.npcTexture[npc.type])
            {
                Color glowmaskColor = Color.White;

                if (CalamityConfig.Instance.Afterimages)
                {
                    for (int i = 1; i < afterimageCount; i++)
                    {
                        Color afterimageColor = npc.GetAlpha(Color.Lerp(glowmaskColor, Color.White, 0.5f)) * ((afterimageCount - i) / 15f);
                        Vector2 afterimageDrawPosition = npc.oldPos[i] + npc.Size * 0.5f - Main.screenPosition + Vector2.UnitY * npc.gfxOffY;
                        spriteBatch.Draw(glowmaskTexture, afterimageDrawPosition, npc.frame, afterimageColor, rotation, origin, scale, spriteEffects, 0f);
                    }
                }

                spriteBatch.Draw(glowmaskTexture, drawPosition, npc.frame, glowmaskColor, rotation, origin, scale, spriteEffects, 0f);
            }

            return false;
        }
    }
}
