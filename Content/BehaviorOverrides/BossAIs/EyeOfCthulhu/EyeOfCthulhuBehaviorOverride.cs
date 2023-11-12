using CalamityMod;
using CalamityMod.Events;
using CalamityMod.Particles;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.EyeOfCthulhu
{
    public class EyeOfCthulhuBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.EyeofCthulhu;

        #region Enumerations
        public enum EoCAttackType
        {
            HoverCharge,
            ChargingServants,
            HorizontalBloodCharge,
            TeethSpit,
            SpinDash,
            BloodShots
        }
        #endregion

        #region AI
        public static int ToothDamage => 75;

        public static int SittingBloodDamage => 80;

        public static int BloodShotDamage => 80;

        public const int GleamTime = 45;

        public const float Phase2LifeRatio = 0.8f;

        public const float Phase3LifeRatio = 0.35f;

        public const float Phase4LifeRatio = 0.15f;

        public static EoCAttackType[] Phase1AttackPattern => new EoCAttackType[]
        {
            EoCAttackType.HoverCharge,
            EoCAttackType.HoverCharge,
            EoCAttackType.ChargingServants,
            EoCAttackType.HoverCharge,
            EoCAttackType.HorizontalBloodCharge,
            EoCAttackType.HoverCharge,
            EoCAttackType.ChargingServants,
        };

        public static EoCAttackType[] Phase2AttackPattern => new EoCAttackType[]
        {
            EoCAttackType.HoverCharge,
            EoCAttackType.HoverCharge,
            EoCAttackType.ChargingServants,
            EoCAttackType.SpinDash,
            EoCAttackType.HoverCharge,
            EoCAttackType.HoverCharge,
            EoCAttackType.TeethSpit,
            EoCAttackType.HoverCharge,
            EoCAttackType.HoverCharge,
            EoCAttackType.SpinDash,
            EoCAttackType.HorizontalBloodCharge,
            EoCAttackType.HoverCharge,
            EoCAttackType.HoverCharge,
            EoCAttackType.TeethSpit,
            EoCAttackType.SpinDash,
        };

        public static EoCAttackType[] Phase3AttackPattern => new EoCAttackType[]
        {
            EoCAttackType.HoverCharge,
            EoCAttackType.HoverCharge,
            EoCAttackType.BloodShots,
            EoCAttackType.ChargingServants,
            EoCAttackType.SpinDash,
            EoCAttackType.HoverCharge,
            EoCAttackType.HoverCharge,
            EoCAttackType.BloodShots,
            EoCAttackType.HoverCharge,
            EoCAttackType.HoverCharge,
            EoCAttackType.HorizontalBloodCharge,
            EoCAttackType.BloodShots,
            EoCAttackType.HoverCharge,
            EoCAttackType.HoverCharge,
            EoCAttackType.TeethSpit,
            EoCAttackType.SpinDash,
        };

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio,
            Phase3LifeRatio,
            Phase4LifeRatio
        };

        public override bool PreAI(NPC npc)
        {
            Player target = Main.player[npc.target];
            if (target.dead || !target.active)
            {
                npc.TargetClosest();
                target = Main.player[npc.target];

                if (target.dead || !target.active)
                {
                    npc.velocity.Y -= 0.18f;
                    npc.rotation = npc.velocity.ToRotation() - PiOver2;

                    if (npc.timeLeft > 120)
                        npc.timeLeft = 120;
                    if (!npc.WithinRange(target.Center, 2300f))
                        npc.active = false;
                    return false;
                }
            }

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();

            ref float attackTimer = ref npc.ai[2];
            ref float phase2ResetTimer = ref npc.Infernum().ExtraAI[6];
            ref float gleamTimer = ref npc.localAI[0];

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool enraged = Main.dayTime && !BossRushEvent.BossRushActive;
            bool phase2 = lifeRatio < Phase2LifeRatio;
            bool phase3 = lifeRatio < Phase3LifeRatio;
            bool phase4 = lifeRatio < Phase4LifeRatio;

            // Reset damage and defense.
            npc.damage = npc.defDamage + 12;
            if (phase2)
            {
                npc.defense = 4;
                npc.damage += 28;
            }

            // Reset the afterimage draw state.
            bool drawAfterimages = false;

            // Handle the Phase 2 transition.
            if (phase2 && phase2ResetTimer < 180f)
            {
                phase2ResetTimer++;
                if (phase2ResetTimer < 120f)
                {
                    npc.Opacity = Lerp(1f, 0f, phase2ResetTimer / 120f);
                    npc.velocity *= 0.94f;
                    if (phase2ResetTimer >= 120f - GleamTime)
                        gleamTimer++;
                }
                if (phase2ResetTimer == 120f)
                {
                    npc.Center = target.Center + Main.rand.NextVector2CircularEdge(405f, 405f);
                    npc.ai[0] = 3f;
                    npc.ai[3] = 0f; // Reset the attack state index.
                    npc.netUpdate = true;
                }
                if (phase2ResetTimer > 120f)
                    npc.alpha = Utils.Clamp(npc.alpha - 25, 0, 255);
                npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(target.Center) - PiOver2, 0.2f);
                return false;
            }

            // Ensure that the gleam doesn't linger in multiplayer due to desyncs.
            if (phase2ResetTimer >= 175f)
                gleamTimer = 0f;

            switch ((EoCAttackType)(int)npc.ai[1])
            {
                case EoCAttackType.HoverCharge:
                    DoBehavior_HoverCharge(npc, target, enraged, phase2, phase4, lifeRatio, ref attackTimer);
                    break;
                case EoCAttackType.ChargingServants:
                    DoBehavior_ChargingServants(npc, target, enraged, phase2, phase4, lifeRatio, ref attackTimer);
                    break;
                case EoCAttackType.HorizontalBloodCharge:
                    DoBehavior_HorizontalBloodCharge(npc, target, enraged, phase2, phase4, ref attackTimer, ref drawAfterimages);
                    break;
                case EoCAttackType.TeethSpit:
                    DoBehavior_TeethSpit(npc, target, enraged, phase3, phase4, ref attackTimer);
                    break;
                case EoCAttackType.SpinDash:
                    DoBehavior_SpinDash(npc, target, enraged, phase4, ref attackTimer, ref drawAfterimages);
                    break;
                case EoCAttackType.BloodShots:
                    DoBehavior_BloodShots(npc, target, enraged, phase4, ref attackTimer);
                    break;
            }

            // Store whether to use afterimages, accessed in PreDraw().
            npc.Infernum().ExtraAI[5] = drawAfterimages.ToInt();
            attackTimer++;
            return false;
        }

        public static void DoBehavior_HoverCharge(NPC npc, Player target, bool enraged, bool phase2, bool phase4, float lifeRatio, ref float attackTimer)
        {
            int hoverTime = 60;
            int chargeTime = 45;
            float chargeSpeed = Lerp(10f, 13.33f, 1f - lifeRatio);
            if (phase2)
            {
                chargeSpeed += 1.5f;
                chargeTime -= 15;
            }
            if (phase4)
            {
                chargeSpeed += 1.6f;
                chargeTime -= 8;
            }

            float hoverAcceleration = Lerp(0.1f, 0.25f, 1f - lifeRatio);
            float hoverSpeed = Lerp(8.5f, 17f, 1f - lifeRatio);

            if (enraged)
            {
                chargeSpeed *= 1.65f;
                hoverAcceleration *= 1.8f;
                hoverSpeed *= 1.45f;
            }
            if (BossRushEvent.BossRushActive)
            {
                chargeSpeed *= 3f;
                hoverAcceleration *= 2.25f;
                hoverSpeed *= 1.75f;
            }

            if (attackTimer < hoverTime)
            {
                Vector2 destination = target.Center - Vector2.UnitY * 185f;
                npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * hoverSpeed, hoverAcceleration);
                npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(target.Center) - PiOver2, 0.2f);
            }
            else if (attackTimer == hoverTime)
            {
                npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                npc.rotation = npc.velocity.ToRotation() - PiOver2;

                // Normal boss roar.
                SoundEngine.PlaySound(SoundID.Roar, npc.Center);
            }
            else if (attackTimer >= hoverTime + chargeTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_ChargingServants(NPC npc, Player target, bool enraged, bool phase2, bool phase4, float lifeRatio, ref float attackTimer)
        {
            npc.damage = 0;

            int servantSummonDelay = 60;
            int servantsToSummon = 6;
            int servantSummonTime = 85;
            float servantSpeed = 3.6f;
            ref float glowInterpolant = ref npc.Infernum().ExtraAI[0];

            if (phase2)
            {
                servantSummonDelay -= 7;
                servantsToSummon += 3;
                servantSummonTime -= 32;
            }
            if (phase4)
            {
                servantSummonDelay -= 6;
                servantSummonTime -= 12;
            }
            if (enraged)
            {
                servantSummonDelay -= 10;
                servantsToSummon += 4;
                servantSpeed *= 1.6f;
            }

            int servantSpawnRate = servantSummonTime / servantsToSummon;

            float hoverAcceleration = Lerp(0.15f, 0.35f, 1f - lifeRatio);
            float hoverSpeed = Lerp(14f, 18f, 1f - lifeRatio);
            if (attackTimer < servantSummonDelay)
            {
                Vector2 destination = target.Center - Vector2.UnitY * 275f;
                npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * hoverSpeed, hoverAcceleration);

                glowInterpolant = CalamityUtils.Convert01To010(attackTimer / servantSummonDelay);
            }
            else
            {
                if ((attackTimer - servantSummonDelay) % servantSpawnRate == servantSpawnRate - 1)
                {
                    Vector2 spawnPosition = npc.Center + Main.rand.NextVector2CircularEdge(120f, 120f);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int eye = NPC.NewNPC(npc.GetSource_FromAI(), (int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<ExplodingServant>());
                        Main.npc[eye].target = npc.target;
                        Main.npc[eye].velocity = Main.npc[eye].SafeDirectionTo(target.Center) * servantSpeed;
                        Main.npc[eye].netUpdate = true;
                    }

                    if (!Main.dedServ)
                    {
                        for (int i = 0; i < 20; i++)
                        {
                            float angle = TwoPi * i / 20f;
                            Dust magicBlood = Dust.NewDustPerfect(spawnPosition + angle.ToRotationVector2() * 4f, 261);
                            magicBlood.color = Color.IndianRed;
                            magicBlood.velocity = angle.ToRotationVector2() * 5f;
                            magicBlood.noGravity = true;
                        }
                    }
                }

                glowInterpolant = 0f;

                Vector2 destination = target.Center - Vector2.UnitY * 300f;
                if (npc.WithinRange(destination, 400f))
                    npc.velocity *= 0.93f;
                else
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * hoverSpeed, hoverAcceleration);
            }

            npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(target.Center) - PiOver2, 0.2f);

            if (attackTimer >= servantSummonDelay + servantSummonTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_HorizontalBloodCharge(NPC npc, Player target, bool enraged, bool phase2, bool phase4, ref float attackTimer, ref bool drawAfterimages)
        {
            int bloodBallReleaseRate = 13;
            int chargeTime = 75;
            if (phase2)
            {
                bloodBallReleaseRate -= 6;
                chargeTime -= 30;
            }
            if (phase4)
            {
                bloodBallReleaseRate -= 2;
                chargeTime -= 8;
            }

            if (enraged)
                bloodBallReleaseRate = (int)(bloodBallReleaseRate * 0.6f);

            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float chargeDirection = ref npc.Infernum().ExtraAI[1];

            if (chargeDirection == 0f)
                chargeDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

            // Redirect.
            if (attackSubstate == 0f)
            {
                // Don't do damage while redirecting.
                npc.damage = 0;

                float redirectSpeed = attackTimer / 15f + 14f;
                Vector2 destination = target.Center + new Vector2(-chargeDirection * 720f, -300f);
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(destination) * redirectSpeed, 0.06f);
                npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(target.Center) - PiOver2, 0.2f);
                if (npc.WithinRange(destination, 32f))
                {
                    attackSubstate = 1f;
                    npc.velocity = npc.SafeDirectionTo(target.Center - Vector2.UnitY * 300f) * 23f;
                    if (BossRushEvent.BossRushActive)
                        npc.velocity *= 1.7f;

                    npc.netUpdate = true;
                    attackTimer = 0f;

                    // High pitched boss roar.
                    SoundEngine.PlaySound(SoundID.ForceRoarPitched, npc.Center);
                }
            }

            // And shoot blood spit/balls.
            if (attackSubstate == 1f)
            {
                if (attackTimer == 10f)
                    npc.netUpdate = true;

                npc.rotation = npc.velocity.ToRotation() - PiOver2;

                // Use afterimages when firing.
                drawAfterimages = true;

                bool closeToPlayer = Math.Abs(npc.Center.X - target.Center.X) <= 300f + target.velocity.X * 6f;
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % bloodBallReleaseRate == bloodBallReleaseRate - 1 && !closeToPlayer)
                {
                    Vector2 spawnPosition = npc.Center + npc.velocity.SafeNormalize(Vector2.Zero) * 72f;
                    Vector2 shootVelocity = npc.velocity;
                    shootVelocity.X *= Main.rand.NextFloat(0.35f, 0.65f);
                    for (int j = 0; j < 3; j++)
                        CreateBloodParticles(npc, Color.Red * 0.8f);
                    Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ModContent.ProjectileType<SittingBlood>(), SittingBloodDamage, 0f);
                }

                if (attackTimer >= chargeTime || Math.Abs(npc.Center.X - target.Center.X) > 920f)
                {
                    npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 10f, 0.35f);
                    SelectNextAttack(npc);
                }
            }
        }

        public static void DoBehavior_TeethSpit(NPC npc, Player target, bool enraged, bool phase3, bool phase4, ref float attackTimer)
        {
            int teethPerShot = 4;
            int totalTeethBursts = 4;
            float teethRadialSpread = 1.21f;
            float teethSpeed = 25.6f;

            if (phase3)
            {
                teethPerShot += 3;
                totalTeethBursts += 2;
                teethRadialSpread += 0.21f;
            }
            if (phase4)
            {
                teethPerShot += 2;
                totalTeethBursts--;
            }
            if (enraged)
            {
                teethPerShot += 4;
                totalTeethBursts += 2;
                teethSpeed *= 1.24f;
            }

            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float teethBurstCounter = ref npc.Infernum().ExtraAI[1];
            ref float teethBurstDelay = ref npc.Infernum().ExtraAI[2];

            // Redirect.
            if (attackSubstate == 0f)
            {
                // Don't do damage while redirecting.
                npc.damage = 0;

                float redirectSpeed = attackTimer / 24f + 14f;
                Vector2 destination = target.Center - Vector2.UnitY * 265f;
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(destination) * redirectSpeed, 0.06f);
                npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(target.Center) - PiOver2, 0.2f);
                if (npc.WithinRange(destination, 32f))
                {
                    attackSubstate = 1f;
                    npc.netUpdate = true;

                    // High pitched boss roar.
                    SoundEngine.PlaySound(SoundID.ForceRoarPitched, npc.Center);
                }
                if (npc.WithinRange(target.Center, 115f))
                    npc.Center -= npc.SafeDirectionTo(target.Center) * 15f;
            }

            // Release teeth into the sky.
            if (attackSubstate == 1f)
            {
                float idealAngle = Lerp(-teethRadialSpread, teethRadialSpread, teethBurstCounter / totalTeethBursts);
                npc.rotation = npc.rotation.AngleTowards(idealAngle - Pi, 0.08f);
                npc.velocity *= 0.9f;

                if (teethBurstDelay <= 0f && Math.Abs(WrapAngle(idealAngle - npc.rotation - Pi)) < 0.07f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 spawnPosition = npc.Center - Vector2.UnitY * 20f;
                        for (int i = 0; i < teethPerShot; i++)
                        {
                            float offsetAngle = Lerp(-0.52f, 0.52f, i / (float)teethPerShot);
                            offsetAngle += Clamp((target.Center.X - npc.Center.X) * 0.0015f, -0.84f, 0.84f);

                            Vector2 toothShootVelocity = -Vector2.UnitY.RotatedBy(offsetAngle) * teethSpeed;
                            if (BossRushEvent.BossRushActive)
                                toothShootVelocity *= 1.6f;

                            for (int j = 0; j < 3; j++)
                                CreateBloodParticles(npc, Color.Red * 0.8f, toothShootVelocity, spawnPosition);

                            Utilities.NewProjectileBetter(spawnPosition, toothShootVelocity, ModContent.ProjectileType<EoCTooth>(), ToothDamage, 0f, 255, npc.target);
                        }
                    }
                    teethBurstDelay = 8f;
                    teethBurstCounter++;
                    npc.netUpdate = true;
                }
                if (teethBurstDelay > 0f)
                    teethBurstDelay--;
            }
            if (teethBurstCounter >= totalTeethBursts)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_SpinDash(NPC npc, Player target, bool enraged, bool phase4, ref float attackTimer, ref bool drawAfterimages)
        {
            int spinCycles = 1;
            int spinTime = 75;
            int chargeDelay = 25;
            int chargeChainCount = 3;
            float chargeTime = 60;
            float chargeSpeed = 18f;
            float chargeAcceleration = 1.006f;
            float spinRadius = 345f;
            bool phase3 = npc.life / (float)npc.lifeMax <= Phase3LifeRatio;

            if (phase4)
            {
                spinTime -= 20;
                chargeDelay -= 5;
                chargeTime -= 8;
                chargeSpeed += 2f;
                spinRadius -= 10f;
            }

            if (enraged)
            {
                chargeTime -= 12;
                chargeSpeed *= 1.35f;
                chargeAcceleration *= 1.008f;
            }

            if (BossRushEvent.BossRushActive)
            {
                chargeChainCount = 2;
                chargeSpeed *= 2.25f;
            }

            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float spinAngle = ref npc.Infernum().ExtraAI[1];
            ref float redirectSpeed = ref npc.Infernum().ExtraAI[2];
            ref float chainChargeCounter = ref npc.Infernum().ExtraAI[3];

            // Redirect.
            if (attackSubstate == 0f)
            {
                // Don't do damage while redirecting.
                npc.damage = 0;

                if (spinAngle == 0f)
                {
                    spinAngle = Main.rand.NextFloat(TwoPi);
                    redirectSpeed = 13f;
                    npc.netUpdate = true;
                }

                if (redirectSpeed < 30f)
                    redirectSpeed *= 1.015f;

                float inertia = Utils.Remap(attackTimer, 0f, 42f, 36f, 5f);
                Vector2 destination = target.Center + spinAngle.ToRotationVector2() * spinRadius;
                npc.velocity = (npc.velocity * (inertia - 1f) + npc.SafeDirectionTo(destination) * redirectSpeed) / inertia;
                npc.rotation = npc.velocity.ToRotation() - PiOver2;

                if (npc.WithinRange(destination, redirectSpeed + 8f))
                {
                    attackTimer = 0f;
                    attackSubstate = 1f;
                    npc.velocity = Vector2.Zero;
                    npc.Center = target.Center + spinAngle.ToRotationVector2() * spinRadius;
                    npc.netUpdate = true;

                    // High pitched boss roar.
                    SoundEngine.PlaySound(SoundID.ForceRoarPitched, npc.Center);
                }
            }

            // Spin.
            if (attackSubstate == 1f)
            {
                // Use afterimages when spinning.
                drawAfterimages = true;

                // Create blood particles in the third phase onward.
                if (phase3)
                    CreateBloodParticles(npc, Color.Red * 0.8f, -npc.velocity * 0.65f);

                spinAngle += TwoPi * spinCycles / spinTime * Utils.GetLerpValue(spinTime + 4f, spinTime - 15f, attackTimer, true);
                npc.Center = target.Center + spinAngle.ToRotationVector2() * spinRadius;
                npc.rotation = spinAngle;
                if (attackTimer >= spinTime)
                {
                    attackTimer = 0f;
                    npc.velocity = (spinAngle + PiOver2).ToRotationVector2() * 9.5f;
                    attackSubstate = 2f;
                    npc.netUpdate = true;
                }
            }

            // Slow down and aim.
            if (attackSubstate == 2f)
            {
                npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(target.Center) - PiOver2, 0.2f);
                npc.velocity *= 0.985f;
                if (attackTimer >= chargeDelay)
                {
                    attackTimer = 0f;
                    npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                    npc.rotation = npc.velocity.ToRotation() - PiOver2;
                    attackSubstate = 3f;
                    npc.netUpdate = true;

                    // Normal boss roar.
                    SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                }
            }

            // Accelerate while charging.
            if (attackSubstate == 3f)
            {
                // Accelerate.
                npc.velocity *= chargeAcceleration;

                // Draw afterimages when accelerating.
                drawAfterimages = true;

                // Create blood particles in the third phase onward.
                if (phase3)
                    CreateBloodParticles(npc, Color.Red * 0.8f, -npc.velocity * 0.85f);

                if (attackTimer >= chargeTime)
                {
                    npc.velocity *= 0.25f;
                    attackTimer = 0f;
                    attackSubstate = 4f;
                    npc.netUpdate = true;
                }
            }

            // Do a chain of multiple charges that become slower and slower.
            if (attackSubstate == 4f)
            {
                if (chainChargeCounter > chargeChainCount)
                    SelectNextAttack(npc);

                // Draw afterimages when performing chain dashes.
                drawAfterimages = true;

                // Create blood particles in the third phase onward. This happens on a timer to prevent particle clutter.
                if (phase3 && attackTimer % 3 == 0)
                    CreateBloodParticles(npc, Color.Red * 0.8f, -npc.velocity);

                if (npc.velocity.Length() < 8f)
                {
                    float idealAngle = npc.AngleTo(target.Center);
                    npc.rotation = npc.rotation.AngleTowards(idealAngle - PiOver2, Pi * 0.08f);
                    npc.velocity *= 0.985f;

                    attackTimer++;
                    if (attackTimer >= chargeDelay)
                    {
                        attackTimer = 0f;
                        chainChargeCounter++;
                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed * 1.5f;
                        npc.velocity *= Lerp(1f, 0.67f, chainChargeCounter / chargeChainCount);
                        npc.netUpdate = true;

                        // High pitched boss roar.
                        SoundEngine.PlaySound(SoundID.ForceRoarPitched, npc.Center);
                    }
                }
                else
                {
                    attackTimer = 0f;
                    npc.velocity *= 0.9785f;
                    if (BossRushEvent.BossRushActive)
                        npc.velocity *= 0.97f;
                }
            }
        }

        public static void DoBehavior_BloodShots(NPC npc, Player target, bool enraged, bool phase4, ref float attackTimer)
        {
            int shootDelay = 75;
            int shootTime = 90;
            int totalShots = 4;
            bool phase3 = npc.life / (float)npc.lifeMax <= Phase3LifeRatio;

            if (phase4)
            {
                shootDelay -= 15;
                totalShots--;
            }

            if (enraged)
            {
                shootDelay -= 10;
                totalShots += 2;
            }

            Vector2 shootDirection = (npc.rotation + PiOver2).ToRotationVector2();
            Vector2 shootCenter = npc.Center + shootDirection * 60f;
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];

            npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(target.Center) - PiOver2, 0.2f);
            if (attackTimer < shootDelay)
            {
                // Attempt to get close to the player.
                if (npc.WithinRange(target.Center, 500f))
                    npc.velocity *= 0.95f;
                else
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center) * 18f, 0.8f);
            }
            else if (attackTimer < shootDelay + shootTime)
            {
                npc.velocity *= 0.9f;

                float wrappedTimer = (attackTimer - shootDelay) % (shootTime / (float)totalShots);

                // Charge up.
                if (wrappedTimer < shootTime / (float)totalShots * 0.8f)
                {
                    Dust blood = Dust.NewDustPerfect(shootCenter + Main.rand.NextVector2Circular(8f, 8f), 267);
                    blood.velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 6f);
                    blood.color = Color.Lerp(Color.Red, Color.DarkRed, Main.rand.NextFloat());
                    blood.scale = Main.rand.NextFloat(1f, 1.4f);
                    blood.noLight = true;
                    blood.noGravity = true;
                }

                // Release blood.
                if ((int)wrappedTimer == 0)
                {
                    npc.velocity -= shootDirection * 8f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int bloodShotCount = Main.rand.Next(3, 6);
                        for (int i = 0; i < bloodShotCount; i++)
                        {
                            Vector2 velocity = shootDirection * 6.4f + Main.rand.NextVector2Square(-3f, 3f);
                            Utilities.NewProjectileBetter(shootCenter - shootDirection * 5f, velocity, ModContent.ProjectileType<BloodShot>(), BloodShotDamage, 0f);
                            int bloodParticleCount = phase3 ? 7 : 4;
                            for (int j = 0; j < bloodParticleCount; j++)
                                CreateBloodParticles(npc, Color.Red * 0.70f, velocity * 2f, shootCenter - shootDirection * 5);
                        }
                    }
                }
            }

            if (attackTimer >= shootDelay + shootTime)
                SelectNextAttack(npc);
        }

        public static void SelectNextAttack(NPC npc)
        {
            npc.ai[3]++;

            EoCAttackType[] patternToUse = Phase1AttackPattern;
            if (npc.life < npc.lifeMax * Phase2LifeRatio)
                patternToUse = Phase2AttackPattern;
            if (npc.life < npc.lifeMax * Phase3LifeRatio)
                patternToUse = Phase3AttackPattern;
            EoCAttackType nextAttackType = patternToUse[(int)(npc.ai[3] % patternToUse.Length)];

            // Select the next AI state.
            npc.ai[1] = (int)nextAttackType;

            // Reset the attack timer.
            npc.ai[2] = 0f;

            // And reset the misc ai slots.
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.TargetClosest();
            npc.netUpdate = true;
        }
        #endregion

        #region Drawing
        public static void CreateBloodParticles(NPC npc, Color color, Vector2 velocity = default, Vector2 spawnPosition = default)
        {
            // Spawn blood particles to add atmosphere.
            if (Main.rand.NextBool())
            {
                if (spawnPosition == default)
                    spawnPosition = npc.Center + Main.rand.NextVector2Circular(70, 70) + npc.velocity * 2f;

                // Allow use of custom velocity for specific movement.
                if (velocity == default)
                    velocity = -npc.velocity.SafeNormalize(Vector2.UnitX * npc.spriteDirection) * Main.rand.NextFloat(6f, 8.75f);
                Particle blood = new EoCBloodParticle(spawnPosition, velocity, 30, Main.rand.NextFloat(0.45f, 0.6f), color, npc.ai[1] == (float)EoCAttackType.BloodShots ? 3 : 22);
                GeneralParticleHandler.SpawnParticle(blood);
            }
        }
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            Texture2D eyeTexture = TextureAssets.Npc[npc.type].Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 eyeOrigin = eyeTexture.Size() / new Vector2(1f, Main.npcFrameCount[npc.type]) * 0.5f;

            // Afterimages.
            // The quantity of afterimages is changed by attacks on a case-by-case basis.
            bool drawAfterimages = npc.Infernum().ExtraAI[5] == 1;
            int afterimageCount = 7;
            float backglowInterpolant = npc.Infernum().ExtraAI[0];
            if (npc.ai[1] != (int)EoCAttackType.ChargingServants)
                backglowInterpolant = 0f;

            Color color = lightColor;
            Color afterimageEndColor = Color.White;
            if (CalamityConfig.Instance.Afterimages && drawAfterimages)
            {
                for (int i = 1; i < afterimageCount; i += 2)
                {
                    Color afterimageColor = npc.GetAlpha(Color.Lerp(color, afterimageEndColor, 0)) * ((afterimageCount - i) / 15f);
                    Vector2 afterimageDrawPosition = npc.oldPos[i] + new Vector2(npc.width, npc.height) / 2f - Main.screenPosition;
                    Main.spriteBatch.Draw(eyeTexture, afterimageDrawPosition, npc.frame, afterimageColor, npc.rotation, eyeOrigin, npc.scale, spriteEffects, 0f);
                }
            }
            if (backglowInterpolant > 0f)
            {
                for (int i = 0; i < 9; i++)
                {
                    Vector2 afterimageDrawPosition = npc.Center - Main.screenPosition + (TwoPi * i / 9f).ToRotationVector2() * backglowInterpolant * 12f;
                    Main.spriteBatch.Draw(eyeTexture, afterimageDrawPosition, npc.frame, npc.GetAlpha(Color.Crimson) with { A = 0 } * 0.3f, npc.rotation, eyeOrigin, npc.scale, spriteEffects, 0f);
                }
            }
            Main.spriteBatch.Draw(eyeTexture, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, eyeOrigin, npc.scale, spriteEffects, 0f);

            float gleamTimer = npc.localAI[0];
            Vector2 pupilPosition = npc.Center + Vector2.UnitY.RotatedBy(npc.rotation) * 74f - Main.screenPosition;
            Texture2D pupilStarTexture = InfernumTextureRegistry.Gleam.Value;
            Vector2 pupilOrigin = pupilStarTexture.Size() * 0.5f;

            Vector2 pupilScale = new Vector2(0.7f, 1.5f) * Utils.GetLerpValue(0f, 8f, gleamTimer, true) * Utils.GetLerpValue(GleamTime, GleamTime - 8f, gleamTimer, true);
            Color pupilColor = Color.Red * 0.6f * Utils.GetLerpValue(0f, 10f, gleamTimer, true) * Utils.GetLerpValue(GleamTime, GleamTime - 10f, gleamTimer, true);
            Main.spriteBatch.Draw(pupilStarTexture, pupilPosition, null, pupilColor, npc.rotation, pupilOrigin, pupilScale, SpriteEffects.None, 0f);
            pupilScale = new Vector2(0.7f, 2.7f);
            Main.spriteBatch.Draw(pupilStarTexture, pupilPosition, null, pupilColor, npc.rotation + PiOver2, pupilOrigin, pupilScale, SpriteEffects.None, 0f);
            return false;
        }
        #endregion

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Mods.InfernumMode.PetDialog.EoCTip1";
            yield return n => "Mods.InfernumMode.PetDialog.EoCTip2";

            yield return n =>
            {
                if (TipsManager.ShouldUseJokeText)
                    return "Mods.InfernumMode.PetDialog.EoCJokeTip1";
                return string.Empty;
            };
            yield return n =>
            {
                if (TipsManager.ShouldUseJokeText)
                    return "Mods.InfernumMode.PetDialog.EoCJokeTip2";
                return string.Empty;
            };
        }
        #endregion Tips
    }
}
