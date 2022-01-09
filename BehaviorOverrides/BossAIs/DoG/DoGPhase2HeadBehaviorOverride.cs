using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public enum BodySegmentFadeType
        {
            InhertHeadOpacity,
            EnterPortal,
            ApproachAheadSegmentOpacity
        }

        #region AI
        public static bool Phase2AI(NPC npc, ref float portalIndex, ref float segmentFadeType)
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
            ref float totalCharges = ref npc.Infernum().ExtraAI[27];
            ref float fadeinTimer = ref npc.Infernum().ExtraAI[28];
            ref float fireballShootTimer = ref npc.Infernum().ExtraAI[31];
            ref float deathTimer = ref npc.Infernum().ExtraAI[32];

            if (!Main.player.IndexInRange(npc.target) || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest(false);

            Player target = Main.player[npc.target];

            target.Calamity().normalityRelocator = false;
            target.Calamity().spectralVeil = false;

            npc.takenDamageMultiplier = 2f;

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
                        portalIndex = Projectile.NewProjectile(npc.Center, Vector2.Zero, ModContent.ProjectileType<DoGRealityRendEntranceGate>(), 0, 0f);
                }

                npc.Opacity = 0f;
                segmentFadeType = (int)BodySegmentFadeType.InhertHeadOpacity;

                npc.Center = Main.player[Player.FindClosest(npc.Center, 1, 1)].Center - Vector2.UnitY * MathHelper.Lerp(6000f, 3000f, fadeinTimer / 280f);
                fadeinTimer++;

                if (fadeinTimer >= 280f)
                {
                    npc.Opacity = 1f;
                    npc.Center = Main.projectile[(int)portalIndex].Center;
                    npc.velocity = npc.SafeDirectionTo(target.Center) * 36f;
                    npc.netUpdate = true;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Projectile.NewProjectile(npc.Center, Vector2.Zero, ModContent.ProjectileType<DoGSpawnBoom>(), 0, 0f);

                    // Reset the special attack portal index to -1.
                    portalIndex = -1f;
                }
                return false;
            }

            // Percent life remaining
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Variables
            bool canPerformSpecialAttacks = lifeRatio < 0.7f;
            bool nearDeath = lifeRatio < 0.2f;
            bool breathFireMore = lifeRatio < 0.15f;

            // Don't take damage when fading out.
            npc.dontTakeDamage = npc.Opacity < 0.5f;
            npc.damage = npc.dontTakeDamage ? 0 : 6000;
            npc.Calamity().DR = 0.3f;

            // Stay in the world.
            npc.position.Y = MathHelper.Clamp(npc.position.Y, 180f, Main.maxTilesY * 16f - 180f);

            // Do the death animation once dead.
            if (deathTimer > 0f)
            {
                DoDeathEffects(npc, deathTimer);
                jawRotation = jawRotation.AngleTowards(0f, 0.07f);
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                deathTimer++;
                return false;
            }

            // Have segments by default inherit the opacity of the head.
            segmentFadeType = (int)BodySegmentFadeType.InhertHeadOpacity;

            // Handle special attacks.
            if (canPerformSpecialAttacks && trapChargeTimer <= 0f)
            {
                // Handle special attack transition.
                int specialAttackDelay = 1335;
                int specialAttackTransitionPreparationTime = 135;
                if (specialAttackState == 0f)
                {
                    // The charge gate attack happens much more frequently when DoG is close to death.
                    specialAttackTimer += nearDeath && specialAttackTimer < specialAttackDelay - specialAttackTransitionPreparationTime - 5f ? 2f : 1f;

                    // Enter a portal before performing a special attack.
                    if (Main.netMode != NetmodeID.MultiplayerClient && specialAttackTimer == specialAttackDelay - specialAttackTransitionPreparationTime)
                    {
                        portalIndex = Projectile.NewProjectile(npc.Center + npc.velocity * 75f, Vector2.Zero, ModContent.ProjectileType<DoGChargeGate>(), 0, 0f);
                        Main.projectile[(int)portalIndex].localAI[0] = 1f;
                        npc.netUpdate = true;
                    }

                    if (specialAttackTimer >= specialAttackDelay)
                    {
                        specialAttackTimer = 0f;
                        specialAttackState = 1f;
                        portalIndex = -1f;

                        // Select a special attack type.
                        npc.Infernum().ExtraAI[2] = Main.rand.Next(10);
                        npc.netUpdate = true;
                    }

                    // Do nothing and drift into the portal.
                    if (specialAttackTimer >= specialAttackDelay - specialAttackTransitionPreparationTime)
                    {
                        npc.damage = 0;
                        npc.dontTakeDamage = true;
                        npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitX) * MathHelper.Lerp(npc.velocity.Length(), 105f, 0.15f);

                        // Disappear when touching the portal.
                        // This same logic applies to body/tail segments.
                        if (Main.projectile.IndexInRange((int)portalIndex) && npc.Hitbox.Intersects(Main.projectile[(int)portalIndex].Hitbox))
                            npc.alpha = 255;

                        segmentFadeType = (int)BodySegmentFadeType.EnterPortal;

                        // Clear away various misc projectiles.
                        int[] projectilesToDelete = new int[]
                        {
                            ProjectileID.CultistBossLightningOrbArc,
                            ModContent.ProjectileType<HomingDoGBurst>(),
                            ModContent.ProjectileType<DoGBeamPortalN>(),
                            ModContent.ProjectileType<DoGBeamN>(),
                            ModContent.ProjectileType<EssenceCleave>(),
                            ModContent.ProjectileType<EssenceExplosion>(),
                        };
                        for (int i = 0; i < Main.maxProjectiles; i++)
                        {
                            if (projectilesToDelete.Contains(Main.projectile[i].type) && Main.projectile[i].active)
                                Main.projectile[i].active = false;
                        }
                        return false;
                    }
                }

                if (specialAttackState == 1f && !DoSpecialAttacks(npc, target, nearDeath, ref specialAttackState, ref specialAttackTimer, ref portalIndex, ref segmentFadeType))
                {
                    if (horizontalRunAnticheeseCounter > 900f)
                        horizontalRunAnticheeseCounter = 900f;
                    return false;
                }
            }
            else
            {
                // Fade in.
                npc.alpha = Utils.Clamp(npc.alpha - 6, 0, 255);

                // Reset the special attack state, just in case.
                if (specialAttackState > 0)
                    specialAttackState = 0;
            }
            portalIndex = -1f;

            if (specialAttackState == 0f)
                npc.Infernum().ExtraAI[2] = 0f;

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
            if (lifeRatio < 0.6f)
                totalSentinelAttacks++;
            if (lifeRatio < 0.45f)
                totalSentinelAttacks++;

            // Do sentinel attacks.
            if (totalSentinelAttacks >= 1 && npc.alpha <= 0)
                sentinelAttackTimer += 1f;
            if (sentinelAttackTimer >= totalSentinelAttacks * 900f)
                sentinelAttackTimer = 0f;

            // TODO - Improve these.
            if (!nearDeath)
                DoSentinelAttacks(npc, target, ref sentinelAttackTimer, ref signusAttackState);

            // Light
            Lighting.AddLight((int)((npc.position.X + npc.width / 2) / 16f), (int)((npc.position.Y + npc.height / 2) / 16f), 0.2f, 0.05f, 0.2f);

            // Worm shit.
            if (npc.ai[3] > 0f)
                npc.realLife = (int)npc.ai[3];

            // Shoot fire projectiles.
            if (Main.netMode != NetmodeID.MultiplayerClient && !nearDeath)
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

            // Despawn if no valid target exists.
            if (target.dead || !target.active)
                Despawn(npc);
            else
                DoAggressiveFlyMovement(npc, target, chomping, ref jawRotation, ref chompTime, ref time, ref flyAcceleration);

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
            // Storm Weaver Effect (Lightning Storm).
            if (sentinelAttackTimer > 0f && sentinelAttackTimer <= 900f && npc.alpha <= 0)
            {
                if (sentinelAttackTimer % 120f == 0f)
                {
                    Main.PlaySound(SoundID.Item12, target.position);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 20; i++)
                        {
                            Vector2 spawnOffset = Vector2.UnitX.RotatedBy(MathHelper.Lerp(-0.54f, 0.54f, i / 19f) + Main.rand.NextFloatDirection() * 0.1f) * 1350f + Main.rand.NextVector2Circular(30f, 30f);
                            Vector2 laserShootVelocity = spawnOffset.SafeNormalize(Vector2.UnitY) * -Main.rand.NextFloat(25f, 30f) + Main.rand.NextVector2Circular(5f, 5f);
                            Utilities.NewProjectileBetter(target.Center + spawnOffset, laserShootVelocity, ModContent.ProjectileType<DoGDeath>(), 415, 0f);

                            spawnOffset = -Vector2.UnitX.RotatedBy(MathHelper.Lerp(-0.54f, 0.54f, i / 19f) + Main.rand.NextFloatDirection() * 0.1f) * 1350f + Main.rand.NextVector2Circular(30f, 30f);
                            laserShootVelocity = spawnOffset.SafeNormalize(Vector2.UnitY) * -Main.rand.NextFloat(25f, 30f) + Main.rand.NextVector2Circular(5f, 5f);
                            Utilities.NewProjectileBetter(target.Center + spawnOffset, laserShootVelocity, ModContent.ProjectileType<DoGDeath>(), 415, 0f);
                        }
                    }
                }
            }

            // Ceaseless Void Effect (Chaser Portal).
            if (sentinelAttackTimer > 900f && sentinelAttackTimer <= 900f * 2f && npc.alpha <= 0)
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
                if (sentinelAttackTimer == 900f * 2f - 1f)
                    signusAttackState = Main.rand.Next(2);
            }

            // Signus Effect (Essence Cleave).
            if (sentinelAttackTimer > 900f * 2f && sentinelAttackTimer <= 900f * 3f && npc.alpha <= 0)
            {
                float wrappedAttackTimer = sentinelAttackTimer % 900f;
                if (wrappedAttackTimer % 90f == 0f)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        int cleave = Utilities.NewProjectileBetter(target.Center, Vector2.Zero, ModContent.ProjectileType<EssenceCleave>(), 0, 0f);
                        if (Main.projectile.IndexInRange(cleave))
                            Main.projectile[cleave].ai[0] = MathHelper.TwoPi * i / 8f;
                    }
                }

                if (wrappedAttackTimer % 90f == 45f)
                    Main.PlaySound(SoundID.Item122, target.Center);
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
                            soundInstance.Volume = MathHelper.Clamp(soundInstance.Volume * 1.8f, 0f, 1f);
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
                    jawRotation = jawRotation.AngleTowards(0f, 0.08f);
                }
            }

            // Lunge if near the player, and prepare to chomp.
            if (distanceFromDestination * 0.5f < 180f && directionToPlayerOrthogonality > 0.45f && npc.velocity.Length() < idealFlySpeed * 2f)
            {
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * npc.velocity.Length() * 1.75f;
                jawRotation = jawRotation.AngleLerp(idealMouthOpeningAngle, 0.55f);

                if (chompTime == 0f)
                {
                    chompTime = 26f;
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.NPCHit, "Sounds/NPCHit/OtherworldlyHit"), npc.Center);
                }
            }
        }

        public static void DoSpecialAttack_LaserWalls(Player target, ref float attackTimer, ref float segmentFadeType)
        {
            // Body segments should be invisible along with the head.
            segmentFadeType = (int)BodySegmentFadeType.InhertHeadOpacity;

            int offsetPerLaser = 95;
            float laserWallSpeed = 16f;
            if (attackTimer % 75f == 74f)
            {
                Main.PlaySound(SoundID.Item12, (int)target.position.X, (int)target.position.Y);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int shootType = ModContent.ProjectileType<DoGDeath>();
                    float targetPosY = target.position.Y + (Main.rand.NextBool(2) ? 50f : 0f);

                    // Side walls
                    for (int x = -10; x < 10; x++)
                    {
                        Utilities.NewProjectileBetter(target.position.X + 1000f, targetPosY + x * offsetPerLaser, -laserWallSpeed, 0f, shootType, 415, 0f);
                        Utilities.NewProjectileBetter(target.position.X - 1000f, targetPosY + x * offsetPerLaser, laserWallSpeed, 0f, shootType, 415, 0f);
                    }

                    if (Main.rand.NextBool(2))
                    {
                        for (int x = -5; x < 5; x++)
                        {
                            Utilities.NewProjectileBetter(target.position.X + 1000f, targetPosY + x * (Main.rand.NextBool(2) ? 180 : 200), -laserWallSpeed, 0f, shootType, 415, 0f);
                            Utilities.NewProjectileBetter(target.position.X - 1000f, targetPosY + x * (Main.rand.NextBool(2) ? 180 : 200), laserWallSpeed, 0f, shootType, 415, 0f);
                        }
                    }

                    // Lower wall
                    for (int x = -12; x <= 12; x++)
                        Utilities.NewProjectileBetter(target.position.X + x * offsetPerLaser, target.position.Y + 1000f, 0f, -laserWallSpeed, shootType, 415, 0f);

                    // Upper wall
                    for (int x = -20; x < 20; x++)
                        Utilities.NewProjectileBetter(target.position.X + x * offsetPerLaser, target.position.Y - 1000f, 0f, laserWallSpeed, shootType, 415, 0f);
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
                    Projectile.NewProjectile(spawnPosition, Vector2.Zero, ModContent.ProjectileType<RealityBreakPortalLaserWall>(), 0, 0f);
                }
            }
        }

        public static void DoSpecialAttack_ChargeGates(NPC npc, Player target, bool nearDeath, ref float attackTimer, ref float portalIndex, ref float segmentFadeType)
        {
            int portalTelegraphTime = 52;
            float wrappedAttackTimer = attackTimer % 135f;
            float chargeSpeed = nearDeath ? 85f : 60f;

            ref float initialTeleportPortal = ref npc.Infernum().ExtraAI[36];

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

                    int segmentCount = 0;
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (Main.npc[i].active && (Main.npc[i].type == ModContent.NPCType<DevourerofGodsBody>() || Main.npc[i].type == ModContent.NPCType<DevourerofGodsTail>()))
                        {
                            Main.npc[i].Center = npc.Center;
                            Main.npc[i].Opacity = Utils.InverseLerp(15f, 0f, segmentCount, true);
                            Main.npc[i].netUpdate = true;
                            segmentCount++;
                        }
                    }
                    npc.velocity = npc.SafeDirectionTo(Main.projectile[(int)initialTeleportPortal].ModProjectile<DoGChargeGate>().Destination) * chargeSpeed;
                    npc.Opacity = 1f;
                    npc.netUpdate = true;

                    // Create a burst of homing flames.
                    float flameBurstOffsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                    for (int i = 0; i < 8; i++)
                    {
                        Vector2 flameShootVelocity = (MathHelper.TwoPi * i / 8f + flameBurstOffsetAngle).ToRotationVector2() * 15f;
                        Utilities.NewProjectileBetter(npc.Center + flameShootVelocity * 3f, flameShootVelocity, ModContent.ProjectileType<HomingDoGBurst>(), 415, 0f);
                    }

                    // Create the portal to go through.
                    if (!target.dead)
                    {
                        Vector2 portalSpawnPosition = npc.Center + npc.velocity.SafeNormalize(Vector2.UnitY) * 1900f;
                        portalIndex = Projectile.NewProjectile(portalSpawnPosition, Vector2.Zero, ModContent.ProjectileType<DoGChargeGate>(), 0, 0f);
                        Main.projectile[(int)portalIndex].localAI[0] = 1f;
                        Main.projectile[(int)portalIndex].ai[1] = portalTelegraphTime;
                    }
                }
                Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/DoGAttack"), target.Center);
            }
            if (wrappedAttackTimer > portalTelegraphTime)
            {
                // Disappear when touching the portal.
                // This same logic applies to body/tail segments.
                if (Main.projectile.IndexInRange((int)portalIndex) && npc.Hitbox.Intersects(Main.projectile[(int)portalIndex].Hitbox))
                    npc.alpha = Utils.Clamp(npc.alpha + 50, 0, 255);

                if (wrappedAttackTimer > portalTelegraphTime + 20f)
                    segmentFadeType = (int)BodySegmentFadeType.EnterPortal;
            }

            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public static bool DoSpecialAttacks(NPC npc, Player target, bool nearDeath, ref float specialAttackState, ref float specialAttackTimer, ref float specialAttackPortalIndex, ref float segmentFadeType)
        {
            SpecialAttackType specialAttackType;

            if (nearDeath)
                npc.Infernum().ExtraAI[2] = 9f;

            if (npc.Infernum().ExtraAI[2] <= 3f)
                specialAttackType = SpecialAttackType.LaserWalls;
            else if (npc.Infernum().ExtraAI[2] <= 5f)
                specialAttackType = SpecialAttackType.CircularLaserBurst;
            else
                specialAttackType = SpecialAttackType.ChargeGates;

            if (npc.alpha >= 255 || specialAttackType == SpecialAttackType.ChargeGates)
            {
                specialAttackTimer++;
                if (specialAttackTimer >= 760f)
                {
                    // Delete lingering laser wall things.
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

                if (specialAttackTimer < 675f)
                {
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
