using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class AttackerGuardianBehaviorOverride : NPCBehaviorOverride
    {
        public enum AttackGuardianAttackState
        {
            Phase1Charges,
            Phase2Transition,
            SpinCharge,
            SpearBarrage,
            MagicFingerBolts,
            ThrowingHands
        }

        public static int TotalRemaininGuardians =>
            NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianBoss>()).ToInt() +
            NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianBoss2>()).ToInt() +
            NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianBoss3>()).ToInt();

        public const float ImmortalUntilPhase2LifeRatio = 0.75f;
        public const float Subphase2LifeRatio = 0.6f;
        public const float Subphase3LifeRatio = 0.45f;
        public const float Subphase4LifeRatio = 0.25f;

        public override int NPCOverrideType => ModContent.NPCType<ProfanedGuardianBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            CalamityGlobalNPC.doughnutBoss = npc.whoAmI;

            // Summon the defender and healer guardian.
            if (Main.netMode != NetmodeID.MultiplayerClient && npc.localAI[1] == 0f)
            {
                NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<ProfanedGuardianBoss3>());
                NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<ProfanedGuardianBoss2>());
                npc.localAI[1] = 1f;
            }

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();

            // Despawn if no valid target exists.
            npc.timeLeft = 3600;
            Player target = Main.player[npc.target];
            if (!target.active || target.dead)
            {
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y - 0.4f, -20f, 6f);
                if (npc.timeLeft < 180)
                    npc.timeLeft = 180;
                if (!npc.WithinRange(target.Center, 2000f))
                    npc.active = false;
                return false;
            }

            // Don't take damage if below the second phase threshold and other guardianas are around.
            npc.dontTakeDamage = false;
            if (npc.life < npc.lifeMax * ImmortalUntilPhase2LifeRatio && TotalRemaininGuardians >= 2f)
                npc.dontTakeDamage = true;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float shouldHandsBeInvisibleFlag = ref npc.localAI[2];

            shouldHandsBeInvisibleFlag = 0f;
            switch ((AttackGuardianAttackState)attackState)
            {
                case AttackGuardianAttackState.Phase1Charges:
                    DoBehavior_Phase1Charges(npc, target, ref attackTimer);
                    break;
                case AttackGuardianAttackState.Phase2Transition:
                    DoBehavior_Phase2Transition(npc, target, ref attackTimer);
                    break;
                case AttackGuardianAttackState.SpinCharge:
                    DoBehavior_SpinCharge(npc, target, lifeRatio, ref attackTimer, ref shouldHandsBeInvisibleFlag);
                    break;
                case AttackGuardianAttackState.SpearBarrage:
                    DoBehavior_SpearBarrage(npc, target, lifeRatio, ref attackTimer);
                    break;
                case AttackGuardianAttackState.MagicFingerBolts:
                    DoBehavior_MagicFingerBolts(npc, target, lifeRatio, ref attackTimer);
                    break;
                case AttackGuardianAttackState.ThrowingHands:
                    DoBehavior_ThrowingHands(npc, target, ref attackTimer);
                    break;
            }
            attackTimer++;

            return false;
        }

        public static void DoBehavior_Phase1Charges(NPC npc, Player target, ref float attackTimer)
        {
            npc.spriteDirection = (npc.velocity.X > 0).ToDirectionInt();
            npc.damage = npc.defDamage;

            float chargeSpeed = 18f;
            float chargeAcceleration = 1.015f;
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];

            // Enter the next phase once alone.
            if (TotalRemaininGuardians <= 1f)
            {
                npc.TargetClosest();
                npc.ai[0] = (int)AttackGuardianAttackState.Phase2Transition;
                npc.velocity = npc.velocity.ClampMagnitude(0f, 23f);
                attackTimer = 0f;
                npc.netUpdate = true;
            }

            // Line up for the charge.
            if (attackSubstate == 0f)
            {
                npc.damage = 0;

                int xOffsetDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                Vector2 destination = target.Center + Vector2.UnitX * 540f * xOffsetDirection;

                float distanceFromDestination = npc.Distance(destination);
                Vector2 linearVelocityToDestination = npc.SafeDirectionTo(destination) * MathHelper.Min(15f + target.velocity.Length() * 0.5f, distanceFromDestination);
                npc.velocity = Vector2.Lerp(linearVelocityToDestination, (destination - npc.Center) / 15f, Utils.GetLerpValue(180f, 420f, distanceFromDestination, true));

                // Prepare to charge.
                if (npc.WithinRange(destination, 12f + target.velocity.Length() * 0.5f))
                {
                    SoundEngine.PlaySound(SoundID.Item45, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        npc.velocity = npc.SafeDirectionTo(target.Center + target.velocity * 3.5f) * chargeSpeed;

                        // Release a burst of spears outward.
                        int spearBurstCount = 3;
                        float spearBurstSpread = MathHelper.ToRadians(21f);
                        if (TotalRemaininGuardians <= 2)
                        {
                            spearBurstCount += 4;
                            spearBurstSpread += MathHelper.ToRadians(10f);
                        }

                        for (int i = 0; i < spearBurstCount; i++)
                        {
                            float offsetAngle = MathHelper.Lerp(-spearBurstSpread, spearBurstSpread, i / (float)spearBurstCount);
                            Vector2 shootVelocity = npc.SafeDirectionTo(target.Center + target.velocity.Y * new Vector2(8f, 36f)).RotatedBy(offsetAngle) * 9f;
                            Utilities.NewProjectileBetter(npc.Center + shootVelocity * 2f, shootVelocity, ModContent.ProjectileType<ProfanedSpear>(), 225, 0f);
                        }

                        attackTimer = 0f;
                        attackSubstate = 1f;
                        npc.netUpdate = true;
                    }
                }
            }

            // Charge.
            if (attackSubstate == 1f)
            {
                npc.velocity *= chargeAcceleration;
                if (attackTimer >= 50f)
                {
                    attackSubstate = 0f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_Phase2Transition(NPC npc, Player target, ref float attackTimer)
        {
            float phase2TransitionTime = 180f;
            if (attackTimer < phase2TransitionTime)
            {
                npc.velocity *= 0.9f;

                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 45 == 0)
                {
                    float shootSpeed = BossRushEvent.BossRushActive ? 17f : 12f;
                    for (int i = 0; i < 12; i++)
                    {
                        Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.TwoPi * i / 11f) * shootSpeed;
                        Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<ProfanedSpear>(), 230, 0f);
                    }
                }
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == phase2TransitionTime - 45)
                {
                    NPC.NewNPC((int)npc.Center.X - 160, (int)npc.Center.Y, ModContent.NPCType<EtherealHand>(), 0, -1);
                    NPC.NewNPC((int)npc.Center.X + 160, (int)npc.Center.Y, ModContent.NPCType<EtherealHand>(), 0, 1);
                }
                return;
            }

            npc.TargetClosest();
            npc.ai[0] = (int)AttackGuardianAttackState.SpinCharge;
            attackTimer = 0f;
            npc.netUpdate = true;
        }

        public static void DoBehavior_SpinCharge(NPC npc, Player target, float lifeRatio, ref float attackTimer, ref float shouldHandsBeInvisibleFlag)
        {
            ref float arcDirection = ref npc.ai[2];

            shouldHandsBeInvisibleFlag = (attackTimer > 45f).ToInt();

            // Fade out.
            if (attackTimer <= 30f)
                npc.velocity *= 0.96f;

            // Reel back.
            if (attackTimer == 30f)
            {
                npc.Center = target.Center + (MathHelper.PiOver2 * Main.rand.Next(4)).ToRotationVector2() * 720f;
                npc.velocity = -npc.SafeDirectionTo(target.Center);
                npc.rotation = npc.AngleTo(target.Center);
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                if (npc.spriteDirection == -1)
                    npc.rotation += MathHelper.Pi;

                npc.netUpdate = true;
            }

            // Move back and re-appear.
            if (attackTimer > 30f && attackTimer < 75f)
            {
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(1f, 6f, Utils.GetLerpValue(30f, 75, attackTimer, true));
                npc.alpha = Utils.Clamp(npc.alpha - 15, 0, 255);
            }

            // Charge and fire a spear.
            if (attackTimer == 75f)
            {
                arcDirection = (Math.Cos(npc.AngleTo(target.Center)) > 0).ToDirectionInt();
                npc.velocity = npc.SafeDirectionTo(target.Center) * 24f;
                if (BossRushEvent.BossRushActive)
                    npc.velocity *= 1.5f;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 spawnPosition = npc.Center - npc.velocity.SafeNormalize(Vector2.Zero) * 40f;
                    Utilities.NewProjectileBetter(spawnPosition, npc.velocity.SafeNormalize(Vector2.Zero) * 40f, ModContent.ProjectileType<ProfanedSpear>(), 210, 0f);
                    Utilities.NewProjectileBetter(spawnPosition, npc.velocity.SafeNormalize(Vector2.Zero) * 47f, ModContent.ProjectileType<ProfanedSpear>(), 200, 0f);
                }

                npc.netUpdate = true;
            }

            // Arc around a bit.
            if (attackTimer >= 75f && attackTimer < 150f)
            {
                npc.velocity = npc.velocity.RotatedBy(arcDirection * MathHelper.TwoPi / 75f);

                if (!npc.WithinRange(target.Center, 180f))
                    npc.Center += npc.SafeDirectionTo(target.Center) * (12f + target.velocity.Length() * 0.15f);

                npc.rotation = npc.velocity.ToRotation();
                if (npc.spriteDirection == -1)
                    npc.rotation += MathHelper.Pi;

                int lightReleaseTime = 21;
                int spearReleaseTime = -1;
                if (lifeRatio < Subphase2LifeRatio)
                    lightReleaseTime -= 4;
                if (lifeRatio < Subphase3LifeRatio)
                {
                    lightReleaseTime -= 4;
                    spearReleaseTime = 50;
                }
                if (lifeRatio < Subphase4LifeRatio)
                    lightReleaseTime -= 5;

                // Release crystal lights when spinning.
                if (attackTimer % lightReleaseTime == 0)
                {
                    SoundEngine.PlaySound(SoundID.DD2_KoboldIgnite, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.PiOver2 * Main.rand.NextFloatDirection()) * 9f;
                        int shot = Utilities.NewProjectileBetter(npc.Center + shootVelocity * 3f, shootVelocity, ModContent.ProjectileType<MagicCrystalShot>(), 220, 0f);
                        Main.projectile[shot].ai[1] = Main.rand.NextFloat();
                    }
                }

                if (Main.netMode != NetmodeID.MultiplayerClient && spearReleaseTime >= 1f && attackTimer % spearReleaseTime == 0f)
                {
                    for (int i = 0; i < 18; i++)
                    {
                        Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.TwoPi * i / 18f) * 19f;
                        Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<ProfanedSpear>(), 220, 0f);
                    }
                }
            }

            // Slow down and fade out again.
            if (attackTimer >= 150f)
            {
                npc.velocity *= 0.94f;
                npc.alpha = Utils.Clamp(npc.alpha + 30, 0, 255);
            }

            // Prepare for the next attack.
            if (attackTimer >= 180f)
            {
                attackTimer = 0f;
                npc.Center = target.Center - Vector2.UnitY * 500f;

                npc.ai[0] = (int)AttackGuardianAttackState.SpearBarrage;
                arcDirection = 0f;

                npc.TargetClosest();
                npc.alpha = 0;
                npc.rotation = 0f;
                npc.velocity = Vector2.Zero;
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_SpearBarrage(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            int teleportDelay = 30;
            int reelbackTime = 30;
            int chargeTime = 50;
            int slowdownTime = 25;
            int frontSpearCount = 5;
            int spearBurstCount = 8;
            int spearBurstShootRate = 22;
            int totalCharges = 4;
            float spearBurstSpread = MathHelper.ToRadians(25f);
            float spearBurstSpeed = 14f;
            float chargeSpeed = MathHelper.Lerp(19f, 23f, Utils.GetLerpValue(ImmortalUntilPhase2LifeRatio, 0f, lifeRatio, true));
            float chargeAcceleration = 1.018f;

            if (lifeRatio < Subphase2LifeRatio)
            {
                reelbackTime -= 5;
                spearBurstCount += 2;
            }
            if (lifeRatio < Subphase3LifeRatio)
            {
                totalCharges--;
                spearBurstCount += 2;
            }
            if (lifeRatio < Subphase4LifeRatio)
            {
                spearBurstCount++;
                spearBurstSpeed *= 1.25f;
            }

            ref float chargeCounter = ref npc.ai[2];

            // Fade out and slow down.
            if (attackTimer <= teleportDelay)
            {
                npc.Opacity = 1f - attackTimer / teleportDelay;
                npc.velocity *= 0.92f;
            }

            // Teleport above the player in a burst of fire.
            if (attackTimer == teleportDelay)
            {
                // Play the fire sound.
                SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/SilvaDispel"), target.Center);
                SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/ProvidenceHolyBlastShoot"), target.Center);

                // And create the fire dust visuals.
                for (int i = 0; i < 75; i++)
                {
                    bool fireIsHoly = Main.rand.NextBool();
                    Dust fire = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(80f, 80f), fireIsHoly ? 244 : 6);
                    fire.velocity = -Vector2.UnitY.RotatedByRandom(0.71f) * Main.rand.NextFloat(3f, 8f) + Main.rand.NextVector2Circular(3f, 3f);
                    fire.scale = Main.rand.NextFloat(1.25f, 1.6f);
                    fire.noGravity = true;

                    if (fireIsHoly)
                    {
                        fire.velocity.Y -= 3f;
                        fire.scale *= 1.3f;
                        fire.fadeIn = 0.3f;
                    }
                }

                npc.Opacity = 1f;
                npc.Center = target.Center + Vector2.UnitX * Main.rand.NextBool().ToDirectionInt() * 960f;
                npc.direction = (target.Center.X > npc.Center.X).ToDirectionInt();
                npc.spriteDirection = npc.direction;
                npc.velocity = Vector2.Zero;
                npc.netUpdate = true;
            }

            // Reel back in anticipation of a charge.
            if (attackTimer > teleportDelay && attackTimer < teleportDelay + reelbackTime)
            {
                float reelbackInterpolant = Utils.GetLerpValue(teleportDelay, teleportDelay + reelbackTime, attackTimer, true);
                float reelbackSpeed = Utils.GetLerpValue(0f, 0.75f, reelbackInterpolant, true) * Utils.GetLerpValue(1f, 0.75f, reelbackInterpolant, true) * 12f;
                npc.velocity = Vector2.UnitX * npc.direction * reelbackSpeed;
            }

            // Charge and release spears.
            if (attackTimer == teleportDelay + reelbackTime)
            {
                SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/ProvidenceHolyBlastImpact"), target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < frontSpearCount; i++)
                    {
                        float offsetAngle = MathHelper.Lerp(-spearBurstSpread, spearBurstSpread, i / (float)(frontSpearCount - 1f));
                        Vector2 shootVelocity = npc.SafeDirectionTo(target.Center + target.velocity.Y * new Vector2(8f, 36f)).RotatedBy(offsetAngle) * 9f;
                        Utilities.NewProjectileBetter(npc.Center + shootVelocity * 2f, shootVelocity, ModContent.ProjectileType<ProfanedSpear>(), 225, 0f);
                    }

                    npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                    npc.velocity = npc.SafeDirectionTo(target.Center + target.velocity * 4f) * chargeSpeed;
                    npc.velocity = (npc.velocity * new Vector2(1f, 0.5f)).SafeNormalize(Vector2.UnitY) * npc.velocity.Length();
                    npc.netUpdate = true;
                }
            }

            // Release bursts of spears after charging. This doesn't happen if the guardian is close to the target, to prevent cheap hits.
            bool slowingDown = attackTimer >= teleportDelay + reelbackTime + chargeTime;
            bool charging = attackTimer >= teleportDelay + reelbackTime && !slowingDown;
            bool readyToShootSpearBurst = charging && attackTimer % spearBurstShootRate == spearBurstShootRate - 1f;
            if (readyToShootSpearBurst && !npc.WithinRange(target.Center, 300f))
            {
                SoundEngine.PlaySound(SoundID.Item73, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < spearBurstCount; i++)
                    {
                        Vector2 shootVelocity = (MathHelper.TwoPi * i / spearBurstCount).ToRotationVector2() * Main.rand.NextFloat(0.6f, 1f) * spearBurstSpeed;
                        Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<ProfanedSpear>(), 225, 0f);
                    }
                    npc.netUpdate = true;
                }
            }

            // Accelerate while charging.
            if (charging)
                npc.velocity *= chargeAcceleration;

            // Slow down to a halt after charging.
            if (slowingDown)
                npc.velocity = npc.velocity.ClampMagnitude(0f, 24f) * 0.9f;

            // Either go to the next attack state or charge again, depending on if the charge counter has reached its limit.
            if (attackTimer > teleportDelay + reelbackTime + chargeTime + slowdownTime)
            {
                if (chargeCounter < totalCharges - 1f)
                {
                    attackTimer = teleportDelay + 1f;
                    chargeCounter++;
                }
                else
                {
                    npc.TargetClosest();
                    npc.ai[0] = (int)AttackGuardianAttackState.MagicFingerBolts;
                    attackTimer = 0f;
                    chargeCounter = 0f;
                }
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_MagicFingerBolts(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            int spearShootRate = 55;

            if (lifeRatio < Subphase2LifeRatio)
                spearShootRate -= 6;
            if (lifeRatio < Subphase3LifeRatio)
                spearShootRate -= 4;
            if (lifeRatio < Subphase4LifeRatio)
                spearShootRate -= 6;

            ref float horizontalOffset = ref npc.ai[2];

            if (horizontalOffset == 0f)
                horizontalOffset = Math.Sign((npc.Center - target.Center).X);

            Vector2 destination = target.Center + new Vector2(horizontalOffset * 450f, -300f);
            Vector2 flyVelocity = (destination - npc.Center).SafeNormalize(Vector2.UnitY) * 17f;

            // Hover in place to the top left/right of the target. Firing is handled by the hand's AI.
            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
            if (npc.Distance(destination) > 12f)
                npc.velocity = Vector2.Lerp(npc.velocity, flyVelocity, 0.14f);
            else
                npc.Center = destination;

            // Release spears periodically.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % spearShootRate == spearShootRate - 1f)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.TwoPi * i / 6f) * 9f;
                    Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<ProfanedSpear>(), 230, 0f);
                }
            }

            if (attackTimer >= 270f)
            {
                attackTimer = 0f;
                horizontalOffset = 0f;
                npc.ai[0] = (int)AttackGuardianAttackState.SpinCharge;
                if (lifeRatio < Subphase4LifeRatio)
                    npc.ai[0] = (int)AttackGuardianAttackState.ThrowingHands;

                npc.TargetClosest();
                npc.alpha = 0;
                npc.rotation = 0f;
                npc.velocity = Vector2.Zero;
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_ThrowingHands(NPC npc, Player target, ref float attackTimer)
        {
            Vector2 hoverDestination = target.Center + new Vector2(npc.spriteDirection * -125f, -10f);
            Vector2 offsetToTarget = hoverDestination - npc.Center;
            float idealMoveSpeed = MathHelper.Lerp(8f, 21f, Utils.GetLerpValue(50f, 400f, offsetToTarget.Length(), true));
            idealMoveSpeed *= Utils.GetLerpValue(0f, 45f, attackTimer, true);

            Vector2 idealVelocity = Vector2.Zero.MoveTowards(offsetToTarget, idealMoveSpeed);

            if (!npc.WithinRange(hoverDestination, 375f))
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.15f).MoveTowards(idealVelocity, 2f);
            else if (npc.velocity.Length() < 30f)
                npc.velocity *= 1.02f;

            // Release magic periodically.
            if (attackTimer % 75f == 74f)
            {
                SoundEngine.PlaySound(SoundID.DD2_KoboldIgnite, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 shootVelocity = npc.SafeDirectionTo(target.Center) * -13f;
                    Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<MagicCrystalShot>(), 230, 0f);
                }
            }

            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
            if (attackTimer > 420f)
            {
                npc.TargetClosest();
                npc.ai[0] = (int)AttackGuardianAttackState.SpinCharge;
                attackTimer = 0f;
                npc.netUpdate = true;
            }
        }
    }
}
