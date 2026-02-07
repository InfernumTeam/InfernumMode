using System;
using System.Collections.Generic;
using CalamityMod.Events;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.Sounds;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.ScreenEffects;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Twins;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Destroyer
{
    public class DestroyerHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.TheDestroyer;

        #region Enumerations
        public enum DestroyerAttackType
        {
            RegularCharge,
            UpwardBombLunge,
            LaserWalls,
            ProbeBombing,
            SuperchargedProbeBombing,
            DiveBombing,
            EnergyBlasts,
            LaserSpin
        }
        #endregion

        #region AI

        public static readonly DestroyerAttackType[] Phase1AttackPattern =
        [
            DestroyerAttackType.RegularCharge,
        ];

        public static readonly DestroyerAttackType[] Phase2AttackPattern =
        [
            DestroyerAttackType.RegularCharge,
            DestroyerAttackType.LaserWalls,
            DestroyerAttackType.ProbeBombing,
            DestroyerAttackType.UpwardBombLunge,
        ];

        public static readonly DestroyerAttackType[] Phase3AttackPattern =
        [
            DestroyerAttackType.RegularCharge,
            DestroyerAttackType.UpwardBombLunge,
            DestroyerAttackType.EnergyBlasts,
            DestroyerAttackType.LaserWalls,
            DestroyerAttackType.DiveBombing,
            DestroyerAttackType.LaserWalls,
            DestroyerAttackType.EnergyBlasts,
            DestroyerAttackType.UpwardBombLunge,
            DestroyerAttackType.DiveBombing,
            DestroyerAttackType.RegularCharge,
        ];

        public static readonly DestroyerAttackType[] Phase4AttackPattern =
        [
            DestroyerAttackType.RegularCharge,
            DestroyerAttackType.UpwardBombLunge,
            DestroyerAttackType.EnergyBlasts,
            DestroyerAttackType.LaserSpin,
            DestroyerAttackType.DiveBombing,
            DestroyerAttackType.LaserSpin,
            DestroyerAttackType.EnergyBlasts,
            DestroyerAttackType.UpwardBombLunge,
            DestroyerAttackType.DiveBombing,
            DestroyerAttackType.RegularCharge,
        ];

        public const int EnergySparkDamage = 124;

        public const int PierceLaserbeamDamage = 124;

        public const int EnergyBombDamage = 140;

        public const int BodySegmentCount = 60;

        public const float Phase2LifeRatio = 0.825f;

        public const float Phase3LifeRatio = 0.45f;

        public const float Phase4LifeRatio = 0.2f;

        public const float SegmentScale = 1.5f;

        public override float[] PhaseLifeRatioThresholds =>
        [
            Phase2LifeRatio,
            Phase3LifeRatio,
            Phase4LifeRatio
        ];

        public override bool PreAI(NPC npc)
        {
            Player target = Main.player[npc.target];
            npc.damage = npc.defDamage - 95;
            npc.dontTakeDamage = false;

            // Reset the segment scale if necessary.
            ResetScale(npc);

            // Fade in.
            npc.alpha = Utils.Clamp(npc.alpha - 20, 0, 255);

            float lifeRatio = npc.life / (float)npc.lifeMax;

            ref float attackTimer = ref npc.ai[2];
            ref float hasInitializedFlag = ref npc.ai[3];

            if (hasInitializedFlag == 0f)
            {
                npc.Center = target.Center + Vector2.UnitY * 2400f;
                SpawnDestroyerSegments(npc);
                hasInitializedFlag = 1f;
                npc.netUpdate = true;
            }

            if (!target.active || target.dead || Main.dayTime)
            {
                npc.TargetClosest(false);
                target = Main.player[npc.target];
                if (!target.active || target.dead || Main.dayTime)
                {
                    npc.velocity.X *= 0.98f;
                    npc.velocity.Y += 0.35f;

                    if (npc.timeLeft > 240)
                        npc.timeLeft = 240;

                    if (!npc.WithinRange(target.Center, 2400f))
                        npc.active = false;

                    npc.rotation = npc.velocity.ToRotation() + PiOver2;
                    return false;
                }
            }

            switch ((DestroyerAttackType)(int)npc.ai[1])
            {
                case DestroyerAttackType.RegularCharge:
                    DoAttack_RegularCharge(npc, target, lifeRatio, ref attackTimer);
                    break;
                case DestroyerAttackType.UpwardBombLunge:
                    DoAttack_UpwardBombLunge(npc, target, ref attackTimer);
                    break;
                case DestroyerAttackType.LaserWalls:
                    DoAttack_LaserWalls(npc, target, lifeRatio, ref attackTimer);
                    break;
                case DestroyerAttackType.ProbeBombing:
                    DoAttack_ProbeBombing(npc, target, lifeRatio, ref attackTimer);
                    break;
                case DestroyerAttackType.DiveBombing:
                    DoAttack_DiveBombing(npc, target, ref attackTimer);
                    break;
                case DestroyerAttackType.EnergyBlasts:
                    DoAttack_EnergyBlasts(npc, target, ref attackTimer);
                    break;
                case DestroyerAttackType.LaserSpin:
                    DoAttack_LaserSpin(npc, target, ref attackTimer);
                    break;
            }

            attackTimer++;
            return false;
        }

        public static void SpawnDestroyerSegments(NPC head)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int previousSegmentIndex = head.whoAmI;
            for (int i = 0; i < BodySegmentCount; i++)
            {
                int newSegment;
                if (i is >= 0 and < (int)(BodySegmentCount - 1f))
                    newSegment = NPC.NewNPC(head.GetSource_FromAI(), (int)head.Center.X, (int)head.Center.Y, NPCID.TheDestroyerBody, head.whoAmI);
                else
                    newSegment = NPC.NewNPC(head.GetSource_FromAI(), (int)head.Center.X, (int)head.Center.Y, NPCID.TheDestroyerTail, head.whoAmI);

                Main.npc[newSegment].realLife = head.whoAmI;

                // Set the ahead segment.
                Main.npc[newSegment].ai[1] = previousSegmentIndex;
                Main.npc[previousSegmentIndex].ai[0] = newSegment;

                // And the segment number.
                Main.npc[newSegment].Infernum().ExtraAI[0] = i;
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, newSegment, 0f, 0f, 0f, 0);

                previousSegmentIndex = newSegment;
            }
        }

        public static void DoAttack_RegularCharge(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            int hoverRedirectTime = 240;
            Vector2 hoverOffset = new Vector2((target.Center.X < npc.Center.X).ToDirectionInt(), (target.Center.Y < npc.Center.Y).ToDirectionInt()) * 485f;
            Vector2 hoverDestination = target.Center + hoverOffset;
            int chargeRedirectTime = 40;
            int chargeTime = 36;
            int chargeSlowdownTime = 12;
            int chargeCount = 2;
            float idealChargeSpeed = Lerp(27.5f, 34.75f, 1f - lifeRatio);
            ref float idealChargeVelocityX = ref npc.Infernum().ExtraAI[0];
            ref float idealChargeVelocityY = ref npc.Infernum().ExtraAI[1];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[2];

            if (BossRushEvent.BossRushActive)
                idealChargeSpeed *= 1.64f;

            // Attempt to get into position for a charge.
            if (attackTimer < hoverRedirectTime)
            {
                float idealHoverSpeed = Lerp(24.5f, 39f, attackTimer / hoverRedirectTime);
                if (BossRushEvent.BossRushActive)
                    idealHoverSpeed *= 1.45f;

                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * Lerp(npc.velocity.Length(), idealHoverSpeed, 0.08f);
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), 0.064f, true) * idealVelocity.Length();
                npc.velocity = npc.velocity.MoveTowards(idealVelocity, 2f);

                // Stop hovering if close to the hover destination
                if (npc.WithinRange(hoverDestination, 40f))
                {
                    attackTimer = hoverRedirectTime;
                    if (npc.velocity.Length() > 24f)
                        npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * 24f;

                    npc.netUpdate = true;
                }
            }

            // Determine a charge velocity to adjust to.
            if (attackTimer == hoverRedirectTime)
            {
                Vector2 idealChargeVelocity = npc.SafeDirectionTo(target.Center + target.velocity * 15f) * idealChargeSpeed;
                idealChargeVelocityX = idealChargeVelocity.X;
                idealChargeVelocityY = idealChargeVelocity.Y;
                npc.netUpdate = true;
            }

            // Move into the charge.
            if (attackTimer > hoverRedirectTime && attackTimer <= hoverRedirectTime + chargeRedirectTime)
            {
                Vector2 idealChargeVelocity = new(idealChargeVelocityX, idealChargeVelocityY);
                npc.velocity = npc.velocity.MoveTowards(idealChargeVelocity, 5f);
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(target.Center) * idealChargeVelocity.Length(), 3f);
            }

            // Slow down after charging.
            if (attackTimer > hoverRedirectTime + chargeRedirectTime + chargeTime)
                npc.velocity *= 0.9f;

            // Release probes and create an impact sound once the charge has begun.
            if (attackTimer == hoverRedirectTime + chargeRedirectTime / 2)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.DestroyerChargeImpactSound, target.Center);
                SoundEngine.PlaySound(InfernumSoundRegistry.DestroyerProbeReleaseSound, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int probeCount = 2;
                    for (int i = 0; i < probeCount; i++)
                    {
                        int probe = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, NPCID.Probe);
                        Main.npc[probe].velocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.45f) * Main.rand.NextFloat(9f, 16f);
                    }
                }
            }

            // Prepare the next charge. If all charges are done, go to the next attack.
            if (attackTimer > hoverRedirectTime + chargeRedirectTime + chargeTime + chargeSlowdownTime)
            {
                chargeCounter++;
                idealChargeVelocityX = 0f;
                idealChargeVelocityY = 0f;
                attackTimer = 0f;
                if (chargeCounter >= chargeCount)
                    SelectNewAttack(npc);

                npc.netUpdate = true;
            }

            npc.rotation = npc.velocity.ToRotation() + PiOver2;
        }

        public static void DoAttack_UpwardBombLunge(NPC npc, Player target, ref float attackTimer)
        {
            int lungeCount = 2;
            int bombReleaseDelay = 23;
            int postBombReleaseRiseTime = 15;
            int bombCount = 32;
            float upwardLungeDistance = 450f;
            ref float lungeCounter = ref npc.Infernum().ExtraAI[0];
            ref float attackSubstate = ref npc.Infernum().ExtraAI[1];

            // Fall into the ground if the first charge started off with the destroyer far in the air.
            if (lungeCounter <= 0f && attackTimer == 1f && npc.Center.Y < target.Center.Y - upwardLungeDistance - 500f)
            {
                attackSubstate = 2f;
                attackTimer = 0f;
                npc.netUpdate = true;
            }

            switch ((int)attackSubstate)
            {
                // Rise upward until sufficiently above the target.
                case 0:
                    float verticalSpeedAdditive = attackTimer * 0.05f;
                    bool readyToReleaseAcid = npc.Center.Y < target.Center.Y - upwardLungeDistance;

                    // Accelerate upward if almost above the target.
                    if (npc.Center.Y < target.Center.Y + 200f)
                    {
                        Vector2 idealVelocity = Vector2.UnitY * -(verticalSpeedAdditive + 25f);
                        npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.025f);
                    }

                    // If below the target, move upward while attempting to meet their horizontal position.
                    else
                    {
                        Vector2 idealVelocity = new(npc.SafeDirectionTo(target.Center).X * 27f, -verticalSpeedAdditive - 24f);
                        if (Distance(target.Center.X, npc.Center.X) >= 600f)
                            idealVelocity.X *= 2f;

                        npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.11f).MoveTowards(idealVelocity, 0.8f);
                    }

                    if (readyToReleaseAcid)
                    {
                        attackSubstate = 1f;
                        attackTimer = 0f;
                        npc.velocity.Y *= 0.36f;
                        npc.netUpdate = true;
                    }

                    break;

                // Release bombs into the air.
                case 1:
                    // Disable damage.
                    npc.damage = 0;

                    // Gain horizontal momentum in anticipation of the upcoming fall.
                    npc.velocity.X = Lerp(npc.velocity.X, Math.Sign(npc.velocity.X) * 12f, 0.064f);

                    // Release the bombs.
                    if (attackTimer == bombReleaseDelay)
                    {
                        SoundEngine.PlaySound(Apollo.MissileLaunchSound, target.Center);

                        target.Infernum_Camera().CurrentScreenShakePower = 8f;
                        ScreenEffectSystem.SetBlurEffect(npc.Center, 0.3f, 10);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 0; i < bombCount; i++)
                            {
                                Vector2 bombVelocity = -Vector2.UnitY.RotatedByRandom(1.23f) * Main.rand.NextFloat(13f, 24.5f) + Main.rand.NextVector2Circular(0.4f, 0.4f);
                                bombVelocity.X += target.velocity.X * 0.5f;
                                Utilities.NewProjectileBetter(npc.Center + bombVelocity, bombVelocity, ModContent.ProjectileType<DestroyerBomb>(), 0, 0f);
                            }
                        }
                    }

                    if (attackTimer >= bombReleaseDelay + postBombReleaseRiseTime)
                    {
                        attackTimer = 0f;
                        attackSubstate = 2f;
                        npc.velocity.Y += 3f;
                        npc.netUpdate = true;
                    }
                    break;

                // Fall into the ground in anticipation of the next rise. The destroyer does not do damage during this subphase.
                case 2:
                    // Disable damage.
                    npc.damage = 0;

                    npc.velocity.X *= 0.99f;
                    npc.velocity.Y = Clamp(npc.velocity.Y + 0.5f, -32f, 25f);
                    if (npc.Center.Y >= target.Center.Y + 1450f)
                    {
                        attackTimer = 0f;
                        attackSubstate = 0f;
                        lungeCounter++;
                        if (lungeCounter >= lungeCount)
                            SelectNewAttack(npc);

                        npc.velocity.Y *= 0.5f;
                        npc.netUpdate = true;
                    }
                    break;
            }

            npc.rotation = npc.velocity.ToRotation() + PiOver2;
        }

        public static void DoAttack_LaserWalls(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            Vector2 destination;
            if (attackTimer <= 90f)
            {
                // Move below the target.
                destination = target.Center + Vector2.UnitY * 400f;
                destination.X -= Math.Sign(target.Center.X - npc.Center.X) * 2300f;
                if (npc.WithinRange(destination, 23f))
                {
                    npc.velocity.X = Math.Sign(target.Center.X - npc.Center.X) * Lerp(17f, 12f, 1f - lifeRatio);
                    npc.velocity.Y = 8f;
                    attackTimer = 90f;
                }
                else
                {
                    npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(destination) * 20f, 0.05f);
                    attackTimer--;
                }

                // Destroy all probes.
                ProbeBehaviorOverride.KillAllProbes();

                if (attackTimer == 1f)
                    SoundEngine.PlaySound(InfernumSoundRegistry.DestroyerLaserTelegraphSound, target.Center);
            }
            else
            {
                npc.velocity.Y *= 0.98f;

                int shootRate = lifeRatio < Phase3LifeRatio ? 40 : 54;
                if (attackTimer > 120f && attackTimer % shootRate == shootRate - 1f)
                {
                    SoundEngine.PlaySound(CommonCalamitySounds.LaserCannonSound, target.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float offset = Main.rand.NextFloat(120f);
                        float laserSpacing = 120f;
                        Vector2 laserDirection = -Vector2.UnitY;

                        // Add some randomness to the lasers in phase 3.
                        if (lifeRatio < Phase3LifeRatio)
                        {
                            float laserAngularOffset = Main.rand.NextFloatDirection() * 0.66f;
                            laserDirection = laserDirection.RotatedBy(laserAngularOffset);
                            laserSpacing += Utils.Remap(Math.Abs(laserAngularOffset), 0.2f, 0.66f, 6f, 48f);
                        }
                        for (float dx = -1080f; dx < 1080f; dx += laserSpacing)
                        {
                            Vector2 laserSpawnPosition = target.Center + new Vector2(dx + offset, 800f);
                            Utilities.NewProjectileBetter(laserSpawnPosition, laserDirection, ModContent.ProjectileType<DestroyerPierceLaserTelegraph>(), 0, 0f, -1, npc.whoAmI);
                        }

                        npc.netUpdate = true;
                    }
                }
            }

            npc.rotation = npc.velocity.ToRotation() + PiOver2;
            if (attackTimer >= 450f)
                SelectNewAttack(npc);
        }

        public static void DoAttack_ProbeBombing(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            Vector2 destination = target.Center + (attackTimer * TwoPi / 150f).ToRotationVector2() * Lerp(1580f, 2700f, Utils.GetLerpValue(360f, 420f, attackTimer, true));
            npc.velocity = npc.SafeDirectionTo(destination) * MathF.Min(Lerp(31f, 15f, Utils.GetLerpValue(360f, 420f, attackTimer, true)), npc.Distance(destination));
            npc.Center = npc.Center.MoveTowards(destination, target.velocity.Length() * 1.2f);
            if (npc.WithinRange(destination, 30f))
                npc.rotation = npc.velocity.ToRotation() + PiOver2;
            else
                npc.rotation = npc.rotation.AngleTowards((attackTimer + 7f) * TwoPi / 150f + PiOver2, 0.15f);

            if (attackTimer % 45f == 44f)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.DestroyerProbeReleaseSound, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int probeCount = (int)Lerp(1f, 3f, 1f - lifeRatio);
                    for (int i = 0; i < probeCount; i++)
                    {
                        int probe = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, NPCID.Probe);
                        Main.npc[probe].velocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.45f) * Main.rand.NextFloat(9f, 16f);
                    }
                }
            }

            if (attackTimer >= 425f)
                SelectNewAttack(npc);
        }

        public static void DoAttack_DiveBombing(NPC npc, Player target, ref float attackTimer)
        {
            int slamCount = 3;
            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float slamCounter = ref npc.Infernum().ExtraAI[1];

            // Rise upwards above the target in antipation of a charge.
            if (attackState == 0f)
            {
                Vector2 flyDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 750f, -1600f);
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(flyDestination) * 20f, 0.08f);
                npc.Center = npc.Center.MoveTowards(flyDestination, 15f);

                if (npc.WithinRange(flyDestination, 70f))
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.DestroyerChargeImpactSound, target.Center);

                    npc.Center = flyDestination;
                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), Pi * 0.66f);
                    attackTimer = 0f;
                    attackState = 1f;
                }
            }

            // Attempt to charge into the target.
            if (attackState == 1f)
            {
                if (attackTimer < 20f)
                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center + target.velocity * 26f), 0.15f) * 1.024f;
                else if (npc.velocity.Length() < 37f)
                    npc.velocity *= 1.025f;

                if (attackTimer > 115f)
                {
                    if (slamCounter < slamCount - 1f)
                    {
                        attackTimer = 0f;
                        attackState = 0f;
                        slamCounter++;
                    }
                    else
                        SelectNewAttack(npc);
                }
            }
            npc.rotation = npc.velocity.ToRotation() + PiOver2;
        }

        public static void DoAttack_EnergyBlasts(NPC npc, Player target, ref float attackTimer)
        {
            ref float attackState = ref npc.Infernum().ExtraAI[0];

            if (attackState == 0f)
            {
                if (!npc.WithinRange(target.Center, 300f))
                {
                    float newSpeed = Lerp(npc.velocity.Length(), BossRushEvent.BossRushActive ? 30f : 23.5f, 0.15f);
                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.03f, true) * newSpeed;
                }

                if (attackTimer < 140f)
                {
                    Dust energy = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2CircularEdge(45f, 45f), DustID.TheDestroyer);
                    energy.velocity = (npc.Center - energy.position) * 0.08f;
                    energy.noGravity = true;
                    energy.scale *= 1.1f;
                }

                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 140f)
                    Utilities.NewProjectileBetter(target.Center, Vector2.Zero, ModContent.ProjectileType<TwinsEnergyExplosion>(), 0, 0f);

                if (attackTimer > 140f && attackTimer <= 185f && attackTimer % 45f == 44f)
                {
                    SoundEngine.PlaySound(PlasmaCaster.FireSound, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 shootVelocity = npc.SafeDirectionTo(target.Center) * 16f;
                        if (BossRushEvent.BossRushActive)
                            shootVelocity *= 1.56f;
                        Utilities.NewProjectileBetter(npc.Center + shootVelocity * 2f, shootVelocity, ModContent.ProjectileType<EnergyBlast2>(), EnergyBombDamage, 0f);
                    }
                }

                if (attackTimer >= 250f)
                {
                    attackState = 1f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Rise upwards above the target in antipation of a charge.
            if (attackState == 1f)
            {
                Vector2 flyDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 750f, -1600f);
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(flyDestination) * 20f, 0.08f);
                npc.Center = npc.Center.MoveTowards(flyDestination, 15f);

                if (attackTimer == 1f)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.DestroyerChargeUpSound with { Pitch = 0.25f, Volume = 1.67f }, target.Center);
                    target.Infernum_Camera().CurrentScreenShakePower = 9f;
                    ScreenEffectSystem.SetBlurEffect(npc.Center, 0.5f, 16);
                }

                if (npc.WithinRange(flyDestination, 70f))
                {
                    npc.Center = flyDestination;
                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), Pi * 0.66f);
                    attackTimer = 0f;
                    attackState = 2f;
                }
            }

            // Attempt to charge into the target.
            if (attackState == 2f && attackTimer > 115f)
                SelectNewAttack(npc);

            npc.rotation = npc.velocity.ToRotation() + PiOver2;
        }

        public static void DoAttack_HyperspeedCharge(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            float idealRotation = npc.AngleTo(target.Center);
            float acceleration = Lerp(0.03f, 0.043f, 1f - lifeRatio);
            float movementSpeed = Lerp(14f, 26f, Utils.GetLerpValue(0f, 120f, attackTimer, true));
            float slowdownInterpolant = Utils.GetLerpValue(480f, 510f, attackTimer, true);
            movementSpeed *= Lerp(1f, 0.35f, slowdownInterpolant);
            movementSpeed += Lerp(0f, 15f, Utils.GetLerpValue(420f, 3000f, npc.Distance(target.Center), true));
            movementSpeed *= BossRushEvent.BossRushActive ? 2.1f : 1f;
            acceleration *= BossRushEvent.BossRushActive ? 2f : 1f;
            acceleration *= Lerp(1f, 0.5f, slowdownInterpolant);

            if (attackTimer == 1f)
                SoundEngine.PlaySound(InfernumSoundRegistry.DestroyerChargeUpSound with { Volume = 2f }, target.Center);

            if (!npc.WithinRange(target.Center, 240f))
            {
                float newSpeed = Lerp(npc.velocity.Length(), movementSpeed, acceleration * 3.2f);
                npc.velocity = npc.velocity.RotateTowards(idealRotation, acceleration, true);
                npc.velocity = Vector2.Lerp(npc.velocity * newSpeed, npc.SafeDirectionTo(target.Center) * newSpeed, 0.03f);
            }
            else if (npc.velocity.Length() < movementSpeed * 1.6f)
                npc.velocity *= 1.04f;

            // Periodically release probes.
            if (attackTimer % 75f == 74f && slowdownInterpolant < 0.3f)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.DestroyerProbeReleaseSound, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int probe = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, NPCID.Probe);
                    Main.npc[probe].velocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.45f) * Main.rand.NextFloat(9f, 16f);
                }
            }

            // Emit smoke and fire.
            for (int i = 0; i < 10; i++)
            {
                Dust smoke = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(10f, 10f), Main.rand.NextBool(3) ? 6 : 31);
                smoke.velocity = -npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.92f) * Main.rand.NextFloat(1f, 13f);
                smoke.position += npc.velocity.SafeNormalize(Vector2.Zero) * 12f;
                smoke.noGravity = Main.rand.NextFloat() < 0.9f;
                smoke.scale *= Main.rand.NextFloat(1.3f, 1.8f);
            }

            npc.rotation = npc.velocity.ToRotation() + PiOver2;

            if (attackTimer > 750f)
                SelectNewAttack(npc);
        }

        public static void DoAttack_LaserSpin(NPC npc, Player target, ref float attackTimer)
        {
            ref float segmentToFire = ref npc.Infernum().ExtraAI[0];

            Vector2 destination = target.Center + (attackTimer * TwoPi / 150f).ToRotationVector2() * Lerp(1580f, 2700f, Utils.GetLerpValue(360f, 420f, attackTimer, true));
            npc.velocity = npc.SafeDirectionTo(destination) * MathF.Min(Lerp(31f, 15f, Utils.GetLerpValue(360f, 420f, attackTimer, true)), npc.Distance(destination));
            npc.Center = npc.Center.MoveTowards(destination, target.velocity.Length() * 1.2f);
            if (npc.WithinRange(destination, 30f))
                npc.rotation = npc.velocity.ToRotation() + PiOver2;
            else
                npc.rotation = npc.rotation.AngleTowards((attackTimer + 7f) * TwoPi / 150f + PiOver2, 0.15f);

            if (attackTimer % 8f == 0f && attackTimer > 60f)
            {
                segmentToFire = (segmentToFire + 2f) % BodySegmentCount;
                npc.netSpam = 0;
                npc.netUpdate = true;
            }

            if (attackTimer % 55f == 54f)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.DestroyerProbeReleaseSound, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        // Prevent probe spam.
                        if (NPC.CountNPCS(NPCID.Probe) >= 7)
                            break;

                        int probe = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, NPCID.Probe);
                        Main.npc[probe].velocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.45f) * Main.rand.NextFloat(9f, 16f);
                    }
                }
            }

            if (attackTimer >= 480f)
                SelectNewAttack(npc);
        }

        public static void SelectNewAttack(NPC npc)
        {
            npc.TargetClosest();

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool phase2 = lifeRatio < Phase2LifeRatio;
            bool phase3 = lifeRatio < Phase3LifeRatio;
            bool phase4 = lifeRatio < Phase4LifeRatio;

            npc.ai[3]++;

            DestroyerAttackType[] patternToUse = phase2 ? Phase2AttackPattern : Phase1AttackPattern;
            if (phase3)
                patternToUse = Phase3AttackPattern;
            if (phase4)
                patternToUse = Phase4AttackPattern;
            DestroyerAttackType nextAttackType = patternToUse[(int)(npc.ai[3] % patternToUse.Length)];
            if (nextAttackType == DestroyerAttackType.LaserSpin)
                HatGirl.SayThingWhileOwnerIsAlive(Main.player[npc.target], "Mods.InfernumMode.PetDialog.DestroyerTip1");

            // Go to the next AI state.
            npc.ai[1] = (int)nextAttackType;

            // Reset the attack timer.
            npc.ai[2] = 0f;

            // And reset the misc ai slots.
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
        }

        public static void ResetScale(NPC npc)
        {
            if (npc.scale != SegmentScale)
            {
                npc.Size /= npc.scale / SegmentScale;
                npc.scale = SegmentScale;
            }
        }

        #endregion

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Mods.InfernumMode.PetDialog.DestroyerTip2";
            yield return n => "Mods.InfernumMode.PetDialog.DestroyerTip3";
        }
        #endregion Tips
    }
}
