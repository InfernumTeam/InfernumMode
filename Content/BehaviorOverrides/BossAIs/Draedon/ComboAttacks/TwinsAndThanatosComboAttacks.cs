using CalamityMod;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.Sounds;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod.NPCs;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.Projectiles.Boss;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.DraedonBehaviorOverride;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.ExoMechManagement;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Thanatos;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon;
using InfernumMode.Core.GlobalInstances.Systems;

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
                ExoMechComboAttackType.TwinsThanatos_CircledLaserSweep => DoBehavior_TwinsThanatos_CircledLaserSweep(npc, target, ref attackTimer, ref frameType),
                _ => false,
            };
        }

        public static bool DoBehavior_TwinsThanatos_ThermoplasmaDashes(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            bool isApollo = npc.type == ModContent.NPCType<Apollo>();
            bool isEitherExoTwin = isApollo || npc.type == ModContent.NPCType<Artemis>();
            int fastFlyDelay = 135;
            int chargeTime = 600;
            int exoTwinShootRate = 60;
            float exoTwinsBlastShootSpeed = 13f;
            float thanatosFlySpeedFactor = 1.02f;

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
                frameType = (int)ThanatosHeadBehaviorOverride.ThanatosFrameType.Open;
                ThanatosHeadBehaviorOverride.DoAggressiveChargeMovement(npc, target, attackTimer, thanatosFlySpeedFactor);
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

        public static bool DoBehavior_TwinsThanatos_CircledLaserSweep(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            int apolloShootRate = 48;
            int laserbeamTelegraphTime = 60;
            int laserbeamSweepTime = ArtemisSweepLaserbeam.LifetimeConst;
            float spinRadius = 720f;
            float spinArc = MathHelper.Pi * 5f;
            float plasmaBlastShootSpeed = 11f;
            NPC artemis = Main.npc[CalamityGlobalNPC.draedonExoMechTwinRed];
            ref float artemisHasRepositioned = ref artemis.Infernum().ExtraAI[0];
            ref float generalTimer = ref npc.Infernum().ExtraAI[1];
            ref float spinDirection = ref artemis.Infernum().ExtraAI[2];
            ref float spinningPointX = ref artemis.Infernum().ExtraAI[3];
            ref float spinningPointY = ref artemis.Infernum().ExtraAI[4];

            // This attack is very timing sensitive and resetting it if the twins suddenly need to enter their second phase is untenable.
            // As a result, if that happens, the attack is simply terminated early and laser beams/telegraphs are all destroyed.
            if (ExoTwinsAreEnteringSecondPhase)
			{
                Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<ArtemisDeathrayTelegraph>(), ModContent.ProjectileType<ArtemisSweepLaserbeam>());
                return true;
			}

            if (ExoTwinsAreInSecondPhase)
            {
                spinArc += MathHelper.Pi;
                apolloShootRate -= 17;
            }

            // Have Thanatos coil around the target.
            if (npc.type == ModContent.NPCType<ThanatosHead>())
            {
                // Disable contact damage briefly.
                if (generalTimer < 90f)
                    npc.damage = 0;

                generalTimer++;
                Vector2 spinDestination = target.Center + (generalTimer * MathHelper.TwoPi / 105f).ToRotationVector2() * 2150f;

                npc.velocity = npc.SafeDirectionTo(spinDestination) * MathHelper.Min(npc.Distance(spinDestination), 34f);
                npc.Center = npc.Center.MoveTowards(spinDestination, target.velocity.Length() * 1.2f + 35f);
                if (npc.WithinRange(spinDestination, 40f))
                    npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                else
                    npc.rotation = npc.rotation.AngleTowards((attackTimer + 8f) * MathHelper.TwoPi / 150f + MathHelper.PiOver2, 0.25f);

                // Decide frames.
                frameType = (int)ThanatosHeadBehaviorOverride.ThanatosFrameType.Open;
            }

            // Have Artemis reposition before the other mechs attack.
            if (artemisHasRepositioned == 0f)
            {
                attackTimer = 0f;
                if (npc.type == ModContent.NPCType<Artemis>())
                {
                    Vector2 hoverDestination = target.Center + Vector2.UnitY * spinRadius;
                    ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 27f, 84f);
                    if (npc.WithinRange(hoverDestination, 50f))
                    {
                        artemisHasRepositioned = 1f;
                        npc.Center = hoverDestination;
                        npc.rotation = 0f;
                        npc.netUpdate = true;
                    }
                }
                return false;
            }

            // Have Artemis cast a telegraph that indicates where the laserbeam will appear.
            if (npc.type == ModContent.NPCType<Artemis>())
            {
                // Disable contact damage.
                npc.damage = 0;

                if (attackTimer == 2f)
                {
                    SoundEngine.PlaySound(CommonCalamitySounds.LaserCannonSound, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Utilities.NewProjectileBetter(npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ArtemisDeathrayTelegraph>(), 0, 0f, -1, 0f, npc.whoAmI);

                        npc.velocity = Vector2.Zero;
                        npc.netUpdate = true;
                    }
                }
            }

            // Create the laserbeam.
            if (npc.type == ModContent.NPCType<Artemis>() && attackTimer == laserbeamTelegraphTime)
            {
                SoundEngine.PlaySound(TeslaCannon.FireSound, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Utilities.NewProjectileBetter(npc.Center, -Vector2.UnitY, ModContent.ProjectileType<ArtemisSweepLaserbeam>(), PowerfulShotDamage, 0f, -1, npc.whoAmI);

                    spinDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                    npc.velocity = Vector2.Zero;
                    npc.netUpdate = true;
                }
            }

            // Have Artemis sweep around.
            if (npc.type == ModContent.NPCType<Artemis>())
            {
                // Disable contact damage.
                npc.damage = 0;

                // Handle frames.
                npc.frameCounter++;
                frameType = (int)Math.Round(MathHelper.Lerp(10f, 19f, (float)npc.frameCounter / 36f % 1f));
                if (ExoTwinsAreInSecondPhase)
                    frameType += 60f;

                if (attackTimer >= laserbeamTelegraphTime)
                {
                    frameType += 10f;
                    float spinAngle = (attackTimer - laserbeamTelegraphTime) / laserbeamSweepTime * spinArc * -spinDirection;
                    npc.velocity = Vector2.Zero;
                    npc.Center = new Vector2(spinningPointX, spinningPointY) + Vector2.UnitY.RotatedBy(spinAngle) * spinRadius;
                    npc.rotation = npc.AngleTo(new Vector2(spinningPointX, spinningPointY)) + MathHelper.PiOver2;
                }
                else
                {
                    if (spinningPointX == 0f || Math.Abs(spinningPointX) > 100000f)
                    {
                        spinningPointX = target.Center.X;
                        spinningPointY = target.Center.Y;
                        npc.netUpdate = true;
                    }
                    npc.velocity = Vector2.Zero;
                    npc.Center = new Vector2(spinningPointX, spinningPointY) + Vector2.UnitY * spinRadius;
                }
            }

            // Apollo fires blasts at the target rapid-fire while circling around them.
            if (npc.type == ModContent.NPCType<Apollo>())
            {
                Vector2 hoverOffset = -Vector2.UnitY * 500f;
                if (attackTimer >= laserbeamTelegraphTime)
                    hoverOffset = hoverOffset.RotatedBy((attackTimer - laserbeamTelegraphTime) * MathHelper.TwoPi / 96f);

                Vector2 hoverDestination = target.Center + hoverOffset;
                ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 36f, 84f);

                // Look at the target.
                npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;

                // Fire plasma blasts.
                if (attackTimer % apolloShootRate == apolloShootRate - 1f)
                {
                    SoundEngine.PlaySound(PlasmaCaster.FireSound, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 plasmaShootCenter = npc.Center + npc.SafeDirectionTo(target.Center) * 70f;
                        Vector2 plasmaShootVelocity = npc.SafeDirectionTo(target.Center) * plasmaBlastShootSpeed;
                        Utilities.NewProjectileBetter(plasmaShootCenter, plasmaShootVelocity, ModContent.ProjectileType<ApolloPlasmaFireball>(), NormalShotDamage, 0f, -1, Main.rand.NextBool().ToDirectionInt());
                    }
                }

                // Handle frames.
                npc.frameCounter++;
                frameType = (int)Math.Round(MathHelper.Lerp(20f, 29f, (float)npc.frameCounter / 30f % 1f));
                if (ExoTwinsAreInSecondPhase)
                    frameType += 60f;
            }

            return attackTimer >= laserbeamTelegraphTime + laserbeamSweepTime;
        }
    }
}
