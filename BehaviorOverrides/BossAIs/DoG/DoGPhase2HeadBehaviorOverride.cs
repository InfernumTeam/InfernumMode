using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    // TODO - Refactor this AI. It really needs it.
    public static class DoGPhase2HeadBehaviorOverride
    {
        public enum SpecialAttackType
        {
            LaserWalls,
            CircularLaserBurst,
            ChargeGates
        }

        #region AI
        public static bool Phase2AI(NPC npc)
        {
            npc.Calamity().CanHaveBossHealthBar = true;
            ref float specialAttackState = ref npc.Infernum().ExtraAI[14];
            ref float specialAttackTimer = ref npc.Infernum().ExtraAI[15];
            ref float nearDeathFlag = ref npc.Infernum().ExtraAI[16];
            ref float spawnedSegmentsFlag = ref npc.Infernum().ExtraAI[17];
            ref float sentinelAttackTimer = ref npc.Infernum().ExtraAI[18];
            ref float signusAttackState = ref npc.Infernum().ExtraAI[19];
            ref float jawRotation = ref npc.Infernum().ExtraAI[20];
            ref float chompTime = ref npc.Infernum().ExtraAI[21];
            ref float time = ref npc.Infernum().ExtraAI[22];
            ref float flyAcceleration = ref npc.Infernum().ExtraAI[23];
            ref float horizontalRunAnticheeseCounter = ref npc.Infernum().ExtraAI[24];
            ref float trapChargeTimer = ref npc.Infernum().ExtraAI[25];
            ref float trapChargePortalIndex = ref npc.Infernum().ExtraAI[26];
            ref float totalCharges = ref npc.Infernum().ExtraAI[27];
            ref float fadeinTimer = ref npc.Infernum().ExtraAI[28];
            ref float fadeinPortalIndex = ref npc.Infernum().ExtraAI[29];
            ref float specialAttackPortalIndex = ref npc.Infernum().ExtraAI[30];
            ref float fireballShootTimer = ref npc.Infernum().ExtraAI[31];
            ref float deathTimer = ref npc.Infernum().ExtraAI[32];

            if (!Main.player.IndexInRange(npc.target) || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest(false);

            Player target = Main.player[npc.target];

            target.Calamity().normalityRelocator = false;
            target.Calamity().spectralVeil = false;

            npc.takenDamageMultiplier = 4f;

            // whoAmI variable
            CalamityGlobalNPC.DoGHead = npc.whoAmI;

            // Handle fade-in logic when the boss is summoned.
            if (fadeinTimer < 280f)
            {
                npc.TargetClosest();
                if (!Utilities.AnyProjectiles(ModContent.ProjectileType<DoGRealityRendEntranceGate>()))
                {
                    npc.Center = Main.player[Player.FindClosest(npc.Center, 1, 1)].Center - Vector2.UnitY * 600f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        fadeinPortalIndex = Projectile.NewProjectile(npc.Center, Vector2.Zero, ModContent.ProjectileType<DoGRealityRendEntranceGate>(), 0, 0f);
                }

                // Reset the special attack portal index to -1.
                specialAttackPortalIndex = -1f;

                npc.Center = Main.player[Player.FindClosest(npc.Center, 1, 1)].Center - Vector2.UnitY * MathHelper.Lerp(6000f, 3000f, fadeinTimer / 280f);
                fadeinTimer++;

                if (fadeinTimer >= 280f)
                {
                    npc.Opacity = 1f;
                    npc.Center = Main.projectile[(int)fadeinPortalIndex].Center;
                    npc.velocity = npc.SafeDirectionTo(target.Center) * 36f;
                    npc.netUpdate = true;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectile(npc.Center, Vector2.Zero, ModContent.ProjectileType<DoGSpawnBoom>(), 0, 0f);
                }
                return false;
            }

            // Percent life remaining
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Variables
            bool canPerformSpecialAttacks = lifeRatio < 0.7;
            bool nearDeath = lifeRatio < 0.2;
            bool breathFireMore = lifeRatio < 0.15;

            // Don't take damage when fading out.
            npc.dontTakeDamage = npc.Opacity < 0.5f;
            npc.damage = npc.dontTakeDamage ? 0 : 6000;
            npc.Calamity().DR = 0.3f;

            // Stay in the world.
            npc.position.Y = MathHelper.Clamp(npc.position.Y, 180f, Main.maxTilesY * 16f - 180f);

            if (deathTimer > 0f)
            {
                DoDeathEffects(npc, deathTimer);
                jawRotation = jawRotation.AngleTowards(0f, 0.07f);
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                deathTimer++;
                return false;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && nearDeath && npc.alpha == 0)
            {
                if (Main.rand.NextBool(950))
                {
                    Vector2 portalSpawnPosition = target.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(475f, 820f);
                    Projectile.NewProjectile(portalSpawnPosition, Vector2.Zero, ModContent.ProjectileType<ScytheSummoningPortal>(), 0, 0f, Main.myPlayer, 0f, Main.myPlayer);
                }
            }

            // Handle special attacks.
            if (canPerformSpecialAttacks && trapChargeTimer <= 0f)
            {
                if (!DoSpecialAttacks(npc, target, nearDeath, ref specialAttackState, ref specialAttackTimer, ref specialAttackPortalIndex))
                    return false;
            }
            else
            {
                // Fade in.
                npc.alpha = Utils.Clamp(npc.alpha - 6, 0, 255);

                // Reset the special attack state, just in case.
                if (specialAttackState > 0)
                    specialAttackState = 0;
            }
            specialAttackPortalIndex = -1f;

            // Anger message
            if (nearDeath)
            {
                if (nearDeathFlag == 0f)
                {
                    Main.NewText("A GOD DOES NOT FEAR DEATH!", Color.Cyan);
                    nearDeathFlag = 1f;
                }
            }

            time++;

            int totalSentinelAttacks = 1;
            if (lifeRatio < 0.8f)
                totalSentinelAttacks++;
            if (lifeRatio < 0.6f)
                totalSentinelAttacks++;

            // Do sentinel attacks.
            if (totalSentinelAttacks >= 1 && npc.alpha <= 0)
                sentinelAttackTimer += 1f;
            if (sentinelAttackTimer >= totalSentinelAttacks * 900f)
                sentinelAttackTimer = 0f;

            DoSentinelAttacks(npc, target, ref sentinelAttackTimer, ref signusAttackState);

            // Light
            Lighting.AddLight((int)((npc.position.X + npc.width / 2) / 16f), (int)((npc.position.Y + npc.height / 2) / 16f), 0.2f, 0.05f, 0.2f);

            // Worm shit.
            if (npc.ai[3] > 0f)
                npc.realLife = (int)npc.ai[3];

            // Fire projectiles.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (npc.alpha <= 0 && !npc.WithinRange(target.Center, 500f))
                {
                    fireballShootTimer++;
                    if (fireballShootTimer >= 150f && fireballShootTimer % (breathFireMore ? 60f : 120f) == 0f)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            Vector2 flameShootVelocity = npc.velocity.RotatedBy(MathHelper.Lerp(-0.4f, 0.4f, i / 3f)) * Main.rand.NextFloat(1.2f, 1.5f);
                            Utilities.NewProjectileBetter(npc.Center, flameShootVelocity, ModContent.ProjectileType<HomingDoGBurst>(), 415, 0f);
                        }
                    }
                }
                else if (npc.WithinRange(target.Center, 250f))
                    fireballShootTimer--;
            }

            // Despawn
            if (!NPC.AnyNPCs(InfernumMode.CalamityMod.NPCType("DevourerofGodsTail")))
                npc.active = false;

            // Chomping after attempting to eat the player.
            bool chomping = npc.Infernum().ExtraAI[14] == 0f && DoChomp(npc, target, ref chompTime, ref jawRotation);

            // Do movement and barrier sneak attack anticheese.
            if (!target.dead && target.active)
            {
                if (specialAttackState == 0f && trapChargeTimer >= 1f)
                    DoBarrierSneakAttack(npc, target, ref trapChargeTimer, ref totalCharges, ref specialAttackTimer, ref trapChargePortalIndex);
                else
                {
                    DoAggressiveFlyMovement(npc, target, chomping, ref jawRotation, ref chompTime, ref time, ref flyAcceleration);

                    if (specialAttackState == 0f)
                        DoAnticheeseRunChecks(npc, target, ref horizontalRunAnticheeseCounter, ref trapChargeTimer);
                }
            }

            // Despawn if no valid target exists.
            else
                Despawn(npc);

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
                    if (Main.npc[a].type == InfernumMode.CalamityMod.NPCType("DevourerofGodsHead") ||
                        Main.npc[a].type == InfernumMode.CalamityMod.NPCType("DevourerofGodsBody") ||
                        Main.npc[a].type == InfernumMode.CalamityMod.NPCType("DevourerofGodsTail"))
                    {
                        Main.npc[a].active = false;
                        Main.npc[a].netUpdate = true;
                    }
                }
            }
        }

        public static void DoSentinelAttacks(NPC npc, Player target, ref float sentinelAttackTimer, ref float signusAttackState)
        {
            // Ceaseless Void Effect (Chaser Portal)
            if (sentinelAttackTimer > 0f && sentinelAttackTimer <= 900f && npc.alpha <= 0)
            {
                if (sentinelAttackTimer % 360f == 0f)
                {
                    Vector2 spawnPosition = new Vector2(Main.rand.NextFloat(300f, 600f) * Main.rand.NextBool().ToDirectionInt(),
                                       Main.rand.NextFloat(300, 600f) * Main.rand.NextBool().ToDirectionInt());
                    Projectile.NewProjectile(target.Center +
                        spawnPosition,
                        Vector2.Zero,
                        ModContent.ProjectileType<DoGBeamPortalN>(),
                        0, 0f);
                }
            }
            // Ceaseless Void Effect (Lightning Storm)
            if (sentinelAttackTimer > 900f && sentinelAttackTimer <= 900f * 2f && npc.alpha <= 0)
            {
                if (sentinelAttackTimer % 200 == 0f)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        int fuck = Projectile.NewProjectile(target.Center +
                            new Vector2(Main.rand.NextFloat(-290f, 290f), -1200f),
                            Vector2.Zero,
                            ModContent.ProjectileType<Lightning>(),
                            85, 0f, npc.target);
                        if (Main.projectile.IndexInRange(fuck))
                            Main.projectile[fuck].ai[0] = 1.4f;
                    }
                }
                if (sentinelAttackTimer == 900f * 2f - 1f)
                    signusAttackState = Main.rand.Next(2);
            }
            // Signus Effect (Essence Storm)
            if (sentinelAttackTimer > 900f * 2f && sentinelAttackTimer <= 900f * 3f && npc.alpha <= 0)
            {
                // Left/Right
                if (sentinelAttackTimer % 900 == 1f)
                {
                    Projectile.NewProjectile(target.Center +
                        ((signusAttackState == 0f) ? MathHelper.Pi : MathHelper.TwoPi).ToRotationVector2() * 800f,
                        Vector2.Zero,
                        ModContent.ProjectileType<EssenceChain>(),
                        0, 0f, npc.target, (signusAttackState == 0f) ? MathHelper.Pi : MathHelper.TwoPi);
                }
                // Up
                if (sentinelAttackTimer % 900 == 200f)
                {
                    Projectile.NewProjectile(target.Center - new Vector2(0f, 800f),
                        Vector2.Zero,
                        ModContent.ProjectileType<EssenceChain>(),
                        0, 0f, npc.target, MathHelper.PiOver2);
                }
                // Right/Left
                if (sentinelAttackTimer % 900 == 400f)
                {
                    Projectile.NewProjectile(target.Center + new Vector2(-800f * (signusAttackState == 0f).ToDirectionInt(), 0f),
                        Vector2.Zero,
                        ModContent.ProjectileType<EssenceChain>(),
                        0, 0f, npc.target, (signusAttackState == 0f) ? MathHelper.TwoPi : MathHelper.Pi);
                }
                // Down
                if (sentinelAttackTimer % 900 == 600f)
                {
                    Projectile.NewProjectile(target.Center +
                        new Vector2(0f, 800f),
                        Vector2.Zero,
                        ModContent.ProjectileType<EssenceChain>(),
                        0, 0f, npc.target, MathHelper.Pi * 1.5f);
                }
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
                    Main.PlaySound(SoundID.Item94, npc.Center);

                List<int> segments = new List<int>()
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

            float idealSpeed = MathHelper.Lerp(9f, 4.75f, Utils.InverseLerp(15f, 210f, deathTimer, true));
            ref float destroyedSegmentsCounts = ref npc.Infernum().ExtraAI[34];
            if (npc.velocity.Length() != idealSpeed)
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(npc.velocity.Length(), idealSpeed, 0.08f);

            if (deathTimer == 120f)
                Main.NewText("I WILL NOT BE DESTROYED!!!", Color.Cyan);
            if (deathTimer == 170f)
                Main.NewText("I WILL NOT BE DESTROYED!!!", Color.Cyan);
            if (deathTimer == 220f)
                Main.NewText("I WILL NOT BE DESTROYED!!!!", Color.Cyan);

            if (deathTimer >= 120f && deathTimer < 380f && deathTimer % 4f == 0f)
            {
                int segmentToDestroy = (int)(Utils.InverseLerp(120f, 380f, deathTimer, true) * 60f);
                destroySegment(segmentToDestroy, ref destroyedSegmentsCounts);
            }

            if (deathTimer == 330f)
                Main.NewText("I WILL NOT...", Color.Cyan);

            if (deathTimer == 420f)
                Main.NewText("I...", Color.Cyan);

            if (deathTimer == 442f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(npc.Center, Vector2.Zero, ModContent.ProjectileType<DoGSpawnBoom>(), 0, 0f);

                if (Main.netMode != NetmodeID.Server)
                {
                    var soundInstance = Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/DevourerSpawn"), npc.Center);
                    if (soundInstance != null)
                        soundInstance.Volume = MathHelper.Clamp(soundInstance.Volume * 1.6f, 0f, 1f);

                    for (int i = 0; i < 3; i++)
                    {
                        soundInstance = Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/TeslaCannonFire"), npc.Center);
                        if (soundInstance != null)
                        {
                            soundInstance.Pitch = -MathHelper.Lerp(0.1f, 0.4f, i / 3f);
                            soundInstance.Volume = 0.21f;
                        }
                    }
                }
            }

            if (deathTimer >= 410f && deathTimer < 470f && deathTimer % 2f == 0f)
            {
                int segmentToDestroy = (int)(Utils.InverseLerp(410f, 470f, deathTimer, true) * 10f) + 60;
                destroySegment(segmentToDestroy, ref destroyedSegmentsCounts);
            }

            float light = Utils.InverseLerp(430f, 465f, deathTimer, true);
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

        public static bool DoChomp(NPC npc, Player target, ref float chompTime, ref float jawRotation)
        {
            bool chomping = chompTime > 0f;
            float idealChompAngle = MathHelper.ToRadians(-18f);
            if (chomping)
            {
                chompTime--;

                if (jawRotation != idealChompAngle)
                {
                    jawRotation = jawRotation.AngleTowards(idealChompAngle, 0.12f);

                    if (Math.Abs(jawRotation - idealChompAngle) < 0.001f)
                    {
                        for (int i = 0; i < 40; i++)
                        {
                            Dust electricity = Dust.NewDustPerfect(npc.Center - Vector2.UnitY.RotatedBy(npc.rotation) * 52f, 229);
                            electricity.velocity = ((MathHelper.TwoPi / 40f * i).ToRotationVector2() * new Vector2(7f, 4f)).RotatedBy(npc.rotation) + npc.velocity * 1.5f;
                            electricity.noGravity = true;
                            electricity.scale = 2.6f;
                        }
                        jawRotation = idealChompAngle;
                    }
                }
            }
            return chomping;
        }

        public static void DoBarrierSneakAttack(NPC npc, Player target, ref float trapChargeTimer, ref float totalCharges, ref float specialAttackTimer, ref float trapChargePortalIndex)
        {
            if (trapChargeTimer < 180f)
                npc.Opacity = MathHelper.Lerp(npc.Opacity, 0f, 0.28f);

            float chargeSpeed = npc.life / (float)npc.lifeMax < 0.2f ? 75f : 68f;
            if ((int)trapChargeTimer == 45f)
            {
                npc.Opacity = 0f;
                trapChargePortalIndex = Projectile.NewProjectile(target.Center + Main.rand.NextVector2CircularEdge(800f, 800f), Vector2.Zero, ModContent.ProjectileType<DoGChargeGate>(), 0, 0f);
            }

            if (trapChargeTimer == 180f)
            {
                Vector2 portalPosition = Main.projectile[(int)trapChargePortalIndex].Center;
                npc.Center = portalPosition;
                npc.netUpdate = true;

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && (Main.npc[i].type == ModContent.NPCType<DevourerofGodsBody>() || Main.npc[i].type == ModContent.NPCType<DevourerofGodsTail>()))
                    {
                        Main.npc[i].Center = npc.Center;
                        Main.npc[i].netUpdate = true;
                    }
                }
                npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/DoGAttack"), target.Center);
            }

            if (trapChargeTimer > 240f && trapChargeTimer < 320f)
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.09f);

            if (trapChargeTimer > 380f && trapChargeTimer < 620f)
            {
                // Create the initial charge portal and release a bunch of homing flames.
                if (Main.netMode != NetmodeID.MultiplayerClient && trapChargeTimer == 381f)
                {
                    trapChargePortalIndex = Projectile.NewProjectile(npc.Center, Vector2.Zero, ModContent.ProjectileType<DoGChargeGate>(), 0, 0f);
                    Main.projectile[(int)trapChargePortalIndex].localAI[0] = 1f;

                    // Create the homing flames.
                    float flameBurstOffsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                    for (int i = 0; i < 15; i++)
                    {
                        Vector2 flameShootVelocity = (MathHelper.TwoPi * i / 15f + flameBurstOffsetAngle).ToRotationVector2() * 23f;
                        Utilities.NewProjectileBetter(npc.Center + flameShootVelocity * 3f, flameShootVelocity, ModContent.ProjectileType<HomingDoGBurst>(), 415, 0f);
                    }

                    npc.netUpdate = true;
                }

                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(npc.velocity.Length(), 64f, 0.2f);
                npc.Opacity = MathHelper.Lerp(npc.Opacity, 0f, 0.8f);
            }

            trapChargeTimer++;
            if (trapChargeTimer > 520f)
                npc.Opacity = MathHelper.Lerp(npc.Opacity, 1f, 0.4f);

            if (trapChargeTimer > 580f)
            {
                trapChargeTimer = totalCharges < 2f ? 0f : 1f;

                // If we do not intend to charge again, delete the lightning barriers, and do a final teleport above the player.
                if (trapChargeTimer == 0f)
                {
                    // Reset the laser wall-esque attack timer.
                    specialAttackTimer = 0f;
                    totalCharges = 0f;

                    // Delete the lightning barriers.
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<DoGLightningBarrier>())
                            Main.projectile[i].Kill();
                    }

                    // Teleport.
                    npc.Center = target.Center - Vector2.UnitY * 3300f;

                    // And bring all segments to the teleport position as well.
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (Main.npc[i].active && (Main.npc[i].type == ModContent.NPCType<DevourerofGodsBody>() || Main.npc[i].type == ModContent.NPCType<DevourerofGodsTail>()))
                        {
                            Main.npc[i].Center = npc.Center;
                            Main.npc[i].netUpdate = true;
                        }
                    }
                    npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed * 0.75f;
                    npc.Opacity = 1f;
                    npc.dontTakeDamage = false;
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoAggressiveFlyMovement(NPC npc, Player target, bool chomping, ref float jawRotation, ref float chompTime, ref float time, ref float flyAcceleration)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float idealFlyAcceleration = MathHelper.Lerp(0.05f, 0.037f, lifeRatio);
            float idealFlySpeed = MathHelper.Lerp(20.5f, 15f, lifeRatio);
            float idealMouthOpeningAngle = MathHelper.ToRadians(32f);
            float flySpeedFactor = 1f + lifeRatio * 0.5f;

            if (BossRushEvent.BossRushActive)
                idealFlySpeed *= 1.4f;

            Vector2 destination = target.Center;

            float distanceFromDestination = npc.Distance(destination);
            if (npc.Distance(destination) > 525f)
            {
                destination += (time % 60f / 60f * MathHelper.TwoPi).ToRotationVector2() * 145f;
                distanceFromDestination = npc.Distance(destination);
                idealFlyAcceleration *= 1.45f;
                flySpeedFactor = 1.55f;
            }

            float swimOffsetAngle = (float)Math.Sin(MathHelper.TwoPi * time / 160f) * Utils.InverseLerp(400f, 540f, distanceFromDestination, true) * 0.41f;

            // Charge if the player is far away.
            // Don't do this at the start of the fight though. Doing so might lead to an unfair
            // charge.
            if (distanceFromDestination > 1500f && time > 120f)
            {
                idealFlyAcceleration = MathHelper.Min(6f, flyAcceleration + 1f);
                idealFlySpeed *= 2f;
            }

            flyAcceleration = MathHelper.Lerp(flyAcceleration, idealFlyAcceleration, 0.3f);

            float directionToPlayerOrthogonality = Vector2.Dot(npc.velocity.SafeNormalize(Vector2.Zero), npc.SafeDirectionTo(destination));

            // Adjust the speed based on how the direction towards the target compares to the direction of the
            // current velocity. This check is unnecessary once close to the target, which would prompt a snap/charge.
            if (npc.Distance(destination) > 200f)
            {
                float speed = npc.velocity.Length();
                if (speed < 18.5f)
                    speed += 0.08f;

                if (speed > 26f)
                    speed -= 0.08f;

                if (directionToPlayerOrthogonality < 0.85f && directionToPlayerOrthogonality > 0.5f)
                    speed += 0.24f;

                if (directionToPlayerOrthogonality < 0.5f && directionToPlayerOrthogonality > -0.7f)
                    speed -= 0.1f;

                speed = MathHelper.Clamp(speed, flySpeedFactor * 15f, flySpeedFactor * 35f);

                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(destination) + swimOffsetAngle, flyAcceleration, true) * speed;
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(destination) * speed, flyAcceleration * 15f);
            }

            // Jaw opening when near player.
            if (!chomping)
            {
                float distanceFromTarget = npc.Distance(target.Center);
                if ((distanceFromTarget < 330f && directionToPlayerOrthogonality > 0.79f) || (distanceFromTarget < 550f && directionToPlayerOrthogonality > 0.87f))
                {
                    jawRotation = jawRotation.AngleTowards(idealMouthOpeningAngle, 0.028f);
                    if (distanceFromDestination * 0.5f < 56f)
                    {
                        if (chompTime == 0f)
                        {
                            chompTime = 18f;
                            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.NPCHit, "Sounds/NPCHit/OtherworldlyHit"), npc.Center);
                        }
                    }
                }
                else
                {
                    jawRotation = jawRotation.AngleTowards(0f, 0.07f);
                }
            }

            // Lunge if near the player, and prepare to chomp.
            if (distanceFromDestination * 0.5f < 160f && directionToPlayerOrthogonality > 0.45f && npc.velocity.Length() < idealFlySpeed * 1.7f)
            {
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * npc.velocity.Length() * 1.5f;
                jawRotation = jawRotation.AngleLerp(idealMouthOpeningAngle, 0.55f);

                if (chompTime == 0f)
                {
                    chompTime = 26f;
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.NPCHit, "Sounds/NPCHit/OtherworldlyHit"), npc.Center);
                }
            }
        }

        public static void DoAnticheeseRunChecks(NPC npc, Player target, ref float horizontalRunAnticheeseCounter, ref float trapChargeTimer)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float anticheeseIncrement = MathHelper.Lerp(0.08f, 1.8f, Utils.InverseLerp(400f, 2800f, npc.Distance(target.Center), true));

            if (lifeRatio > 0.7f)
                anticheeseIncrement *= 0.6f;
            horizontalRunAnticheeseCounter += anticheeseIncrement;

            if (horizontalRunAnticheeseCounter >= 1020f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        float xOffset = Main.rand.NextFloat(1185f, 1205f);
                        Utilities.NewProjectileBetter(target.Center + Vector2.UnitX * xOffset, Vector2.Zero, ModContent.ProjectileType<DoGLightningBarrier>(), 655, 0f, npc.target);

                        xOffset = Main.rand.NextFloat(1185f, 1205f);
                        Utilities.NewProjectileBetter(target.Center - Vector2.UnitX * xOffset, Vector2.Zero, ModContent.ProjectileType<DoGLightningBarrier>(), 655, 0f, npc.target);
                    }
                }
                trapChargeTimer = 1f;
                horizontalRunAnticheeseCounter = 0f;
            }
        }

        public static bool DoSpecialAttacks(NPC npc, Player target, bool nearDeath, ref float specialAttackState, ref float specialAttackTimer, ref float specialAttackPortalIndex)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            SpecialAttackType specialAttackType;

            ref float initialTeleportPortal = ref npc.Infernum().ExtraAI[36];

            if (nearDeath)
                npc.Infernum().ExtraAI[2] = 9f;

            if (npc.Infernum().ExtraAI[2] <= 3)
                specialAttackType = SpecialAttackType.LaserWalls;
            else if (npc.Infernum().ExtraAI[2] <= 5)
                specialAttackType = SpecialAttackType.CircularLaserBurst;
            else
                specialAttackType = SpecialAttackType.ChargeGates;

            if (specialAttackState == 0) // Start special attack phase.
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // The charge gate attack happens much more frequently when DoG is close to death.
                    specialAttackTimer += nearDeath && specialAttackTimer < 1150f ? 2f : 1f;

                    // Enter a portal before performing a special attack.
                    if (Main.netMode != NetmodeID.MultiplayerClient && (int)specialAttackTimer == 1200)
                    {
                        specialAttackPortalIndex = Projectile.NewProjectile(npc.Center + npc.velocity * 75f, Vector2.Zero, ModContent.ProjectileType<DoGChargeGate>(), 0, 0f);
                        Main.projectile[(int)specialAttackPortalIndex].localAI[0] = 1f;
                        npc.netUpdate = true;
                    }

                    if (specialAttackTimer >= 1450)
                    {
                        specialAttackTimer = 0f;
                        specialAttackState = 1f;
                        specialAttackPortalIndex = -1f;

                        // Select a special attack type.
                        npc.Infernum().ExtraAI[2] = Main.rand.Next(10);
                        npc.netUpdate = true;
                    }

                    // Do nothing and drift into the portal.
                    if (specialAttackTimer >= 1200f)
                    {
                        npc.damage = 0;
                        npc.dontTakeDamage = true;
                        npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitX) * MathHelper.Lerp(npc.velocity.Length(), 105f, 0.15f);

                        // Disappear when touching the portal.
                        // This same logic applies to body/tail segments.
                        if (Main.projectile.IndexInRange((int)specialAttackPortalIndex) && npc.Hitbox.Intersects(Main.projectile[(int)specialAttackPortalIndex].Hitbox))
                            npc.alpha = 255;

                        return false;
                    }
                }
            }
            else if (specialAttackState == 1) // Enter a portal and begin the special attack.
            {
                if (npc.alpha >= 255 || specialAttackType == SpecialAttackType.ChargeGates)
                {
                    specialAttackTimer += 1f;
                    if (specialAttackTimer >= 710)
                    {
                        specialAttackTimer = 0f;
                        specialAttackState = 2f;
                    }

                    if (specialAttackTimer < 650f)
                    {
                        switch (specialAttackType)
                        {
                            case SpecialAttackType.LaserWalls:
                                int offsetPerLaser = 95;
                                float laserWallSpeed = 16f;
                                if (specialAttackTimer % 75f == 74f && specialAttackTimer <= 620f)
                                {
                                    Main.PlaySound(SoundID.Item12, (int)target.position.X, (int)target.position.Y);

                                    float targetPosY = target.position.Y + (Main.rand.NextBool(2) ? 50f : 0f);

                                    // Side walls
                                    for (int x = -10; x < 10; x++)
                                    {
                                        Utilities.NewProjectileBetter(target.position.X + 1000f, targetPosY + x * offsetPerLaser, -laserWallSpeed, 0f, InfernumMode.CalamityMod.ProjectileType("DoGDeath"), 415, 0f, Main.myPlayer, 0f, 0f);
                                        Utilities.NewProjectileBetter(target.position.X - 1000f, targetPosY + x * offsetPerLaser, laserWallSpeed, 0f, InfernumMode.CalamityMod.ProjectileType("DoGDeath"), 415, 0f, Main.myPlayer, 0f, 0f);
                                    }

                                    if (Main.rand.NextBool(2))
                                    {
                                        for (int x = -5; x < 5; x++)
                                        {
                                            Utilities.NewProjectileBetter(target.position.X + 1000f, targetPosY + x * (Main.rand.NextBool(2) ? 180 : 200), -laserWallSpeed, 0f, InfernumMode.CalamityMod.ProjectileType("DoGDeath"), 415, 0f, Main.myPlayer, 0f, 0f);
                                            Utilities.NewProjectileBetter(target.position.X - 1000f, targetPosY + x * (Main.rand.NextBool(2) ? 180 : 200), laserWallSpeed, 0f, InfernumMode.CalamityMod.ProjectileType("DoGDeath"), 415, 0f, Main.myPlayer, 0f, 0f);
                                        }
                                    }

                                    // Lower wall
                                    for (int x = -12; x <= 12; x++)
                                    {
                                        Utilities.NewProjectileBetter(target.position.X + x * offsetPerLaser, target.position.Y + 1000f, 0f, -laserWallSpeed, InfernumMode.CalamityMod.ProjectileType("DoGDeath"), 415, 0f, Main.myPlayer, 0f, 0f);
                                    }

                                    // Upper wall
                                    if (lifeRatio < 0.4f)
                                    {
                                        for (int x = -20; x < 20; x++)
                                            Utilities.NewProjectileBetter(target.position.X + x * offsetPerLaser, target.position.Y - 1000f, 0f, laserWallSpeed, InfernumMode.CalamityMod.ProjectileType("DoGDeath"), 415, 0f, Main.myPlayer, 0f, 0f);
                                    }
                                    else
                                    {
                                        for (int x = -12; x <= 12; x++)
                                            Utilities.NewProjectileBetter(target.position.X + x * offsetPerLaser, target.position.Y - 1000f, 0f, laserWallSpeed, InfernumMode.CalamityMod.ProjectileType("DoGDeath"), 415, 0f, Main.myPlayer, 0f, 0f);
                                    }
                                }
                                break;
                            case SpecialAttackType.CircularLaserBurst:
                                float radius = 700 - 150f * (1f - lifeRatio);
                                if (specialAttackTimer % 25f == 24f)
                                {
                                    for (int i = 0; i < 3; i++)
                                    {
                                        Vector2 spawnOffset = (specialAttackTimer * (MathHelper.Pi / 100f) + MathHelper.TwoPi * i / 3f).ToRotationVector2() * radius;
                                        Vector2 spawnPosition = target.Center + spawnOffset;
                                        Projectile.NewProjectile(spawnPosition, Vector2.Zero, ModContent.ProjectileType<RealityBreakPortalLaserWall>(), 0, 0f);
                                    }
                                }
                                break;
                            case SpecialAttackType.ChargeGates:
                                float wrappedAttackTimer = specialAttackTimer % 120f;
                                int portalTelegraphTime = 45;
                                float chargeSpeed = nearDeath ? 85f : 60f;

                                // Fade in.
                                if (wrappedAttackTimer <= portalTelegraphTime + 20f)
                                    npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.15f, 0f, 1f);

                                // Create a portal to teleport from.
                                if (Main.netMode != NetmodeID.MultiplayerClient && wrappedAttackTimer == 20f)
                                {
                                    specialAttackPortalIndex = -1f;
                                    Vector2 portalSpawnPosition = target.Center + Main.rand.NextVector2CircularEdge(600f, 600f);
                                    initialTeleportPortal = Projectile.NewProjectile(portalSpawnPosition, Vector2.Zero, ModContent.ProjectileType<DoGChargeGate>(), 0, 0f);
                                    Main.projectile[(int)initialTeleportPortal].ai[1] = portalTelegraphTime;
                                    npc.netUpdate = true;
                                }

                                // Teleport and charge.
                                if (wrappedAttackTimer == portalTelegraphTime + 20f)
                                {
                                    if (Main.netMode != NetmodeID.MultiplayerClient)
                                    {
                                        npc.Center = Main.projectile[(int)initialTeleportPortal].Center;

                                        for (int i = 0; i < Main.maxNPCs; i++)
                                        {
                                            if (Main.npc[i].active && (Main.npc[i].type == ModContent.NPCType<DevourerofGodsBody>() || Main.npc[i].type == ModContent.NPCType<DevourerofGodsTail>()))
                                            {
                                                Main.npc[i].Center = npc.Center;
                                                Main.npc[i].Opacity = 0f;
                                                Main.npc[i].netUpdate = true;
                                            }
                                        }
                                        npc.velocity = npc.SafeDirectionTo(Main.projectile[(int)initialTeleportPortal].ModProjectile<DoGChargeGate>().Destination) * chargeSpeed;
                                        npc.netUpdate = true;

                                        // Create a burst of homing flames.
                                        float flameBurstOffsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                                        for (int i = 0; i < 12; i++)
                                        {
                                            Vector2 flameShootVelocity = (MathHelper.TwoPi * i / 12f + flameBurstOffsetAngle).ToRotationVector2() * 15f;
                                            Utilities.NewProjectileBetter(npc.Center + flameShootVelocity * 3f, flameShootVelocity, ModContent.ProjectileType<HomingDoGBurst>(), 415, 0f);
                                        }

                                        // Create the portal to go through.
                                        Vector2 portalSpawnPosition = npc.Center + npc.velocity.SafeNormalize(Vector2.UnitY) * 1900f;
                                        specialAttackPortalIndex = Projectile.NewProjectile(portalSpawnPosition, Vector2.Zero, ModContent.ProjectileType<DoGChargeGate>(), 0, 0f);
                                        Main.projectile[(int)specialAttackPortalIndex].localAI[0] = 1f;
                                        Main.projectile[(int)specialAttackPortalIndex].ai[1] = portalTelegraphTime;
                                    }
                                    Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/DoGAttack"), target.Center);
                                }
                                if (wrappedAttackTimer > portalTelegraphTime)
                                {
                                    // Disappear when touching the portal.
                                    // This same logic applies to body/tail segments.
                                    if (Main.projectile.IndexInRange((int)specialAttackPortalIndex) && npc.Hitbox.Intersects(Main.projectile[(int)specialAttackPortalIndex].Hitbox))
                                        npc.alpha = Utils.Clamp(npc.alpha + 50, 0, 255);
                                }

                                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                                return false;
                        }
                    }
                    else if (specialAttackType == SpecialAttackType.ChargeGates)
                        npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.1f, 0f, 1f);
                }
                else
                    npc.alpha += 5;
            }
            else if (specialAttackState == 2) // Appear out of a portal above the player.
            {
                npc.alpha -= 5;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].type == ModContent.ProjectileType<DoGDeath>())
                        Main.projectile[i].Kill();
                }

                npc.Center = target.Center - Vector2.UnitY * 3300f;
                npc.netUpdate = true;

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && (Main.npc[i].type == ModContent.NPCType<DevourerofGodsBody>() || Main.npc[i].type == ModContent.NPCType<DevourerofGodsTail>()))
                    {
                        Main.npc[i].Center = npc.Center;
                        Main.npc[i].netUpdate = true;
                    }
                }
                npc.velocity = npc.SafeDirectionTo(target.Center) * 32f;
                npc.Opacity = 1f;
                npc.dontTakeDamage = false;
                specialAttackState = 0f;
                specialAttackTimer = 0f;
            }

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

            Texture2D headTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2Head");
            Texture2D glowTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2HeadGlow");
            npc.frame = new Rectangle(0, 0, headTexture.Width, headTexture.Height);
            if (npc.Size != headTexture.Size())
                npc.Size = headTexture.Size();

            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 headTextureOrigin = headTexture.Size() * 0.5f;
            drawPosition -= headTexture.Size() * npc.scale * 0.5f;
            drawPosition += headTextureOrigin * npc.scale + new Vector2(0f, 4f + npc.gfxOffY);

            Texture2D jawTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2Jaw");
            Vector2 jawOrigin = jawTexture.Size() * 0.5f;

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
                spriteBatch.Draw(jawTexture, jawPosition, null, npc.GetAlpha(lightColor), npc.rotation + jawRotation * i, jawOrigin, npc.scale, jawSpriteEffect, 0f);
            }

            spriteBatch.Draw(headTexture, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, headTextureOrigin, npc.scale, spriteEffects, 0f);
            spriteBatch.Draw(glowTexture, drawPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, headTextureOrigin, npc.scale, spriteEffects, 0f);
            return false;
        }
        #endregion Drawing
    }
}
