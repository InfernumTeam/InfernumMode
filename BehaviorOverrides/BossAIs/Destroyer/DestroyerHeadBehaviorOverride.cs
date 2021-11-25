using CalamityMod;
using CalamityMod.Events;
using CalamityMod.Projectiles.Boss;
using InfernumMode.BehaviorOverrides.BossAIs.Twins;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Destroyer
{
	public class DestroyerHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.TheDestroyer;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        #region Enumerations
        public enum DestroyerAttackType
        {
            RegularCharge,
            DivingAttack,
            LaserBarrage,
            ProbeBombing,
            SuperchargedProbes,
            DiveBombing,
            EnergyBlasts
        }
        #endregion

        #region AI

        internal static readonly DestroyerAttackType[] Phase1AttackPattern = new DestroyerAttackType[]
        {
            DestroyerAttackType.RegularCharge,
        };

        internal static readonly DestroyerAttackType[] Phase2AttackPattern = new DestroyerAttackType[]
        {
            DestroyerAttackType.RegularCharge,
            DestroyerAttackType.LaserBarrage,
            DestroyerAttackType.ProbeBombing,
            DestroyerAttackType.DivingAttack,
        };

        internal static readonly DestroyerAttackType[] Phase3AttackPattern = new DestroyerAttackType[]
        {
            DestroyerAttackType.RegularCharge,
            DestroyerAttackType.DivingAttack,
            DestroyerAttackType.EnergyBlasts,
            DestroyerAttackType.DiveBombing,
            DestroyerAttackType.SuperchargedProbes,
            DestroyerAttackType.ProbeBombing,
            DestroyerAttackType.LaserBarrage,
            DestroyerAttackType.SuperchargedProbes,
            DestroyerAttackType.EnergyBlasts,
            DestroyerAttackType.DiveBombing,
            DestroyerAttackType.RegularCharge,
            DestroyerAttackType.ProbeBombing,
        };

        internal const int BodySegmentCount = 60;

        public override bool PreAI(NPC npc)
        {
            Player target = Main.player[npc.target];
            npc.damage = npc.defDamage;
            npc.dontTakeDamage = false;

            npc.alpha = Utils.Clamp(npc.alpha - 20, 0, 255);

            float lifeRatio = npc.life / (float)npc.lifeMax;

            ref float attackTimer = ref npc.ai[2];
            ref float spawnedSegmentsFlag = ref npc.ai[3];

            if (spawnedSegmentsFlag == 0f)
            {
                SpawnDestroyerSegments(npc);
                spawnedSegmentsFlag = 1f;
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

                    npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                    return false;
                }
            }

            switch ((DestroyerAttackType)(int)npc.ai[1])
            {
                case DestroyerAttackType.RegularCharge:
                    DoAttack_RegularCharge(npc, target, lifeRatio, ref attackTimer);
                    break;
                case DestroyerAttackType.DivingAttack:
                    int diveTime = 200;
                    int ascendTime = 150;
                    float maxDiveDescendSpeed = 18f;
                    float diveAcceleration = 0.3f;
                    float maxDiveAscendSpeed = 30.5f;

                    if (BossRushEvent.BossRushActive)
                    {
                        diveAcceleration += 0.315f;
                    }

                    if (attackTimer < diveTime)
                    {
                        if (Math.Abs(npc.velocity.X) > 2f)
                            npc.velocity.X *= 0.97f;
                        if (npc.velocity.Y < maxDiveDescendSpeed)
                            npc.velocity.Y += diveAcceleration;
                    }
                    else if (attackTimer < diveTime + ascendTime)
                    {
                        Vector2 idealVelocity = Vector2.Lerp(Vector2.UnitY, -Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X), 0.3f) * -maxDiveAscendSpeed;

                        if (attackTimer < diveTime + ascendTime - 30f)
                            npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), MathHelper.Pi * 0.016f, true) * MathHelper.Lerp(npc.velocity.Length(), maxDiveAscendSpeed, 0.1f);

                        // Create shake effects for players.
                        Main.LocalPlayer.Infernum().CurrentScreenShakePower = Utils.InverseLerp(diveTime + ascendTime / 2, diveTime + ascendTime, attackTimer, true);
                        Main.LocalPlayer.Infernum().CurrentScreenShakePower = MathHelper.Lerp(Main.LocalPlayer.Infernum().CurrentScreenShakePower, 2f, 7f);
                        Main.LocalPlayer.Infernum().CurrentScreenShakePower *= Utils.InverseLerp(2000f, 1100f, npc.Distance(Main.LocalPlayer.Center), true);

                        if (attackTimer == diveTime + ascendTime - 15f)
                            Main.PlaySound(SoundID.DD2_ExplosiveTrapExplode, target.Center);

                        if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= diveTime + ascendTime - 30f)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                int type = Main.rand.NextBool(2) ? ModContent.ProjectileType<ScavengerLaser>() : ModContent.ProjectileType<DestroyerBomb>();
                                int damage = type == ModContent.ProjectileType<ScavengerLaser>() ? 110 : 0;
                                Utilities.NewProjectileBetter(npc.Center, npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.8f) * 17f, type, damage, 0f);
                            }
                        }
                    }

                    npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

                    if (attackTimer >= diveTime + ascendTime + 40f)
                        SelectNewAttack(npc);
                    break;
                case DestroyerAttackType.LaserBarrage:
                    Vector2 destination;
                    if (attackTimer <= 90f)
                    {
                        destination = target.Center + Vector2.UnitY * 400f;
                        destination.X -= Math.Sign(target.Center.X - npc.Center.X) * 2300f;
                        if (npc.WithinRange(destination, 23f))
                        {
                            npc.velocity.X = Math.Sign(target.Center.X - npc.Center.X) * MathHelper.Lerp(17f, 12f, 1f - lifeRatio);
                            npc.velocity.Y = 8f;
                            attackTimer = 90f;
                        }
                        else
                        {
                            npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(destination) * 20f, 0.05f);
                            attackTimer--;
                        }
                    }
                    else
                    {
                        npc.velocity.Y *= 0.98f;
                        if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer > 120f && attackTimer % 60f == 59f)
                        {
                            float offset = Main.rand.NextFloat(120f);
                            for (float dx = -1400f; dx < 1400f; dx += 120f)
                            {
                                Vector2 laserSpawnPosition = target.Center + new Vector2(dx + offset, 800f);
                                int telegraph = Utilities.NewProjectileBetter(laserSpawnPosition, -Vector2.UnitY, ModContent.ProjectileType<DestroyerPierceLaserTelegraph>(), 0, 0f);
                                if (Main.projectile.IndexInRange(telegraph))
                                    Main.projectile[telegraph].ai[0] = npc.whoAmI;
                            }
                        }
                    }

                    npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                    if (attackTimer >= 450f)
                        SelectNewAttack(npc);
                    break;
                case DestroyerAttackType.ProbeBombing:
                    destination = target.Center + (attackTimer * MathHelper.TwoPi / 150f).ToRotationVector2() * MathHelper.Lerp(1580f, 2700f, Utils.InverseLerp(360f, 420f, attackTimer, true));
                    npc.velocity = npc.SafeDirectionTo(destination) * MathHelper.Min(MathHelper.Lerp(31f, 15f, Utils.InverseLerp(360f, 420f, attackTimer, true)), npc.Distance(destination));
                    npc.Center = npc.Center.MoveTowards(destination, target.velocity.Length() * 1.2f);
                    if (npc.WithinRange(destination, 30f))
                        npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                    else
                        npc.rotation = npc.rotation.AngleTowards((attackTimer + 7f) * MathHelper.TwoPi / 150f + MathHelper.PiOver2, 0.15f);

                    if (attackTimer % 45f == 44f)
                    {
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PlasmaCasterFire"), target.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int probeCount = (int)MathHelper.Lerp(1f, 3f, 1f - lifeRatio);
                            for (int i = 0; i < probeCount; i++)
                            {
                                int probe = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.Probe);
                                Main.npc[probe].velocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.45f) * Main.rand.NextFloat(9f, 16f);
                            }
                        }
                    }

                    if (attackTimer >= 425f)
                        SelectNewAttack(npc);
                    break;
                case DestroyerAttackType.SuperchargedProbes:
                    destination = target.Center + (attackTimer * MathHelper.TwoPi / 150f).ToRotationVector2() * MathHelper.Lerp(1580f, 2700f, Utils.InverseLerp(360f, 420f, attackTimer, true));
                    npc.velocity = npc.SafeDirectionTo(destination) * MathHelper.Min(MathHelper.Lerp(31f, 15f, Utils.InverseLerp(360f, 420f, attackTimer, true)), npc.Distance(destination));
                    npc.Center = npc.Center.MoveTowards(destination, target.velocity.Length() * 1.2f);
                    if (npc.WithinRange(destination, 30f))
                        npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                    else
                        npc.rotation = npc.rotation.AngleTowards((attackTimer + 7f) * MathHelper.TwoPi / 150f + MathHelper.PiOver2, 0.15f);

                    if (attackTimer == 90f)
                    {
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PlasmaCasterFire"), target.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int probeCount = (int)Math.Round(MathHelper.Lerp(3f, 6f, 1f - lifeRatio));
                            for (int i = 0; i < probeCount; i++)
                            {
                                int probe = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<SuperchargedProbe>());
                                Main.npc[probe].velocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.45f) * Main.rand.NextFloat(9f, 16f);
                                Main.npc[probe].ai[3] = (i == 0f).ToInt();
                            }
                        }
                    }

                    if (attackTimer >= SuperchargedProbe.Lifetime + 90f)
                        SelectNewAttack(npc);
                    break;
                case DestroyerAttackType.DiveBombing:
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
                            npc.Center = flyDestination;
                            npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), MathHelper.Pi * 0.66f);
                            attackTimer = 0f;
                            attackState = 1f;
						}
					}

                    // Attempt to charge into the target.
                    if (attackState == 1f)
					{
                        if (attackTimer < 20f)
                        {
                            npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center + target.velocity * 20f), 0.15f) * 1.018f;
                            int type = ModContent.ProjectileType<ScavengerLaser>();
                            int damage = 120;
                            Vector2 laserVelocity = Vector2.Lerp(npc.velocity.SafeNormalize(Vector2.UnitY), -Vector2.UnitY, 0.5f);
                            laserVelocity = laserVelocity.RotatedByRandom(0.8f) * Main.rand.NextFloat(14f, 17f);
                            Utilities.NewProjectileBetter(npc.Center, laserVelocity, type, damage, 0f);
                        }
                        else if (npc.velocity.Length() < 33f)
                            npc.velocity *= 1.018f;

                        if (attackTimer > 115f)
						{
                            if (slamCounter < slamCount)
                            {
                                attackTimer = 0f;
                                attackState = 0f;
                                slamCounter++;
                            }
                            else
                                SelectNewAttack(npc);
						}
                    }
                    npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                    break;
                case DestroyerAttackType.EnergyBlasts:
                    attackState = ref npc.Infernum().ExtraAI[0];

                    if (attackState == 0f)
                    {
                        // Move away from the target.
                        if (attackTimer < 80f)
                            npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * -21f, 0.3f);
                        else
                        {
                            float newSpeed = MathHelper.Lerp(npc.velocity.Length(), BossRushEvent.BossRushActive ? 30f : 19f, 0.15f);
                            npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.03f, true) * newSpeed;

                            if (attackTimer < 140f)
                            {
                                Dust energy = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2CircularEdge(45f, 45f), 182);
                                energy.velocity = (npc.Center - energy.position) * 0.08f;
                                energy.noGravity = true;
                                energy.scale *= 1.1f;
                            }

                            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 140f)
                                Utilities.NewProjectileBetter(target.Center, Vector2.Zero, ModContent.ProjectileType<TwinsEnergyExplosion>(), 0, 0f);

                            if (attackTimer > 140f && attackTimer <= 285f && attackTimer % 45f == 44f)
                            {
                                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PlasmaCasterFire"), npc.Center);
                                if (Main.netMode != NetmodeID.MultiplayerClient)
                                {
                                    Vector2 shootVelocity = npc.SafeDirectionTo(target.Center) * 16f;
                                    if (BossRushEvent.BossRushActive)
                                        shootVelocity *= 1.56f;
                                    Utilities.NewProjectileBetter(npc.Center + shootVelocity * 2f, shootVelocity, ModContent.ProjectileType<EnergyBlast2>(), 135, 0f);
                                }
                            }
                        }

                        if (attackTimer >= 360f)
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

                        if (npc.WithinRange(flyDestination, 70f))
                        {
                            npc.Center = flyDestination;
                            npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), MathHelper.Pi * 0.66f);
                            attackTimer = 0f;
                            attackState = 2f;
                        }
                    }

                    // Attempt to charge into the target.
                    if (attackState == 2f)
                    {
                        if (attackTimer > 115f)
                            SelectNewAttack(npc);
                    }

                    npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                    break;
            }

            attackTimer++;
            return false;
        }

        internal static void SpawnDestroyerSegments(NPC head)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int previousSegmentIndex = head.whoAmI;
            for (int i = 0; i < BodySegmentCount; i++)
            {
                int newSegment;
                if (i >= 0 && i < BodySegmentCount - 1f)
                    newSegment = NPC.NewNPC((int)head.position.X + (head.width / 2), (int)head.position.Y + (head.height / 2), NPCID.TheDestroyerBody, head.whoAmI);
                else
                    newSegment = NPC.NewNPC((int)head.position.X + (head.width / 2), (int)head.position.Y + (head.height / 2), NPCID.TheDestroyerTail, head.whoAmI);

                Main.npc[newSegment].realLife = head.whoAmI;

                // Set the ahead segment.
                Main.npc[newSegment].ai[1] = previousSegmentIndex;
                Main.npc[previousSegmentIndex].ai[0] = newSegment;

                // And the segment number.
                Main.npc[newSegment].localAI[0] = i;

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
            int chargeTime = 45;
            int chargeSlowdownTime = 25;
            int chargeCount = 2;
            float idealChargeSpeed = MathHelper.Lerp(19.5f, 23f, 1f - lifeRatio);
            ref float idealChargeVelocityX = ref npc.Infernum().ExtraAI[0];
            ref float idealChargeVelocityY = ref npc.Infernum().ExtraAI[1];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[2];

            if (BossRushEvent.BossRushActive)
                idealChargeSpeed *= 1.64f;

            // Attempt to get into position for a charge.
            if (attackTimer < hoverRedirectTime)
            {
                float idealHoverSpeed = MathHelper.Lerp(20.5f, 39f, attackTimer / hoverRedirectTime);
                if (BossRushEvent.BossRushActive)
                    idealHoverSpeed *= 1.45f;

                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * MathHelper.Lerp(npc.velocity.Length(), idealHoverSpeed, 0.08f);
                npc.velocity = npc.velocity.RotateTowards(idealVelocity.ToRotation(), 0.064f, true) * idealVelocity.Length();

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
                Vector2 idealChargeVelocity = new Vector2(idealChargeVelocityX, idealChargeVelocityY);
                npc.velocity = npc.velocity.RotateTowards(idealChargeVelocity.ToRotation(), 0.08f, true) * MathHelper.Lerp(npc.velocity.Length(), idealChargeVelocity.Length(), 0.15f);
                npc.velocity = npc.velocity.MoveTowards(idealChargeVelocity, 5f);
            }

            // Slow down after charging.
            if (attackTimer > hoverRedirectTime + chargeRedirectTime + chargeTime)
                npc.velocity *= 0.95f;

            // Release lightning from behind the worm once the charge has begun.
            if (attackTimer == hoverRedirectTime + chargeRedirectTime / 2)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LargeWeaponFire"), target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int probeCount = (int)MathHelper.Lerp(1f, 3f, 1f - lifeRatio);
                    for (int i = 0; i < probeCount; i++)
                    {
                        int probe = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.Probe);
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

            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public static void SelectNewAttack(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool phase2 = lifeRatio < 0.75f;
            bool phase3 = lifeRatio < 0.4f;

            npc.ai[3]++;

            DestroyerAttackType[] patternToUse = phase2 ? Phase2AttackPattern : Phase1AttackPattern;
            if (phase3)
                patternToUse = Phase3AttackPattern;
            DestroyerAttackType nextAttackType = patternToUse[(int)(npc.ai[3] % patternToUse.Length)];

            // Going to the next AI state.
            npc.ai[1] = (int)nextAttackType;

            // Resetting the attack timer.
            npc.ai[2] = 0f;

            // And the misc ai slots.
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
        }

        #endregion
    }
}