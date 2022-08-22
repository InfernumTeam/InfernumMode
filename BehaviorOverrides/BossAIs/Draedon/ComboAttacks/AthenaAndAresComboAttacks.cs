using CalamityMod;
using CalamityMod.Items.Weapons.DraedonsArsenal;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.Sounds;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.Athena;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.DraedonBehaviorOverride;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.ExoMechManagement;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.ComboAttacks
{
    public static partial class ExoMechComboAttackContent
    {
        public static bool UseAthenaAresComboAttack(NPC npc, ref float attackTimer, ref float frameType)
        {
            NPC initialMech = FindInitialMech();
            if (initialMech is null)
                return false;

            // Ensure that the player has a bit of time to compose themselves after killing the third mech.
            bool secondTwoAtOncePhase = CurrentAresPhase == 3 || CurrentThanatosPhase == 3 || CurrentTwinsPhase == 3 || CurrentAthenaPhase == 3;
            if (initialMech.Infernum().ExtraAI[23] < 180f && attackTimer >= 3f && secondTwoAtOncePhase)
            {
                initialMech.Infernum().ExtraAI[23]++;
                attackTimer = 3f;
            }

            Player target = Main.player[initialMech.target];
            return (ExoMechComboAttackType)initialMech.ai[0] switch
            {
                ExoMechComboAttackType.AthenaAres_ExowlPressureCannons => DoBehavior_AthenaAres_ExowlPressureCannons(npc, target, ref attackTimer, ref frameType),
                ExoMechComboAttackType.AthenaAres_ExowlCannonTheft => DoBehavior_AthenaAres_ExowlCannonTheft(npc, target, ref attackTimer, ref frameType),
                _ => false,
            };
        }

        public static bool DoBehavior_AthenaAres_ExowlPressureCannons(NPC npc, Player target, ref float attackTimer, ref float frame)
        {
            int shootDelay = 42;
            int sweepDelay = shootDelay + 60;
            int attackDuration = sweepDelay + 480;
            int illusionCount = 10;
            float maximumPressureAngle = MathHelper.ToRadians(44f);
            float predictivenessFactor = 21.5f;
            bool nonPressureArmsCanAttack = false;
            bool isPlasmaArm = npc.type == ModContent.NPCType<AresPlasmaFlamethrower>();
            bool isLaserArm = npc.type == ModContent.NPCType<AresLaserCannon>();
            bool isPressureArm = npc.type == ModContent.NPCType<AresPulseCannon>() || isLaserArm;
            bool isNonPressureArm = npc.type == ModContent.NPCType<AresTeslaCannon>() || isPlasmaArm;

            if (CurrentAthenaPhase != 4 || CurrentAresPhase != 4)
            {
                illusionCount += 3;
                nonPressureArmsCanAttack = true;
            }

            if (CalamityGlobalNPC.draedonExoMechPrime == -1)
                return false;

            NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];
            ref float cannonPressureAngle = ref aresBody.Infernum().ExtraAI[0];

            // Ares hovers above the player while his pulse and laser cannons aim downward before moving to a horizonal direction, creating a scissor with increasing pressure.
            // This leaves the target with less vertical room as the attack goes on.
            if (npc.type == ModContent.NPCType<AresBody>())
            {
                frame = (int)AresBodyBehaviorOverride.AresBodyFrameType.Normal;

                // Ensure that the backarm swap state is consistent.
                npc.Infernum().ExtraAI[14] = 240f;
                npc.Infernum().ExtraAI[15] = 1f;

                Vector2 hoverDestination = target.Center - Vector2.UnitY * 420f;
                ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 27f, 84f);

                if (attackTimer >= sweepDelay)
                {
                    // Apply a power to the interpolant to make the sweep non-linear, so that it starts out slow before accelerating.
                    float sweepInterpolant = (float)Math.Pow(Utils.GetLerpValue(sweepDelay, attackDuration - 24f, attackTimer, true), 1.67);
                    cannonPressureAngle = MathHelper.Lerp(MathHelper.PiOver2, MathHelper.Pi - maximumPressureAngle, sweepInterpolant);
                }

                // Give the taret infinite flight time.
                target.wingTime = target.wingTimeMax;
            }

            // Refer to the above comment for behavioral details.
            if (isPressureArm)
            {
                float _ = 0f;
                int projectileID = isLaserArm ? ModContent.ProjectileType<CannonLaser>() : ModContent.ProjectileType<AresPulseBlast>();
                int projectileShootRate = 10;
                float blastSpeed = 14.5f;
                SoundStyle shootSound = isLaserArm ? CommonCalamitySounds.LaserCannonSound : PulseRifle.FireSound;

                Vector2 aimDirection = cannonPressureAngle.ToRotationVector2() * new Vector2(isLaserArm.ToDirectionInt(), 1f);
                ExoMechAIUtilities.PerformAresArmDirectioning(npc, aresBody, target, aimDirection, false, false, ref _);

                // Fire the pressure projectiles. They do increased damage to punish the player for getting hit/trying to escape.
                if (attackTimer >= shootDelay && attackTimer % projectileShootRate == projectileShootRate - 1f)
                {
                    SoundEngine.PlaySound(shootSound, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int proj = Utilities.NewProjectileBetter(npc.Center + aimDirection * 96f, aimDirection * blastSpeed, projectileID, StrongerNormalShotDamage, 0f);
                        if (isLaserArm)
                            Main.projectile[proj].ai[1] = npc.whoAmI;
                    }
                }
            }

            // In the second to last phase, Ares' secondary cannons slowly fire projectiles at the target.
            // To prevent being unfair, they do not split, connect, or do any other fancy effects.
            if (isNonPressureArm)
            {
                float _ = 0f;
                int projectileID = isPlasmaArm ? ModContent.ProjectileType<AresPlasmaFireball>() : ModContent.ProjectileType<NonConnectingAresTeslaOrb>();
                int projectileShootRate = 48;
                float blastSpeed = 10.4f;
                SoundStyle shootSound = isPlasmaArm ? PlasmaCaster.FireSound : CommonCalamitySounds.PlasmaBoltSound;

                Vector2 aimDirection = npc.SafeDirectionTo(target.Center + target.velocity * predictivenessFactor);
                ExoMechAIUtilities.PerformAresArmDirectioning(npc, aresBody, target, aimDirection, false, false, ref _);

                // Fire the secondary projectiles.
                if (nonPressureArmsCanAttack && attackTimer >= shootDelay && attackTimer % projectileShootRate == projectileShootRate - 1f)
                {
                    SoundEngine.PlaySound(shootSound, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int proj = Utilities.NewProjectileBetter(npc.Center + aimDirection * 76f, aimDirection * blastSpeed, projectileID, NormalShotDamage, 0f);
                        if (isPlasmaArm)
                            Main.projectile[proj].ai[1] = -1f;
                    }
                }
            }

            // Athena hovers to the top left/right of the target after releasing a barrage of Exowls that swarm the player while Ares creates pressure.
            if (npc.type == ModContent.NPCType<AthenaNPC>())
            {
                frame = (int)AthenaNPC.AthenaTurretFrameType.Blinking;

                // Hover in place.
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 450f, -400f);
                if (npc.WithinRange(hoverDestination, 120f))
                    npc.velocity *= 0.96f;
                else
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 25f, 0.4f);

                if (attackTimer == shootDelay)
                {
                    SoundEngine.PlaySound(CommonCalamitySounds.LargeWeaponFireSound, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int realExowlIndex = Main.rand.Next(illusionCount);
                        for (int i = 0; i < illusionCount; i++)
                        {
                            Vector2 hologramPosition = npc.Center + Main.rand.NextVector2Circular(32f, 32f);
                            int exowl = NPC.NewNPC(npc.GetSource_FromAI(), (int)hologramPosition.X, (int)hologramPosition.Y, ModContent.NPCType<Exowl>(), npc.whoAmI);
                            if (Main.npc.IndexInRange(exowl))
                            {
                                Main.npc[exowl].ModNPC<Exowl>().UseConfusionEffect = true;
                                Main.npc[exowl].ModNPC<Exowl>().IsIllusion = i != realExowlIndex;
                                Main.npc[exowl].ModNPC<Exowl>().ChargeSpeedFactor = 0.5f;
                            }
                        }
                        npc.netUpdate = true;
                    }
                }

                // Make the Exowls self-destruct before transitioning to the next attack.
                if (attackTimer == attackDuration - 32f)
                    Exowl.MakeAllExowlsExplode();
            }

            return attackTimer >= attackDuration;
        }

        public static bool DoBehavior_AthenaAres_ExowlCannonTheft(NPC npc, Player target, ref float attackTimer, ref float frame)
        {
            // Ares' body hovers above the player and sometimes fires electric bolts from his core.
            if (npc.type == ModContent.NPCType<AresBody>())
            {
                frame = (int)AresBodyBehaviorOverride.AresBodyFrameType.Normal;

                // Ensure that the backarm swap state is consistent.
                npc.Infernum().ExtraAI[14] = 240f;
                npc.Infernum().ExtraAI[15] = 1f;
                
                Vector2 hoverDestination = target.Center - Vector2.UnitY * 375f;
                ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 27f, 84f);
            }

            return false;
        }
    }
}
