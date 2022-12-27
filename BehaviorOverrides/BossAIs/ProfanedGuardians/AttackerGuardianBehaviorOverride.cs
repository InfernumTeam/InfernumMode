using CalamityMod;
using CalamityMod.Events;
using CalamityMod.Items.Armor.Silva;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.Projectiles.Boss;
using InfernumMode.BehaviorOverrides.BossAIs.Providence;
using InfernumMode.OverridingSystem;
using InfernumMode.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using ProvidenceBoss = CalamityMod.NPCs.Providence.Providence;

namespace InfernumMode.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class AttackerGuardianBehaviorOverride : NPCBehaviorOverride
    {
        public enum AttackerGuardianAttackState
        {
            Phase1Charges,
            Phase2Transition,
            SpinCharge,
            SpearBarrage,
            MagicFingerBolts,
            ThrowingHands,
            DeathAnimation
        }

        public static int TotalRemaininGuardians =>
            NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianCommander>()).ToInt() +
            NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianDefender>()).ToInt() +
            NPC.AnyNPCs(ModContent.NPCType<ProfanedGuardianHealer>()).ToInt();

        public const int BrightnessWidthFactorIndex = 5;

        public const float ImmortalUntilPhase2LifeRatio = 0.75f;

        public const float Phase2LifeRatio = 0.6f;

        public const float Phase3LifeRatio = 0.45f;

        public const float Phase4LifeRatio = 0.25f;

        public override int NPCOverrideType => ModContent.NPCType<ProfanedGuardianCommander>();

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            ImmortalUntilPhase2LifeRatio,
            Phase4LifeRatio
        };

        #region AI and Behaviors
        public override bool PreAI(NPC npc)
        {
            CalamityGlobalNPC.doughnutBoss = npc.whoAmI;

            // Summon the defender and healer guardian.
            if (Main.netMode != NetmodeID.MultiplayerClient && npc.localAI[1] == 0f)
            {
                NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<ProfanedGuardianDefender>());
                NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<ProfanedGuardianHealer>());
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
                if (!npc.WithinRange(target.Center, 2000f) || target.dead)
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
            ref float attackDelay = ref npc.ai[3];
            ref float shouldHandsBeInvisibleFlag = ref npc.localAI[2];

            // Wait before attacking.
            if (attackDelay < 90f)
            {
                attackDelay++;
                npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * 6f, 0.025f);
                npc.dontTakeDamage = true;
                return false;
            }

            // Draw things 

            shouldHandsBeInvisibleFlag = 0f;
            switch ((AttackerGuardianAttackState)attackState)
            {
                case AttackerGuardianAttackState.Phase1Charges:
                    DoBehavior_Phase1Charges(npc, target, ref attackTimer);
                    break;
                case AttackerGuardianAttackState.Phase2Transition:
                    DoBehavior_Phase2Transition(npc, target, ref attackTimer);
                    break;
                case AttackerGuardianAttackState.SpinCharge:
                    DoBehavior_SpinCharge(npc, target, lifeRatio, ref attackTimer, ref shouldHandsBeInvisibleFlag);
                    break;
                case AttackerGuardianAttackState.SpearBarrage:
                    DoBehavior_SpearBarrage(npc, target, lifeRatio, ref attackTimer);
                    break;
                case AttackerGuardianAttackState.MagicFingerBolts:
                    DoBehavior_MagicFingerBolts(npc, target, lifeRatio, ref attackTimer);
                    break;
                case AttackerGuardianAttackState.ThrowingHands:
                    DoBehavior_ThrowingHands(npc, target, ref attackTimer);
                    break;
                case AttackerGuardianAttackState.DeathAnimation:
                    DoBehavior_DeathAnimation(npc, target, ref attackTimer);
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
                npc.ai[0] = (int)AttackerGuardianAttackState.Phase2Transition;
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
                Vector2 linearVelocityToDestination = npc.SafeDirectionTo(destination) * MathHelper.Min(11f + target.velocity.Length() * 0.4f, distanceFromDestination);
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
                            spearBurstSpread += MathHelper.ToRadians(15f);
                        }

                        for (int i = 0; i < spearBurstCount; i++)
                        {
                            float offsetAngle = MathHelper.Lerp(-spearBurstSpread, spearBurstSpread, i / (float)spearBurstCount);
                            Vector2 shootVelocity = npc.SafeDirectionTo(target.Center + target.velocity.Y * new Vector2(8f, 15f)).RotatedBy(offsetAngle) * 9f;
                            Utilities.NewProjectileBetter(npc.Center + shootVelocity * 2f, shootVelocity, ModContent.ProjectileType<ProfanedSpearInfernum>(), 225, 0f);
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
                        Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<ProfanedSpearInfernum>(), 230, 0f);
                    }
                }
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == phase2TransitionTime - 45)
                {
                    NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X - 160, (int)npc.Center.Y, ModContent.NPCType<EtherealHand>(), 0, -1);
                    NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X + 160, (int)npc.Center.Y, ModContent.NPCType<EtherealHand>(), 0, 1);
                }
                return;
            }

            npc.TargetClosest();
            npc.ai[0] = (int)AttackerGuardianAttackState.SpinCharge;
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
                npc.Center = target.Center + (MathHelper.PiOver2 * Main.rand.Next(4)).ToRotationVector2() * 600f;
                npc.velocity = -npc.SafeDirectionTo(target.Center);
                npc.rotation = npc.AngleTo(target.Center);
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                if (npc.spriteDirection == -1)
                    npc.rotation += MathHelper.Pi;

                npc.netUpdate = true;
            }

            // Move back and re-appear.
            if (attackTimer is > 30f and < 75f)
            {
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(1f, 6f, Utils.GetLerpValue(30f, 75f, attackTimer, true));
                npc.alpha = Utils.Clamp(npc.alpha - 15, 0, 255);
            }

            // Charge and fire a spear.
            if (attackTimer == 75f)
            {
                arcDirection = (Math.Cos(npc.AngleTo(target.Center)) > 0).ToDirectionInt();
                npc.velocity = npc.SafeDirectionTo(target.Center) * 18.75f;
                if (BossRushEvent.BossRushActive)
                    npc.velocity *= 1.5f;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 spawnPosition = npc.Center - npc.velocity.SafeNormalize(Vector2.Zero) * 40f;
                    Utilities.NewProjectileBetter(spawnPosition, npc.velocity.SafeNormalize(Vector2.Zero) * 40f, ModContent.ProjectileType<ProfanedSpearInfernum>(), 210, 0f);
                    Utilities.NewProjectileBetter(spawnPosition, npc.velocity.SafeNormalize(Vector2.Zero) * 47f, ModContent.ProjectileType<ProfanedSpearInfernum>(), 200, 0f);
                    int telegraph = Utilities.NewProjectileBetter(spawnPosition, npc.velocity.SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<CrystalTelegraphLine>(), 0, 0f);
                    if (Main.projectile.IndexInRange(telegraph))
                        Main.projectile[telegraph].ai[1] = 30f;
                }

                npc.netUpdate = true;
            }

            // Arc around a bit.
            if (attackTimer is >= 75f and < 150f)
            {
                npc.velocity = npc.velocity.RotatedBy(arcDirection * MathHelper.TwoPi / 75f);

                if (!npc.WithinRange(target.Center, 180f))
                    npc.Center += npc.SafeDirectionTo(target.Center) * (12f + target.velocity.Length() * 0.15f);

                npc.rotation = npc.velocity.ToRotation();
                if (npc.spriteDirection == -1)
                    npc.rotation += MathHelper.Pi;

                int lightReleaseTime = 21;
                int spearReleaseTime = -1;
                if (lifeRatio < Phase2LifeRatio)
                    lightReleaseTime -= 4;
                if (lifeRatio < Phase3LifeRatio)
                {
                    lightReleaseTime -= 4;
                    spearReleaseTime = 50;
                }
                if (lifeRatio < Phase4LifeRatio)
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
                        Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<ProfanedSpearInfernum>(), 220, 0f);
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

                npc.ai[0] = (int)AttackerGuardianAttackState.SpearBarrage;
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
            int reelbackTime = 38;
            int chargeTime = 50;
            int slowdownTime = 25;
            int frontSpearCount = 5;
            int spearBurstCount = 8;
            int spearBurstShootRate = 22;
            int totalCharges = 4;
            float spearBurstSpread = MathHelper.ToRadians(25f);
            float spearBurstSpeed = 5f;
            float chargeSpeed = MathHelper.Lerp(19f, 23f, Utils.GetLerpValue(ImmortalUntilPhase2LifeRatio, 0f, lifeRatio, true));
            float chargeAcceleration = 1.018f;

            if (lifeRatio < Phase2LifeRatio)
            {
                reelbackTime -= 5;
                spearBurstCount += 2;
            }
            if (lifeRatio < Phase3LifeRatio)
            {
                totalCharges--;
                spearBurstCount += 2;
            }
            if (lifeRatio < Phase4LifeRatio)
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
                SoundEngine.PlaySound(SilvaHeadSummon.DispelSound, target.Center);
                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceHolyBlastShootSound, target.Center);

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
                npc.Center = target.Center + Vector2.UnitX * Main.rand.NextBool().ToDirectionInt() * 700f;
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
                SoundEngine.PlaySound(HolyBlast.ImpactSound, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < frontSpearCount; i++)
                    {
                        float offsetAngle = MathHelper.Lerp(-spearBurstSpread, spearBurstSpread, i / (float)(frontSpearCount - 1f));
                        Vector2 shootVelocity = npc.SafeDirectionTo(target.Center + target.velocity.Y * new Vector2(8f, 36f)).RotatedBy(offsetAngle) * 9f;
                        Utilities.NewProjectileBetter(npc.Center + shootVelocity * 2f, shootVelocity, ModContent.ProjectileType<ProfanedSpearInfernum>(), 225, 0f);
                    }

                    npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                    npc.velocity = npc.SafeDirectionTo(target.Center + target.velocity * 4f) * chargeSpeed;
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
                        Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<ProfanedSpearInfernum>(), 225, 0f);
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
                    npc.ai[0] = (int)AttackerGuardianAttackState.MagicFingerBolts;
                    attackTimer = 0f;
                    chargeCounter = 0f;
                }
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_MagicFingerBolts(NPC npc, Player target, float lifeRatio, ref float attackTimer)
        {
            int spearShootRate = 55;

            if (lifeRatio < Phase2LifeRatio)
                spearShootRate -= 6;
            if (lifeRatio < Phase3LifeRatio)
                spearShootRate -= 4;
            if (lifeRatio < Phase4LifeRatio)
                spearShootRate -= 6;

            ref float horizontalOffset = ref npc.ai[2];

            if (horizontalOffset == 0f)
                horizontalOffset = Math.Sign((npc.Center - target.Center).X);

            Vector2 destination = target.Center + new Vector2(horizontalOffset * 600f, -300f);
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
                    Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.TwoPi * i / 6f) * 6f;
                    Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<ProfanedSpearInfernum>(), 230, 0f);
                }
            }

            if (attackTimer >= 270f)
            {
                attackTimer = 0f;
                horizontalOffset = 0f;
                npc.ai[0] = (int)AttackerGuardianAttackState.SpinCharge;
                if (lifeRatio < Phase4LifeRatio)
                    npc.ai[0] = (int)AttackerGuardianAttackState.ThrowingHands;

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
                npc.ai[0] = (int)AttackerGuardianAttackState.SpinCharge;
                attackTimer = 0f;
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_DeathAnimation(NPC npc, Player target, ref float attackTimer)
        {
            int widthExpandDelay = 90;
            int firstExpansionTime = 20;
            int secondExpansionDelay = 1;
            int secondExpansionTime = 132;
            ref float fadeOutFactor = ref npc.Infernum().ExtraAI[0];
            ref float brightnessWidthFactor = ref npc.Infernum().ExtraAI[BrightnessWidthFactorIndex];

            // Slow to a screeching halt.
            npc.velocity *= 0.9f;

            // Disable damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            // Close the boss bar.
            npc.Calamity().ShouldCloseHPBar = true;

            if (attackTimer == widthExpandDelay + firstExpansionTime - 10f)
                SoundEngine.PlaySound(ProvidenceBoss.HolyRaySound with { Volume = 3f, Pitch = 0.4f });

            // Determine the brightness width factor.
            float expansion1 = Utils.GetLerpValue(widthExpandDelay, widthExpandDelay + firstExpansionTime, attackTimer, true) * 0.9f;
            float expansion2 = Utils.GetLerpValue(0f, secondExpansionTime, attackTimer - widthExpandDelay - firstExpansionTime - secondExpansionDelay, true) * 3.2f;
            brightnessWidthFactor = expansion1 + expansion2;
            fadeOutFactor = Utils.GetLerpValue(0f, -25f, attackTimer - widthExpandDelay - firstExpansionTime - secondExpansionDelay - secondExpansionTime, true);

            // Fade out over time.
            npc.Opacity = Utils.GetLerpValue(3f, 1.9f, brightnessWidthFactor, true);

            // Disappear and drop loot.
            if (attackTimer >= widthExpandDelay + firstExpansionTime + secondExpansionDelay + secondExpansionTime)
            {
                npc.life = 0;
                npc.Center = target.Center;
                npc.checkDead();
                npc.active = false;
            }
        }
        #endregion AI and Behaviors

        #region Draw Effects
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            int afterimageCount = 7;
            float brightnessWidthFactor = npc.Infernum().ExtraAI[BrightnessWidthFactorIndex];
            float fadeToBlack = Utils.GetLerpValue(1.84f, 2.66f, brightnessWidthFactor, true);
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Texture2D glowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ProfanedGuardians/ProfanedGuardianCommanderGlow").Value;
            SpriteEffects direction = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Vector2 origin = npc.frame.Size() * 0.5f;

            // Draw the pillar of light behind the guardian when ready.
            if (brightnessWidthFactor > 0f)
            {
                if (!Main.dedServ)
                {
                    if (!Filters.Scene["CrystalDestructionColor"].IsActive())
                        Filters.Scene.Activate("CrystalDestructionColor");

                    Filters.Scene["CrystalDestructionColor"].GetShader().UseColor(Color.Orange.ToVector3());
                    Filters.Scene["CrystalDestructionColor"].GetShader().UseIntensity(Utils.GetLerpValue(0.96f, 1.92f, brightnessWidthFactor, true) * 0.9f);
                }

                Vector2 lightPillarPosition = npc.Center - Main.screenPosition + Vector2.UnitY * 3000f;
                for (int i = 0; i < 16; i++)
                {
                    float intensity = MathHelper.Clamp(brightnessWidthFactor * 1.1f - i / 15f, 0f, 1f);
                    Vector2 lightPillarOrigin = new(TextureAssets.MagicPixel.Value.Width / 2f, TextureAssets.MagicPixel.Value.Height);
                    Vector2 lightPillarScale = new((float)Math.Sqrt(intensity + i) * brightnessWidthFactor * 200f, 6f);
                    Color lightPillarColor = new Color(0.7f, 0.55f, 0.38f, 0f) * intensity * npc.Infernum().ExtraAI[0] * 0.4f;
                    spriteBatch.Draw(TextureAssets.MagicPixel.Value, lightPillarPosition, null, lightPillarColor, 0f, lightPillarOrigin, lightPillarScale, 0, 0f);
                }
            }

            // Draw afterimages of the commander.
            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < afterimageCount; i += 2)
                {
                    Color afterimageDrawColor = npc.GetAlpha(Color.Lerp(lightColor, Color.White, 0.5f)) * ((afterimageCount - i) / 15f);
                    Vector2 afterimageDrawPosition = npc.oldPos[i] + npc.Size * 0.5f - Main.screenPosition;
                    spriteBatch.Draw(texture, afterimageDrawPosition, npc.frame, afterimageDrawColor * (1f - fadeToBlack), npc.rotation, origin, npc.scale, direction, 0f);
                }
            }
            
            // Draw back afterimages, indicating that the guardian is fading away into ashes.
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            float radius = Utils.Remap(npc.Opacity, 1f, 0f, 0f, 55f);
            if (radius > 0.5f && npc.ai[0] == (int)AttackerGuardianAttackState.DeathAnimation)
            {
                for (int i = 0; i < 24; i++)
                {
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 24f).ToRotationVector2() * radius;
                    Color backimageColor = Color.Black;
                    backimageColor.A = (byte)MathHelper.Lerp(164f, 0f, npc.Opacity);
                    spriteBatch.Draw(texture, drawPosition + drawOffset, npc.frame, backimageColor * npc.Opacity, npc.rotation, origin, npc.scale, direction, 0f);
                }
            }

            spriteBatch.Draw(texture, drawPosition, npc.frame, Color.Lerp(npc.GetAlpha(lightColor), Color.Black * npc.Opacity, fadeToBlack), npc.rotation, origin, npc.scale, direction, 0f);
            spriteBatch.Draw(glowmask, drawPosition, npc.frame, Color.Lerp(Color.White, Color.Black, fadeToBlack) * npc.Opacity, npc.rotation, origin, npc.scale, direction, 0f);
            return false;
        }
        #endregion Draw Effects

        #region Death Effects
        public override bool CheckDead(NPC npc)
        {
            // Reset the crystal shader. This is necessary since the vanilla values are only stored once.
            Filters.Scene["CrystalDestructionColor"].GetShader().UseColor(1f, 0f, 0.75f);

            // Just die as usual if the Profaned Guardian is killed during the death animation. This is done so that Cheat Sheet and other butcher effects can kill it quickly.
            if (npc.ai[0] == (int)AttackerGuardianAttackState.DeathAnimation)
                return true;

            npc.ai[0] = (int)AttackerGuardianAttackState.DeathAnimation;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            // Get rid of the silly hands.
            int handID = ModContent.NPCType<EtherealHand>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].type == handID && Main.npc[i].active)
                {
                    Main.npc[i].active = false;
                    Main.npc[i].netUpdate = true;
                }
            }

            npc.life = npc.lifeMax;
            npc.netUpdate = true;
            return false;
        }
        #endregion Death Effects

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Stay away from those energy fields! Being too close to them will hurt you!";
            yield return n => "Going in a tight circular pattern helps with the attacker guardian's spears!";
        }
        #endregion Tips
    }
}
