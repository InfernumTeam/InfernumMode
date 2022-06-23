using CalamityMod;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.Sounds;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.Athena;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.ExoMechManagement;
using ArtemisLaserInfernum = InfernumMode.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo.ArtemisLaser;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
    public static partial class ExoMechComboAttackContent
    {
        public static bool UseTwinsAthenaComboAttack(NPC npc, float twinsHoverSide, ref float attackTimer, ref float frameType)
        {
            NPC initialMech = FindInitialMech();
            if (initialMech is null)
                return false;

            Player target = Main.player[initialMech.target];
            switch ((ExoMechComboAttackType)initialMech.ai[0])
            {
                case ExoMechComboAttackType.TwinsAthena_ThermoplasmaDance:
                    return DoBehavior_TwinsAthena_ThermoplasmaDance(npc, target, ref attackTimer, ref frameType);
                case ExoMechComboAttackType.TwinsAthena_ThermoplasmaChargeupBursts:
                    return DoBehavior_TwinsAthena_ThermoplasmaChargeupBursts(npc, target, ref attackTimer, ref frameType);
            }
            return false;
        }

        public static bool DoBehavior_TwinsAthena_ThermoplasmaDance(NPC npc, Player target, ref float attackTimer, ref float frame)
        {
            int redirectTime = 135;
            int spinTime = 480;
            int spinSlowdownTime = 60;
            int twinsShootRate = 60;
            int laserShootCount = 3;
            int hoverTime = 35;
            int chargeTime = 45;
            float hoverSpeed = 34f;
            float chargeSpeed = 24f;
            float spinAngularVelocity = MathHelper.ToRadians(1.8f);
            float spinSlowdownInterpolant = Utils.GetLerpValue(redirectTime + spinTime, redirectTime + spinTime - spinSlowdownTime, attackTimer, true);
            ref float twinsSpinRotation = ref npc.Infernum().ExtraAI[0];

            if (CurrentTwinsPhase >= 2)
                twinsShootRate -= 8;
            if (CurrentTwinsPhase != 4 && CurrentTwinsPhase >= 2)
            {
                twinsShootRate -= 20;
                chargeTime += 6;
                spinAngularVelocity *= 1.5f;
            }

            if (EnrageTimer > 0f)
            {
                twinsShootRate -= 21;
                spinAngularVelocity *= 2.3f;
            }

            // Inherit the attack timer from the initial mech.
            attackTimer = FindInitialMech()?.ai[1] ?? attackTimer;

            // Halt attacking if Artemis and Apollo are busy entering their second phase.
            if (ExoTwinsAreEnteringSecondPhase)
                attackTimer = 0f;

            // Artemis and Apollo spin around the player and fire projectiles inward.
            // Artemis releases a burst of lasers in a thin spread while Apollo releases plasma.
            bool isEitherExoTwin = npc.type == ModContent.NPCType<Apollo>() || npc.type == ModContent.NPCType<Artemis>();
            bool exoTwinIsShooting = attackTimer > redirectTime && attackTimer % twinsShootRate == twinsShootRate - 1f;
            Vector2 aimDirection = npc.SafeDirectionTo(target.Center);
            if (npc.type == ModContent.NPCType<Apollo>())
            {
                if (attackTimer == 1f)
                {
                    twinsSpinRotation = Main.rand.NextFloat(MathHelper.TwoPi);
                    npc.netUpdate = true;
                }

                // Begin spinning around after redirecting.
                // Artemis will inherit attributes from this.
                if (attackTimer > redirectTime)
                    twinsSpinRotation += spinAngularVelocity * spinSlowdownInterpolant;

                // Shoot fireballs.
                if (exoTwinIsShooting)
                {
                    SoundEngine.PlaySound(PlasmaCaster.FireSound, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 plasmaShootVelocity = aimDirection * 8f;
                        int plasma = Utilities.NewProjectileBetter(npc.Center + aimDirection * 70f, plasmaShootVelocity, ModContent.ProjectileType<ApolloPlasmaFireball>(), 500, 0f);
                        if (Main.projectile.IndexInRange(plasma))
                            Main.projectile[plasma].ai[0] = Main.rand.NextBool().ToDirectionInt();
                    }
                }
            }

            if (npc.type == ModContent.NPCType<Artemis>())
            {
                // Inhert the spin rotation from Apollo and use the opposite side.
                twinsSpinRotation = Main.npc[CalamityGlobalNPC.draedonExoMechTwinGreen].Infernum().ExtraAI[0] + MathHelper.Pi;

                // Shoot lasers.
                if (exoTwinIsShooting)
                {
                    SoundEngine.PlaySound(CommonCalamitySounds.LaserCannonSound, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < laserShootCount; i++)
                        {
                            float laserOffsetRotation = MathHelper.Lerp(-0.39f, 0.39f, i / (float)(laserShootCount - 1f));
                            Vector2 laserShootVelocity = aimDirection.RotatedBy(laserOffsetRotation) * 5.5f;
                            int laser = Utilities.NewProjectileBetter(npc.Center + aimDirection * 70f, laserShootVelocity, ModContent.ProjectileType<ArtemisLaserInfernum>(), 500, 0f);
                            if (Main.projectile.IndexInRange(laser))
                            {
                                Main.projectile[laser].ModProjectile<ArtemisLaserInfernum>().InitialDestination = target.Center + laserShootVelocity.SafeNormalize(Vector2.Zero) * 400f;
                                Main.projectile[laser].ai[1] = npc.whoAmI;
                                Main.projectile[laser].netUpdate = true;
                            }
                        }
                    }
                }
            }

            if (isEitherExoTwin)
            {
                // Do hover movement.
                Vector2 hoverDestination = target.Center + twinsSpinRotation.ToRotationVector2() * 810f;
                npc.Center = Vector2.Lerp(npc.Center, hoverDestination, 0.033f);
                if (attackTimer >= redirectTime)
                    npc.Center = Vector2.Lerp(npc.Center, hoverDestination, 0.08f);

                // Look at the target.
                npc.velocity = Vector2.Zero;
                npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;

                // Handle frames.
                npc.frameCounter++;
                frame = (int)Math.Round(MathHelper.Lerp(10f, 19f, (float)npc.frameCounter / 36f % 1f));
                if (attackTimer > redirectTime)
                    frame += 10f;
                if (ExoTwinsAreInSecondPhase)
                    frame += 60f;
            }

            // Athena attempts to charge above the target, releasing homing missiles.
            if (npc.type == ModContent.NPCType<AthenaNPC>())
            {
                float wrappedTime = attackTimer % (hoverTime + chargeTime);
                if (wrappedTime < hoverTime - 15f || attackTimer < redirectTime)
                {
                    Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 300f, -420f);
                    npc.Center = npc.Center.MoveTowards(hoverDestination, hoverTime * 0.3f);
                    npc.velocity = (npc.velocity * 4f + npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), hoverSpeed)) / 5f;
                }
                else if (wrappedTime < hoverTime)
                    npc.velocity *= 0.94f;

                else
                {
                    if (wrappedTime == hoverTime + 1f)
                    {
                        npc.velocity = Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X) * chargeSpeed;
                        npc.netUpdate = true;
                        SoundEngine.PlaySound(CommonCalamitySounds.ELRFireSound, target.Center);
                    }

                    // Release rockets upward.
                    if (Main.netMode != NetmodeID.MultiplayerClient && wrappedTime % 8f == 7f)
                    {
                        Vector2 rocketShootVelocity = -Vector2.UnitY.RotatedByRandom(0.49f) * Main.rand.NextFloat(12f, 16f);
                        Utilities.NewProjectileBetter(npc.Center, rocketShootVelocity, ModContent.ProjectileType<AthenaRocket>(), 500, 0f);
                    }

                    npc.velocity *= 1.01f;
                }
            }

            return attackTimer > redirectTime + spinTime;
        }

        public static bool DoBehavior_TwinsAthena_ThermoplasmaChargeupBursts(NPC npc, Player target, ref float attackTimer, ref float frame)
        {
            int redirectTime = 135;
            int attackTime = 420;
            int athenaShootRate = 24;
            int artemisShootRate = 45;
            float artemisShootSpeed = 6.5f;
            float apolloChargeSpeed = 31f;
            float wrappedAttackTimer = attackTimer % (redirectTime + attackTime);
            bool apolloIsAboutToCharge = wrappedAttackTimer >= redirectTime * 0.45f && wrappedAttackTimer < redirectTime;
            bool canShoot = wrappedAttackTimer >= redirectTime && wrappedAttackTimer < redirectTime + attackTime - 60f;

            if (CurrentTwinsPhase >= 2)
                artemisShootRate -= 8;
            if (CurrentTwinsPhase != 4 && CurrentTwinsPhase >= 2)
            {
                artemisShootRate -= 15;
                artemisShootSpeed += 2.5f;
                apolloChargeSpeed += 7.5f;
            }

            // Inherit the attack timer from the initial mech.
            attackTimer = FindInitialMech()?.ai[1] ?? attackTimer;

            // Halt attacking if Artemis and Apollo are busy entering their second phase.
            if (ExoTwinsAreEnteringSecondPhase)
                attackTimer = 0f;

            // Have Artemis hover below the target and fire lasers.
            if (npc.type == ModContent.NPCType<Artemis>())
            {
                ref float hoverOffset = ref npc.Infernum().ExtraAI[0];
                ref float shootTimer = ref npc.Infernum().ExtraAI[1];

                // Handle frames.
                npc.frameCounter++;
                frame = (int)Math.Round(MathHelper.Lerp(10f, 19f, (float)npc.frameCounter / 36f % 1f));
                if (ExoTwinsAreInSecondPhase)
                    frame += 60f;

                // Disable contact damage.
                npc.damage = 0;

                // Move to the appropriate side of the target.
                Vector2 hoverDestination = target.Center + new Vector2(hoverOffset, 500f);
                ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 30f, 84f);

                // Increment the shoot timer once ready to begin firing.
                if (canShoot)
                {
                    frame += 10f;
                    shootTimer++;
                }

                // Shoot lasers.
                if (npc.WithinRange(hoverDestination, 200f) && shootTimer >= artemisShootRate)
                {
                    SoundEngine.PlaySound(CommonCalamitySounds.LaserCannonSound, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 laserVelocity = npc.SafeDirectionTo(target.Center) * artemisShootSpeed;
                        int laser = Utilities.NewProjectileBetter(npc.Center, laserVelocity, ModContent.ProjectileType<ArtemisLaserInfernum>(), 500, 0f);
                        if (Main.projectile.IndexInRange(laser))
                        {
                            Main.projectile[laser].ModProjectile<ArtemisLaserInfernum>().InitialDestination = target.Center + laserVelocity.SafeNormalize(Vector2.UnitY) * 2400f;
                            Main.projectile[laser].ai[1] = npc.whoAmI;
                            Main.projectile[laser].netUpdate = true;
                        }
                    }

                    shootTimer = 0f;
                    hoverOffset = Main.rand.NextFloat(-300f, 300f);
                    npc.netUpdate = true;
                }

                // Decide rotation.
                npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;
            }

            // Have Apollo do loops and carpet bombs with plasma missiles.
            if (npc.type == ModContent.NPCType<Apollo>())
            {
                ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
                ref float generalAttackTimer = ref npc.Infernum().ExtraAI[1];

                // Reset contact damage.
                npc.damage = 0;

                // Reset the flash effect.
                npc.ModNPC<Apollo>().ChargeComboFlash = 0f;

                // Simply hover in place at first.
                if (!canShoot && attackSubstate == 0f)
                {
                    Vector2 hoverDestination = target.Center + new Vector2(600f, -400f);
                    ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 30f, 75f);

                    // Decide rotation.
                    npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;
                }
                else
                {
                    switch ((int)attackSubstate)
                    {
                        // Rise upward.
                        case 0:
                        default:
                            Vector2 flyDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 670f, -400f);
                            Vector2 idealVelocity = npc.SafeDirectionTo(flyDestination) * 24f;
                            npc.velocity = (npc.velocity * 29f + idealVelocity) / 30f;
                            npc.velocity = npc.velocity.MoveTowards(idealVelocity, 1.5f);

                            // Decide rotation.
                            npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;

                            if (npc.WithinRange(flyDestination, 40f) || generalAttackTimer > 150f)
                            {
                                attackSubstate = 1f;
                                npc.velocity *= 0.65f;
                                npc.netUpdate = true;
                            }
                            break;

                        // Slow down and look at the target.
                        case 1:
                            npc.velocity *= 0.95f;
                            npc.velocity = npc.velocity.MoveTowards(Vector2.Zero, 0.8f);
                            npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(target.Center) + MathHelper.PiOver2, 0.4f);

                            // Charge once sufficiently slowed down.
                            if (npc.velocity.Length() < 1.25f)
                            {
                                SoundEngine.PlaySound(CommonCalamitySounds.ELRFireSound, target.Center);
                                for (int i = 0; i < 36; i++)
                                {
                                    Dust laser = Dust.NewDustPerfect(npc.Center, 182);
                                    laser.velocity = (MathHelper.TwoPi * i / 36f).ToRotationVector2() * 6f;
                                    laser.scale = 1.1f;
                                    laser.noGravity = true;
                                }

                                attackSubstate = 2f;
                                generalAttackTimer = 0f;
                                npc.velocity = npc.SafeDirectionTo(target.Center) * apolloChargeSpeed;
                                npc.netUpdate = true;
                            }
                            break;

                        // Charge, swoop, and do a loop midair while releasing rockets that home in on the target.
                        case 2:
                            // Slow down a bit.
                            if (npc.velocity.Length() > apolloChargeSpeed * 0.6f)
                                npc.velocity *= 0.98f;

                            if (generalAttackTimer < 50f)
                            {
                                float angularTurnSpeed = MathHelper.Pi / 300f;
                                idealVelocity = npc.SafeDirectionTo(target.Center);
                                Vector2 leftVelocity = npc.velocity.RotatedBy(-angularTurnSpeed);
                                Vector2 rightVelocity = npc.velocity.RotatedBy(angularTurnSpeed);
                                if (leftVelocity.AngleBetween(idealVelocity) < rightVelocity.AngleBetween(idealVelocity))
                                    npc.velocity = leftVelocity;
                                else
                                    npc.velocity = rightVelocity;
                            }
                            else
                            {
                                // Once the attack has gone on for half a second rotate the velocity 4 degrees every frame while rising upward.
                                float adjustedTimer = generalAttackTimer - 50f;
                                if (adjustedTimer > 30f)
                                    npc.velocity = npc.velocity.RotatedBy(MathHelper.TwoPi / 90f);
                                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y - 2f, -42f, 42f);

                                // Release rockets.
                                if (adjustedTimer % 15f == 14f && !npc.WithinRange(target.Center, 250f))
                                {
                                    SoundEngine.PlaySound(SoundID.Item36, target.Center);

                                    if (Main.netMode != NetmodeID.MultiplayerClient)
                                    {
                                        int type = ModContent.ProjectileType<ApolloRocketInfernum>();
                                        Vector2 rocketVelocity = npc.velocity.SafeNormalize(Vector2.UnitY) * 12.5f;
                                        Vector2 rocketSpawnPosition = npc.Center + npc.velocity.SafeNormalize(Vector2.Zero) * 70f;
                                        int rocket = Utilities.NewProjectileBetter(rocketSpawnPosition, rocketVelocity, type, 500, 0f, Main.myPlayer, 0f, target.Center.Y);
                                        if (Main.projectile.IndexInRange(rocket))
                                            Main.projectile[rocket].tileCollide = false;
                                    }
                                }

                                if (adjustedTimer > 90f)
                                {
                                    attackSubstate = 0f;
                                    generalAttackTimer = 0f;
                                    npc.velocity *= 0.4f;
                                    npc.netUpdate = true;
                                }
                            }

                            // Decide rotation.
                            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                            break;
                    }
                    generalAttackTimer++;
                }

                // Handle frames.
                npc.frameCounter++;
                frame = (int)Math.Round(MathHelper.Lerp(10f, 19f, (float)npc.frameCounter / 36f % 1f));
                if (canShoot)
                    frame += 10f;
                if (ExoTwinsAreInSecondPhase)
                    frame += 60f;

                // Release a dust telegraph prior to firing.
                if (apolloIsAboutToCharge)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Dust plasma = Dust.NewDustPerfect(npc.Center, Main.rand.NextBool() ? 107 : 110);
                        plasma.position = npc.Center + (npc.rotation - MathHelper.PiOver2).ToRotationVector2() * 85f;
                        plasma.velocity = Main.rand.NextVector2Circular(4f, 4f) + (npc.rotation - MathHelper.PiOver2).ToRotationVector2() * 5f;
                        plasma.noGravity = true;
                    }
                }
            }

            // Athena hovers around the target and releases pulse lasers at them.
            if (npc.type == ModContent.NPCType<AthenaNPC>())
            {
                // Do the spin movement.
                float spinInterpolant = (float)Math.Pow(Utils.GetLerpValue(0f, attackTime, attackTimer - redirectTime, true), 1.4);
                float hoverOffsetRotation = MathHelper.TwoPi * spinInterpolant * 3f;
                Vector2 hoverDestination = target.Center - Vector2.UnitY.RotatedBy(hoverOffsetRotation) * 800f;
                ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 30f, 75f);

                npc.ModNPC<AthenaNPC>().TurretFrameState = canShoot ? AthenaNPC.AthenaTurretFrameType.OpenMainTurret : AthenaNPC.AthenaTurretFrameType.Blinking;

                // Release lasers.
                if (canShoot && attackTimer % athenaShootRate == athenaShootRate - 1f)
                {
                    SoundEngine.PlaySound(CommonCalamitySounds.LaserCannonSound, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int type = ModContent.ProjectileType<PulseLaser>();
                        int laser = Utilities.NewProjectileBetter(npc.Center, npc.SafeDirectionTo(target.Center) * 8f, type, 500, 0f, Main.myPlayer);
                        if (Main.projectile.IndexInRange(laser))
                        {
                            Main.projectile[laser].owner = npc.target;
                            Main.projectile[laser].ModProjectile<PulseLaser>().InitialDestination = target.Center;
                            Main.projectile[laser].ModProjectile<PulseLaser>().TurretOffsetIndex = 2;
                            Main.projectile[laser].ai[1] = npc.whoAmI;
                            Main.projectile[laser].netUpdate = true;
                        }
                    }
                }
            }

            return attackTimer >= redirectTime + attackTime;
        }
    }
}
