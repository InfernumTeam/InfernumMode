using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Artemis;
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

            if (CurrentTwinsPhase != 4)
            {
                twinsShootRate -= 8;
                chargeTime += 6;
                spinAngularVelocity *= 1.33f;
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
                    SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Item/PlasmaCasterFire"), npc.Center);

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
                    SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Item/LaserCannon"), npc.Center);

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
                        SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Item/ELRFire"), target.Center);
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
    }
}
