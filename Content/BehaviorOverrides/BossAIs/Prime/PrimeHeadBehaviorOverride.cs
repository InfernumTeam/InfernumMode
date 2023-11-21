using CalamityMod.Events;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.Particles;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Twins;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Prime
{
    public class PrimeHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.SkeletronPrime;

        #region Enumerations
        public enum PrimeAttackType
        {
            SpawnEffects,

            GenericCannonAttacking,
            SynchronizedMeleeArmCharges,
            SlowSparkShrapnelMeleeCharges,

            MetalBurst,
            RocketRelease,
            HoverCharge,
            LightningSupercharge,
            ReleaseTeslaMines
        }

        public enum PrimeFrameType
        {
            ClosedMouth,
            OpenMouth,
            Spikes
        }
        #endregion

        #region AI
        public static bool AnyArms => NPC.AnyNPCs(NPCID.PrimeCannon) || NPC.AnyNPCs(NPCID.PrimeLaser) || NPC.AnyNPCs(NPCID.PrimeVice) || NPC.AnyNPCs(NPCID.PrimeSaw);

        // This exists primarily for mecha mayhem compatibility.
        public static bool CanPerformCameraEffects => !NPC.AnyNPCs(NPCID.TheDestroyer) && !NPC.AnyNPCs(NPCID.Spazmatism) && !NPC.AnyNPCs(NPCID.Retinazer);

        public const int MetalSpikeDamage = 135;

        public const int SkullDamage = 135;

        public const int SmallLaserDamage = 135;

        public const int ElectricBoltDamage = 140;

        public const int MissileDamage = 140;

        public const int TeslaBombDamage = 140;

        public const int SawSparkDamage = 150;

        public const int TeslaCloudDamage = 150;

        public const int DeathrayDamage = 250;

        public const int CannonsShouldNotFireIndex = 0;

        public const int CannonCycleTimerIndex = 1;

        public const int HasCreatedShieldIndex = 6;

        public const int HasPerformedLaserRayAttackIndex = 7;

        public const int ShudderAmountIndex = 8;

        public const int BaseCollectiveCannonHP = 22000;

        public const int BaseCollectiveCannonHPBossRush = 346000;

        public const int CannonAttackCycleTime = 600;

        // These two constants should together add up to a clean integer dividend from CannonAttackCycleTime.
        public const int GenericCannonTelegraphTime = 54;

        public const int GenericCannonShootTime = 146;

        public const float Phase2LifeRatio = 0.4f;

        public const float ForcedLaserRayLifeRatio = 0.2f;

        public static Dictionary<int, Vector2> ArmPositionOrdering => new()
        {
            [NPCID.PrimeLaser] = new(-350f, 200f),
            [NPCID.PrimeSaw] = new(-200f, 150f),
            [NPCID.PrimeVice] = new(200f, 150f),
            [NPCID.PrimeCannon] = new(350f, 200f),
        };

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio
        };

        public override bool PreAI(NPC npc)
        {
            // Vanilla appears to have hardcoded spaghetti for its pupil drawing code that cannot easily be undone with TML.
            // As a consequence, it's simply rendered so far away that it doesn't actually matter.
            npc.frame = new Rectangle(10000000, 10000000, 94, 94);

            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float armCycleTimer = ref npc.ai[2];
            ref float hasRedoneSpawnAnimation = ref npc.ai[3];
            ref float frameType = ref npc.localAI[0];
            ref float cannonsShouldNotFire = ref npc.Infernum().ExtraAI[CannonsShouldNotFireIndex];
            ref float hasCreatedShield = ref npc.Infernum().ExtraAI[HasCreatedShieldIndex];
            ref float shudderAmount = ref npc.Infernum().ExtraAI[ShudderAmountIndex];

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();

            float lifeRatio = npc.life / (float)npc.lifeMax;
            Player target = Main.player[npc.target];

            // Continuously reset defense and damage.
            npc.defense = npc.defDefense;
            npc.damage = npc.defDamage + 24;
            npc.timeLeft = 3600;
            shudderAmount = 0f;

            // Someone is going to get hurt.
            if (target.HasBuff(BuffID.Electrified))
                target.ClearBuff(BuffID.Electrified);

            // Don't allow damage to happen if any arms remain or the shield is still up.
            List<Projectile> shields = Utilities.AllProjectilesByID(ModContent.ProjectileType<PrimeShield>()).ToList();
            npc.dontTakeDamage = AnyArms || shields.Count > 0;

            // Create the shield.
            if (Main.netMode != NetmodeID.MultiplayerClient && hasCreatedShield == 0f)
            {
                Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<PrimeShield>(), 0, 0f, -1, npc.whoAmI);
                hasCreatedShield = 1f;
            }

            if (!target.active || target.dead || Main.dayTime)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * 26f, 0.08f);
                if (!npc.WithinRange(target.Center, 1560f))
                    npc.active = false;

                return false;
            }

            // Do the spawn animation again once entering the second phase.
            if (!AnyArms && hasRedoneSpawnAnimation == 0f && attackType != (int)PrimeAttackType.SpawnEffects)
            {
                // Clear any stray projectiles.
                Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<PrimeMissile>(), ModContent.ProjectileType<PrimeSmallLaser>(), ModContent.ProjectileType<SawSpark>());

                attackTimer = 0f;
                attackType = (int)PrimeAttackType.SpawnEffects;
                hasRedoneSpawnAnimation = 1f;
                npc.netUpdate = true;
            }

            switch ((PrimeAttackType)(int)attackType)
            {
                case PrimeAttackType.SpawnEffects:
                    DoBehavior_SpawnEffects(npc, target, attackTimer, ref frameType);
                    break;

                // The behaviors between these attacks do differ between arms, but the head does not need to behave any differently.
                case PrimeAttackType.GenericCannonAttacking:
                case PrimeAttackType.SynchronizedMeleeArmCharges:
                case PrimeAttackType.SlowSparkShrapnelMeleeCharges:
                    DoBehavior_GenericCannonAttacking(npc, target, ref attackTimer, ref frameType, ref cannonsShouldNotFire);
                    break;
                case PrimeAttackType.MetalBurst:
                    DoBehavior_MetalBurst(npc, target, attackTimer, ref frameType);
                    break;
                case PrimeAttackType.RocketRelease:
                    DoBehavior_RocketRelease(npc, target, lifeRatio, attackTimer, ref frameType);
                    break;
                case PrimeAttackType.HoverCharge:
                    DoBehavior_HoverCharge(npc, target, lifeRatio, attackTimer, ref frameType);
                    break;
                case PrimeAttackType.LightningSupercharge:
                    DoBehavior_LightningSupercharge(npc, target, ref attackTimer, ref frameType);
                    break;
                case PrimeAttackType.ReleaseTeslaMines:
                    DoBehavior_ReleaseTeslaMines(npc, target, lifeRatio, ref attackTimer, ref frameType);
                    break;
            }

            if (npc.position.Y < 900f)
                npc.position.Y = 900f;

            armCycleTimer++;
            attackTimer++;
            return false;
        }

        #region Specific Attacks
        public static void DoBehavior_SpawnEffects(NPC npc, Player target, float attackTimer, ref float frameType)
        {
            int hoverTime = 90;
            int animationTime = 210;
            bool canHover = attackTimer < hoverTime;

            bool phaseTwo = npc.ai[3] > 0;

            ref float shudderAmount = ref npc.Infernum().ExtraAI[ShudderAmountIndex];

            // Focus on the boss as it spawns.
            if (npc.WithinRange(Main.LocalPlayer.Center, 3700f) && CanPerformCameraEffects)
            {
                Main.LocalPlayer.Infernum_Camera().ScreenFocusPosition = npc.Center;
                Main.LocalPlayer.Infernum_Camera().ScreenFocusInterpolant = Utils.GetLerpValue(0f, 15f, attackTimer, true);
                Main.LocalPlayer.Infernum_Camera().ScreenFocusInterpolant *= Utils.GetLerpValue(animationTime, animationTime - 8f, attackTimer, true);
            }

            // Don't do damage during the spawn animation.
            npc.damage = 0;

            if (canHover)
            {
                Vector2 hoverDestination = target.Center - Vector2.UnitY * 500f;

                npc.velocity = npc.SafeDirectionTo(hoverDestination) * MathF.Min(npc.Distance(hoverDestination), 32f);
                npc.rotation = npc.rotation.AngleLerp(npc.velocity.X * 0.04f, 0.1f);

                if (npc.WithinRange(target.Center, 90f))
                {
                    npc.Center = target.Center - npc.SafeDirectionTo(target.Center, Vector2.UnitY) * 90f;
                    npc.ai[1] = 89f;
                    npc.netUpdate = true;
                }

                frameType = (int)PrimeFrameType.ClosedMouth;
            }
            else
            {
                if (attackTimer >= animationTime - 15f)
                    frameType = (int)PrimeFrameType.OpenMouth;

                npc.velocity *= 0.85f;
                npc.rotation = npc.rotation.AngleLerp(0f, 0.2f);

                if (phaseTwo)
                {
                    // Shudder around angrily.
                    float interpolant = Utils.GetLerpValue(hoverTime, animationTime, attackTimer, true);
                    shudderAmount = Lerp(1f, 5f, interpolant);

                    if (Main.rand.NextBool((int)Lerp(15f, 7f, interpolant)))
                    {
                        Vector2 angerParticleSpawnPosition = npc.Center - Vector2.UnitY.RotatedBy(npc.spriteDirection * npc.rotation) * 50f + Main.rand.NextVector2Circular(50f, 30f);
                        int angerParticleLifetime = Main.rand.Next(65, 90);
                        float angerParticleScale = Main.rand.NextFloat(0.27f, 0.4f);
                        CartoonAngerParticle angy = new(angerParticleSpawnPosition, Color.Red, Color.DarkRed, angerParticleLifetime, Main.rand.NextFloat(TwoPi), angerParticleScale);
                        GeneralParticleHandler.SpawnParticle(angy);
                    }
                }

                if (attackTimer > animationTime)
                {
                    SoundEngine.PlaySound(SoundID.Roar, target.Center);

                    if (phaseTwo)
                    {
                        SoundEngine.PlaySound(InfernumSoundRegistry.DestroyerChargeImpactSound, target.Center);
                        SoundEngine.PlaySound(InfernumSoundRegistry.ExoMechImpendingDeathSound with { Pitch = 0.2f }, target.Center);

                        SoundEngine.PlaySound(SoundID.DD2_LightningBugZap, target.Center);
                        for (int i = 0; i < 20; i++)
                        {
                            Vector2 sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f, 11f);
                            Color sparkColor = Color.Lerp(Color.Orange, Color.IndianRed, Main.rand.NextFloat(0.4f, 1f));
                            GeneralParticleHandler.SpawnParticle(new SparkParticle(npc.Center, sparkVelocity, false, 60, 2f, sparkColor));

                            sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 10f);
                            Color arcColor = Color.Lerp(Color.Orange, Color.Red, Main.rand.NextFloat(0.3f, 1f));
                            GeneralParticleHandler.SpawnParticle(new ElectricArc(npc.Center, sparkVelocity, arcColor, 0.84f, 30));
                        }
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        if (!phaseTwo)
                            SpawnArms(npc);
                        else
                            Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<TwinsEnergyExplosion>(), 0, 0f);
                    }

                    SelectNextAttack(npc);
                }
            }
        }

        public static void DoBehavior_GenericCannonAttacking(NPC npc, Player target, ref float attackTimer, ref float frameType, ref float cannonsShouldNotFire)
        {
            // Don't do anything if there are no cannons.
            if (!AnyArms)
            {
                SelectNextAttack(npc);
                return;
            }

            // Disable contact damage entirely.
            npc.damage = 0;

            // Hover in place, either above or below the target.
            PrimeAttackType attackType = (PrimeAttackType)npc.ai[0];
            float wrappedAttackTimer = attackTimer % CannonAttackCycleTime;
            PerformDefaultArmPhaseHover(npc, target, attackTimer, attackType);

            // Make cannons not fire if near the target.
            cannonsShouldNotFire = npc.WithinRange(target.Center, 160f).ToInt();

            // Calculate the cannon attack timer.
            GetCannonAttributesByAttack(attackType, out _, out _, out int shootCycleTime);

            npc.Infernum().ExtraAI[CannonCycleTimerIndex] = wrappedAttackTimer % shootCycleTime;

            // Rotate based on velocity.
            npc.rotation = npc.velocity.X * 0.032f;

            // Keep the mouth closed.
            frameType = (int)PrimeFrameType.ClosedMouth;

            if (attackTimer >= CannonAttackCycleTime * 2f && attackType == PrimeAttackType.GenericCannonAttacking)
                SelectNextAttack(npc);
            if (attackTimer >= CannonAttackCycleTime && attackType == PrimeAttackType.SynchronizedMeleeArmCharges)
                SelectNextAttack(npc);
            if (attackTimer >= CannonAttackCycleTime * 0.62f && attackType == PrimeAttackType.SlowSparkShrapnelMeleeCharges)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_MetalBurst(NPC npc, Player target, float attackTimer, ref float frameType)
        {
            int shootRate = AnyArms ? 125 : 70;
            int shootCount = AnyArms ? 4 : 5;
            int spikesPerBurst = AnyArms ? 7 : 23;
            float hoverSpeed = AnyArms ? 15f : 36f;
            float wrappedTime = attackTimer % shootRate;

            if (BossRushEvent.BossRushActive)
            {
                spikesPerBurst += 10;
                hoverSpeed = MathF.Max(hoverSpeed, 30f) * 1.2f;
            }

            // Don't do contact damage, to prevent cheap hits.
            npc.damage = 0;

            Vector2 destination = target.Center - Vector2.UnitY * (AnyArms ? 550f : 435f);
            npc.Center = npc.Center.MoveTowards(destination, 5f);
            if (!npc.WithinRange(destination, 40f))
                npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * hoverSpeed, hoverSpeed / 37f);
            else
                npc.velocity *= 1.02f;
            npc.rotation = npc.velocity.X * 0.04f;

            bool canFire = attackTimer <= shootRate * shootCount && attackTimer > 75f;

            // Open the mouth a little bit before shooting.
            frameType = wrappedTime >= shootRate * 0.7f ? (int)PrimeFrameType.OpenMouth : (int)PrimeFrameType.ClosedMouth;

            // Only shoot projectiles if above and not extremely close to the player.
            if (wrappedTime == shootRate - 1f && npc.Center.Y < target.Center.Y - 150f && !npc.WithinRange(target.Center, 200f) && canFire)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < spikesPerBurst; i++)
                    {
                        Vector2 spikeVelocity = (TwoPi * i / spikesPerBurst).ToRotationVector2() * 5.5f;
                        if (AnyArms)
                            spikeVelocity *= 0.56f;
                        if (BossRushEvent.BossRushActive)
                            spikeVelocity *= 3f;

                        Utilities.NewProjectileBetter(npc.Center + spikeVelocity * 12f, spikeVelocity, ModContent.ProjectileType<MetallicSpike>(), MetalSpikeDamage, 0f);
                    }
                }
                SoundEngine.PlaySound(SoundID.Item101, target.Center);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= shootRate * shootCount + 90f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_RocketRelease(NPC npc, Player target, float lifeRatio, float attackTimer, ref float frameType)
        {
            int cycleTime = 36;
            int rocketCountPerCycle = 7;
            int shootCycleCount = AnyArms ? 4 : 6;
            int rocketShootDelay = AnyArms ? 60 : 35;

            // The attack lasts longer when only the laser and cannon are around so you can focus them down.
            if (!NPC.AnyNPCs(NPCID.PrimeVice) && !NPC.AnyNPCs(NPCID.PrimeSaw) && NPC.AnyNPCs(NPCID.PrimeLaser) && NPC.AnyNPCs(NPCID.PrimeCannon))
                shootCycleCount = 6;

            float wrappedTime = attackTimer % cycleTime;

            // Rotate and stop doing damage.
            npc.rotation = npc.velocity.X * 0.04f;
            npc.damage = 0;

            frameType = (int)PrimeFrameType.ClosedMouth;
            if (wrappedTime > cycleTime - rocketCountPerCycle * 2f && attackTimer > rocketShootDelay)
            {
                frameType = (int)PrimeFrameType.OpenMouth;

                if (!npc.WithinRange(target.Center, 250f))
                    npc.velocity *= 0.87f;

                if (wrappedTime % 3f == 2f)
                {
                    SoundEngine.PlaySound(Apollo.MissileLaunchSound with { Pitch = 0.2f }, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float rocketSpeed = Main.rand.NextFloat(12.5f, 14f) * (AnyArms ? 0.825f : 1f);
                        if (!AnyArms)
                            rocketSpeed += (1f - lifeRatio) * 5.6f;
                        Vector2 rocketVelocity = Main.rand.NextVector2CircularEdge(rocketSpeed, rocketSpeed);
                        if (rocketVelocity.Y < -1f)
                            rocketVelocity.Y = -1f;
                        rocketVelocity = Vector2.Lerp(rocketVelocity, npc.SafeDirectionTo(target.Center).RotatedByRandom(0.1f) * rocketVelocity.Length(), 0.96f);
                        rocketVelocity = rocketVelocity.SafeNormalize(-Vector2.UnitY) * rocketSpeed;
                        Utilities.NewProjectileBetter(npc.Center + Vector2.UnitY * 33f, rocketVelocity, ModContent.ProjectileType<PrimeMissile>(), MissileDamage, 0f);
                    }
                }

                if (Main.netMode != NetmodeID.MultiplayerClient && wrappedTime % 12f == 11f && !AnyArms)
                {
                    Vector2 idealVelocity = npc.SafeDirectionTo(target.Center + Main.rand.NextVector2Circular(100f, 100f)) * 8f;
                    idealVelocity += Main.rand.NextVector2Circular(2f, 2f);
                    Vector2 spawnPosition = npc.Center + idealVelocity * 3f;

                    int skull = Utilities.NewProjectileBetter(spawnPosition, idealVelocity, ProjectileID.Skull, SkullDamage, 0f, Main.myPlayer, -1f, 0f);
                    Main.projectile[skull].ai[0] = -1f;
                    Main.projectile[skull].timeLeft = 300;
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= cycleTime * (shootCycleCount + 0.4f))
                SelectNextAttack(npc);
        }

        public static void DoBehavior_HoverCharge(NPC npc, Player target, float lifeRatio, float attackTimer, ref float frameType)
        {
            int chargeCount = 4;
            int hoverTime = AnyArms ? 120 : 60;
            int chargeTime = AnyArms ? 72 : 45;
            float hoverSpeed = AnyArms ? 14f : 33f;
            float chargeSpeed = AnyArms ? 15f : 29f;

            // Have a bit longer of a delay for the first charge.
            if (attackTimer < hoverTime + chargeTime)
                hoverTime += 15;

            float wrappedTime = attackTimer % (hoverTime + chargeTime);

            if (BossRushEvent.BossRushActive)
            {
                hoverSpeed *= 1.3f;
                chargeSpeed *= 1.6f;
            }

            if (!AnyArms)
                chargeSpeed += (1f - lifeRatio) * 5f;

            if (wrappedTime < hoverTime - 15f)
            {
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 365f, -300f);
                npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathF.Min(npc.Distance(hoverDestination), hoverSpeed)) / 8f;

                if (npc.WithinRange(target.Center, 150f))
                {
                    npc.Center = target.Center - npc.SafeDirectionTo(target.Center, Vector2.UnitY) * 150f;
                    npc.velocity = Vector2.Zero;
                }
                npc.rotation = npc.velocity.X * 0.04f;
                frameType = (int)PrimeFrameType.ClosedMouth;
            }
            else if (wrappedTime < hoverTime)
            {
                npc.velocity *= 0.94f;
                npc.rotation = npc.velocity.X * 0.04f;
                frameType = (int)PrimeFrameType.OpenMouth;
            }
            else
            {
                if (wrappedTime == hoverTime + 1f)
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                    npc.netUpdate = true;

                    if (!AnyArms)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 0; i < 11; i++)
                            {
                                Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(Lerp(-0.7f, 0.7f, i / 10f)) * 8f;
                                Utilities.NewProjectileBetter(npc.Center + shootVelocity * 7f, shootVelocity, ModContent.ProjectileType<MetallicSpike>(), MetalSpikeDamage, 0f);
                            }
                        }
                        SoundEngine.PlaySound(InfernumSoundRegistry.PrimeChargeSound, target.Center);

                        // Release some smoke backwards.
                        for (int i = 0; i < 32; i++)
                        {
                            Color smokeColor = Color.Lerp(Color.Orange, Color.Red, Main.rand.NextFloat(0.65f));
                            SmallSmokeParticle smoke = new(npc.Center + Main.rand.NextVector2Circular(90f, 90f), -npc.velocity.RotatedByRandom(0.52f) * Main.rand.NextFloat(0.3f, 2f), smokeColor, Color.DarkGray, 1f, 255f, Main.rand.NextFloat(0.01f));
                            GeneralParticleHandler.SpawnParticle(smoke);
                        }
                        for (int i = 0; i < 20; i++)
                        {
                            Color smokeColor = Color.Lerp(Color.Orange, Color.Red, Main.rand.NextFloat(0.55f));
                            MediumMistParticle smoke = new(npc.Center + Main.rand.NextVector2Circular(90f, 90f), -npc.velocity.RotatedByRandom(0.52f) * Main.rand.NextFloat(0.2f, 1.2f), smokeColor, Color.White, 1f, 255f, Main.rand.NextFloat(0.01f));
                            GeneralParticleHandler.SpawnParticle(smoke);
                        }
                    }
                }

                frameType = (int)PrimeFrameType.Spikes;
                npc.rotation += npc.velocity.Length() * 0.018f;
            }

            if (attackTimer >= (hoverTime + chargeTime) * chargeCount + 20)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_LightningSupercharge(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            int lightningCreationDelay = 35;
            ref float struckByLightningFlag = ref npc.Infernum().ExtraAI[0];
            ref float lineTelegraphInterpolant = ref npc.Infernum().ExtraAI[1];
            ref float superchargeTimer = ref npc.Infernum().ExtraAI[2];
            ref float laserSignDirection = ref npc.Infernum().ExtraAI[3];
            ref float laserOffsetAngle = ref npc.Infernum().ExtraAI[4];
            ref float laserDirection = ref npc.Infernum().ExtraAI[5];

            // Reset the line telegraph interpolant.
            lineTelegraphInterpolant = 0f;

            if (attackTimer < lightningCreationDelay)
            {
                npc.velocity *= 0.84f;
                npc.rotation = npc.velocity.X * 0.04f;
            }

            // Create a bunch of scenic lightning and decide the laser direction.
            if (attackTimer == lightningCreationDelay)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.CalThunderStrikeSound, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 lightningSpawnPosition = npc.Center - Vector2.UnitY * 1300f + Main.rand.NextVector2Circular(30f, 30f);
                        if (lightningSpawnPosition.Y < 600f)
                            lightningSpawnPosition.Y = 600f;
                        Utilities.NewProjectileBetter(lightningSpawnPosition, Vector2.UnitY * Main.rand.NextFloat(1.7f, 2f), ModContent.ProjectileType<LightningStrike>(), 0, 0f, -1, PiOver2, Main.rand.Next(100));
                    }
                }

                if (laserSignDirection == 0f)
                {
                    laserSignDirection = Main.rand.NextBool().ToDirectionInt();
                    npc.netUpdate = true;
                }
            }

            frameType = (int)PrimeFrameType.ClosedMouth;

            // Stop the attack timer if lightning has not supercharged yet. Also declare the laser direction for laser.
            if (attackTimer > lightningCreationDelay + 1f && struckByLightningFlag == 0f)
            {
                attackTimer = lightningCreationDelay + 1f;
                laserDirection = npc.AngleTo(target.Center);
            }

            else if (struckByLightningFlag == 1f)
            {
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 320f, -270f) - npc.velocity * 4f;
                float movementSpeed = Lerp(1f, 0.7f, Utils.GetLerpValue(45f, 90f, attackTimer, true)) * npc.Distance(target.Center) * 0.0074f;
                if (movementSpeed < 4.25f)
                    movementSpeed = 0f;

                npc.velocity = (npc.velocity * 6f + npc.SafeDirectionTo(hoverDestination) * MathF.Min(npc.Distance(hoverDestination), movementSpeed)) / 7f;
                npc.rotation = npc.velocity.X * 0.04f;

                if (npc.WithinRange(target.Center, 150f))
                {
                    npc.Center = target.Center - npc.SafeDirectionTo(target.Center, Vector2.UnitY) * 150f;
                    npc.velocity = Vector2.Zero;
                }

                superchargeTimer++;

                // Prepare line telegraphs.
                if (attackTimer < 165f)
                {
                    lineTelegraphInterpolant = Utils.GetLerpValue(lightningCreationDelay, 165, attackTimer, true);
                    laserDirection += Utils.GetLerpValue(0f, 0.6f, lineTelegraphInterpolant, true) * Utils.GetLerpValue(1f, 0.7f, lineTelegraphInterpolant, true) * Pi / 300f;
                }

                // Roar as a telegraph.
                if (attackTimer == 130f)
                {
                    SoundEngine.PlaySound(SoundID.Roar, target.Center);
                    SoundEngine.PlaySound(InfernumSoundRegistry.PBGMechanicalWarning, target.Center);
                }
                if (attackTimer > 95f)
                    frameType = (int)PrimeFrameType.OpenMouth;

                float shootSpeedAdditive = npc.Distance(target.Center) * 0.0084f;
                if (BossRushEvent.BossRushActive)
                    shootSpeedAdditive += 10f;

                // Fire 9 lasers outward. They intentionally avoid intersecting the player's position and do not rotate.
                // Their purpose is to act as a "border".
                if (attackTimer == 165f)
                {
                    SoundEngine.PlaySound(AresLaserCannon.LaserbeamShootSound, target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 12; i++)
                        {
                            Vector2 laserFirePosition = npc.Center - Vector2.UnitY * 16f;
                            Vector2 individualLaserDirection = (TwoPi * i / 12f).ToRotationVector2();
                            Utilities.NewProjectileBetter(laserFirePosition, individualLaserDirection, ModContent.ProjectileType<EvenlySpreadPrimeLaserRay>(), DeathrayDamage, 0f, -1, 0f, npc.whoAmI);
                        }
                    }
                }

                // Use the spike frame type and make the laser move.
                if (attackTimer > 165f)
                {
                    frameType = (int)PrimeFrameType.Spikes;
                    laserOffsetAngle += Utils.GetLerpValue(165f, 255f, attackTimer, true) * laserSignDirection * Pi / 300f;
                }

                // Release electric sparks periodically, along with missiles.
                Vector2 mouthPosition = npc.Center + Vector2.UnitY * 33f;
                if (attackTimer > 180f && attackTimer < 435f && attackTimer % 44f == 43f)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.AresTeslaShotSound, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float offsetAngle = Main.rand.NextFloat(TwoPi);
                        for (int i = 0; i < 12; i++)
                        {
                            Vector2 electricityVelocity = (target.Center - mouthPosition).SafeNormalize(Vector2.UnitY).RotatedBy(TwoPi * i / 12f + offsetAngle) * (shootSpeedAdditive + 9f);
                            Utilities.NewProjectileBetter(mouthPosition, electricityVelocity, ProjectileID.MartianTurretBolt, ElectricBoltDamage, 0f);
                        }
                    }
                }
                if (attackTimer > 180f && attackTimer < 435f && attackTimer % 30f == 29f)
                {
                    SoundEngine.PlaySound(Apollo.MissileLaunchSound, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 rocketVelocity = (target.Center - mouthPosition).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.47f) * (shootSpeedAdditive + 8f);
                        Utilities.NewProjectileBetter(mouthPosition, rocketVelocity, ModContent.ProjectileType<PrimeMissile>(), MissileDamage, 0f);
                    }
                }
            }

            if (attackTimer > 435f)
                superchargeTimer = Utils.GetLerpValue(465f, 435f, attackTimer, true) * 30f;

            if (attackTimer > 465f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_ReleaseTeslaMines(NPC npc, Player target, float lifeRatio, ref float attackTimer, ref float frameType)
        {
            int slowdownTime = 45;
            int bombCount = (int)Lerp(10f, 20f, 1f - lifeRatio);

            // Choose the frame type.
            frameType = (int)PrimeFrameType.Spikes;

            // Sit in place for a moment.
            if (attackTimer < slowdownTime)
            {
                npc.velocity *= 0.9f;
                npc.rotation = npc.velocity.X * 0.04f;
            }

            // Release a bunch of tesla orb bombs around the target.
            if (attackTimer == slowdownTime)
            {
                SoundEngine.PlaySound(Karasawa.FireSound, target.Center);
                HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.PrimeTeslaTip");
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < bombCount; i++)
                    {
                        Vector2 bombSpawnPosition = target.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(400f, 1550f);
                        int bomb = Utilities.NewProjectileBetter(bombSpawnPosition, Vector2.Zero, ModContent.ProjectileType<TeslaBomb>(), TeslaBombDamage, 0f);
                        if (Main.projectile.IndexInRange(bomb))
                        {
                            Main.projectile[bomb].ai[0] = Main.rand.Next(45, 70);
                            Main.projectile[bomb].netUpdate = true;
                        }
                    }
                }
            }

            if (attackTimer == slowdownTime + 75f)
                SelectNextAttack(npc);
        }

        #endregion Specific Attacks

        #region General Helper Functions
        public static void SelectNextAttack(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            PrimeAttackType oldAttack = (PrimeAttackType)(int)npc.ai[0];
            WeightedRandom<PrimeAttackType> attackSelector = new(Main.rand);
            if (!AnyArms)
            {
                attackSelector.Add(PrimeAttackType.MetalBurst);
                attackSelector.Add(PrimeAttackType.RocketRelease);
                attackSelector.Add(PrimeAttackType.HoverCharge);

                if (lifeRatio < Phase2LifeRatio)
                {
                    attackSelector.Add(PrimeAttackType.ReleaseTeslaMines, 1.7);
                    if (Main.rand.NextFloat() < 0.3f)
                        attackSelector.Add(PrimeAttackType.LightningSupercharge, 20D);
                }
            }
            else
            {
                attackSelector.Add(PrimeAttackType.GenericCannonAttacking);
                attackSelector.Add(PrimeAttackType.SynchronizedMeleeArmCharges);
                attackSelector.Add(PrimeAttackType.SlowSparkShrapnelMeleeCharges);
            }

            // Reduce old velocity so that Prime doesn't fly off somewhere after an attack concludes.
            npc.velocity *= Lerp(1f, 0.25f, Utils.GetLerpValue(14f, 30f, npc.velocity.Length()));

            do
                npc.ai[0] = (int)attackSelector.Get();
            while (npc.ai[0] == (int)oldAttack);

            // Force Prime to use the laser ray attack if it hasn't yet and is at sufficiently low health.
            // This obviously does not happen if arms are present, on the offhand chance that the player melts him so quickly that this check is triggered during the
            // desperation phase.
            if (lifeRatio < ForcedLaserRayLifeRatio && npc.Infernum().ExtraAI[HasPerformedLaserRayAttackIndex] == 0f && !AnyArms)
            {
                npc.ai[0] = (int)PrimeAttackType.LightningSupercharge;
                npc.Infernum().ExtraAI[HasPerformedLaserRayAttackIndex] = 1f;
            }

            if (oldAttack is PrimeAttackType.SynchronizedMeleeArmCharges or PrimeAttackType.SlowSparkShrapnelMeleeCharges)
                npc.ai[0] = (int)PrimeAttackType.GenericCannonAttacking;

            // Always start the fight with generic cannon attacks.
            if (oldAttack is PrimeAttackType.SpawnEffects)
                npc.ai[0] = (int)PrimeAttackType.GenericCannonAttacking;

            npc.TargetClosest();
            npc.ai[1] = 0f;
            for (int i = 0; i < 6; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            // Reset local variables for hands.
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n.ai[1] != npc.whoAmI)
                    continue;

                if (n.type is not NPCID.PrimeCannon and not NPCID.PrimeLaser and not NPCID.PrimeSaw and not NPCID.PrimeVice)
                    continue;

                for (int j = 0; j < 5; j++)
                    n.Infernum().ExtraAI[j] = 0f;
                n.netUpdate = true;
            }

            npc.netUpdate = true;
        }

        public static void GetCannonAttributesByAttack(PrimeAttackType attackState, out int telegraphTime, out int totalShootTime, out int shootCycleTime)
        {
            telegraphTime = GenericCannonTelegraphTime;
            totalShootTime = GenericCannonShootTime;
            shootCycleTime = telegraphTime + totalShootTime;

            // The synchronized arm charges happen only once, and last throughout the duration of the overall cycle.
            if (attackState is PrimeAttackType.SynchronizedMeleeArmCharges or PrimeAttackType.SlowSparkShrapnelMeleeCharges)
            {
                shootCycleTime = CannonAttackCycleTime;
                totalShootTime = shootCycleTime - telegraphTime;
            }
        }

        public static bool CannonCanBeUsed(NPC cannon, out float telegraphInterpolant)
        {
            telegraphInterpolant = 0f;
            NPC head = Main.npc[(int)cannon.ai[1]];
            if (!head.active)
                return false;

            PrimeAttackType attackState = (PrimeAttackType)head.ai[0];
            ref float cannonAttackTimer = ref head.Infernum().ExtraAI[CannonCycleTimerIndex];

            GetCannonAttributesByAttack(attackState, out int telegraphTime, out _, out int shootCycleTime);

            // All cannons can fire once their collective HP is below a certain life threshold while performing the generic attack.
            bool meleeCannon = cannon.type is NPCID.PrimeVice or NPCID.PrimeSaw;
            bool rangedCannon = cannon.type is NPCID.PrimeLaser or NPCID.PrimeCannon;
            bool onlyRangedCannons = head.ai[1] % (shootCycleTime * 2f) < shootCycleTime;
            bool allCannonsCanFire = false;
            bool useTelegraphs = true;
            if (attackState is PrimeAttackType.SynchronizedMeleeArmCharges or PrimeAttackType.SlowSparkShrapnelMeleeCharges)
                onlyRangedCannons = false;

            if (attackState == PrimeAttackType.SlowSparkShrapnelMeleeCharges)
                useTelegraphs = false;

            float shootTime = cannonAttackTimer;

            // Dedicate the first attack cycle to ranged cannons.
            // After this, only melee cannons will attack, and the two will alternate.
            if (shootTime < telegraphTime)
            {
                telegraphInterpolant = useTelegraphs ? shootTime / telegraphTime : 0f;

                if (!allCannonsCanFire)
                {
                    if (!onlyRangedCannons && rangedCannon)
                        telegraphInterpolant = 0f;
                    if (onlyRangedCannons && meleeCannon)
                        telegraphInterpolant = 0f;
                    return false;
                }
            }

            if (allCannonsCanFire)
                return head.Infernum().ExtraAI[CannonsShouldNotFireIndex] == 0f;

            return (onlyRangedCannons ? rangedCannon : meleeCannon) && head.Infernum().ExtraAI[CannonsShouldNotFireIndex] == 0f;
        }

        public static void PerformDefaultArmPhaseHover(NPC npc, Player target, float attackTimer, PrimeAttackType attackType)
        {
            float hoverSpeed = 23f;
            float hoverAcceleration = 0.48f;
            float verticalHoverOffset = 360f;

            if (attackType == PrimeAttackType.SlowSparkShrapnelMeleeCharges)
                verticalHoverOffset += 120f;

            bool hoverAboveTarget = attackTimer % (CannonAttackCycleTime * 2f) < CannonAttackCycleTime;
            Vector2 hoverDestination = target.Center - Vector2.UnitY * hoverAboveTarget.ToDirectionInt() * verticalHoverOffset;
            Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * hoverSpeed;
            bool tooCloseToTarget = Distance(npc.Center.Y, target.Center.Y) < verticalHoverOffset + 100f;
            bool tooFarFromDestination = Distance(npc.Center.Y, target.Center.Y) > verticalHoverOffset - 100f;
            if (npc.WithinRange(hoverDestination, 50f))
            {
                idealVelocity *= 0.2f;
                hoverAcceleration *= 0.15f;
            }

            npc.SimpleFlyMovement(idealVelocity, hoverAcceleration);

            // Snap back into the vertical position if too far from the destination or too close to the target.
            if (tooCloseToTarget || tooFarFromDestination)
                npc.velocity.Y = Lerp(npc.velocity.Y, idealVelocity.Y, 0.12f);

            // Slow down and settle in place if near the hover destination.
            if (npc.WithinRange(hoverDestination, 100f))
            {
                npc.velocity *= 0.8f;
                if (npc.velocity.Length() < 0.5f)
                    npc.velocity = Vector2.Zero;
            }
        }

        public static void SpawnArms(NPC npc, int? lifeOverride = null)
        {
            npc.TargetClosest();
            int arm = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, NPCID.PrimeCannon, npc.whoAmI);
            int firstArm = arm;
            Main.npc[arm].ai[0] = -1f;
            Main.npc[arm].ai[1] = npc.whoAmI;
            Main.npc[arm].target = npc.target;
            Main.npc[arm].life = lifeOverride ?? Main.npc[arm].life;
            Main.npc[arm].netUpdate = true;

            arm = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, NPCID.PrimeLaser, npc.whoAmI);
            Main.npc[arm].ai[0] = 1f;
            Main.npc[arm].ai[1] = npc.whoAmI;
            Main.npc[arm].target = npc.target;
            Main.npc[arm].realLife = firstArm;
            Main.npc[arm].life = lifeOverride ?? Main.npc[arm].life;
            Main.npc[arm].netUpdate = true;

            arm = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, NPCID.PrimeSaw, npc.whoAmI);
            Main.npc[arm].ai[0] = 1f;
            Main.npc[arm].ai[1] = npc.whoAmI;
            Main.npc[arm].target = npc.target;
            Main.npc[arm].realLife = firstArm;
            Main.npc[arm].life = lifeOverride ?? Main.npc[arm].life;
            Main.npc[arm].netUpdate = true;

            arm = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, NPCID.PrimeVice, npc.whoAmI);
            Main.npc[arm].ai[0] = -1f;
            Main.npc[arm].ai[1] = npc.whoAmI;
            Main.npc[arm].target = npc.target;
            Main.npc[arm].realLife = firstArm;
            Main.npc[arm].life = lifeOverride ?? Main.npc[arm].life;
            Main.npc[arm].netUpdate = true;
        }
        #endregion General Helper Functions

        #endregion AI

        #region Frames and Drawcode
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            ref float shudderAmount = ref npc.Infernum().ExtraAI[ShudderAmountIndex];

            NPCID.Sets.MustAlwaysDraw[npc.type] = true;

            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Texture2D eyes = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Prime/PrimeEyes").Value;
            Rectangle frame = texture.Frame(1, Main.npcFrameCount[npc.type], 0, (int)npc.localAI[0]);
            Vector2 baseDrawPosition = npc.Center - Main.screenPosition + Main.rand.NextVector2Circular(shudderAmount, shudderAmount);
            for (int i = 9; i >= 0; i -= 2)
            {
                Vector2 drawPosition = npc.oldPos[i] + npc.Size * 0.5f - Main.screenPosition;
                Color afterimageColor = npc.GetAlpha(lightColor);
                afterimageColor.R = (byte)(afterimageColor.R * (10 - i) / 20);
                afterimageColor.G = (byte)(afterimageColor.G * (10 - i) / 20);
                afterimageColor.B = (byte)(afterimageColor.B * (10 - i) / 20);
                afterimageColor.A = (byte)(afterimageColor.A * (10 - i) / 20);
                Main.spriteBatch.Draw(TextureAssets.Npc[npc.type].Value, drawPosition, frame, afterimageColor, npc.rotation, frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            }

            float superchargePower = Utils.GetLerpValue(0f, 30f, npc.Infernum().ExtraAI[1], true);
            if (npc.ai[0] != (int)PrimeAttackType.LightningSupercharge)
                superchargePower = 0f;

            if (superchargePower > 0f)
            {
                float outwardness = superchargePower * 6f + Cos(Main.GlobalTimeWrappedHourly * 2f) * 0.5f;
                for (int i = 0; i < 8; i++)
                {
                    Vector2 drawOffset = (TwoPi * i / 8f + Main.GlobalTimeWrappedHourly * 2.9f).ToRotationVector2() * outwardness;
                    Color drawColor = Color.Red * 0.42f;
                    drawColor.A = 0;

                    Main.spriteBatch.Draw(texture, baseDrawPosition + drawOffset, frame, npc.GetAlpha(drawColor), npc.rotation, frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
                }
            }

            Main.spriteBatch.Draw(texture, baseDrawPosition, frame, npc.GetAlpha(lightColor), npc.rotation, frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(eyes, baseDrawPosition, frame, Color.White, npc.rotation, frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);

            // Draw line telegraphs for the lightning attack.
            float lineTelegraphInterpolant = npc.Infernum().ExtraAI[1];
            if (npc.ai[0] == (int)PrimeAttackType.LightningSupercharge && lineTelegraphInterpolant > 0f)
            {
                Main.spriteBatch.SetBlendState(BlendState.Additive);

                float angularOffset = npc.Infernum().ExtraAI[5];
                Texture2D line = InfernumTextureRegistry.BloomLine.Value;
                Color outlineColor = Color.Lerp(Color.Red, Color.White, lineTelegraphInterpolant);
                Vector2 origin = new(line.Width / 2f, line.Height);
                Vector2 beamScale = new(lineTelegraphInterpolant * 0.5f, 2.4f);
                for (int i = 0; i < 12; i++)
                {
                    Vector2 beamDirection = (TwoPi * i / 12f + angularOffset).ToRotationVector2();
                    Vector2 drawPosition = npc.Center - Vector2.UnitY * 16f + beamDirection * 2f - Main.screenPosition;
                    float beamRotation = beamDirection.ToRotation() - PiOver2;
                    Main.spriteBatch.Draw(line, drawPosition, null, outlineColor, beamRotation, origin, beamScale, 0, 0f);
                }
                Main.spriteBatch.ResetBlendState();
            }
            return false;
        }
        #endregion Frames and Drawcode

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Mods.InfernumMode.PetDialog.PrimeTip1";
            yield return n =>
            {
                if (TipsManager.ShouldUseJokeText)
                    return "Mods.InfernumMode.PetDialog.PrimeJokeTip1";
                return string.Empty;
            };
        }
        #endregion Tips
    }
}
