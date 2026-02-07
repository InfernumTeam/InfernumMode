using System;
using System.Collections.Generic;
using System.Linq;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Events;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.NPCs.AstrumDeus;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Sounds;
using InfernumMode.Assets.Sounds;
using InfernumMode.Content.BehaviorOverrides.BossAIs.AstrumAureus;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AstrumDeus
{
    public class AstrumDeusHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<AstrumDeusHead>();

        #region Enumerations
        public enum DeusAttackType
        {
            WarpCharge,
            AstralMeteorShower,
            RubbleFromBelow,
            VortexLemniscate,
            PlasmaAndCrystals,
            AstralSolarSystem,
            InfectedStarWeave,
            DarkGodsOutburst,
            AstralGlobRush,
            ConstellationExplosions
        }
        #endregion Enumerations

        #region AI

        public static int AstralPlasmaSparkDamage => 175;

        public static int AstralFlameDamage => 180;

        public static int AstralLaserDamage => 180;

        public static int AstralPlasmaFireballDamage => 180;

        public static int AstralRubbleDamage => 180;

        public static int DarkBoltDamage => 180;

        public static int InfectionGlobDamage => 180;

        public static int AstralVortexDamage => 220;

        public static int BlackHoleDamage => 240;

        public static int BlackHoleLaserDamage => 240;

        public const float Phase2LifeRatio = 0.6f;

        public const float Phase3LifeRatio = 0.33333f;

        public const float EnrageStartDistance = 4800f;

        public override float[] PhaseLifeRatioThresholds =>
        [
            Phase2LifeRatio,
            Phase3LifeRatio
        ];

        public override bool PreAI(NPC npc)
        {
            // Emit a pale white light idly.
            Lighting.AddLight(npc.Center, 0.3f, 0.3f, 0.3f);

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();

            // Reset damage. Do none by default if somewhat transparent.
            npc.defDamage = 180;
            npc.damage = npc.alpha > 40 ? 0 : npc.defDamage;

            // If none was found or it was too far away, despawn.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || Main.dayTime || !Main.player[npc.target].active)
            {
                DoBehavior_Despawn(npc);
                return false;
            }

            Player target = Main.player[npc.target];

            // Lol. Lmao.
            if (target.HasBuff(ModContent.BuffType<AstralInfectionDebuff>()))
                target.ClearBuff(ModContent.BuffType<AstralInfectionDebuff>());

            // Create a beacon if none exists.
            List<Projectile> beacons = Utilities.AllProjectilesByID(ModContent.ProjectileType<DeusRitualDrama>()).ToList();
            if (beacons.Count == 0)
            {
                Projectile.NewProjectile(npc.GetSource_FromAI(), target.Center, Vector2.Zero, ModContent.ProjectileType<DeusRitualDrama>(), 0, 0f);
                beacons = Utilities.AllProjectilesByID(ModContent.ProjectileType<DeusRitualDrama>()).ToList();
            }

            // Allow Calamity's code to drop loot as usual.
            npc.Calamity().newAI[0] = 3f;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            float beaconAngerFactor = Utils.GetLerpValue(EnrageStartDistance, EnrageStartDistance + 800f, Distance(beacons.First().Center.X, target.Center.X), true);
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float hasCreatedSegments = ref npc.localAI[0];
            ref float releasingParticlesFlag = ref npc.localAI[1];
            ref float inFinalPhase = ref npc.Infernum().ExtraAI[7];
            ref float dontTakeDamage = ref npc.Infernum().ExtraAI[9];

            bool phase2 = lifeRatio < Phase2LifeRatio;
            bool phase3 = inFinalPhase == 1f;

            // Save the beacon anger factor in a variable for use by the sky code.
            npc.Infernum().ExtraAI[6] = beaconAngerFactor;
            npc.Calamity().CurrentlyEnraged = beaconAngerFactor > 0.7f;

            // Create segments and initialize on the first frame.
            if (Main.netMode != NetmodeID.MultiplayerClient && hasCreatedSegments == 0f)
            {
                CreateSegments(npc, 65, ModContent.NPCType<AstrumDeusBody>(), ModContent.NPCType<AstrumDeusTail>());
                attackType = (int)DeusAttackType.AstralMeteorShower;
                hasCreatedSegments = 1f;
                dontTakeDamage = 1f;
                npc.netUpdate = true;
            }

            // Prevent natural despawns.
            npc.timeLeft = 3600;

            bool enteringLastPhase = lifeRatio < Phase3LifeRatio && inFinalPhase == 0f;

            // Clamp position into the world.
            npc.position.X = Clamp(npc.position.X, 1300f, Main.maxTilesX * 16f - 1300f);

            // Don't take damage if requested.
            if (dontTakeDamage == 1f)
                npc.dontTakeDamage = true;
            else
                npc.dontTakeDamage = false;

            // Have Deus fly high into the sky and shed its shell before flying back down in the final phase.
            if (enteringLastPhase)
            {
                int deusSpawnID = ModContent.NPCType<DeusSpawn>();
                Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<AstralConstellation>(),
                    ModContent.ProjectileType<AstralPlasmaFireball>(),
                    ModContent.ProjectileType<AstralPlasmaSpark>(),
                    ModContent.ProjectileType<AstralFlame2>(),
                    ModContent.ProjectileType<AstralCrystal>(),
                    ModContent.ProjectileType<AstralVortex>(),
                    ModContent.ProjectileType<MassiveInfectedStar>());
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == deusSpawnID)
                        Main.npc[i].active = false;
                }

                attackTimer = 0f;
                npc.velocity.Y = Clamp(npc.velocity.Y - 0.4f, -30f, 13.5f);
                npc.rotation = npc.velocity.ToRotation() + PiOver2;
                if (npc.Center.Y < -3500f || !npc.WithinRange(target.Center, 6000f))
                {
                    HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.AstrumDeusShellTip");

                    SelectNextAttack(npc);
                    attackType = (int)DeusAttackType.WarpCharge;
                    inFinalPhase = 1f;
                    npc.HitSound = SoundID.NPCHit1;
                    npc.velocity.Y = 4.5f;
                    npc.netUpdate = true;
                }
                return false;
            }
            else
                npc.position.Y = Clamp(npc.position.Y, 600f, Main.maxTilesY * 16f - 600f);

            // Quickly fade in.
            npc.alpha = Utils.Clamp(npc.alpha - 16, 0, 255);

            switch ((DeusAttackType)(int)attackType)
            {
                case DeusAttackType.WarpCharge:
                    DoBehavior_WarpCharge(npc, target, phase2, phase3, beaconAngerFactor, ref attackTimer);
                    break;
                case DeusAttackType.AstralMeteorShower:
                    DoBehavior_AstralMeteorShower(npc, target, phase2, phase3, beaconAngerFactor, ref attackTimer);
                    break;
                case DeusAttackType.RubbleFromBelow:
                    DoBehavior_RubbleFromBelow(npc, target, phase2, phase3, beaconAngerFactor, ref attackTimer);
                    break;
                case DeusAttackType.VortexLemniscate:
                    DoBehavior_VortexLemniscate(npc, target, phase2, phase3, ref attackTimer);
                    break;
                case DeusAttackType.PlasmaAndCrystals:
                    DoBehavior_PlasmaAndCrystals(npc, target, phase2, phase3, beaconAngerFactor, ref attackTimer);
                    break;
                case DeusAttackType.AstralSolarSystem:
                    DoBehavior_AstralSolarSystem(npc, target, phase2, phase3, beaconAngerFactor, ref attackTimer);
                    break;
                case DeusAttackType.InfectedStarWeave:
                    DoBehavior_InfectedStarWeave(npc, target, phase2, phase3, beaconAngerFactor, ref attackTimer);
                    break;
                case DeusAttackType.DarkGodsOutburst:
                    DoBehavior_DarkGodsOutburst(npc, target, ref attackTimer);
                    break;
                case DeusAttackType.AstralGlobRush:
                    DoBehavior_AstralGlobRush(npc, target, ref attackTimer);
                    break;
                case DeusAttackType.ConstellationExplosions:
                    DoBehavior_ConstellationExplosions(npc, target, ref attackTimer);
                    break;
            }
            attackTimer++;
            return false;
        }

        #region Custom Behaviors

        public static void DoBehavior_Despawn(NPC npc)
        {
            // Ascend into the sky and disappear.
            npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * 30f, 0.08f);
            npc.velocity.X *= 0.975f;
            npc.rotation = npc.velocity.ToRotation() + PiOver2;

            // Cap the despawn timer so that the boss can swiftly disappear.
            npc.timeLeft = Utils.Clamp(npc.timeLeft - 1, 0, 120);

            if (npc.timeLeft <= 0)
            {
                npc.life = 0;
                npc.active = false;
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_WarpCharge(NPC npc, Player target, bool phase2, bool phase3, float beaconAngerFactor, ref float attackTimer)
        {
            int driftTime = 32;
            int fadeInTime = 40;
            int chargeTime = 36;
            int chargeCount = 3;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float teleportOutwardness = Lerp(1580f, 1250f, 1f - lifeRatio) - beaconAngerFactor * 240f;
            float chargeSpeed = Lerp(34f, 42.5f, 1f - lifeRatio) + beaconAngerFactor * 12f;

            if (phase2)
            {
                fadeInTime -= 14;
                chargeTime -= 4;
            }
            if (phase3)
            {
                fadeInTime -= 6;
                chargeTime -= 3;
            }
            if (BossRushEvent.BossRushActive)
            {
                chargeTime -= 4;
                chargeSpeed *= 1.425f;
            }

            int fadeOutTime = fadeInTime / 2 + 8;
            float wrappedTimer = attackTimer % (fadeInTime + chargeTime + fadeOutTime);

            if (wrappedTimer < fadeInTime)
            {
                // Drift towards the player. Contact damage is possible, but should be of little threat.
                if (!npc.WithinRange(target.Center, 375f) && wrappedTimer < driftTime)
                {
                    float newSpeed = Lerp(npc.velocity.Length(), 16f, 0.085f);
                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.032f, true) * newSpeed;
                }
                else if (wrappedTimer >= driftTime)
                {
                    float maxSpeed = BossRushEvent.BossRushActive ? 33f : 26f;
                    if (npc.velocity.Length() < maxSpeed)
                        npc.velocity *= 1.0167f;
                }

                // Rapidly fade out and do the teleport.
                npc.Opacity = Clamp(npc.Opacity - 0.13f, 0f, 1f);
                if (wrappedTimer == fadeInTime - 1f)
                {
                    Vector2 teleportOffsetDirection = -target.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.456f);

                    SoundEngine.PlaySound(AstrumDeusHead.SplitSound, target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 topLeftOfWorld = Vector2.One * 1200f;
                        Vector2 bottomRightOfWorld = new Vector2(Main.maxTilesX, Main.maxTilesY) * 16f - topLeftOfWorld;
                        Vector2 teleportPosition = Vector2.Clamp(target.Center + teleportOffsetDirection * teleportOutwardness * 1.35f, topLeftOfWorld, bottomRightOfWorld);
                        npc.Center = teleportPosition;

                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                        npc.netUpdate = true;
                        Utilities.NewProjectileBetter(npc.Center, npc.SafeDirectionTo(target.Center), ModContent.ProjectileType<AstralTelegraphLine>(), 0, 0f, -1, 0f, 32f);

                        for (int i = 0; i < 7; i++)
                        {
                            float shootOffsetAngle = Lerp(-0.45f, 0.45f, i / 6f);
                            Vector2 fireVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(shootOffsetAngle) * 9f;
                            if (BossRushEvent.BossRushActive)
                                fireVelocity *= 2.3f;

                            Utilities.NewProjectileBetter(npc.Center, fireVelocity, ModContent.ProjectileType<AstralFlame2>(), AstralFlameDamage, 0f);
                        }

                        BringAllSegmentsToNPCPosition(npc);
                    }
                }
            }

            // Fade back in after the charge.
            else if (wrappedTimer < fadeInTime + chargeTime)
                npc.Opacity = Clamp(npc.Opacity + 0.15f, 0f, 1f);

            else if (wrappedTimer < fadeInTime + chargeTime + fadeOutTime)
            {
                npc.Opacity = Clamp(npc.Opacity - 0.06f, 0f, 1f);

                // Attempt to rotate towards the target after the teleport if not super close to them.
                if (!npc.WithinRange(target.Center, 200f))
                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.03f);

                // Teleport near the player again at the end of the cycle.
                if (wrappedTimer == fadeInTime + chargeTime + fadeOutTime)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        npc.Center = target.Center + Main.rand.NextVector2CircularEdge(teleportOutwardness, teleportOutwardness) * 1.75f;
                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                        npc.netUpdate = true;

                        BringAllSegmentsToNPCPosition(npc);
                    }
                }
            }

            if (attackTimer >= chargeCount * (fadeInTime + chargeTime + fadeOutTime) + 3)
                SelectNextAttack(npc);

            // Adjust rotation.
            npc.rotation = npc.velocity.ToRotation() + PiOver2;
        }

        public static void DoBehavior_AstralMeteorShower(NPC npc, Player target, bool phase2, bool phase3, float beaconAngerFactor, ref float attackTimer)
        {
            int upwardRiseTime = 135;
            int meteorShootDelay = 45;
            int meteorReleaseRate = 8;
            int meteorShootTime = 270;
            int attackTransitionDelay = 90;
            float maxUpwardRiseSpeed = 29f;
            float upwardRiseAcceleration = 0.56f;
            float downwardSlamGravity = 0.35f;
            float downwardSlamSpeed = 13.5f;
            float meteorSpeed = 14f;
            ref float meteorRainAngle = ref npc.Infernum().ExtraAI[0];

            if (phase2)
            {
                meteorReleaseRate -= 2;
                maxUpwardRiseSpeed += 4f;
                upwardRiseAcceleration *= 1.4f;
                downwardSlamGravity *= 1.4f;
                downwardSlamSpeed *= 1.1f;
            }
            if (phase3)
                meteorReleaseRate--;

            if (BossRushEvent.BossRushActive)
            {
                meteorReleaseRate--;
                meteorSpeed += 3.5f;
            }

            // Apply distance-enrage buffs.
            meteorReleaseRate = Utils.Clamp((int)(meteorReleaseRate - beaconAngerFactor * 5f), 3, 20);
            meteorSpeed = Lerp(meteorSpeed, 24.5f, beaconAngerFactor);

            // Initialize the meteor rain angle.
            if (meteorRainAngle == 0f)
                meteorRainAngle = Main.rand.NextFloatDirection() * Pi / 12f;

            // Determine rotation.
            // This technically has a one-frame buffer due to velocity calculations happening below, but it shouldn't be significant enough to make
            // a real difference.
            npc.rotation = npc.velocity.ToRotation() + PiOver2;

            // Rise into the sky in anticipation of the downward charge and meteor shower.
            if (attackTimer < upwardRiseTime)
            {
                // Ensure that the horizontal speed does not exceed a low threshold, to prevent Deus from flying too far away from the original position.
                // A nudge towards the horizontal position of the target is constantly applied to mitigate this as well.
                if (Math.Abs(npc.velocity.X) > 4f)
                    npc.velocity.X *= 0.97f;
                if (Distance(npc.Center.X, target.Center.X) > 540f)
                    npc.position.X += npc.SafeDirectionTo(target.Center).X * 11f;

                // Perform vertical movement.
                if (npc.velocity.Y > -maxUpwardRiseSpeed)
                    npc.velocity.Y -= upwardRiseAcceleration;

                // Only allow transitioning to happen once sufficiently far above the player, as a fail-safe.
                if (attackTimer >= upwardRiseTime - 5f && npc.Center.Y > target.Center.Y + 800f)
                    attackTimer = upwardRiseTime - 5f;
                return;
            }

            // Play a powerful thunder sound before the meteor show begins, as a telegraph.
            if (attackTimer == upwardRiseTime + 1f)
                SoundEngine.PlaySound(InfernumSoundRegistry.CalThunderStrikeSound, target.Center);

            // Slam downward, and attempt to aim horizontally in such a way that Deus loosely tries to hit the target.
            // While this isn't necessarily supposed to be the primary source of damage, it's best to be closer to the target than not,
            // to allow for consistent damage from the player.
            if (npc.velocity.Y < 0f)
            {
                npc.velocity.X = Lerp(npc.velocity.X, npc.SafeDirectionTo(target.Center).X * 19f, 0.1f);
                npc.velocity *= 0.965f;
            }
            npc.velocity.Y = Clamp(npc.velocity.Y + downwardSlamGravity, -maxUpwardRiseSpeed, downwardSlamSpeed);

            // Create meteors.
            bool withinShootInterval = attackTimer >= upwardRiseTime + meteorShootDelay && attackTimer < upwardRiseTime + meteorShootDelay + meteorShootTime;
            if (Main.netMode != NetmodeID.MultiplayerClient && withinShootInterval && attackTimer % meteorReleaseRate == meteorReleaseRate - 1f)
            {
                float horizontalOffset = Main.rand.NextFloat(-1050, 1050f);
                if (Math.Abs(target.Center.X) < 1f)
                    horizontalOffset *= 0.1f;

                Vector2 cometSpawnPosition = target.Center + new Vector2(horizontalOffset, -780f);
                Vector2 shootDirection = Vector2.UnitY.RotatedBy(meteorRainAngle);
                Vector2 shootVelocity = shootDirection * meteorSpeed;

                int cometType = ModContent.ProjectileType<AstralBlueComet>();
                Utilities.NewProjectileBetter(cometSpawnPosition, shootVelocity, cometType, AstralFlameDamage, 0f);
            }

            if (withinShootInterval && !npc.WithinRange(target.Center, 600f))
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.04f);

            if (attackTimer >= upwardRiseTime + meteorShootDelay + meteorShootTime + attackTransitionDelay)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_RubbleFromBelow(NPC npc, Player target, bool phase2, bool phase3, float beaconAngerFactor, ref float attackTimer)
        {
            int minDescendTime = 90;
            int minRiseTime = 75;
            int rubbleCount = 15;
            int attackTransitionDelay = 75;
            float rubbleFlySpeedFactor = Lerp(1f, 1.5f, beaconAngerFactor);
            float descendGravity = Lerp(0.6f, 1.36f, beaconAngerFactor);

            // This is fast, but not too fast, to prevent potential cheap hits if Deus happens to fall on top of the player.
            float maxDescendSpeed = 26f;
            float riseAcceleration = Lerp(0.84f, 1.56f, beaconAngerFactor);
            float maxRiseSpeed = 34.5f;
            bool movingBackDownAgain = attackTimer >= minDescendTime + minRiseTime + 8f;

            if (phase2)
            {
                rubbleCount += 5;
                rubbleFlySpeedFactor += 0.36f;
            }
            if (phase3)
            {
                rubbleCount += 9;
                rubbleFlySpeedFactor += 0.25f;
            }

            // Determine rotation.
            // This technically has a one-frame buffer due to velocity calculations happening below, but it shouldn't be significant enough to make
            // a real difference.
            npc.rotation = npc.velocity.ToRotation() + PiOver2;

            // Descend downward and make any remaining horizontal movement fizzle out.
            if (attackTimer < minDescendTime || movingBackDownAgain)
            {
                bool isntFarEnoughDown = npc.Center.Y < target.Center.Y + 700f && !movingBackDownAgain;

                // Let the descent persist if not sufficiently far down below the target yet.
                if (isntFarEnoughDown && attackTimer >= minDescendTime - 5f)
                    attackTimer = minDescendTime - 5f;

                if (attackTimer >= minDescendTime + minRiseTime + attackTransitionDelay)
                {
                    SelectNextAttack(npc);
                }

                npc.velocity.X *= 0.985f;
                npc.velocity.Y = Clamp(npc.velocity.Y + descendGravity, -16f, maxDescendSpeed);
                return;
            }

            // Rise back up into the air.
            if (attackTimer < minDescendTime + minRiseTime)
            {
                bool isntFarEnoughUp = npc.Center.Y > target.Center.Y - 840f;

                // Rapidly degrade any old downward movement.
                if (npc.velocity.Y > 0f)
                    npc.velocity.Y *= 0.925f;

                // Try to stay horizontally near the target.
                if (Distance(target.Center.X, npc.Center.X) > 150f)
                    npc.velocity.X = Lerp(npc.velocity.X, npc.SafeDirectionTo(target.Center).X * 20f, 0.05f);

                // Let the rise persist if not sufficiently far down below the target yet.
                if (isntFarEnoughUp && attackTimer >= minDescendTime + minRiseTime - 5f)
                    attackTimer = minDescendTime + minRiseTime - 5f;

                npc.velocity.Y = Clamp(npc.velocity.Y - riseAcceleration, -maxRiseSpeed, maxDescendSpeed);
            }

            // Release rubble upward in a mostly even spread at the apex of the rise.
            if (attackTimer == minDescendTime + minRiseTime + 5f)
            {
                HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.AstrumDeusRubbleTip");
                SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < rubbleCount; i++)
                    {
                        for (float j = 0.2f; j <= 1f; j += 0.2f)
                        {
                            float shootOffsetAngle = Lerp(-1.24f, 1.24f, i / (float)(rubbleCount - 1f)) + Main.rand.NextFloatDirection() * 0.032f;
                            Vector2 rubbleVelocity = Vector2.Lerp(-Vector2.UnitY, (npc.rotation + PiOver2).ToRotationVector2(), 0.32f);
                            rubbleVelocity = rubbleVelocity.SafeNormalize(Vector2.UnitY).RotatedBy(shootOffsetAngle) * Main.rand.NextFloat(17.5f, 25f) * rubbleFlySpeedFactor * j;
                            Utilities.NewProjectileBetter(npc.Center + rubbleVelocity * 2f, rubbleVelocity, ModContent.ProjectileType<AstralRubble>(), AstralRubbleDamage, 0f);
                        }
                    }
                }
            }
        }

        public static void DoBehavior_VortexLemniscate(NPC npc, Player target, bool phase2, bool phase3, ref float attackTimer)
        {
            int flameSpawnRate = 60;
            int vortexCreationDelay = 60;
            int chargeAtPlayerDelay = AstralVortex.ScaleFadeinTime + 30;

            if (phase2)
                flameSpawnRate -= 12;
            if (phase3)
                flameSpawnRate -= 15;
            if (BossRushEvent.BossRushActive)
                flameSpawnRate -= 8;

            ref float lemniscateCenterX = ref npc.Infernum().ExtraAI[0];
            ref float lemniscateCenterY = ref npc.Infernum().ExtraAI[1];
            ref float hasCharged = ref npc.Infernum().ExtraAI[2];

            // Disable contact damage.
            npc.damage = 0;

            // Approach the target before performing the attack.
            if (!npc.WithinRange(target.Center, 650f) && attackTimer < vortexCreationDelay)
            {
                npc.velocity = npc.SafeDirectionTo(target.Center) * 40f;
                attackTimer = vortexCreationDelay - 2f;
                return;
            }

            // The parametric form of a lemniscate of bernoulli is as follows:
            // x = r * cos(t) / (1 + sin^2(t))
            // y = r * sin(t) * cos(t) / (1 + sin^2(t))
            // Given that these provide positions, we can determine the velocity path that Deus must follow to move in this pattern
            // via taking derivatives of both components.

            // Quotient rule:
            // (g(x) / h(x))' = (g'(x) * h(x) - g(x) * h'(x)) / h(x)^2

            // Shorthands:
            // g(t) = cos(t) --- cos(t) * sin(t)
            // g'(t) = -sin(t) --- cos(2t)
            // h(t) = 1 + sin^2(t)
            // h'(t) = sin(2t)

            // Calculations for dx/dt:
            // (g'(t) * h(t) - g(t) * h'(t)) / h(t)^2 = 
            // r * ((-sin(t) * (1 + sin^2(t)) - cos(t) * sin(2t)) / (1 + sin^2(t))^2 =
            // r * (-sin(t) - sin^3(t) - cos(t) * sin(2t)) / (1 + 2sin^2(t) + sin^4(t))

            // Calculations for dy/dt:
            // (g'(t) * h(t) - g(t) * h'(t)) / h(t)^2 = 
            // r * ((cos(2t) * (1 + sin^2(t)) - sin(t) * cos(t) * sin(2t)) / (1 + sin^2(t))^2 =
            // r * (cos^2(t) - 2sin^4(t) - sin(t) * cos(t) * sin(2t)) / (1 + 2sin^2(t) + sin^4(t))
            float flySpeed = 54f;
            if (hasCharged == 0f)
            {
                float t = TwoPi * attackTimer / 150f;
                float sinT = Sin(t);
                float sin2T = Sin(t * 2f);
                float cosT = Cos(t);
                float denominator = 1f + 1f * Pow(Sin(t), 2f) + Pow(sinT, 4f);

                float speedX = flySpeed * (-sinT - Pow(sinT, 3f) - cosT * sin2T) / denominator;
                float speedY = flySpeed * (Pow(cosT, 2f) - Pow(sinT, 4f) * 2f - sinT * cosT * sin2T) / denominator;
                npc.velocity = new(speedX, speedY);
            }

            npc.rotation = npc.velocity.ToRotation() + PiOver2;

            // Initialize the center of the lemniscate.
            if (lemniscateCenterX == 0f || lemniscateCenterY == 0f)
            {
                lemniscateCenterX = npc.Center.X + flySpeed * 12f;
                lemniscateCenterY = npc.Center.Y + 420f;
                npc.netUpdate = true;
            }

            // Create the vortices at the foci positions once ready.
            if (attackTimer == vortexCreationDelay)
            {
                Vector2 lemniscateCenter = new(lemniscateCenterX, lemniscateCenterY);
                Vector2[] lemniscateFoci =
                [
                    lemniscateCenter - Vector2.UnitX * flySpeed * 11.5f,
                    lemniscateCenter + Vector2.UnitX * flySpeed * 11.5f
                ];

                bool cyan = true;
                List<int> vortices = [];
                foreach (Vector2 focus in lemniscateFoci)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(vortex => vortex.localAI[0] = cyan.ToInt());

                        int vortex = Utilities.NewProjectileBetter(focus, Vector2.Zero, ModContent.ProjectileType<AstralVortex>(), AstralVortexDamage, 0f);
                        if (Main.projectile.IndexInRange(vortex))
                        {
                            vortices.Add(vortex);
                            cyan = false;
                        }
                    }

                    for (int i = 0; i < vortices.Count; i++)
                    {
                        Main.projectile[vortices[i]].ai[1] = vortices[(i + 1) % vortices.Count];
                        Main.projectile[vortices[i]].netUpdate = true;
                    }
                }
            }

            // Charge at the player and hurl both vortices at them if they're closely within Deus' line of sight.
            bool targetInLineOfSight = npc.velocity.AngleBetween(npc.SafeDirectionTo(target.Center)) < 0.71f;
            if (attackTimer >= vortexCreationDelay + chargeAtPlayerDelay && targetInLineOfSight)
            {
                if (hasCharged == 0f)
                {
                    SoundEngine.PlaySound(ScorchedEarth.RocketShoot, target.Center);
                    foreach (Projectile vortex in Utilities.AllProjectilesByID(ModContent.ProjectileType<AstralVortex>()))
                    {
                        vortex.velocity = vortex.SafeDirectionTo(target.Center) * (BossRushEvent.BossRushActive ? 24f : 11f);
                        vortex.ModProjectile<AstralVortex>().FlameSpawnRate = flameSpawnRate;
                        vortex.netUpdate = true;
                    }

                    npc.velocity = npc.SafeDirectionTo(target.Center) * npc.velocity.Length();
                    hasCharged = 1f;
                    npc.netUpdate = true;
                }
            }

            // Try to spin around the target after charging.
            if (hasCharged == 1f)
            {
                Vector2 spinDestination = target.Center + (attackTimer * TwoPi / 150f).ToRotationVector2() * 1080f;

                // Periodically release plasma fireballs at the target.
                if (attackTimer % 50f == 49f)
                {
                    SoundEngine.PlaySound(PlasmaCaster.FireSound, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 plasmaShootVelocity = npc.SafeDirectionTo(target.Center) * (BossRushEvent.BossRushActive ? 20.5f : 13f);
                        Utilities.NewProjectileBetter(npc.Center + plasmaShootVelocity * 3f, plasmaShootVelocity, ModContent.ProjectileType<AstralPlasmaFireball>(), AstralPlasmaFireballDamage, 0f);
                        npc.netUpdate = true;
                    }
                }

                npc.Center = npc.Center.MoveTowards(spinDestination, target.velocity.Length() * 1.2f + 35f);
                npc.velocity = npc.SafeDirectionTo(spinDestination) * MathF.Min(npc.Distance(spinDestination), 34f);
                if (!Utilities.AnyProjectiles(ModContent.ProjectileType<AstralVortex>()) && !Utilities.AnyProjectiles(ModContent.ProjectileType<AstralFlame2>()))
                    SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_PlasmaAndCrystals(NPC npc, Player target, bool phase2, bool phase3, float beaconAngerFactor, ref float attackTimer)
        {
            int shootTime = 420;
            int attackTransitionDelay = 105;
            int plasmaShootRate = (int)Lerp(54f, 28f, beaconAngerFactor);
            int crystalShootRate = 45;
            bool closeEnoughToSnap = npc.WithinRange(target.Center, 375f);
            float flySpeed = 18.5f;
            float flyTurnSpeed = 0.039f;
            float plasmaShootSpeed = 12.5f;
            ref float plasmaShootTimer = ref npc.Infernum().ExtraAI[0];

            if (phase2)
            {
                plasmaShootRate -= 10;
                crystalShootRate -= 7;
                flySpeed *= 1.25f;
                flyTurnSpeed *= 1.3f;
                shootTime -= 32;
            }
            if (phase3)
            {
                plasmaShootRate -= 10;
                crystalShootRate -= 6;
                flySpeed *= 1.25f;
            }
            if (BossRushEvent.BossRushActive)
            {
                flySpeed += 8.4f;
                plasmaShootSpeed += 7f;
            }

            // Fly near the target and snap at them if sufficiently close.
            float nextFlySpeed = Lerp(npc.velocity.Length(), flySpeed, 0.1f);
            Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * flySpeed;
            if (closeEnoughToSnap && npc.velocity.AngleBetween(npc.SafeDirectionTo(target.Center)) < 0.59f)
                npc.velocity *= 1.016f;
            else
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), flyTurnSpeed, true) * nextFlySpeed;
            npc.rotation = npc.velocity.ToRotation() + PiOver2;

            // Periodically release astral plasma fireballs if not close enough to snap at the target.
            if (!closeEnoughToSnap && attackTimer < shootTime)
            {
                plasmaShootTimer++;
                if (plasmaShootTimer >= plasmaShootRate)
                {
                    SoundEngine.PlaySound(PlasmaCaster.FireSound, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 plasmaShootVelocity = npc.velocity.SafeNormalize(Vector2.UnitY) * plasmaShootSpeed;
                        Utilities.NewProjectileBetter(npc.Center + plasmaShootVelocity * 3f, plasmaShootVelocity, ModContent.ProjectileType<AstralPlasmaFireball>(), AstralPlasmaFireballDamage, 0f);

                        plasmaShootTimer = 0f;
                        npc.netUpdate = true;
                    }
                }
            }

            // Release crystals off of body segments.
            if (attackTimer < shootTime)
            {
                int bodyID = ModContent.NPCType<AstrumDeusBody>();
                List<NPC> crystalShootCandidates = Main.npc.Take(Main.maxNPCs).Where(n =>
                {
                    return n.active && n.type == bodyID && !n.WithinRange(target.Center, 375f) && n.WithinRange(target.Center, 960f);
                }).ToList();
                if (attackTimer % crystalShootRate == crystalShootRate - 1f && crystalShootCandidates.Count >= 1)
                {
                    NPC bodyToShootFrom = Main.rand.Next(crystalShootCandidates);
                    SoundEngine.PlaySound(SoundID.Item27, bodyToShootFrom.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 shootPosition = bodyToShootFrom.Center;
                        Vector2 shootVelocity = (target.Center - shootPosition).SafeNormalize(Vector2.UnitY) * 8f;
                        Utilities.NewProjectileBetter(shootPosition, shootVelocity, ModContent.ProjectileType<AstralCrystal>(), AstralFlameDamage, 0f);
                    }
                }
            }

            if (attackTimer >= shootTime + attackTransitionDelay)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_AstralSolarSystem(NPC npc, Player target, bool phase2, bool phase3, float beaconAngerFactor, ref float attackTimer)
        {
            int planetCount = 7;
            int flyTime = 270;
            int attackTransitionDelay = 210;
            int plasmaShootRate = (int)Lerp(50f, 28f, beaconAngerFactor);
            bool closeEnoughToSnap = npc.WithinRange(target.Center, 375f);
            float plasmaShootSpeed = 12f;
            float flySpeed = 17.25f;
            float flyTurnSpeed = 0.035f;
            float spawnFlySpeed = 9f;

            if (phase2)
            {
                planetCount += 3;
                plasmaShootRate -= 7;
            }
            if (phase3)
            {
                planetCount += 2;
                plasmaShootRate -= 9;
            }
            if (BossRushEvent.BossRushActive)
            {
                plasmaShootSpeed += 6.5f;
                spawnFlySpeed += 7f;
            }

            int deusSpawnID = ModContent.NPCType<DeusSpawn>();
            ref float plasmaShootTimer = ref npc.Infernum().ExtraAI[0];

            // Determine rotation.
            // This technically has a one-frame buffer due to velocity calculations happening below, but it shouldn't be significant enough to make
            // a real difference.
            npc.rotation = npc.velocity.ToRotation() + PiOver2;

            // Periodically release astral plasma fireballs if not close enough to snap at the target.
            if (!closeEnoughToSnap && attackTimer < flyTime)
            {
                plasmaShootTimer++;
                if (plasmaShootTimer >= plasmaShootRate)
                {
                    SoundEngine.PlaySound(PlasmaCaster.FireSound, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 plasmaShootVelocity = npc.velocity.SafeNormalize(Vector2.UnitY) * plasmaShootSpeed;
                        Utilities.NewProjectileBetter(npc.Center + plasmaShootVelocity * 3f, plasmaShootVelocity, ModContent.ProjectileType<AstralPlasmaFireball>(), AstralPlasmaFireballDamage, 0f);

                        plasmaShootTimer = 0f;
                        npc.netUpdate = true;
                    }
                }
            }

            // Create the Deus spawns as a solar system on the first frame.
            if (attackTimer == 2f)
            {
                SoundEngine.PlaySound(SoundID.Item28, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < planetCount; i++)
                    {
                        // In degrees.
                        float orbitalAngularVelocity = Main.rand.NextFloatDirection() * 4f;
                        float offsetRadius = Lerp(105f, 360f, i / (float)(planetCount - 1f));

                        NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, deusSpawnID, npc.whoAmI, TwoPi * i / planetCount, offsetRadius, orbitalAngularVelocity);
                    }
                }
            }

            if (attackTimer < flyTime)
            {
                // Fly near the target and snap at them if sufficiently close.
                float nextFlySpeed = Lerp(npc.velocity.Length(), flySpeed, 0.1f);
                Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * flySpeed;
                if (closeEnoughToSnap && npc.velocity.AngleBetween(npc.SafeDirectionTo(target.Center)) < 0.59f)
                    npc.velocity *= 1.016f;
                else
                    npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), flyTurnSpeed, true) * nextFlySpeed;
                return;
            }

            // Make all deus spawns fly towards the target after a sufficient quantity of time has passed.
            if (attackTimer == flyTime + 1f)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (!Main.npc[i].active || Main.npc[i].type != deusSpawnID)
                        continue;

                    Main.npc[i].ai[3] = 1f;
                    Main.npc[i].velocity = Main.npc[i].SafeDirectionTo(target.Center) * spawnFlySpeed;
                    Main.npc[i].netUpdate = true;
                }
            }

            if (attackTimer >= flyTime + attackTransitionDelay)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_InfectedStarWeave(NPC npc, Player target, bool phase2, bool phase3, float beaconAngerFactor, ref float attackTimer)
        {
            int repositionTimeBuffer = 10;
            int starGrowTime = 165;
            int laserShootRate = (int)Lerp(12f, 4f, beaconAngerFactor);
            int attackTransitionDelay = 165;

            if (phase2)
            {
                starGrowTime -= 40;
                attackTransitionDelay -= 27;
            }
            if (phase3)
            {
                starGrowTime -= 50;
                attackTransitionDelay -= 30;
            }

            ref float starSpawnCenterX = ref npc.Infernum().ExtraAI[0];
            ref float starSpawnCenterY = ref npc.Infernum().ExtraAI[1];
            ref float hasCharged = ref npc.Infernum().ExtraAI[2];

            // Decide the position to spawn the star at on the first frame.
            if (attackTimer == 1f)
            {
                Vector2 starSpawnPosition = target.Center + Main.rand.NextVector2CircularEdge(1150f, 1150f);
                starSpawnCenterX = starSpawnPosition.X;
                starSpawnCenterY = starSpawnPosition.Y;

                npc.netUpdate = true;
            }

            // Ensure the star position stays within the world.
            while (starSpawnCenterX < 1600f)
                starSpawnCenterX++;
            while (starSpawnCenterX > Main.maxTilesX * 16f - 1600f)
                starSpawnCenterX--;
            while (starSpawnCenterY < 1600f)
                starSpawnCenterY++;
            while (starSpawnCenterY > Main.maxTilesY * 16f - 1600f)
                starSpawnCenterY--;

            // Circle around the star spawn center.
            if (attackTimer < repositionTimeBuffer + starGrowTime)
            {
                // Disable contact damage.
                npc.damage = 0;

                Vector2 spinDestination = new Vector2(starSpawnCenterX, starSpawnCenterY) + (TwoPi * attackTimer / 135f).ToRotationVector2() * 640f;
                Vector2 oldCenter = npc.Center;
                npc.Center = npc.Center.MoveTowards(spinDestination, target.velocity.Length() * 1.2f + 35f);
                npc.velocity = npc.SafeDirectionTo(spinDestination) * MathF.Min(npc.Distance(spinDestination), 34f);
                if (npc.velocity == Vector2.Zero)
                    npc.rotation = (spinDestination - oldCenter).ToRotation() + PiOver2;

                // Don't let the attack proceed until in position for the spin.
                if ((int)attackTimer == repositionTimeBuffer && !npc.WithinRange(spinDestination, 200f))
                    attackTimer = repositionTimeBuffer - 1f;
            }
            else if (npc.velocity == Vector2.Zero || npc.velocity.Length() < 0.1f)
                npc.velocity = npc.SafeDirectionTo(target.Center);

            // Create the star once ready.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == repositionTimeBuffer + 1f)
            {
                ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(star =>
                {
                    star.ModProjectile<MassiveInfectedStar>().GrowTime = starGrowTime;
                });
                Utilities.NewProjectileBetter(new(starSpawnCenterX, starSpawnCenterY), Vector2.Zero, ModContent.ProjectileType<MassiveInfectedStar>(), BlackHoleDamage, 0f);
            }

            // Send energy bolts towards the star.
            int bodyID = ModContent.NPCType<AstrumDeusBody>();
            List<NPC> bodySegments = Main.npc.Take(Main.maxNPCs).Where(n =>
            {
                return n.active && n.type == bodyID;
            }).ToList();
            if (attackTimer > repositionTimeBuffer && attackTimer < repositionTimeBuffer + starGrowTime && attackTimer % 7f == 6f)
            {
                Vector2 startCenter = new(starSpawnCenterX, starSpawnCenterY);
                SoundEngine.PlaySound(SoundID.Item28, startCenter);
                Dust.QuickDustLine(Main.rand.Next(bodySegments).Center, startCenter, 100f, Main.rand.NextBool() ? Color.Orange : Color.Cyan);
            }

            // Charge and hurl the star at the player after it has grown to its full size.
            if (attackTimer >= repositionTimeBuffer + starGrowTime && npc.velocity.AngleBetween(npc.SafeDirectionTo(target.Center)) < 0.55f && hasCharged == 0f)
            {
                npc.velocity = npc.SafeDirectionTo(target.Center) * 25f;
                foreach (Projectile star in Utilities.AllProjectilesByID(ModContent.ProjectileType<MassiveInfectedStar>()))
                {
                    star.velocity = star.SafeDirectionTo(target.Center) * 21.5f;
                    star.netUpdate = true;
                }
                hasCharged = 1f;
            }

            // Fire lasers at the target from the body segments after the star has been hurled.
            if (attackTimer > repositionTimeBuffer + starGrowTime && attackTimer % laserShootRate == laserShootRate - 1f)
            {
                NPC bodyToShoot = Main.rand.Next(bodySegments);
                SoundEngine.PlaySound(CommonCalamitySounds.LaserCannonSound, bodyToShoot.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 laserShootVelocity = bodyToShoot.SafeDirectionTo(target.Center) * 16f;
                    Utilities.NewProjectileBetter(bodyToShoot.Center, laserShootVelocity, ModContent.ProjectileType<AstralShot2>(), AstralLaserDamage, 0f);
                }
            }

            // Determine rotation.
            if (npc.velocity != Vector2.Zero)
                npc.rotation = npc.velocity.ToRotation() + PiOver2;

            if (attackTimer >= repositionTimeBuffer + starGrowTime + attackTransitionDelay)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_DarkGodsOutburst(NPC npc, Player target, ref float attackTimer)
        {
            int starsInConstellation = 40;
            ref float blackHoleCenterX = ref npc.Infernum().ExtraAI[0];
            ref float blackHoleCenterY = ref npc.Infernum().ExtraAI[1];

            // Decide the position to spawn the black hole and create the dark star constellation at on the first frame.
            if (attackTimer == 1f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int tries = 0;
                    do
                    {
                        tries++;
                        if (tries >= 500)
                            break;

                        Vector2 blackHoleCenter = target.Center + Main.rand.NextVector2CircularEdge(700f, 700f);
                        blackHoleCenterX = blackHoleCenter.X;
                        blackHoleCenterY = blackHoleCenter.Y;
                    }

                    // Avoid placing the black hole near tiles. The laser spin needs to be able to be dodged by letting the target
                    // spin in tandem with the lasers. If they're blocked by tiles then they could recieve an unfair hit or two.
                    while (Collision.SolidCollision(new Vector2(blackHoleCenterX, blackHoleCenterY) - Vector2.One * 600f, 1200, 1200));

                    for (int i = 0; i < starsInConstellation; i++)
                    {
                        float offsetAngle = TwoPi * i / starsInConstellation;
                        Vector2 starPosition = DarkStar.CalculateStarPosition(new Vector2(blackHoleCenterX, blackHoleCenterY), offsetAngle, 0f);
                        Vector2 blackHoleCenter = new(blackHoleCenterX, blackHoleCenterY);

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(star =>
                        {
                            star.ModProjectile<DarkStar>().InitialOffsetAngle = offsetAngle;
                            star.ModProjectile<DarkStar>().AnchorPoint = blackHoleCenter;
                        });
                        Utilities.NewProjectileBetter(starPosition, Vector2.Zero, ModContent.ProjectileType<DarkStar>(), 0, 0f, -1, i, (i + 1f) % starsInConstellation);
                    }
                    Utilities.NewProjectileBetter(new(blackHoleCenterX, blackHoleCenterY), Vector2.Zero, ModContent.ProjectileType<AstralBlackHole>(), BlackHoleDamage, 0f);

                    npc.netUpdate = true;
                }

                HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.AstrumDeusDarkBurstTip");
            }

            // Disable contact damage.
            npc.damage = 0;

            // Circle around the black hole spawn center.
            Vector2 spinDestination = new Vector2(blackHoleCenterX, blackHoleCenterY) + (TwoPi * attackTimer / 135f).ToRotationVector2() * 640f;
            Vector2 oldCenter = npc.Center;
            npc.Center = npc.Center.MoveTowards(spinDestination, target.velocity.Length() * 1.2f + 35f);
            npc.velocity = npc.SafeDirectionTo(spinDestination) * MathF.Min(npc.Distance(spinDestination), 34f);
            npc.rotation = (spinDestination - oldCenter).ToRotation() + PiOver2;

            if (!Utilities.AnyProjectiles(ModContent.ProjectileType<AstralBlackHole>()) && attackTimer >= 2f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_AstralGlobRush(NPC npc, Player target, ref float attackTimer)
        {
            int chargeTime = 420;
            int blobShootThreshold = 24;
            int chargeBlobCount = 14;
            bool closeEnoughToSnap = npc.WithinRange(target.Center, 250f);
            float flySpeed = 24.5f;
            float flyTurnSpeed = 0.05f;
            ref float blobShootTimer = ref npc.Infernum().ExtraAI[0];

            // Fly near the target and snap at them if sufficiently close.
            float nextFlySpeed = Lerp(npc.velocity.Length(), flySpeed, 0.1f);
            Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * flySpeed;
            if (closeEnoughToSnap && npc.velocity.AngleBetween(npc.SafeDirectionTo(target.Center)) < 0.59f)
                npc.velocity *= 1.016f;
            else
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), flyTurnSpeed, true) * nextFlySpeed;
            npc.rotation = npc.velocity.ToRotation() + PiOver2;

            // Shoot infection globs.
            if (npc.WithinRange(target.Center, 420f))
                blobShootTimer++;
            if (blobShootTimer >= blobShootThreshold && closeEnoughToSnap && npc.velocity.AngleBetween(npc.SafeDirectionTo(target.Center)) < 0.5f)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath23, npc.Center);

                Vector2 shootDirection = npc.SafeDirectionTo(target.Center);
                bool lineOfSightIsClear = Collision.CanHit(npc.Center, 1, 1, npc.Center + shootDirection * 120f, 1, 1);

                if (Main.netMode != NetmodeID.MultiplayerClient && lineOfSightIsClear)
                {
                    for (int i = 0; i < chargeBlobCount; i++)
                    {
                        Vector2 blobVelocity = shootDirection * 24f + Main.rand.NextVector2Circular(4f, 4f);
                        Utilities.NewProjectileBetter(npc.Center + blobVelocity, blobVelocity, ModContent.ProjectileType<InfectionGlob>(), InfectionGlobDamage, 0f, -1, 0f, target.Center.Y);
                    }
                    blobShootTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            if (attackTimer >= chargeTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_ConstellationExplosions(NPC npc, Player target, ref float attackTimer)
        {
            int initialAnimationTime = 54;
            int starCreationRate = 2;
            int totalStarsToCreate = 15;
            int explosionTime = 100;
            int constellationCount = 3;
            int starCreationTime = totalStarsToCreate * starCreationRate;
            float plasmaShootSpeed = 13f;
            float animationCompletionRatio = Clamp(attackTimer / initialAnimationTime, 0f, 1f);
            float wrappedAttackTimer = attackTimer % (initialAnimationTime + starCreationTime + explosionTime);
            ref float constellationPatternType = ref npc.Infernum().ExtraAI[0];
            ref float constellationSeed = ref npc.Infernum().ExtraAI[1];

            // Rotate towards the target.
            if (!npc.WithinRange(target.Center, 250f))
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.032f, true) * 14.5f;
            npc.rotation = npc.velocity.ToRotation() + PiOver2;

            // Determine what constellation pattern this arm will use. Each arm has their own pattern that they create.
            if (Main.netMode != NetmodeID.MultiplayerClient && wrappedAttackTimer == initialAnimationTime - 30f)
            {
                constellationSeed = Main.rand.NextFloat();
                constellationPatternType = Main.rand.Next(3);
                npc.netUpdate = true;
            }

            // Create stars.
            if (wrappedAttackTimer >= initialAnimationTime &&
                wrappedAttackTimer < initialAnimationTime + starCreationTime &&
                (wrappedAttackTimer - initialAnimationTime) % starCreationRate == 0f)
            {
                float patternCompletion = Utils.GetLerpValue(initialAnimationTime, initialAnimationTime + starCreationTime, wrappedAttackTimer, true);
                Vector2 currentPoint;
                switch ((int)constellationPatternType)
                {
                    // Diagonal stars from top left to bottom right.
                    case 0:
                        Vector2 startingPoint = target.Center + new Vector2(-800f, -600f);
                        Vector2 endingPoint = target.Center + new Vector2(200f, 600f);
                        currentPoint = Vector2.Lerp(startingPoint, endingPoint, patternCompletion);
                        break;

                    // Diagonal stars from top right to bottom left.
                    case 1:
                        startingPoint = target.Center + new Vector2(200f, -600f);
                        endingPoint = target.Center + new Vector2(-800f, 600f);
                        currentPoint = Vector2.Lerp(startingPoint, endingPoint, patternCompletion);
                        break;

                    // Horizontal sinusoid.
                    case 2:
                    default:
                        float horizontalOffset = Lerp(-775f, 775f, patternCompletion);
                        float verticalOffset = Cos(patternCompletion * Pi + constellationSeed * TwoPi) * 420f;
                        currentPoint = target.Center + new Vector2(horizontalOffset, verticalOffset);
                        break;
                }

                SoundEngine.PlaySound(SoundID.Item72, currentPoint);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int starIndex = (int)(patternCompletion * totalStarsToCreate);
                    Utilities.NewProjectileBetter(currentPoint, Vector2.Zero, ModContent.ProjectileType<AstralConstellation>(), 0, 0f, -1, starIndex, npc.whoAmI);
                }
            }

            if (wrappedAttackTimer == initialAnimationTime)
            {
                SoundEngine.PlaySound(PlasmaCaster.FireSound, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 plasmaShootVelocity = npc.velocity.SafeNormalize(Vector2.UnitY) * plasmaShootSpeed;
                    Utilities.NewProjectileBetter(npc.Center + plasmaShootVelocity * 3f, plasmaShootVelocity, ModContent.ProjectileType<AstralPlasmaFireball>(), AstralPlasmaFireballDamage, 0f);
                    npc.netUpdate = true;
                }
            }

            // Make all constellations spawned by this hand prepare to explode.
            if (wrappedAttackTimer == initialAnimationTime + starCreationTime)
            {
                foreach (Projectile star in Utilities.AllProjectilesByID(ModContent.ProjectileType<AstralConstellation>()).Where(p => p.ai[1] == npc.whoAmI))
                    star.timeLeft = 50;
            }

            if (attackTimer >= (initialAnimationTime + starCreationTime + explosionTime) * constellationCount - 1f)
                SelectNextAttack(npc);
        }

        #endregion Custom Behaviors

        #region Misc AI Operations
        public static void SelectNextAttack(NPC npc)
        {
            // Select a new target.
            npc.TargetClosest();

            float lifeRatio = npc.life / (float)npc.lifeMax;
            DeusAttackType oldAttackState = (DeusAttackType)(int)npc.ai[0];
            DeusAttackType secondLastAttackState = (DeusAttackType)(int)npc.ai[3];
            DeusAttackType newAttackState;

            WeightedRandom<DeusAttackType> attackSelector = new();
            if (lifeRatio < Phase2LifeRatio)
            {
                attackSelector.Add(DeusAttackType.ConstellationExplosions, 2);
                attackSelector.Add(DeusAttackType.VortexLemniscate, 2);
                attackSelector.Add(DeusAttackType.AstralSolarSystem, 2);
            }
            if (lifeRatio < Phase3LifeRatio)
            {
                attackSelector.Add(DeusAttackType.DarkGodsOutburst, 7.5);
                attackSelector.Add(DeusAttackType.AstralGlobRush, 5);
            }
            else
            {
                attackSelector.Add(DeusAttackType.WarpCharge);
                attackSelector.Add(DeusAttackType.PlasmaAndCrystals);
                attackSelector.Add(DeusAttackType.AstralMeteorShower);
                attackSelector.Add(DeusAttackType.RubbleFromBelow);
                attackSelector.Add(DeusAttackType.InfectedStarWeave);
            }

            do
                newAttackState = attackSelector.Get();
            while (newAttackState == oldAttackState || newAttackState == secondLastAttackState);

            npc.ai[0] = (int)newAttackState;
            npc.ai[1] = 0f;
            npc.ai[3] = (int)oldAttackState;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            // Reset the don't take damage flag.
            if (npc.Infernum().ExtraAI[9] == 1f)
                npc.Infernum().ExtraAI[9] = 0f;
            npc.netUpdate = true;
        }

        public static void CreateSegments(NPC npc, int wormLength, int bodyType, int tailType)
        {
            int previousIndex = npc.whoAmI;
            for (int i = 0; i < wormLength; i++)
            {
                int nextIndex;
                if (i < wormLength - 1)
                    nextIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, bodyType, npc.whoAmI);
                else
                    nextIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, tailType, npc.whoAmI);

                Main.npc[nextIndex].realLife = npc.whoAmI;
                Main.npc[nextIndex].ai[2] = i;
                Main.npc[nextIndex].ai[1] = npc.whoAmI;
                Main.npc[nextIndex].ai[0] = previousIndex;
                if (i < wormLength - 1)
                    Main.npc[nextIndex].localAI[3] = i % 2;

                // Force sync the new segment into existence.
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, nextIndex, 0f, 0f, 0f, 0);

                previousIndex = nextIndex;
            }
        }

        public static void BringAllSegmentsToNPCPosition(NPC npc)
        {
            int segmentCount = 0;
            int bodyType = ModContent.NPCType<AstrumDeusBody>();
            int tailType = ModContent.NPCType<AstrumDeusTail>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && (Main.npc[i].type == bodyType || Main.npc[i].type == tailType))
                {
                    Main.npc[i].Center = npc.Center - (npc.rotation - PiOver2).ToRotationVector2() * segmentCount;
                    Main.npc[i].netUpdate = true;
                    segmentCount++;
                }
            }
        }

        #endregion Misc AI Operations

        #endregion AI

        #region Drawing
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/AstrumDeus/AstrumDeusHead").Value;
            if (npc.Infernum().ExtraAI[7] == 1f)
            {
                texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/AstrumDeus/AstrumDeusHeadExposed").Value;
                lightColor = Color.White;
            }
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            lightColor = Color.Lerp(lightColor, Color.White, 0.6f);
            Main.spriteBatch.Draw(texture, drawPosition, null, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, 0, 0f);
            return false;
        }
        #endregion Drawing

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Mods.InfernumMode.PetDialog.AstrumDeusTip1";
        }
        #endregion Tips
    }
}
