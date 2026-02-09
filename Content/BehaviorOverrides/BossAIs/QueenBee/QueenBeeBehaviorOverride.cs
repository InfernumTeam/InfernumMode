using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Events;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.QueenBee
{
    public class QueenBeeBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.QueenBee;

        #region Enumerations
        public enum QueenBeeAttackState
        {
            HorizontalCharge,
            StingerBurst,
            HoneyBlast,
            CreateMinionsFromAbdomen,
            InwardMovingBees,
            BeeletHell
        }

        public enum QueenBeeFrameType
        {
            HorizontalCharge,
            UpwardFly
        }
        #endregion

        #region AI

        public static int TinyBeeDamage => 85;

        public static int ConvergingHornetDamage => 90;

        public static int HoneyBlastDamage => 90;

        public static int HornetHiveDamage => 90;

        public static int StingerDamage => 95;

        public const float FinalPhaseLifeRatio = 0.225f;

        public override float[] PhaseLifeRatioThresholds =>
        [
            FinalPhaseLifeRatio
        ];

        public override bool PreAI(NPC npc)
        {
            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();

            // If none was found or it was too far away, despawn.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead ||
                !Main.player[npc.target].active || !npc.WithinRange(Main.player[npc.target].Center, 3400f))
            {
                DoDespawnEffects(npc);
                return false;
            }

            Player target = Main.player[npc.target];

            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float finalPhaseTransitionTimer = ref npc.ai[2];
            ref float frameType = ref npc.localAI[0];
            ref float hasBegunFinalPhaseTransition = ref npc.localAI[1];
            ref float generalTimer = ref npc.Infernum().ExtraAI[5];
            ref float enrageTimer = ref npc.Infernum().ExtraAI[6];

            bool outOfBiome = !target.ZoneJungle && !BossRushEvent.BossRushActive;
            enrageTimer = Clamp(enrageTimer + outOfBiome.ToDirectionInt(), 0f, 480f);
            npc.defense = enrageTimer >= 300f ? 70 : npc.defDefense;
            npc.damage = npc.defDamage;
            npc.Calamity().CurrentlyEnraged = outOfBiome;

            // Do the initial stuff before attacking.
            generalTimer++;
            if (generalTimer < 150f)
            {
                frameType = (int)QueenBeeFrameType.UpwardFly;
                DoSpawnAnimationStuff(npc, target, generalTimer);
                return false;
            }

            if (npc.life < npc.lifeMax * FinalPhaseLifeRatio && Main.netMode != NetmodeID.MultiplayerClient && hasBegunFinalPhaseTransition == 0f)
            {
                hasBegunFinalPhaseTransition = 1f;
                finalPhaseTransitionTimer = 75f;
            }

            if (finalPhaseTransitionTimer > 0f)
            {
                attackTimer = 0f;
                npc.dontTakeDamage = true;
                frameType = (int)QueenBeeFrameType.UpwardFly;
                finalPhaseTransitionTimer--;
                if (finalPhaseTransitionTimer == 0f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<BeeWave>(), 0, 0f);
                    Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<ConvergingHornet>());

                    SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                    SelectNextAttack(npc);
                }

                npc.velocity *= 0.93f;

                return false;
            }

            npc.dontTakeDamage = false;

            switch ((QueenBeeAttackState)(int)attackType)
            {
                case QueenBeeAttackState.HorizontalCharge:
                    DoAttack_HorizontalCharge(npc, target, generalTimer - 150f, ref frameType);
                    break;
                case QueenBeeAttackState.StingerBurst:
                    DoAttack_StingerBurst(npc, target, ref frameType, ref attackTimer);
                    break;
                case QueenBeeAttackState.HoneyBlast:
                    DoAttack_HoneyBlast(npc, target, ref frameType, ref attackTimer);
                    break;
                case QueenBeeAttackState.CreateMinionsFromAbdomen:
                    DoAttack_CreateMinionsFromAbdomen(npc, target, ref frameType, ref attackTimer);
                    break;
                case QueenBeeAttackState.InwardMovingBees:
                    DoAttack_InwardMovingBees(npc, target, ref frameType, ref attackTimer);
                    break;
                case QueenBeeAttackState.BeeletHell:
                    DoAttack_BeeletHell(npc, target, ref frameType, ref attackTimer);
                    break;
            }
            attackTimer++;
            return false;
        }

        #region Specific Attacks
        public static void DoSpawnAnimationStuff(NPC npc, Player target, float animationTimer)
        {
            npc.Opacity = Utils.GetLerpValue(0f, 45f, animationTimer, true);
            npc.damage = 0;
            npc.dontTakeDamage = true;

            if (animationTimer < 75f)
            {
                Vector2 hoverDestination = target.Center - Vector2.UnitY * 300f;

                npc.velocity = npc.SafeDirectionTo(hoverDestination) * MathF.Min(npc.Distance(hoverDestination), 32f);
                if (npc.WithinRange(target.Center, 90f))
                {
                    npc.Center = target.Center - npc.SafeDirectionTo(target.Center, Vector2.UnitY) * 90f;
                    npc.ai[1] = 89f;
                    npc.netUpdate = true;
                }
            }
            else
            {
                npc.velocity *= 0.85f;
                npc.rotation = npc.rotation.AngleLerp(0f, 0.2f);
            }
        }

        public static void DoDespawnEffects(NPC npc)
        {
            npc.velocity.Y = Lerp(npc.velocity.Y, 17f, 0.1f);
            npc.damage = 0;
            if (npc.timeLeft > 180)
                npc.timeLeft = 180;
        }

        public static void DoAttack_HorizontalCharge(NPC npc, Player target, float generalAttackTimer, ref float frameType)
        {
            int chargeCount = 4;
            float baseChargeSpeed = 19.5f;
            float chargeSpeedup = 0.0067f;
            float hoverOffset = 320f;

            if (npc.life < npc.lifeMax * 0.6)
                baseChargeSpeed += 4.5f;

            if (npc.life < npc.lifeMax * 0.33)
                chargeCount += 2;

            if (npc.life < npc.lifeMax * FinalPhaseLifeRatio)
            {
                baseChargeSpeed *= 1.1f;
                hoverOffset += 70f;
            }

            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float speedBoost = ref npc.Infernum().ExtraAI[1];
            ref float totalChargesDone = ref npc.Infernum().ExtraAI[2];

            if (BossRushEvent.BossRushActive)
            {
                chargeCount += 8;
                chargeSpeedup = 0.1f;
                hoverOffset -= 110f;
            }

            // Line up.
            if (attackState == 0f)
            {
                Vector2 destination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * hoverOffset;
                npc.Center = npc.Center.MoveTowards(destination, Utils.GetLerpValue(0f, 40f, generalAttackTimer, true) * 5f);
                npc.velocity += npc.SafeDirectionTo(destination) * Utils.GetLerpValue(0f, 40f, generalAttackTimer, true) * baseChargeSpeed / 30f;

                frameType = (int)QueenBeeFrameType.UpwardFly;
                if (npc.WithinRange(destination, 48f) || Math.Abs(target.Center.Y - npc.Center.Y) < 15f)
                {
                    npc.Center = new Vector2(Lerp(npc.Center.X, destination.X, 0.1f), Lerp(npc.Center.Y, destination.Y, 0.5f));
                    npc.velocity = npc.SafeDirectionTo(target.Center, Vector2.UnitX) * baseChargeSpeed;
                    if (npc.life < npc.lifeMax * FinalPhaseLifeRatio && Math.Abs(target.velocity.Y) > 3f)
                        npc.velocity.Y += target.velocity.Y * 0.5f;
                    attackState = 1f;
                    frameType = (int)QueenBeeFrameType.HorizontalCharge;

                    SoundEngine.PlaySound(SoundID.Roar, npc.Center);

                    npc.netUpdate = true;
                }
                npc.spriteDirection = Math.Sign(npc.velocity.X);
            }

            // Do the charge.
            else
            {
                speedBoost += chargeSpeedup;
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitX) * (npc.velocity.Length() + speedBoost);

                frameType = (int)QueenBeeFrameType.HorizontalCharge;
                float destinationOffset = Lerp(540f, 450f, 1f - npc.life / (float)npc.lifeMax);

                if (npc.spriteDirection == 1 && npc.Center.X - target.Center.X > destinationOffset ||
                    npc.spriteDirection == -1 && npc.Center.X - target.Center.X < -destinationOffset)
                {
                    npc.velocity = npc.velocity.ClampMagnitude(0f, 20f) * 0.5f;
                    attackState = 0f;
                    speedBoost = 0f;
                    totalChargesDone++;
                    npc.netUpdate = true;
                }
            }

            if (totalChargesDone >= chargeCount)
                SelectNextAttack(npc);
        }

        public static void DoAttack_StingerBurst(NPC npc, Player target, ref float frameType, ref float attackTimer)
        {
            ref float flyDestinationX = ref npc.Infernum().ExtraAI[0];
            ref float flyDestinationY = ref npc.Infernum().ExtraAI[1];
            Vector2 currentFlyDestination = new(flyDestinationX, flyDestinationY);

            if (flyDestinationX == 0f || flyDestinationY == 0f || flyDestinationY > target.Center.Y - 40f)
                currentFlyDestination = target.Center - Vector2.UnitY * 270f;

            if (!target.WithinRange(currentFlyDestination, 500f))
            {
                currentFlyDestination = target.Center - Vector2.UnitY * 200f;
                npc.netUpdate = true;
            }

            int shootRate = 42;
            int totalStingersToShoot = 5;
            float shootSpeed = 12f;
            if (npc.life < npc.lifeMax * 0.5)
            {
                shootRate = 28;
                totalStingersToShoot = 8;
                shootSpeed = 13.75f;
            }

            if (BossRushEvent.BossRushActive)
            {
                shootRate = (int)(shootRate * 0.6f);
                shootSpeed *= 2.35f;
            }

            bool canShoot = npc.Bottom.Y < target.position.Y;
            Vector2 baseStingerSpawnPosition = new(npc.Center.X + Main.rand.Next(20) * npc.spriteDirection, npc.Center.Y + npc.height * 0.3f);

            frameType = (int)QueenBeeFrameType.UpwardFly;

            if (attackTimer % shootRate == shootRate - 1f && canShoot && Collision.CanHit(baseStingerSpawnPosition, 1, 1, target.Center, 1, 1))
            {
                // Play a shoot sound when firing.
                SoundEngine.PlaySound(SoundID.Item17, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 12; i++)
                    {
                        float offsetAngle = TwoPi * 1.61808f * i / 12f;
                        Vector2 stingerSpawnPosition = baseStingerSpawnPosition + offsetAngle.ToRotationVector2() * Lerp(4f, 28f, i / 12f);
                        Vector2 stingerShootVelocity = (target.Center - baseStingerSpawnPosition).SafeNormalize(Vector2.UnitY) * shootSpeed;
                        float burstOutwardness = Lerp(0.04f, 0.12f, 1f - npc.life / (float)npc.lifeMax);
                        stingerShootVelocity = stingerShootVelocity.RotatedBy(Lerp(-burstOutwardness, burstOutwardness, i / 11f));

                        int stinger = Utilities.NewProjectileBetter(stingerSpawnPosition, stingerShootVelocity, ProjectileID.Stinger, StingerDamage, 0f);
                        if (Main.projectile.IndexInRange(stinger))
                            Main.projectile[stinger].tileCollide = false;
                    }

                    // Determine a new position to fly at.
                    Matrix offsetRotationMatrix = Matrix.CreateRotationX(attackTimer / shootRate * 2.8f + 0.56f);
                    offsetRotationMatrix *= Matrix.CreateRotationY((attackTimer / shootRate * 5.3f + 0.66f) * 0.41f);
                    offsetRotationMatrix *= Matrix.CreateRotationZ(attackTimer / shootRate * Pi * 0.33f);

                    Vector3 tansformedRotationData = Vector3.Transform(Vector3.UnitY, offsetRotationMatrix);
                    Vector2 flyDestinationOffset = new Vector2(tansformedRotationData.X, tansformedRotationData.Y) * new Vector2(210f, 125f);
                    currentFlyDestination = target.Center - Vector2.UnitY * 260f + flyDestinationOffset;
                    flyDestinationOffset.X += target.velocity.X * 30f;
                    if (target.WithinRange(currentFlyDestination, 400f))
                        currentFlyDestination = target.Center + target.SafeDirectionTo(currentFlyDestination) * 400f;

                    npc.netUpdate = true;
                }
            }

            // Fly above the target.
            if (npc.WithinRange(currentFlyDestination, 35f))
            {
                npc.velocity *= 0.85f;
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
            }
            else
            {
                DoHoverMovement(npc, currentFlyDestination, 0.25f);
                if (Math.Abs(npc.velocity.X) > 2f)
                    npc.spriteDirection = Math.Sign(npc.velocity.X);
            }

            flyDestinationX = currentFlyDestination.X;
            flyDestinationY = currentFlyDestination.Y;

            if (attackTimer >= totalStingersToShoot * shootRate)
                SelectNextAttack(npc);
        }

        public static void DoAttack_HoneyBlast(NPC npc, Player target, ref float frameType, ref float attackTimer)
        {
            // Fly above the target.
            Vector2 flyDestination = target.Center - new Vector2((target.Center.X - npc.Center.X > 0).ToDirectionInt() * 270f, 240f);
            DoHoverMovement(npc, flyDestination, 0.14f);

            frameType = (int)QueenBeeFrameType.UpwardFly;
            npc.spriteDirection = (target.Center.X - npc.Center.X > 0).ToDirectionInt();

            // Release blasts of honey.
            bool honeyIsPoisonous = npc.life < npc.lifeMax * 0.5f;
            int shootRate = honeyIsPoisonous ? 10 : 20;
            int totalBlastsToShoot = 18;
            float shootSpeed = 10f;
            if (BossRushEvent.BossRushActive)
            {
                shootRate = (int)(shootRate * 0.6f);
                shootSpeed *= 2.64f;
            }

            bool canShoot = npc.Bottom.Y < target.position.Y;
            if (attackTimer % shootRate == shootRate - 1f && canShoot)
            {
                // Play a shoot sound when firing.
                SoundEngine.PlaySound(SoundID.Item17, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 honeySpawnPosition = new(npc.Center.X, npc.Center.Y + npc.height * 0.325f);
                    Vector2 honeyShootVelocity = (target.Center - honeySpawnPosition).SafeNormalize(Vector2.UnitY) * shootSpeed;
                    int honeyBlast = Utilities.NewProjectileBetter(honeySpawnPosition, honeyShootVelocity, ModContent.ProjectileType<HoneyBlast>(), HoneyBlastDamage, 0f);
                    if (Main.projectile.IndexInRange(honeyBlast))
                        Main.projectile[honeyBlast].ai[0] = honeyIsPoisonous.ToInt();
                }
            }

            if (attackTimer >= shootRate * totalBlastsToShoot)
                SelectNextAttack(npc);
        }

        public static void DoAttack_CreateMinionsFromAbdomen(NPC npc, Player target, ref float frameType, ref float attackTimer)
        {
            Vector2 destination = target.Center - Vector2.UnitY * 210f;
            DoHoverMovement(npc, destination, 0.1f);

            frameType = (int)QueenBeeFrameType.UpwardFly;

            bool canShootHornetHives = npc.life < npc.lifeMax * 0.75f;
            int totalThingsToSummon = canShootHornetHives ? 4 : 7;

            // Shoot 3 hives instead of 2 once below 33% life.
            if (npc.life < npc.lifeMax * 0.33f)
                totalThingsToSummon = 3;

            int summonRate = 25;
            if (canShootHornetHives)
                summonRate = 45;

            if (Distance(target.Center.X, npc.Center.X) > 60f)
                npc.spriteDirection = (target.Center.X - npc.Center.X > 0).ToDirectionInt();

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % summonRate == summonRate - 1f)
            {
                Vector2 spawnPosition = new(npc.Center.X, npc.Center.Y + npc.height * 0.325f);
                spawnPosition += Main.rand.NextVector2Circular(25f, 25f);

                if (canShootHornetHives)
                {
                    Vector2 hiveShootVelocity = (target.Center - spawnPosition).SafeNormalize(Vector2.UnitY) * 11.5f;
                    spawnPosition += hiveShootVelocity * 2f;
                    Utilities.NewProjectileBetter(spawnPosition, hiveShootVelocity, ModContent.ProjectileType<HornetHive>(), HornetHiveDamage, 0f);
                }
                else
                {
                    int bee = NPC.NewNPC(npc.GetSource_FromAI(), (int)spawnPosition.X, (int)spawnPosition.Y, NPCID.Bee);
                    Main.npc[bee].velocity = Main.npc[bee].SafeDirectionTo(target.Center, Vector2.UnitY).RotatedByRandom(0.37f) * 4f;
                }
            }

            if (attackTimer >= summonRate * totalThingsToSummon)
                SelectNextAttack(npc);
        }

        public static void DoAttack_InwardMovingBees(NPC npc, Player target, ref float frameType, ref float attackTimer)
        {
            int hoverTime = 150;
            int beeSummonDelay = 60;
            int beeShootRate = 23;
            int beeShootTime = 480;
            float coneSpread = 0.37f;

            if (npc.life < npc.lifeMax * FinalPhaseLifeRatio)
            {
                hoverTime -= 30;
                beeShootRate -= 5;
                beeShootTime -= 60;
            }

            ref float beeAimConeDirection = ref npc.Infernum().ExtraAI[0];

            frameType = (int)QueenBeeFrameType.UpwardFly;

            // Hover to the side of the target before beginning the attack.
            if (attackTimer < hoverTime)
            {
                Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X - npc.Center.X < 0f).ToDirectionInt() * 540f;
                npc.spriteDirection = (target.Center.X - npc.Center.X > 0).ToDirectionInt();
                npc.velocity *= 0.9f;
                npc.Center = Vector2.Lerp(npc.Center, hoverDestination, 0.04f).MoveTowards(hoverDestination, 8f);
                return;
            }

            if (attackTimer == hoverTime)
            {
                SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                beeAimConeDirection = npc.AngleTo(target.Center);
                npc.netUpdate = true;
            }

            // Release a flurry of stingers as a pseudo-arena.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 3f == 2f && attackTimer < hoverTime + beeSummonDelay + beeShootTime)
            {
                float stingerShootSpeed = 11f;
                Vector2 stingerSpawnPosition = new(npc.Center.X + Main.rand.NextFloat(4f) * npc.spriteDirection, npc.Center.Y + npc.height * 0.3f);
                Vector2 stingerShootVelocity = (beeAimConeDirection - coneSpread).ToRotationVector2() * stingerShootSpeed;
                int stinger = Utilities.NewProjectileBetter(stingerSpawnPosition, stingerShootVelocity, ProjectileID.Stinger, StingerDamage, 0f);
                if (Main.projectile.IndexInRange(stinger))
                    Main.projectile[stinger].tileCollide = false;

                stingerShootVelocity = (beeAimConeDirection + coneSpread).ToRotationVector2() * stingerShootSpeed;
                stinger = Utilities.NewProjectileBetter(stingerSpawnPosition, stingerShootVelocity, ProjectileID.Stinger, StingerDamage, 0f);
                if (Main.projectile.IndexInRange(stinger))
                    Main.projectile[stinger].tileCollide = false;
            }

            // Summon bees that converge inward.
            bool isTimeToSummonBees = attackTimer >= hoverTime + beeSummonDelay && attackTimer < hoverTime + beeSummonDelay + beeShootTime;
            if (Main.netMode != NetmodeID.MultiplayerClient && isTimeToSummonBees && attackTimer % beeShootRate == beeShootRate - 1f)
            {
                Vector2 beeSpawnPosition = target.Center + npc.SafeDirectionTo(target.Center).RotatedByRandom(beeAimConeDirection * 0.6f) * 500f;
                Vector2 beeShootVelocity = (target.Center - beeSpawnPosition).SafeNormalize(Vector2.UnitY) * 6f;
                Utilities.NewProjectileBetter(beeSpawnPosition, beeShootVelocity, ModContent.ProjectileType<ConvergingHornet>(), ConvergingHornetDamage, 0f);
            }

            // Bob up and down.
            if (isTimeToSummonBees)
                npc.velocity = Vector2.UnitY * Cos(TwoPi * attackTimer / 150f) * 3f;

            // Delete far away stingers.
            foreach (Projectile stinger in Utilities.AllProjectilesByID(ProjectileID.Stinger))
            {
                if (!stinger.WithinRange(npc.Center, 1500f))
                    stinger.Kill();
            }

            // Approach the target if they're too far away.
            if (isTimeToSummonBees && !npc.WithinRange(target.Center, 900f))
                npc.Center = npc.Center.MoveTowards(target.Center, 8f);

            if (attackTimer == hoverTime + beeSummonDelay + beeShootTime + 75f)
                ConvergingHornet.MakeAllBeesFlyOutward();

            if (attackTimer >= hoverTime + beeSummonDelay + beeShootTime + 145f)
            {
                Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<ConvergingHornet>());
                SelectNextAttack(npc);
            }
        }

        public static void DoAttack_BeeletHell(NPC npc, Player target, ref float frameType, ref float attackTimer)
        {
            frameType = (int)QueenBeeFrameType.UpwardFly;

            int hoverTime = 75;
            if (attackTimer < hoverTime)
            {
                Vector2 flyDestination = target.Center - new Vector2((target.Center.X - npc.Center.X > 0).ToDirectionInt() * 270f, 240f);
                DoHoverMovement(npc, flyDestination, 0.15f);
            }
            else if (attackTimer < 775f)
            {
                // Gain some extra defense to prevent melting.
                npc.defense += 22;

                npc.velocity *= 0.9785f;

                // Roar and make a circle of honey dust as an indicator before release the bees.
                if (attackTimer == hoverTime + 1f)
                {
                    SoundEngine.PlaySound(SoundID.Roar, target.Center);
                    for (int i = 0; i < 30; i++)
                    {
                        Vector2 honeyDustVelocity = (TwoPi * i / 30f).ToRotationVector2() * 5f;
                        Dust honey = Dust.NewDustPerfect(npc.Center, DustID.Honey2);
                        honey.scale = Main.rand.NextFloat(1f, 1.85f);
                        honey.velocity = honeyDustVelocity;
                        honey.noGravity = true;
                    }
                }

                int shootRate = npc.life < npc.lifeMax * FinalPhaseLifeRatio ? 16 : 20;
                if (attackTimer % shootRate == shootRate - 1f && attackTimer < 600f)
                {
                    Vector2 beeSpawnPosition = target.Center + new Vector2(Main.rand.NextBool(2).ToDirectionInt() * 1200f, Main.rand.NextFloat(-900f, 0f));
                    Vector2 beeVelocity = (target.Center - beeSpawnPosition).SafeNormalize(Vector2.UnitY) * new Vector2(4f, 20f);
                    Utilities.NewProjectileBetter(beeSpawnPosition, beeVelocity, ModContent.ProjectileType<TinyBee>(), TinyBeeDamage, 0f);
                }
            }

            if (attackTimer >= 805f)
                SelectNextAttack(npc);

            npc.spriteDirection = (target.Center.X - npc.Center.X > 0).ToDirectionInt();
        }

        public static void DoHoverMovement(NPC npc, Vector2 destination, float flyAcceleration)
        {
            Vector2 idealVelocity = npc.SafeDirectionTo(destination) * 29f;
            if (npc.velocity.X < idealVelocity.X)
            {
                npc.velocity.X += flyAcceleration;
                if (npc.velocity.X < 0f && idealVelocity.X > 0f)
                    npc.velocity.X += flyAcceleration * 2f;
            }
            else if (npc.velocity.X > idealVelocity.X)
            {
                npc.velocity.X -= flyAcceleration;
                if (npc.velocity.X > 0f && idealVelocity.X < 0f)
                    npc.velocity.X -= flyAcceleration * 2f;
            }
            if (npc.velocity.Y < idealVelocity.Y)
            {
                npc.velocity.Y += flyAcceleration;
                if (npc.velocity.Y < 0f && idealVelocity.Y > 0f)
                    npc.velocity.Y += flyAcceleration * 2f;
            }
            else if (npc.velocity.Y > idealVelocity.Y)
            {
                npc.velocity.Y -= flyAcceleration;
                if (npc.velocity.Y > 0f && idealVelocity.Y < 0f)
                    npc.velocity.Y -= flyAcceleration * 2f;
            }
        }
        #endregion Specific Attacks

        #region AI Utility Methods

        public static void SelectNextAttack(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;

            QueenBeeAttackState oldAttackType = (QueenBeeAttackState)(int)npc.ai[0];
            QueenBeeAttackState newAttackType = QueenBeeAttackState.HorizontalCharge;
            switch (oldAttackType)
            {
                case QueenBeeAttackState.HorizontalCharge:
                    newAttackType = QueenBeeAttackState.StingerBurst;
                    break;
                case QueenBeeAttackState.StingerBurst:
                    newAttackType = QueenBeeAttackState.HoneyBlast;
                    break;
                case QueenBeeAttackState.HoneyBlast:
                    newAttackType = lifeRatio < 0.5f ? QueenBeeAttackState.InwardMovingBees : QueenBeeAttackState.CreateMinionsFromAbdomen;
                    break;
                case QueenBeeAttackState.CreateMinionsFromAbdomen:
                case QueenBeeAttackState.InwardMovingBees:
                    newAttackType = lifeRatio < 0.5f ? QueenBeeAttackState.BeeletHell : QueenBeeAttackState.HorizontalCharge;
                    break;
            }

            if (lifeRatio < 0.1f)
                newAttackType = oldAttackType == QueenBeeAttackState.HorizontalCharge ? QueenBeeAttackState.BeeletHell : QueenBeeAttackState.HorizontalCharge;

            npc.ai[0] = (int)newAttackType;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }

        #endregion AI Utility Methods

        #endregion AI

        #region Drawing and Frames

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frameCounter++;
            if (npc.frameCounter % 5 == 4)
                npc.frame.Y += frameHeight;
            switch ((QueenBeeFrameType)(int)npc.localAI[0])
            {
                case QueenBeeFrameType.UpwardFly:
                    if (npc.frame.Y < frameHeight * 4)
                        npc.frame.Y = frameHeight * 4;
                    if (npc.frame.Y >= frameHeight * Main.npcFrameCount[npc.type])
                        npc.frame.Y = frameHeight * 4;
                    break;
                case QueenBeeFrameType.HorizontalCharge:
                    if (npc.frame.Y >= frameHeight * 4)
                        npc.frame.Y = 0;
                    break;
            }
        }
        #endregion

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Mods.InfernumMode.PetDialog.QueenBeeTip1";
            yield return n => "Mods.InfernumMode.PetDialog.QueenBeeTip2";
            yield return n => "Mods.InfernumMode.PetDialog.QueenBeeTip3";

            yield return n =>
            {
                if (TipsManager.ShouldUseJokeText)
                    return "Mods.InfernumMode.PetDialog.QueenBeeJokeTip1";
                return string.Empty;
            };
            yield return n =>
            {
                if (TipsManager.ShouldUseJokeText)
                    return "Mods.InfernumMode.PetDialog.QueenBeeJokeTip2";
                return string.Empty;
            };
        }
        #endregion Tips
    }
}
