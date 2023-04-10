using CalamityMod;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Sounds;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.DraedonBehaviorOverride;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.ExoMechManagement;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Thanatos.ThanatosHeadBehaviorOverride;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.ComboAttacks
{
    public static partial class ExoMechComboAttackContent
    {
        public static bool UseTwinsThanatosComboAttack(NPC npc, float twinsHoverSide, ref float attackTimer, ref float frameType)
        {
            NPC initialMech = FindInitialMech();
            if (initialMech is null)
                return false;

            Player target = Main.player[initialMech.target];
            return (ExoMechComboAttackType)initialMech.ai[0] switch
            {
                ExoMechComboAttackType.TwinsThanatos_ThermoplasmaDashes => DoBehavior_TwinsThanatos_ThermoplasmaDashes(npc, target, ref attackTimer, ref frameType),
                ExoMechComboAttackType.TwinsThanatos_AlternatingTwinsBursts => DoBehavior_TwinsThanatos_AlternatingTwinsBursts(npc, target, ref attackTimer, ref frameType),
                _ => false,
            };
        }

        public static bool DoBehavior_TwinsThanatos_ThermoplasmaDashes(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            bool isApollo = npc.type == ModContent.NPCType<Apollo>();
            bool isEitherExoTwin = isApollo || npc.type == ModContent.NPCType<Artemis>();
            int fastFlyDelay = 135;
            int chargeTime = 480;
            int exoTwinShootRate = 60;
            float exoTwinsBlastShootSpeed = 13f;
            float thanatosFlySpeedFactor = 0.9f;
            if (attackTimer <= 60f)
                thanatosFlySpeedFactor *= Utils.GetLerpValue(-15f, 60f, attackTimer, true);

            // Halt attacking if Artemis and Apollo are busy entering their second phase.
            if (ExoTwinsAreEnteringSecondPhase)
                attackTimer = 0f;

            if (ExoTwinsAreInSecondPhase)
            {
                thanatosFlySpeedFactor += 0.04f;
                exoTwinShootRate -= 8;

                bool twinsInSecondPhase = CurrentTwinsPhase is not 4 and not 0;
                if (twinsInSecondPhase || CurrentThanatosPhase != 4)
                {
                    thanatosFlySpeedFactor += 0.045f;
                    exoTwinShootRate -= 12;
                }
            }

            if (CalamityGlobalNPC.draedonExoMechWorm == -1)
                return false;

            // The Exo Twins circle Thanatos' head and fire blasts that explode into lingering fire/plasma gas.
            if (isEitherExoTwin)
            {
                NPC thanatosHead = Main.npc[CalamityGlobalNPC.draedonExoMechWorm];
                Vector2 hoverDestination = thanatosHead.Center + (MathHelper.TwoPi * attackTimer / 180f).ToRotationVector2() * isApollo.ToDirectionInt() * 210f;
                ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 40f, 84f);

                Vector2 aimDestination = target.Center + target.velocity * 10f;
                Vector2 aimDirection = npc.SafeDirectionTo(aimDestination);

                // Look at the target.
                npc.rotation = aimDirection.ToRotation() + MathHelper.PiOver2;

                // Disable contact damage.
                npc.damage = 0;

                // Handle frames.
                npc.frameCounter++;
                frameType = (int)Math.Round(MathHelper.Lerp(10f, 19f, (float)npc.frameCounter / 36f % 1f));
                if (attackTimer > fastFlyDelay)
                    frameType += 10f;
                if (ExoTwinsAreInSecondPhase)
                    frameType += 60f;

                // Fire blasts.
                if (attackTimer >= fastFlyDelay && attackTimer % exoTwinShootRate == exoTwinShootRate - 1f && !npc.WithinRange(target.Center, 360f))
                {
                    SoundEngine.PlaySound(PlasmaCaster.FireSound, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int blastID = isApollo ? ModContent.ProjectileType<ApolloPlasmaFireball>() : ModContent.ProjectileType<ArtemisGasFireballBlast>();

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(fireball =>
                        {
                            if (!isApollo)
                                return;

                            fireball.ModProjectile<ApolloPlasmaFireball>().GasExplosionVariant = true;
                        });
                        Utilities.NewProjectileBetter(npc.Center + aimDirection * 70f, aimDirection * exoTwinsBlastShootSpeed, blastID, StrongerNormalShotDamage, 0f);
                    }
                }
            }

            // Thanatos charges at the target.
            else
            {
                // Decide frames.
                frameType = (int)ThanatosFrameType.Open;
                DoAggressiveChargeMovement(npc, target, attackTimer, thanatosFlySpeedFactor);
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            }

            // Delete blasts and gas before transitioning to the next attack.
            if (attackTimer == fastFlyDelay + chargeTime - 1f)
            {
                Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<ArtemisGasFireballBlast>(), ModContent.ProjectileType<ApolloPlasmaFireball>(),
                    ModContent.ProjectileType<SuperheatedExofireGas>(), ModContent.ProjectileType<PlasmaGas>());
            }

            return attackTimer >= fastFlyDelay + chargeTime;
        }

        public static bool DoBehavior_TwinsThanatos_AlternatingTwinsBursts(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            int attackDelay = 90;
            int redirectTime = 20;
            int thanatosChargeTime = 80;
            float thanatosFlyAcceleration = 1.0184f;

            int exoTwinAttackCycleTime = 150;
            int exoTwinAttackCycleCount = 2;
            int artemisHoverTime = exoTwinAttackCycleTime - ArtemisSpinLaser.LaserLifetime;
            int artemisSpinTime = exoTwinAttackCycleTime - artemisHoverTime;
            int exoTwinThatShouldAttack = (attackTimer - attackDelay) % (exoTwinAttackCycleTime * 2f) >= exoTwinAttackCycleTime ? ModContent.NPCType<Artemis>() : ModContent.NPCType<Apollo>();
            int apolloTelegraphDelay = 96;
            int apolloPlasmaShootRate = 5;
            bool isExoTwin = npc.type == ModContent.NPCType<Artemis>() || npc.type == ModContent.NPCType<Apollo>();
            bool isAttackingExoTwin = npc.type == exoTwinThatShouldAttack;
            float artemisSpinRadius = 560f;
            float artemisSpinArc = MathHelper.Pi * 0.95f;

            // TODO -- This may need to be represented as a series and then numerically solved via some root-approximating method if
            // Artemis should accelerate when spinning and not have a constant angular velocity.
            float artemisSpinSpeed = artemisSpinArc * artemisSpinRadius / artemisSpinTime;

            float baseApolloChargeSpeed = 19f;
            float apolloChargeAcceleration = 1.06f;
            float apolloFireballExplosionRadius = 640f;
            float apolloPlasmaFireballSpeed = Main.rand.NextFloat(10f, 32f);

            if (ExoTwinsAreInSecondPhase)
            {
                baseApolloChargeSpeed += 4f;
                artemisSpinArc *= 1.2f;
                apolloFireballExplosionRadius += 60f;
                apolloPlasmaFireballSpeed *= 1.2f;
            }

            ref float artemisSpinDirection = ref npc.Infernum().ExtraAI[0];
            ref float apolloHoverOffsetAngle = ref npc.Infernum().ExtraAI[0];

            // This attack is very timing sensitive and resetting it if the twins suddenly need to enter their second phase is untenable.
            // As a result, if that happens, the attack is simply terminated early and laser beams/telegraphs are all destroyed.
            if (ExoTwinsAreEnteringSecondPhase)
            {
                if (npc.type == ModContent.NPCType<ThanatosHead>())
                    npc.damage = 0;

                Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<ArtemisDeathrayTelegraph>(), ModContent.ProjectileType<ArtemisSweepLaserbeam>());
                return true;
            }

            // Have Thanatos snap at the target.
            if (npc.type == ModContent.NPCType<ThanatosHead>())
            {
                float wrappedAttackTimer = (attackTimer - attackDelay) % (redirectTime + thanatosChargeTime);

                // Hover near the target before the attack begins.
                if (attackTimer < attackDelay)
                {
                    // Disable contact damage before the attack happens, to prevent cheap hits.
                    npc.damage = 0;

                    if (!npc.WithinRange(target.Center, 300f))
                        npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 13f, 0.06f);
                    npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                    return false;
                }

                // Redirect and look at the target before charging.
                // Thanatos will zip towards the target during this if necessary, to ensure that he's nearby by the time the attack begins.
                if (wrappedAttackTimer <= redirectTime)
                {
                    float flySpeed = Utils.Remap(npc.Distance(target.Center), 750f, 2700f, 6f, 24f);
                    float aimInterpolant = Utils.Remap(wrappedAttackTimer, 0f, redirectTime - 4f, 0.01f, 0.5f);
                    npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), MathHelper.Pi * aimInterpolant, true) * flySpeed;

                    if (!npc.WithinRange(target.Center, 1100f) && Vector2.Dot(npc.velocity, npc.SafeDirectionTo(target.Center)) < 0f)
                        npc.velocity *= -0.1f;
                }

                // Accelerate.
                else
                    npc.velocity *= thanatosFlyAcceleration;

                // Decide the current rotation based on velocity.
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

                // Decide frames.
                frameType = (int)ThanatosFrameType.Open;
            }

            // Exo Twins alternate between two different attack states.
            // The one that isn't busy attacking simply passively hovers.
            // The details of the attack states are described in comments below.
            if (isExoTwin)
            {
                // Decide frames.
                npc.frameCounter++;
                frameType = (int)Math.Round(MathHelper.Lerp(10f, 19f, (float)npc.frameCounter / 36f % 1f));
                if (ExoTwinsAreInSecondPhase)
                    frameType += 60f;

                if (isAttackingExoTwin)
                {
                    float localAttackTimer = (attackTimer - attackDelay) % exoTwinAttackCycleTime;

                    // Artemis performs a single laserbeam sweep, forcing the player to reposition.
                    if (npc.type == ModContent.NPCType<Artemis>())
                    {
                        // Cast laser telegraphs before firing.
                        if (localAttackTimer == artemisHoverTime - ArtemisLaserbeamTelegraph.TrueLifetime)
                        {
                            SoundEngine.PlaySound(InfernumSoundRegistry.ArtemisSpinLaserbeamSound with { Volume = 1.4f });
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                for (int i = 0; i < 2; i++)
                                {
                                    float telegraphAngularOffset = MathHelper.Lerp(-0.62f, 0.62f, i);
                                    Utilities.NewProjectileBetter(npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ArtemisLaserbeamTelegraph>(), 0, 0f, -1, npc.whoAmI, telegraphAngularOffset);
                                }
                            }
                        }

                        // Hover in place before the laserbeam appears.
                        if (localAttackTimer <= artemisHoverTime)
                        {
                            float slowdownInterpolant = Utils.GetLerpValue(artemisHoverTime - 40f, artemisHoverTime - 24f, localAttackTimer, true);
                            Vector2 hoverDestination = target.Center + Vector2.UnitY * artemisSpinRadius;
                            Vector2 idealVelocity = (hoverDestination - npc.Center) * (1f - slowdownInterpolant) * 0.1f;
                            npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.16f);

                            npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;
                            if (slowdownInterpolant >= 1f)
                                npc.rotation = 0f;
                        }

                        // Initialize Artemis' spin direction.
                        if (artemisSpinDirection == 0f && localAttackTimer >= artemisHoverTime - 9f)
                        {
                            float angularOffset = MathHelper.WrapAngle(npc.AngleTo(target.Center) - npc.rotation + MathHelper.PiOver2);
                            if (Math.Abs(angularOffset) > 0.01f)
                            {
                                artemisSpinDirection = Math.Sign(angularOffset);
                                npc.netUpdate = true;
                            }
                        }

                        // Fire the laserbeam.
                        if (localAttackTimer == artemisHoverTime)
                        {
                            // Create an incredibly violent screen shake effect.
                            Utilities.CreateShockwave(npc.Center, 4, 15, 192f, false);
                            ScreenEffectSystem.SetFlashEffect(npc.Center, 1f, 20);

                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                npc.velocity = Vector2.UnitX * artemisSpinSpeed * artemisSpinDirection;
                                int type = ModContent.ProjectileType<ArtemisSpinLaser>();
                                Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, type, PowerfulShotDamage, 0f, -1, npc.whoAmI, artemisSpinDirection);
                            }
                        }

                        // Spin around.
                        if (localAttackTimer >= artemisHoverTime)
                        {
                            npc.velocity = npc.velocity.RotatedBy(artemisSpinArc / artemisSpinTime * -artemisSpinDirection);
                            npc.rotation = npc.velocity.ToRotation();
                            if (artemisSpinDirection == -1f)
                                npc.rotation += MathHelper.Pi;
                        }
                    }

                    // Apollo will attempt to hover into position above the target and perform angled dashes towards them, leaving behind plasma bombs.
                    if (npc.type == ModContent.NPCType<Apollo>())
                    {
                        // Hover and cast telegraphs.
                        if (localAttackTimer <= apolloTelegraphDelay)
                        {
                            // Initialize the hover offset angle.
                            if (localAttackTimer == 1f)
                            {
                                apolloHoverOffsetAngle = Main.rand.NextFloatDirection() * 0.8f;
                                npc.netUpdate = true;
                            }

                            float slowdownInterpolant = Utils.GetLerpValue(apolloTelegraphDelay - 50f, apolloTelegraphDelay - 36f, localAttackTimer, true);
                            Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 600f, -350f).RotatedBy(apolloHoverOffsetAngle);
                            Vector2 idealVelocity = (hoverDestination - npc.Center) * (1f - slowdownInterpolant) * 0.1f;
                            npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.13f);

                            // Cast a telegraph line for the charge.
                            if (localAttackTimer == apolloTelegraphDelay - 36f)
                            {
                                SoundEngine.PlaySound(Artemis.ChargeTelegraphSound, target.Center);
                                if (Main.netMode != NetmodeID.MultiplayerClient)
                                {
                                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(telegraph =>
                                    {
                                        telegraph.ModProjectile<PlasmaChargeTelegraph>().ChargePositions = new[]
                                        {
                                            npc.Center,
                                            npc.Center + npc.SafeDirectionTo(target.Center) * 1600f
                                        };
                                    });

                                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Vector2.Zero, ModContent.ProjectileType<PlasmaChargeTelegraph>(), 0, 0f, npc.target, 0f, npc.whoAmI);

                                    // Cease any and all movement, to ensure that the telegraph doesn't become slightly off-center.
                                    npc.velocity = Vector2.Zero;
                                    npc.netUpdate = true;
                                }
                            }

                            // Look at the target.
                            if (slowdownInterpolant < 1f)
                                npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;
                        }

                        // Perform charge behaviors.
                        else
                        {
                            // Charge at the target.
                            if (localAttackTimer == apolloTelegraphDelay + 1f)
                            {
                                SoundEngine.PlaySound(Artemis.ChargeSound, target.Center);
                                npc.velocity = (npc.rotation - MathHelper.PiOver2).ToRotationVector2() * baseApolloChargeSpeed;
                            }

                            // Release a lot of plasma.
                            if (localAttackTimer % apolloPlasmaShootRate == 0f)
                            {
                                SoundEngine.PlaySound(CommonCalamitySounds.ExoPlasmaShootSound, npc.Center);
                                if (Main.netMode != NetmodeID.MultiplayerClient)
                                {
                                    Vector2 plasmaShootVelocity = Main.rand.NextVector2CircularEdge(15f, 15f);
                                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(bomb =>
                                    {
                                        bomb.timeLeft = 54;
                                    });
                                    Utilities.NewProjectileBetter(npc.Center, Main.rand.NextVector2Unit() * apolloPlasmaFireballSpeed - npc.velocity * 0.2f, ModContent.ProjectileType<ExoplasmaBomb>(), 0, 0f, -1, apolloFireballExplosionRadius);
                                }
                            }

                            // Accelerate.
                            if (npc.velocity.Length() < baseApolloChargeSpeed * 3.2f)
                                npc.velocity *= apolloChargeAcceleration;

                            // Look in the direction of current velocity.
                            if (localAttackTimer >= apolloTelegraphDelay + 1f)
                            {
                                npc.damage = npc.defDamage;
                                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                            }
                        }
                    }

                    // Switch to attacking frames.
                    frameType += 10f;
                }

                // Perform passive hover behaviors.
                else
                {
                    artemisSpinDirection = 0f;

                    Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 500f, -250f);
                    Vector2 idealVelocity = (hoverDestination - npc.Center) * 0.1f;
                    npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.08f);

                    // Look at the target.
                    npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;
                }
            }

            return attackTimer >= attackDelay + exoTwinAttackCycleTime * exoTwinAttackCycleCount * 2f;
        }
    }
}
