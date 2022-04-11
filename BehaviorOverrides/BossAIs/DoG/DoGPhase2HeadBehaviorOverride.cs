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
using Terraria.Audio;

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

        public const int PassiveMovementTimeP2 = 360;
        public const int AggressiveMovementTimeP2 = 720;
        public const float CanUseSpecialAttacksLifeRatio = 0.7f;
        public const float FinalPhaseLifeRatio = 0.2f;
        public const float RipperRemovalLifeRatio = 0.35f;

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

        public static float GetAggressiveFade
        {
            get
            {
                if (CalamityGlobalNPC.DoGHead < 0)
                    return 0f;

                NPC npc = Main.npc[CalamityGlobalNPC.DoGHead];

                int passiveMoveTime = !InPhase2 ? DoGPhase1HeadBehaviorOverride.PassiveMovementTimeP1 : PassiveMovementTimeP2;
                int aggressiveMoveTime = !InPhase2 ? DoGPhase1HeadBehaviorOverride.AggressiveMovementTimeP1 : AggressiveMovementTimeP2;
                float phaseCycleTimer = npc.Infernum().ExtraAI[12] % (passiveMoveTime + aggressiveMoveTime);
                float aggressiveFade = Utils.GetLerpValue(passiveMoveTime - 120f, passiveMoveTime, phaseCycleTimer, true);
                aggressiveFade *= Utils.GetLerpValue(1f, 0.8f, aggressiveFade, true);
                if (npc.life < npc.lifeMax * FinalPhaseLifeRatio)
                    aggressiveFade = 0f;
                if (npc.Infernum().ExtraAI[14] != 0f)
                    return 0f;

                return aggressiveFade;
            }
        }

        public static float GetPassiveFade
        {
            get
            {
                if (CalamityGlobalNPC.DoGHead < 0)
                    return 0f;

                NPC npc = Main.npc[CalamityGlobalNPC.DoGHead];

                int passiveMoveTime = !InPhase2 ? DoGPhase1HeadBehaviorOverride.PassiveMovementTimeP1 : PassiveMovementTimeP2;
                int aggressiveMoveTime = !InPhase2 ? DoGPhase1HeadBehaviorOverride.AggressiveMovementTimeP1 : AggressiveMovementTimeP2;
                float phaseCycleTimer = npc.Infernum().ExtraAI[12] % (passiveMoveTime + aggressiveMoveTime);
                float passiveFade = Utils.GetLerpValue(passiveMoveTime + aggressiveMoveTime - 120f, passiveMoveTime + aggressiveMoveTime, phaseCycleTimer, true);
                passiveFade *= Utils.GetLerpValue(1f, 0.8f, passiveFade, true);
                if (npc.life < npc.lifeMax * FinalPhaseLifeRatio)
                    passiveFade = 0f;
                if (npc.Infernum().ExtraAI[14] != 0f)
                    return 0f;

                return passiveFade;
            }
        }
        public static readonly Color PassiveFadeColor = Color.DeepSkyBlue;
        public static readonly Color AggressiveFadeColor = Color.Red;

        #region AI
        public static bool Phase2AI(NPC npc, ref float phaseCycleTimer, ref float passiveAttackDelay, ref float portalIndex, ref float segmentFadeType)
        {
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

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
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
                passiveAttackDelay = 0f;

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
            bool canPerformSpecialAttacks = lifeRatio < CanUseSpecialAttacksLifeRatio;
            bool nearDeath = lifeRatio < FinalPhaseLifeRatio;

            // Prevent the player from using rage and adrenaline past a point.
            if (lifeRatio < RipperRemovalLifeRatio)
                target.Infernum().MakeAnxious(45);

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

                if (specialAttackState == 1f)
                {
                    bool doingSpecialAttacks = DoSpecialAttacks(npc, target, nearDeath, ref specialAttackState, ref specialAttackTimer, ref portalIndex, ref segmentFadeType);
                    if (doingSpecialAttacks)
                        phaseCycleTimer = 0f;

                    if (!doingSpecialAttacks)
                    {
                        if (horizontalRunAnticheeseCounter > 900f)
                            horizontalRunAnticheeseCounter = 900f;
                        return false;
                    }
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
                    Utilities.DisplayText("A GOD DOES NOT FEAR DEATH!", Color.Cyan);
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
            if (sentinelAttackTimer >= totalSentinelAttacks * 450f)
                sentinelAttackTimer = 0f;

            // Light
            Lighting.AddLight((int)((npc.position.X + npc.width / 2) / 16f), (int)((npc.position.Y + npc.height / 2) / 16f), 0.2f, 0.05f, 0.2f);

            // Worm shit.
            if (npc.ai[3] > 0f)
                npc.realLife = (int)npc.ai[3];

            // Despawn
            if (!NPC.AnyNPCs(InfernumMode.CalamityMod.Find<ModNPC>("DevourerofGodsTail").Type))
                npc.active = false;

            // Chomping after attempting to eat the player.
            bool chomping = npc.Infernum().ExtraAI[14] == 0f && DoChomp(npc, ref chompTime, ref jawRotation);

            // Despawn if no valid target exists.
            if (target.dead || !target.active)
                Despawn(npc);
            else if (phaseCycleTimer % (PassiveMovementTimeP2 + AggressiveMovementTimeP2) < PassiveMovementTimeP2 && !nearDeath)
            {
                DoPassiveFlyMovement(npc, ref jawRotation, ref chompTime);
                if (passiveAttackDelay >= 300f)
                    DoSentinelAttacks(npc, target, ref sentinelAttackTimer, ref signusAttackState);
            }
            else
            {
                bool dontChompYet = (phaseCycleTimer % (PassiveMovementTimeP2 + AggressiveMovementTimeP2)) - PassiveMovementTimeP2 < 90f;
                DoAggressiveFlyMovement(npc, target, dontChompYet, chomping, ref jawRotation, ref chompTime, ref time, ref flyAcceleration);
            }

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
                    if (Main.npc[a].type == InfernumMode.CalamityMod.Find<ModNPC>("DevourerofGodsHead") .Type||
                        Main.npc[a].type == InfernumMode.CalamityMod.Find<ModNPC>("DevourerofGodsBody") .Type||
                        Main.npc[a].type == InfernumMode.CalamityMod.Find<ModNPC>("DevourerofGodsTail").Type)
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
            int attackTime = 450;
            bool nearEndOfAttack = sentinelAttackTimer < attackTime * 2f - 125f;
            if (sentinelAttackTimer > 0f && sentinelAttackTimer <= attackTime && npc.alpha <= 128)
            {
                if (sentinelAttackTimer % 120f == 0f && nearEndOfAttack)
                {
                    SoundEngine.PlaySound(SoundID.Item12, target.position);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 24; i++)
                        {
                            Vector2 spawnOffset = (MathHelper.TwoPi * i / 24f).ToRotationVector2() * 1580f + Main.rand.NextVector2Circular(105f, 105f);
                            Vector2 laserShootVelocity = spawnOffset.SafeNormalize(Vector2.UnitY) * -Main.rand.NextFloat(30f, 36f) + Main.rand.NextVector2Circular(3f, 3f);
                            Utilities.NewProjectileBetter(target.Center + spawnOffset, laserShootVelocity, ModContent.ProjectileType<DoGDeath>(), 415, 0f);
                        }
                    }
                }
            }

            // Ceaseless Void Effect (Infernal Blasts).
            if (sentinelAttackTimer > attackTime && sentinelAttackTimer <= attackTime * 2f && npc.alpha <= 128)
            {
                if (npc.velocity.Length() > 14.5f)
                    npc.velocity *= 0.75f;

                bool shouldFire = sentinelAttackTimer % 70f == 69f && !nearEndOfAttack;
                if (Main.netMode != NetmodeID.MultiplayerClient && shouldFire && !npc.WithinRange(target.Center, 300f))
                {
                    for (int i = 0; i < 12; i++)
                    {
                        Vector2 flameShootVelocity = npc.velocity.RotatedBy(MathHelper.Lerp(-0.7f, 0.7f, i / 11f)) * Main.rand.NextFloat(1.3f, 1.65f);
                        Utilities.NewProjectileBetter(npc.Center, flameShootVelocity, ModContent.ProjectileType<HomingDoGBurst>(), 415, 0f);
                    }

                    for (int i = 0; i < 8; i++)
                    {
                        Vector2 flameShootVelocity = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 18f;
                        Utilities.NewProjectileBetter(npc.Center, flameShootVelocity, ModContent.ProjectileType<HomingDoGBurst>(), 415, 0f);
                    }
                }
                if (sentinelAttackTimer == attackTime * 2f - 1f)
                    signusAttackState = Main.rand.Next(2);
            }

            // Signus Effect (Essence Cleave).
            if (sentinelAttackTimer > attackTime * 2f && sentinelAttackTimer <= attackTime * 3f && npc.alpha <= 128)
            {
                float wrappedAttackTimer = sentinelAttackTimer % attackTime;
                if (wrappedAttackTimer % 90f == 0f)
                {
                    for (int i = 0; i < 12; i++)
                    {
                        int cleave = Utilities.NewProjectileBetter(target.Center, Vector2.Zero, ModContent.ProjectileType<EssenceCleave>(), 0, 0f);
                        if (Main.projectile.IndexInRange(cleave))
                            Main.projectile[cleave].ai[0] = MathHelper.TwoPi * i / 12f;
                    }
                }

                if (wrappedAttackTimer % 90f == 45f)
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
                    Projectile.NewProjectile(npc.Center, Vector2.Zero, ModContent.ProjectileType<DoGSpawnBoom>(), 0, 0f);

                if (Main.netMode != NetmodeID.Server)
                {
                    var soundInstance = SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/DevourerSpawn"), npc.Center);
                    if (soundInstance != null)
                        soundInstance.Volume = MathHelper.Clamp(soundInstance.Volume * 1.6f, 0f, 1f);

                    for (int i = 0; i < 3; i++)
                    {
                        soundInstance = SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Item/TeslaCannonFire"), npc.Center);
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

        public static bool DoChomp(NPC npc, ref float chompTime, ref float jawRotation)
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

        public static void DoPassiveFlyMovement(NPC npc, ref float jawRotation, ref float chompTime)
        {
            chompTime = 0f;
            jawRotation = jawRotation.AngleTowards(0f, 0.08f);

            // Move towards the target.
            Vector2 destination = Main.player[npc.target].Center - Vector2.UnitY * 430f;
            if (!npc.WithinRange(destination, 100f))
            {
                float flySpeed = MathHelper.Lerp(29f, 38f, 1f - npc.life / (float)npc.lifeMax);
                Vector2 idealVelocity = npc.SafeDirectionTo(destination) * flySpeed;
                npc.velocity = npc.velocity.MoveTowards(idealVelocity, 2f).RotateTowards(idealVelocity.ToRotation(), 0.032f);
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(npc.velocity.Length(), idealVelocity.Length(), 0.1f);
            }
        }

        public static void DoAggressiveFlyMovement(NPC npc, Player target, bool dontChompYet, bool chomping, ref float jawRotation, ref float chompTime, ref float time, ref float flyAcceleration)
        {
            npc.Center = npc.Center.MoveTowards(target.Center, 2.4f);

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

            float swimOffsetAngle = (float)Math.Sin(MathHelper.TwoPi * time / 160f) * Utils.GetLerpValue(400f, 540f, distanceFromDestination, true) * 0.41f;

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
                            SoundEngine.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.NPCHit, "Sounds/NPCHit/OtherworldlyHit"), npc.Center);
                        }
                    }
                }
                else
                {
                    jawRotation = jawRotation.AngleTowards(0f, 0.08f);
                }
            }

            // Lunge if near the player, and prepare to chomp.
            if (distanceFromDestination * 0.5f < 180f && directionToPlayerOrthogonality > 0.45f && npc.velocity.Length() < idealFlySpeed * 2f && !dontChompYet)
            {
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * npc.velocity.Length() * 1.75f;
                jawRotation = jawRotation.AngleLerp(idealMouthOpeningAngle, 0.55f);

                if (chompTime == 0f)
                {
                    chompTime = 26f;
                    SoundEngine.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.NPCHit, "Sounds/NPCHit/OtherworldlyHit"), npc.Center);
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
                SoundEngine.PlaySound(SoundID.Item12, (int)target.position.X, (int)target.position.Y);

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
            int fireballCount = 8;
            int idealPortalTelegraphTime = 52;
            float wrappedAttackTimer = attackTimer % 135f;
            float lifeRatio = npc.life / (float)npc.lifeMax;

            if (lifeRatio < 0.15f)
            {
                fireballCount -= 2;
                idealPortalTelegraphTime -= 10;
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
            ref float initialTeleportPortal = ref npc.Infernum().ExtraAI[36];
            ref float portalTelegraphTime = ref npc.Infernum().ExtraAI[40];

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
                            Main.npc[i].Opacity = Utils.GetLerpValue(15f, 0f, segmentCount, true);
                            Main.npc[i].netUpdate = true;
                            segmentCount++;
                        }
                    }
                    npc.velocity = npc.SafeDirectionTo(Main.projectile[(int)initialTeleportPortal].ModProjectile<DoGChargeGate>().Destination) * chargeSpeed;
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
                        portalIndex = Projectile.NewProjectile(portalSpawnPosition, Vector2.Zero, ModContent.ProjectileType<DoGChargeGate>(), 0, 0f);
                        Main.projectile[(int)portalIndex].localAI[0] = 1f;
                        Main.projectile[(int)portalIndex].ai[1] = portalTelegraphTime;
                    }
                }
                SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.Instance, "Sounds/Custom/DoGAttack"), target.Center);
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
                    npc.Infernum().ExtraAI[40] = 0f;
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
            npc.frame = new Rectangle(0, 0, headTexture.Width, headTexture.Height);
            if (npc.Size != headTexture.Size())
                npc.Size = headTexture.Size();

            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 headTextureOrigin = headTexture.Size() * 0.5f;
            drawPosition -= headTexture.Size() * npc.scale * 0.5f;
            drawPosition += headTextureOrigin * npc.scale + new Vector2(0f, 4f + npc.gfxOffY);

            Texture2D jawTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP2Jaw").Value;
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

            // Draw head backimages as a telegraph.
            float aggressiveFade = GetAggressiveFade;
            float passiveFade = GetPassiveFade;
            if (aggressiveFade > 0f || passiveFade > 0f)
            {
                Color afterimageColor = Color.Transparent;
                if (aggressiveFade > 0f)
                    afterimageColor = Color.Lerp(afterimageColor, AggressiveFadeColor, (float)Math.Sqrt(aggressiveFade));
                if (passiveFade > 0f)
                    afterimageColor = Color.Lerp(afterimageColor, PassiveFadeColor, (float)Math.Sqrt(passiveFade));
                afterimageColor.A = 50;
                float afterimageOffsetFactor = MathHelper.Max(aggressiveFade, passiveFade) * 24f;

                for (int i = 0; i < 12; i++)
                {
                    Vector2 afterimageOffset = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * afterimageOffsetFactor;
                    spriteBatch.Draw(headTexture, drawPosition + afterimageOffset, npc.frame, npc.GetAlpha(afterimageColor) * 0.45f, npc.rotation, headTextureOrigin, npc.scale, spriteEffects, 0f);
                }
            }

            spriteBatch.Draw(headTexture, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, headTextureOrigin, npc.scale, spriteEffects, 0f);
            spriteBatch.Draw(glowTexture, drawPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, headTextureOrigin, npc.scale, spriteEffects, 0f);
            return false;
        }
        #endregion Drawing
    }
}
