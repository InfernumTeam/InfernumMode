using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Events;
using CalamityMod.NPCs.AstrumAureus;
using CalamityMod.Projectiles.Boss;
using InfernumMode.BehaviorOverrides.BossAIs.Ravager;
using InfernumMode.GlobalInstances;
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
            MeteorSlam,
            AstralLaserBursts,
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
        public const float Phase3LifeRatio = 0.45f;
        public const float EnragedDamageFactor = 1.5f;

        public override bool PreAI(NPC npc)
        {
            // Do targeting.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            // Manually offset the boss graphically.
            npc.gfxOffY = -6;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float enrageCountdown = ref npc.ai[3];
            ref float nextAttackType = ref npc.Infernum().ExtraAI[5];
            ref float onlyDoOneJump = ref npc.Infernum().ExtraAI[6];
            ref float frameType = ref npc.localAI[0];
            ref float phase2AnimationTimer = ref npc.localAI[1];

            // Reset things every frame.
            npc.defDefense = 27;
            npc.damage = npc.defDamage;
            npc.defense = npc.defDefense;
            npc.Calamity().DR = 0.4f;

            // Reset gravity affection and tile collision.
            npc.noGravity = false;
            npc.noTileCollide = false;

            GlobalNPCOverrides.AstrumAureus = npc.whoAmI;

            bool enraged = enrageCountdown > 0f || Main.dayTime;

            // Handle enrage interactions.
            if (enraged)
                npc.Calamity().CurrentlyEnraged = true;
            if (enrageCountdown > 0f)
                enrageCountdown--;

            // Start glowing in Phase 2.
            if (lifeRatio < Phase2LifeRatio)
                phase2AnimationTimer++;

            // Despawn if necessary.
            npc.timeLeft = 3600;
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
                    DoAttack_WalkAndShootLasers(npc, target, enraged, lifeRatio, ref attackTimer, ref frameType);
                    break;
                case AureusAttackType.LeapAtTarget:
                case AureusAttackType.MeteorSlam:
                    bool meteorSlam = (AureusAttackType)(int)attackState == AureusAttackType.MeteorSlam;
                    DoAttack_LeapAtTarget(npc, target, meteorSlam, enraged, lifeRatio, ref attackTimer, ref frameType, ref onlyDoOneJump);
                    break;
                case AureusAttackType.RocketBarrage:
                    DoAttack_RocketBarrage(npc, target, enraged, lifeRatio, ref attackTimer, ref frameType);
                    break;
                case AureusAttackType.AstralLaserBursts:
                    DoAttack_AstralLaserBursts(npc, target, enraged, lifeRatio, ref attackTimer, ref frameType, ref enrageCountdown, ref nextAttackType, ref onlyDoOneJump);
                    break;
                case AureusAttackType.CreateAureusSpawnRing:
                    DoAttack_CreateAureusSpawnRing(npc, lifeRatio, ref attackTimer, ref frameType);
                    break;
                case AureusAttackType.CelestialRain:
                    DoAttack_CelestialRain(npc, target, enraged, lifeRatio, ref attackTimer, ref frameType);
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
            npc.noTileCollide = true;

            // Fade away.
            npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.015f, 0f, 1f);

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

            // Disable contact damage.
            npc.damage = 0;

            frameType = (int)AureusFrameType.SitAndRecharge;

            // Go to the next attack after 1.5 seconds, or if hit before then.
            if (attackTimer > 90f || npc.justHit)
                SelectNextAttack(npc);
        }

        public static void DoAttack_Recharge(NPC npc, float attackTimer, ref float frameType)
        {
            // Fall and cease horizontal movement.
            npc.velocity.X *= 0.9f;
            npc.noGravity = false;
            npc.noTileCollide = false;

            // Disable contact damage.
            npc.damage = 0;

            // Use less defense.
            npc.defense = npc.defDefense / 2;

            frameType = (int)AureusFrameType.SitAndRecharge;

            // Go to the next attack after a brief period of time.
            if (attackTimer > 60f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_WalkAndShootLasers(NPC npc, Player target, bool enraged, float lifeRatio, ref float attackTimer, ref float frameType)
        {
            // Adjust directioning.
            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

            // And frames.
            frameType = (int)AureusFrameType.Walk;

            npc.noGravity = true;
            npc.noTileCollide = true;

            float horizontalDistanceFromTarget = MathHelper.Distance(target.Center.X, npc.Center.X);
            bool shouldSlowDown = horizontalDistanceFromTarget < 50f;

            int laserShootRate = (int)MathHelper.Lerp(80f, 50f, 1f - lifeRatio);
            float walkSpeed = MathHelper.Lerp(7f, 10.45f, 1f - lifeRatio);
            walkSpeed += horizontalDistanceFromTarget * 0.0075f;
            walkSpeed *= npc.SafeDirectionTo(target.Center).X;
            if (BossRushEvent.BossRushActive)
            {
                laserShootRate /= 2;
                walkSpeed *= 2.64f;
            }

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

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int laserCount = 16;
                    int laserDamage = 165;
                    float laserSpread = 0.78f;
                    if (enraged)
                    {
                        laserCount += 8;
                        laserDamage = (int)(laserDamage * EnragedDamageFactor);
                        laserSpread += 0.07f;
                    }

                    float openAreaAngle = Main.rand.NextFloatDirection() * laserSpread * 0.6f;
                    for (int i = 0; i < laserCount; i++)
                    {
                        float shootOffsetAngle = MathHelper.Lerp(-laserSpread, laserSpread, i / (float)(laserCount - 1f)) + Main.rand.NextFloatDirection() * 0.02f;
                        if (MathHelper.Distance(openAreaAngle, shootOffsetAngle) < 0.0567f)
                            continue;

                        Vector2 laserShootVelocity = npc.SafeDirectionTo(target.Center + target.velocity * 20f).RotatedBy(shootOffsetAngle) * Main.rand.NextFloat(16f, 20f);
                        Utilities.NewProjectileBetter(npc.Center + laserShootVelocity * 2f, laserShootVelocity, ModContent.ProjectileType<AstralLaser>(), laserDamage, 0f);
                    }

                    laserShootCounter = 0f;
                    npc.netUpdate = true;
                }
            }

            DoTileCollisionStuff(npc, target);
            if (attackTimer >= 540f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_LeapAtTarget(NPC npc, Player target, bool meteorSlam, bool enraged, float lifeRatio, ref float attackTimer, ref float frameType, ref float onlyDoOneJump)
        {
            // Reset tile collision.
            npc.noTileCollide = false;

            // Reset the frame type. It will be changed below as needed.
            frameType = (int)AureusFrameType.Idle;

            // Reset frames if necessary.
            if (attackTimer == 1f)
                npc.frame.Y = 0;

            int jumpDelay = meteorSlam ? 85 : 50;
            int attackTransitionDelay = meteorSlam ? 50 : 1;
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

                        // Jump after a delay.
                        if (attackTimer >= jumpDelay)
                        {
                            npc.spriteDirection = (npc.Center.X < target.Center.X).ToDirectionInt();
                            npc.velocity.X = npc.spriteDirection * 21f;

                            jumpIntensity = 1f;
                            float distanceBelowTarget = npc.Top.Y - (target.Top.Y + 80f);

                            if (distanceBelowTarget > 0f)
                                jumpIntensity = 1f + distanceBelowTarget * 0.00115f;

                            jumpIntensity *= 1.15f;
                            if (enraged)
                                jumpIntensity *= 1.4f;

                            if (jumpIntensity > 3f)
                                jumpIntensity = 3f;

                            // Fly upward.
                            npc.velocity.Y = -17.5f;

                            // Play a sound as an indicator if doing a meteor slam.
                            if (meteorSlam)
                                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ProvidenceHolyBlastShoot"), target.Center);

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
                    if (npc.velocity.Y == 0f && attackTimer > 30f)
                    {
                        frameType = (int)AureusFrameType.Stomp;

                        // Play a stomp sound.
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/LegStomp"), npc.Center);

                        // Reset the jump intensity.
                        jumpIntensity = 0f;

                        // Determine whether the attack should be repeated.
                        int stompCount = meteorSlam ? 3 : 4;
                        if (onlyDoOneJump == 1f)
                            stompCount = 1;

                        stompCounter++;
                        if (stompCounter >= stompCount)
                        {
                            npc.noTileCollide = false;
                            npc.noGravity = false;
                            attackTimer = 0f;
                            attackState = 2f;
                            npc.netUpdate = true;
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

                        // Perform stomp effects.
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int shockwaveDamage = 200;
                            if (enraged)
                                shockwaveDamage = (int)(shockwaveDamage * EnragedDamageFactor);

                            if (meteorSlam)
                            {
                                int meteorDamage = 180;
                                if (enraged)
                                    meteorDamage = (int)(meteorDamage * EnragedDamageFactor);

                                for (int i = 0; i < 2; i++)
                                {
                                    Vector2 spawnPosition = target.Top + new Vector2(MathHelper.Lerp(-900f, 900f, i) + target.velocity.X * 60f, -640f);
                                    Vector2 showerDirection = (target.Center - spawnPosition + target.velocity * 100f).SafeNormalize(Vector2.UnitY);
                                    if (showerDirection.Y < 0.4f)
                                    {
                                        showerDirection.Y = 0.4f;
                                        showerDirection = showerDirection.SafeNormalize(Vector2.UnitY);
                                    }

                                    Utilities.NewProjectileBetter(spawnPosition, showerDirection * 16f, ModContent.ProjectileType<AstralMeteor>(), meteorDamage, 0f, Main.myPlayer);
                                }
                            }
                            else
                            {
                                int missileDamage = 155;
                                if (enraged)
                                    missileDamage = (int)(missileDamage * EnragedDamageFactor);

                                if (lifeRatio < Phase2LifeRatio)
                                {
                                    for (int i = 0; i < 6; i++)
                                    {
                                        Vector2 crystalVelocity = npc.SafeDirectionTo(target.Center).RotatedByRandom(0.9f) * Main.rand.NextFloat(12f, 18f);
                                        Utilities.NewProjectileBetter(npc.Bottom + Vector2.UnitY * 40f, crystalVelocity, ModContent.ProjectileType<AstralMissile>(), missileDamage, 0f);
                                    }
                                }
                                Utilities.NewProjectileBetter(npc.Bottom + Vector2.UnitY * 40f, Vector2.Zero, ModContent.ProjectileType<StompShockwave>(), shockwaveDamage, 0f);
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
                                float fallSpeed = MathHelper.Lerp(0.6f, 0.9f, 1f - lifeRatio);

                                if (jumpIntensity > 1f)
                                    fallSpeed *= jumpIntensity;

                                npc.velocity.Y += fallSpeed;
                            }
                        }
                        else
                        {
                            float horizontalMovementSpeed = Math.Abs(npc.Center.X - target.Center.X) * 0.0001f + 0.08f;

                            if (npc.spriteDirection < 0)
                                npc.velocity.X -= horizontalMovementSpeed;
                            else if (npc.spriteDirection > 0)
                                npc.velocity.X += horizontalMovementSpeed;

                            npc.velocity.X = MathHelper.Clamp(npc.velocity.X, -18f, 18f);
                        }

                        EnforceCustomGravity(npc);
                        npc.noGravity = true;
                    }
                    break;

                // Sit in place for a moment to allow the target to compose themselves.
                case 2:
                    // Slow down.
                    npc.velocity.X *= 0.8f;

                    if (attackTimer > attackTransitionDelay)
                    {
                        onlyDoOneJump = 0f;
                        attackTimer = 0f;
                        attackState = 0f;
                        npc.netUpdate = true;
                        SelectNextAttack(npc);
                    }
                    break;
            }
        }

        public static void DoAttack_RocketBarrage(NPC npc, Player target, bool enraged, float lifeRatio, ref float attackTimer, ref float frameType)
        {
            frameType = (int)AureusFrameType.Idle;

            // Sit in place and periodically release rockets.
            // They shoot more quickly the farther away the target is from the boss.
            int rocketReleaseRate = (int)MathHelper.Lerp(10f, 5f, 1f - lifeRatio);
            ref float rocketShootTimer = ref npc.Infernum().ExtraAI[0];

            if (BossRushEvent.BossRushActive)
                rocketReleaseRate /= 2;

            // Slow down.
            npc.velocity.X *= 0.9f;

            rocketShootTimer++;
            if (rocketShootTimer >= rocketReleaseRate && attackTimer > 75f && attackTimer < 240f)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PlasmaBlast"), npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int missileDamage = 165;
                    Vector2 rocketShootVelocity = npc.SafeDirectionTo(target.Center + target.velocity * 15f).RotatedByRandom(1.16f) * Main.rand.NextFloat(18f, 20.5f);
                    if (enraged)
                    {
                        missileDamage = (int)(missileDamage * EnragedDamageFactor);
                        rocketShootVelocity *= 1.45f;
                    }

                    Utilities.NewProjectileBetter(npc.Center + rocketShootVelocity * 3f, rocketShootVelocity, ModContent.ProjectileType<AstralMissile>(), missileDamage, 0f);

                    rocketShootTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            if (attackTimer >= 300f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_AstralLaserBursts(NPC npc, Player target, bool enraged, float lifeRatio, ref float attackTimer, ref float frameType, ref float enrageCountdown, ref float nextAttack, ref float onlyDoOneJump)
        {
            // Adjust directioning.
            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

            npc.noGravity = true;
            npc.noTileCollide = true;

            float horizontalDistanceFromTarget = MathHelper.Distance(target.Center.X, npc.Center.X);
            bool shouldSlowDown = horizontalDistanceFromTarget < 50f;

            int laserShootDelay = 225;
            float laserSpeed = 9.25f;
            float walkSpeed = MathHelper.Lerp(8f, 12f, 1f - lifeRatio);
            walkSpeed += horizontalDistanceFromTarget * 0.0075f;
            walkSpeed *= npc.SafeDirectionTo(target.Center).X;

            if (npc.WithinRange(target.Center, 920f))
                walkSpeed *= Utils.InverseLerp(laserShootDelay - 90f, 210f, attackTimer, true);
            if (BossRushEvent.BossRushActive)
                laserSpeed *= 2.5f;
            laserSpeed += npc.Distance(target.Center) * 0.01f;

            // Enrage if the player moves too far away.
            if (!npc.WithinRange(target.Center, 1250f) && enrageCountdown <= 0f && attackTimer > laserShootDelay)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/PlagueSounds/PBGNukeWarning"), target.Center);
                enrageCountdown = 360f;
                npc.netUpdate = true;
            }

            // Handle enrage interactions.
            if (enraged)
            {
                laserSpeed *= 1.65f;
                walkSpeed += 5f;
            }

            // Jump to get to the target if far.
            if (attackTimer < laserShootDelay && !npc.WithinRange(target.Center, 1350f))
            {
                nextAttack = (int)AureusAttackType.LeapAtTarget;
                onlyDoOneJump = 1f;
                SelectNextAttack(npc);
                return;
            }

            if (shouldSlowDown)
            {
                // Make the attack go by more quickly if close to the target horizontally.
                if (attackTimer < 360f)
                    attackTimer++;

                npc.velocity.X *= 0.8f;
                if (Math.Abs(npc.velocity.X) < 0.1f)
                    npc.velocity.X = 0f;
            }
            else
                npc.velocity.X = (npc.velocity.X * 15f + walkSpeed) / 16f;

            // Adjust frames.
            frameType = Math.Abs(walkSpeed) > 0f ? (int)AureusFrameType.Walk : (int)AureusFrameType.Idle;

            // Release slow spreads of lasers.
            if (attackTimer > laserShootDelay && attackTimer % 25f == 24f)
            {
                Main.PlaySound(SoundID.Item33, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 25; i++)
                    {
                        Vector2 laserShootVelocity = (MathHelper.TwoPi * (i + Main.rand.NextFloat()) / 25f).ToRotationVector2() * Main.rand.NextFloat(0.8f, 1f) * laserSpeed;
                        Utilities.NewProjectileBetter(npc.Center + laserShootVelocity * 2f, laserShootVelocity, ModContent.ProjectileType<AstralLaser>(), 165, 0f);
                    }

                    npc.netUpdate = true;
                }
            }

            DoTileCollisionStuff(npc, target);
            if (attackTimer >= laserShootDelay + 270f)
                SelectNextAttack(npc);
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
                SelectNextAttack(npc);
        }

        public static void DoAttack_CelestialRain(NPC npc, Player target, bool enraged, float lifeRatio, ref float attackTimer, ref float frameType)
        {
            frameType = (int)AureusFrameType.Idle;

            int cometShootRate = lifeRatio < Phase3LifeRatio ? 3 : 6;
            ref float rainAngle = ref npc.Infernum().ExtraAI[0];

            // Slow down horziontally.
            npc.velocity.X *= 0.9f;

            if (rainAngle == 0f)
                rainAngle = Main.rand.NextFloatDirection() * MathHelper.Pi / 12f;

            // Charge up astral energy and gain a good amount of extra defense.
            if (attackTimer < 90f)
            {
                int dustCount = attackTimer >= 50f ? 2 : 1;
                for (int i = 0; i < dustCount; i++)
                {
                    int dustType = Main.rand.NextBool(2) ? ModContent.DustType<AstralOrange>() : ModContent.DustType<AstralBlue>();
                    float dustScale = i % 2 == 1 ? 1.65f : 1.2f;

                    Vector2 dustSpawnPosition = npc.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(75f, 120f);
                    Dust chargeDust = Dust.NewDustPerfect(dustSpawnPosition, dustType);
                    chargeDust.velocity = (npc.Center - dustSpawnPosition).SafeNormalize(Vector2.UnitY) * (dustCount == 2 ? 7f : 4.6f);
                    chargeDust.scale = dustScale;
                    chargeDust.noGravity = true;
                }

                npc.defense = npc.defDefense + 42;
            }

            // Make an explosion prior to the comets being released.
            if (attackTimer == 120f)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ProvidenceHolyBlastImpact"), target.Center);
                Utilities.CreateGenericDustExplosion(npc.Center, ModContent.DustType<AstralOrange>(), 60, 11f, 1.8f);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % cometShootRate == cometShootRate - 1f && attackTimer > 120f)
            {
                int cometDamage = 160;
                Vector2 cometSpawnPosition = target.Center + new Vector2(Main.rand.NextFloat(-1050, 1050f), -780f);
                Vector2 shootDirection = Vector2.UnitY.RotatedBy(rainAngle);
                Vector2 shootVelocity = shootDirection * 14.5f;
                if (enraged)
                {
                    cometDamage = (int)(cometDamage * EnragedDamageFactor);
                    shootVelocity *= 1.3f;
                }

                int cometType = ModContent.ProjectileType<AstralBlueComet>();
                Utilities.NewProjectileBetter(cometSpawnPosition, shootVelocity, cometType, cometDamage, 0f);
            }

            if (attackTimer > 520f)
                SelectNextAttack(npc);
        }

        public static void DoAttack_AstralDrillLaser(NPC npc, Player target, float lifeRatio, ref float attackTimer, ref float frameType)
        {
            // Adjust directioning.
            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

            npc.noGravity = true;
            npc.noTileCollide = true;

            float horizontalDistanceFromTarget = MathHelper.Distance(target.Center.X, npc.Center.X);
            bool shouldSlowDown = horizontalDistanceFromTarget < 50f;

            int laserShootDelay = 210;
            float walkSpeed = MathHelper.Lerp(8.5f, 13f, 1f - lifeRatio);
            walkSpeed += horizontalDistanceFromTarget * 0.0075f;
            walkSpeed *= Utils.InverseLerp(laserShootDelay * 0.76f, laserShootDelay * 0.5f, attackTimer, true) * npc.SafeDirectionTo(target.Center).X;
            if (BossRushEvent.BossRushActive)
                walkSpeed *= 2.64f;

            if (shouldSlowDown)
            {
                // Make the attack go by more quickly if close to the target horizontally.
                if (attackTimer < 360f)
                    attackTimer++;

                npc.velocity.X *= 0.8f;
                if (Math.Abs(npc.velocity.X) < 0.1f)
                    npc.velocity.X = 0f;
            }
            else
                npc.velocity.X = (npc.velocity.X * 15f + walkSpeed) / 16f;

            // Play a charge sound as a telegraph prior to firing.
            if (attackTimer == laserShootDelay - 155f)
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/CrystylCharge"), target.Center);

            // Adjust frames.
            frameType = Math.Abs(walkSpeed) > 0f ? (int)AureusFrameType.Walk : (int)AureusFrameType.Idle;

            // Release multiple lasers downward that arc upwards over time.
            if (attackTimer == laserShootDelay - 5f)
            {
                // Create a laserbeam fire sound effect.
                Main.PlaySound(SoundID.Zombie, npc.Center, 104);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        int laserbeamType = i == 0 ? ModContent.ProjectileType<OrangeLaserbeam>() : ModContent.ProjectileType<BlueLaserbeam>();
                        int laser = Utilities.NewProjectileBetter(npc.Center, Vector2.UnitY, laserbeamType, 280, 0f);
                        if (Main.projectile.IndexInRange(laser))
                        {
                            int laserDirection = (i == 0).ToDirectionInt();
                            Main.projectile[i].Infernum().ExtraAI[0] = i;
                            Main.projectile[i].ai[0] = MathHelper.Pi / OrangeLaserbeam.LaserLifetime * laserDirection * 0.84f;
                            Main.projectile[i].ai[1] = npc.whoAmI;
                        }
                    }
                }
            }

            // Release slow spreads of lasers after the beams have been released.
            if (attackTimer > laserShootDelay && attackTimer % 18f == 17f && attackTimer < laserShootDelay + OrangeLaserbeam.LaserLifetime)
            {
                Main.PlaySound(SoundID.Item33, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        Vector2 laserShootVelocity = (MathHelper.TwoPi * (i + Main.rand.NextFloat()) / 16f).ToRotationVector2() * 8f;
                        Utilities.NewProjectileBetter(npc.Center + laserShootVelocity * 2f, laserShootVelocity, ModContent.ProjectileType<AstralLaser>(), 165, 0f);
                    }

                    npc.netUpdate = true;
                }
            }

            // Release a volley of lasers that gains more spread the longer the laser has been moving.
            // This is done to prevent people from RoD-ing away to avoid the laserbeams.
            if (attackTimer > laserShootDelay && attackTimer < laserShootDelay + OrangeLaserbeam.LaserLifetime)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float laserLifetimeCompletion = Utils.InverseLerp(laserShootDelay, laserShootDelay + OrangeLaserbeam.LaserLifetime, attackTimer, true);

                    for (int i = 0; i < 1 + laserLifetimeCompletion * 2f; i++)
                    {
                        float laserRotation = MathHelper.Pi * laserLifetimeCompletion * OrangeLaserbeam.FullCircleRotationFactor;
                        Vector2 laserShootVelocity = Vector2.UnitY.RotatedByRandom(laserRotation) * Main.rand.NextFloat(9f, 18f);
                        Utilities.NewProjectileBetter(npc.Center + laserShootVelocity * 2f, laserShootVelocity, ModContent.ProjectileType<AstralLaser>(), 180, 0f);
                    }
                }
            }

            DoTileCollisionStuff(npc, target);
            if (attackTimer >= laserShootDelay + OrangeLaserbeam.LaserLifetime + 75)
                SelectNextAttack(npc);
        }
        #endregion Custom Behaviors

        #region Misc AI Operations
        public static void DoTileCollisionStuff(NPC npc, Player target)
        {
            // Check if tile collision ignoral is necessary.
            int horizontalCheckArea = 80;
            int verticalCheckArea = 20;
            Vector2 checkPosition = new Vector2(npc.Center.X - horizontalCheckArea * 0.5f, npc.Bottom.Y - verticalCheckArea);
            bool onPlatforms = false;
            for (int i = (int)(npc.BottomLeft.X / 16f); i < (int)(npc.BottomRight.X / 16f); i++)
            {
                Tile tile = CalamityUtils.ParanoidTileRetrieval(i, (int)(npc.Bottom.Y / 16f) + 1);
                if (CalamityUtils.IsTileSolidGround(tile))
                {
                    onPlatforms = true;
                    break;
                }
            }
            if (target.Top.Y > npc.Bottom.Y)
                onPlatforms = false;

            if (Collision.SolidCollision(checkPosition, horizontalCheckArea, verticalCheckArea) || onPlatforms)
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

        public static void SelectNextAttack(NPC npc)
        {
            Player target = Main.player[npc.target];

            // Increment the attack counter.
            npc.ai[2]++;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            double jumpWeight = 1D + Utils.InverseLerp(640f, 1860f, npc.Center.Y - target.Center.Y, true) * 4f;
            if (jumpWeight < 1D)
                jumpWeight = 1D;

            AureusAttackType oldAttackState = (AureusAttackType)(int)npc.ai[0];
            AureusAttackType newAttackState;
            WeightedRandom<AureusAttackType> attackSelector = new WeightedRandom<AureusAttackType>(Main.rand);
            attackSelector.Add(AureusAttackType.WalkAndShootLasers);
            if (oldAttackState != AureusAttackType.CelestialRain)
            {
                attackSelector.Add(AureusAttackType.LeapAtTarget, jumpWeight * 0.7);
                attackSelector.Add(AureusAttackType.MeteorSlam, jumpWeight * 0.7);
            }
            attackSelector.Add(AureusAttackType.RocketBarrage);

            if (lifeRatio >= Phase3LifeRatio)
                attackSelector.Add(AureusAttackType.AstralLaserBursts);

            if (lifeRatio < Phase2LifeRatio)
            {
                attackSelector.Add(AureusAttackType.CreateAureusSpawnRing, 0.85);
                if (oldAttackState != AureusAttackType.LeapAtTarget && oldAttackState != AureusAttackType.MeteorSlam)
                    attackSelector.Add(AureusAttackType.CelestialRain);
            }

            if (lifeRatio < Phase3LifeRatio && !NPC.AnyNPCs(ModContent.NPCType<AureusSpawn>()))
                attackSelector.Add(AureusAttackType.AstralDrillLaser, 2D);

            do
                newAttackState = attackSelector.Get();
            while (newAttackState == oldAttackState || (int)newAttackState == (int)npc.Infernum().ExtraAI[7]);

            // Always use a consistent attack after the spawn activation.
            if (oldAttackState == AureusAttackType.SpawnActivation)
                newAttackState = AureusAttackType.WalkAndShootLasers;

            // Recharge once the attack counter every few attacks.
            if (npc.ai[2] >= 4f)
            {
                newAttackState = AureusAttackType.Recharge;
                npc.ai[2] = 0f;
            }

            if (npc.Infernum().ExtraAI[5] > 0f)
            {
                newAttackState = (AureusAttackType)npc.Infernum().ExtraAI[5];
                npc.Infernum().ExtraAI[5] = 0f;
                npc.ai[2]--;
            }

            npc.ai[0] = (int)newAttackState;
            npc.ai[1] = 0f;

            if (newAttackState != AureusAttackType.Recharge)
                npc.Infernum().ExtraAI[7] = npc.ai[0];
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

            Vector2 drawPosition = npc.Center - Main.screenPosition + Vector2.UnitY * npc.gfxOffY;

            // Draw the second phase back afterimage if applicable.
            float backAfterimageInterpolant = Utils.InverseLerp(0f, 180f, npc.localAI[1], true) * npc.Opacity;
            if (backAfterimageInterpolant > 0f)
            {
                for (int i = 0; i < 6; i++)
                {
                    float colorInterpolant = (float)Math.Cos(MathHelper.SmoothStep(0f, MathHelper.TwoPi, i / 6f) + Main.GlobalTime * 10f) * 0.5f + 0.5f;
                    Color backAfterimageColor = Color.Lerp(new Color(109, 242, 196, 0), new Color(255, 119, 102, 0), colorInterpolant);
                    backAfterimageColor *= backAfterimageInterpolant;
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * backAfterimageInterpolant * 8f;
                    spriteBatch.Draw(texture, drawPosition + drawOffset, npc.frame, backAfterimageColor, rotation, origin, scale, spriteEffects, 0f);
                }
            }

            // Draw the normal texture.
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
