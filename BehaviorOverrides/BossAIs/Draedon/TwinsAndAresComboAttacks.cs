using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using DraedonNPC = CalamityMod.NPCs.ExoMechs.Draedon;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.AresBodyBehaviorOverride;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.ExoMechManagement;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
    public static partial class ExoMechComboAttackContent
    {
        public static float EnrageTimer
        {
            get
            {
                NPC initialMech = FindInitialMech();
                if (initialMech is null)
                    return 0f;

                if (initialMech.Opacity <= 0f)
                    initialMech.Infernum().ExtraAI[ApolloBehaviorOverride.ComplementMechEnrageTimerIndex] = 0f;

                return initialMech.Infernum().ExtraAI[ApolloBehaviorOverride.ComplementMechEnrageTimerIndex];
            }
            set
            {
                NPC initialMech = FindInitialMech();
                if (initialMech is null)
                    return;

                initialMech.Infernum().ExtraAI[ApolloBehaviorOverride.ComplementMechEnrageTimerIndex] = value;
            }
        }

        public static bool DoBehavior_AresTwins_PressureLaser(NPC npc, Player target, float twinsHoverSide, ref float attackTimer, ref float frame)
        {
            int attackDelay = 210;
            int pressureTime = 240;
            int laserTelegraphTime = AresBeamTelegraph.Lifetime;
            int laserReleaseTime = AresDeathray.Lifetime;
            int apolloPlasmaShootRate = 60;
            float apolloPlasmaSpread = 0.27f;
            float apolloPlasmaShootSpeed = 10f;

            if (CurrentTwinsPhase != 4)
            {
                attackDelay -= 20;
                pressureTime += 15;
                apolloPlasmaShootRate -= 5;
            }

            if (EnrageTimer > 0f)
            {
                apolloPlasmaSpread *= 2f;
                apolloPlasmaShootSpeed += 5f;
                apolloPlasmaShootRate -= 18;
            }

            bool aresSlowdownPreparationInProgress = attackTimer > attackDelay + pressureTime * 0.2f;

            // Don't do contact damage.
            npc.damage = 0;

            if (CalamityGlobalNPC.draedonExoMechPrime >= 0 && target.Center.Y < Main.npc[CalamityGlobalNPC.draedonExoMechPrime].Center.Y)
            {
                apolloPlasmaShootRate /= 6;
                apolloPlasmaShootSpeed += 8.5f;
                apolloPlasmaSpread *= 3f;
            }

            float artemisVerticalOffset = MathHelper.Lerp(540f, 80f, Utils.InverseLerp(50f, attackDelay + 50f, attackTimer, true));

            // Inherit the attack timer from the initial mech.
            attackTimer = FindInitialMech()?.ai[1] ?? attackTimer;

            // Have Artemis hover below the player, release a laserbeam, and rise upward.
            if (npc.type == ModContent.NPCType<Artemis>())
            {
                // Play a charge sound as a telegraph.
                if (attackTimer == 1f)
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/CrystylCharge"), target.Center);

                // Reset the flash effect.
                npc.ModNPC<Artemis>().ChargeFlash = 0f;

                // And create some fire dust on the eye crystal as a telegraph.
                if (attackTimer > 30f && attackTimer < attackDelay)
                {
                    int dustCount = attackTimer > attackDelay * 0.65f ? 3 : 1;
                    Vector2 dustSpawnCenter = npc.Center + (npc.rotation - MathHelper.PiOver2).ToRotationVector2() * 80f;
                    for (int i = 0; i < dustCount; i++)
                    {
                        float scale = Main.rand.NextFloat(1f, 1.425f);
                        Vector2 dustSpawnPosition = dustSpawnCenter + Main.rand.NextVector2Unit() * Main.rand.NextFloat(16f, 56f);
                        Vector2 dustVelocity = (dustSpawnCenter - dustSpawnPosition) / scale * 0.1f;

                        Dust fire = Dust.NewDustPerfect(dustSpawnPosition, 267);
                        fire.scale = scale;
                        fire.velocity = dustVelocity + Main.rand.NextVector2Circular(0.06f, 0.06f);
                        fire.color = Color.Lerp(Color.Orange, Color.Red, Main.rand.NextFloat(0.6f));
                        fire.noGravity = true;
                    }
                }

                // Hover in place.
                Vector2 hoverDestination = target.Center + new Vector2(twinsHoverSide * 600f, artemisVerticalOffset);
                DoHoverMovement(npc, hoverDestination, 17f, 60f);
                if (attackTimer >= attackDelay + 50f)
                {
                    npc.velocity.Y *= 0.5f;

                    // Only move downward if above the player.
                    bool abovePlayer = npc.Center.Y < target.Center.Y;
                    if (npc.velocity.Y > 0f && !abovePlayer)
                        npc.velocity.Y = 0f;
                }

                // Release the laserbeam when ready.
                // If the player attempts to sneakily teleport below Artemis they will descend and damage them with the laser.
                if (attackTimer == attackDelay)
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LaserCannon"), npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int type = ModContent.ProjectileType<ArtemisPressureLaser>();
                        int laser = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, type, 1100, 0f, Main.myPlayer, npc.whoAmI);
                        if (Main.npc.IndexInRange(laser))
                        {
                            Main.projectile[laser].ai[0] = npc.whoAmI;
                            Main.projectile[laser].ai[1] = 1f;
                        }
                    }
                }

                // Decide rotation.
                float idealRotation = npc.AngleTo(target.Center);
                if (npc.WithinRange(hoverDestination, 60f) || attackTimer > attackDelay * 0.65f)
                    idealRotation = twinsHoverSide == -1f ? 0f : MathHelper.Pi;
                idealRotation += MathHelper.PiOver2;
                npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.3f).AngleLerp(idealRotation, 0.075f);

                // Handle frames.
                npc.frameCounter++;
                if (attackTimer < attackDelay)
                    frame = (int)Math.Round(MathHelper.Lerp(70f, 79f, (float)npc.frameCounter / 36f % 1f));
                else
                    frame = (int)Math.Round(MathHelper.Lerp(80f, 89f, (float)npc.frameCounter / 36f % 1f));
            }

            // Have Apollo hover in place and release bursts of plasma bolts.
            if (npc.type == ModContent.NPCType<Apollo>())
            {
                ref float hoverOffsetX = ref npc.Infernum().ExtraAI[0];
                ref float hoverOffsetY = ref npc.Infernum().ExtraAI[1];

                // Reset the flash effect.
                npc.ModNPC<Apollo>().ChargeComboFlash = 0f;

                // Hover in place.
                Vector2 hoverDestination = target.Center + new Vector2(twinsHoverSide * 600f + hoverOffsetX, hoverOffsetY - 550f);
                DoHoverMovement(npc, hoverDestination, 37f, 75f);

                // Decide rotation.
                Vector2 aimDirection = npc.SafeDirectionTo(target.Center + target.velocity * 12f);
                npc.rotation = aimDirection.ToRotation() + MathHelper.PiOver2;

                // Periodically release plasma.
                // If the player attempts to go above Ares the shoot rate is dramatically faster as as punishment.
                if (attackTimer > attackDelay && attackTimer % apolloPlasmaShootRate == apolloPlasmaShootRate - 1f)
                {
                    Vector2 plasmaSpawnPosition = npc.Center + aimDirection * 70f;
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PlasmaCasterFire"), npc.Center);

                    for (int i = 0; i < 40; i++)
                    {
                        Vector2 dustVelocity = aimDirection.RotatedByRandom(0.35f) * Main.rand.NextFloat(1.8f, 3f);
                        int randomDustType = Main.rand.NextBool() ? 107 : 110;

                        Dust plasma = Dust.NewDustDirect(npc.position, npc.width, npc.height, randomDustType, dustVelocity.X, dustVelocity.Y, 200, default, 1.7f);
                        plasma.position = plasmaSpawnPosition + Main.rand.NextVector2Circular(48f, 48f);
                        plasma.noGravity = true;
                        plasma.velocity *= 3f;

                        plasma = Dust.NewDustDirect(npc.position, npc.width, npc.height, randomDustType, dustVelocity.X, dustVelocity.Y, 100, default, 0.8f);
                        plasma.position = plasmaSpawnPosition + Main.rand.NextVector2Circular(48f, 48f);
                        plasma.velocity *= 2f;

                        plasma.noGravity = true;
                        plasma.fadeIn = 1f;
                        plasma.color = Color.Green * 0.5f;
                    }

                    for (int i = 0; i < 20; i++)
                    {
                        Vector2 dustVelocity = npc.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(0.35f) * Main.rand.NextFloat(1.8f, 3f);
                        int randomDustType = Main.rand.NextBool() ? 107 : 110;

                        Dust plasma = Dust.NewDustDirect(npc.position, npc.width, npc.height, randomDustType, dustVelocity.X, dustVelocity.Y, 0, default, 2f);
                        plasma.position = plasmaSpawnPosition + Vector2.UnitX.RotatedByRandom(MathHelper.Pi).RotatedBy(npc.velocity.ToRotation()) * 16f;
                        plasma.noGravity = true;
                        plasma.velocity *= 0.5f;
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        hoverOffsetX = Main.rand.NextFloat(-30f, 30f);
                        hoverOffsetY = Main.rand.NextFloat(-85f, 85f);
                        for (int i = 0; i < 6; i++)
                        {
                            Vector2 plasmaShootVelocity = aimDirection.RotatedByRandom(apolloPlasmaSpread) * apolloPlasmaShootSpeed * Main.rand.NextFloat(0.6f, 1f);
                            Utilities.NewProjectileBetter(plasmaSpawnPosition, plasmaShootVelocity, ModContent.ProjectileType<TypicalPlasmaSpark>(), 580, 0f);
                        }
                        npc.netUpdate = true;
                    }
                }

                // Handle frames.
                npc.frameCounter++;
                frame = (int)Math.Round(MathHelper.Lerp(70f, 79f, (float)npc.frameCounter / 36f % 1f));
            }

            // Have Ares linger above the player, charge up, and eventually release a laserbeam.
            // Ares' arms will enforce a border and attempt to punish the player if they attempt to leave.
            if (npc.type == ModContent.NPCType<AresBody>())
            {
                ref float laserDirection = ref npc.Infernum().ExtraAI[0];

                // Hover in place.
                if (!aresSlowdownPreparationInProgress)
                {
                    Vector2 hoverDestination = target.Center - Vector2.UnitY * 410f;
                    DoHoverMovement(npc, hoverDestination, 24f, 75f);
                }
                else
                    npc.velocity = npc.velocity.ClampMagnitude(0f, 22f) * 0.92f;

                // Release a border of lasers to prevent from the player from just RoD-ing away.
                float minHorizontalOffset = MathHelper.Lerp(900f, 400f, Utils.InverseLerp(0f, attackDelay + 90f, attackTimer, true));
                for (int i = -1; i <= 1; i += 2)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        if (!Main.rand.NextBool(3))
                            break;

                        float horizontalOffset = Main.rand.NextFloat(minHorizontalOffset, 1900f) * i;
                        if (Main.rand.NextFloat() < 0.6f)
                            horizontalOffset = minHorizontalOffset * i + Main.rand.NextFloat(0f, 30f) * -i;
                        Vector2 laserSpawnPosition = new Vector2(npc.Center.X + horizontalOffset, target.Center.Y + Main.rand.NextBool().ToDirectionInt() * 1600f);
                        Vector2 laserShootVelocity = Vector2.UnitY * Math.Sign(target.Center.Y - laserSpawnPosition.Y) * Main.rand.NextFloat(7f, 8f);
                        if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(5) && attackTimer > 150f)
                        {
                            int lightning = Utilities.NewProjectileBetter(laserSpawnPosition, laserShootVelocity, ModContent.ProjectileType<ExoLightning>(), 750, 0f);
                            if (Main.projectile.IndexInRange(lightning))
                            {
                                Main.projectile[lightning].ai[0] = Main.projectile[lightning].velocity.ToRotation();
                                Main.projectile[lightning].ai[1] = Main.rand.Next(100);
                            }
                        }
                    }
                }

                // Create chargeup visuals before firing.
                if (aresSlowdownPreparationInProgress)
                {
                    float chargeupPower = Utils.InverseLerp(attackDelay + pressureTime * 0.35f, attackDelay + pressureTime, attackTimer, true);
                    for (int i = 0; i < 1f + chargeupPower * 3f; i++)
                    {
                        Vector2 laserDustSpawnPosition = npc.Center + Vector2.UnitY * 26f + Main.rand.NextVector2CircularEdge(20f, 20f);
                        Dust laser = Dust.NewDustPerfect(laserDustSpawnPosition, 182);
                        laser.velocity = -Vector2.UnitY * Main.rand.NextFloat(1f, 3.5f) * MathHelper.Lerp(0.35f, 1f, chargeupPower);
                        laser.scale = MathHelper.Lerp(0.8f, 1.5f, chargeupPower) * Main.rand.NextFloat(0.75f, 1f);
                        laser.noGravity = true;

                        float secondaryDustSpeed = -laser.velocity.Length() * (1f + chargeupPower * 1.56f);
                        Dust.CloneDust(laser).velocity = (npc.Center - laser.position).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.4f) * secondaryDustSpeed;
                    }
                }

                // Create telegraphs prior to firing.
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == attackDelay + pressureTime - laserTelegraphTime)
                {
                    // Point the lasers at the player.
                    laserDirection = npc.AngleTo(target.Center);

                    int type = ModContent.ProjectileType<AresBeamTelegraph>();
                    for (int b = 0; b < 7; b++)
                    {
                        int beam = Projectile.NewProjectile(npc.Center, Vector2.Zero, type, 0, 0f, 255, npc.whoAmI);

                        // Determine the initial offset angle of telegraph. It will be smoothened to give a "stretch" effect.
                        if (Main.projectile.IndexInRange(beam))
                        {
                            float squishedRatio = (float)Math.Pow((float)Math.Sin(MathHelper.Pi * b / 7f), 2D);
                            float smoothenedRatio = MathHelper.SmoothStep(0f, 1f, squishedRatio);
                            Main.projectile[beam].ai[0] = npc.whoAmI;
                            Main.projectile[beam].ai[1] = MathHelper.Lerp(-0.55f, 0.55f, smoothenedRatio) + laserDirection;
                            Main.projectile[beam].localAI[0] = laserDirection;
                        }
                    }
                }

                // Release the laserbeam.
                if (attackTimer == attackDelay + pressureTime)
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/TeslaCannonFire"), target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int type = ModContent.ProjectileType<AresDeathray>();
                        int beam = Utilities.NewProjectileBetter(npc.Center, laserDirection.ToRotationVector2(), type, 1200, 0f);
                        if (Main.projectile.IndexInRange(beam))
                            Main.projectile[beam].ai[1] = npc.whoAmI;
                    }
                }

                // Rise upward a little bit after the laserbeam is released.
                if (attackTimer > attackDelay + pressureTime)
                    npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * 6f, 0.06f);

                // Handle frames.
                frame = (int)AresBodyFrameType.Normal;
                if (aresSlowdownPreparationInProgress)
                    frame = (int)AresBodyFrameType.Laugh;
            }

            bool attackAboutToConclude = attackTimer > attackDelay + pressureTime + laserReleaseTime + 30f;

            // Clear lasers when the attack should end.
            if (attackAboutToConclude)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    if (Main.projectile[i].type == ModContent.ProjectileType<ArtemisPressureLaser>())
                        Main.projectile[i].Kill();
                }
            }
            return attackAboutToConclude;
        }

        public static bool DoBehavior_AresTwins_DualLaserCharges(NPC npc, Player target, float twinsHoverSide, ref float attackTimer, ref float frame)
        {
            int laserBurstCount = 2;
            int redirectTime = 75;
            int chargeupTime = 40;
            int laserTelegraphTime = AresBeamTelegraph.Lifetime;
            int laserSpinTime = AresSpinningRedDeathray.Lifetime;
            float wrappedAttackTimer = attackTimer % (redirectTime + chargeupTime + laserTelegraphTime + laserSpinTime);
            bool deathraysHaveBeenFired = wrappedAttackTimer >= redirectTime + chargeupTime + laserTelegraphTime;
            float apolloChargeSpeed = 45f;
            float artemisChargeSpeed = 30f;
            int artemisChargeTime = 64;
            int artemisLaserReleaseRate = 16;
            int artemisLaserBurstCount = 8;
            float maxLaserTurnSpeed = MathHelper.TwoPi / 240f;

            if (CurrentTwinsPhase != 4)
            {
                apolloChargeSpeed += 5f;
                artemisLaserReleaseRate -= 2;
            }

            if (EnrageTimer > 0f)
            {
                apolloChargeSpeed += 9f;
                artemisLaserReleaseRate /= 2;
                artemisLaserBurstCount += 6;
                maxLaserTurnSpeed *= 1.5f;
            }

            bool aresSlowdownPreparationInProgress = wrappedAttackTimer >= redirectTime;
            bool apolloIsAboutToCharge = wrappedAttackTimer > redirectTime + chargeupTime + laserTelegraphTime - 90f && !deathraysHaveBeenFired;

            // Inherit the attack timer from the initial mech.
            attackTimer = FindInitialMech()?.ai[1] ?? attackTimer;

            // Have Artemis attempt to do horizontal sweep while releasing lasers in bursts. This only happens after Ares has released the laserbeams.
            if (npc.type == ModContent.NPCType<Artemis>())
            {
                ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
                ref float generalAttackTimer = ref npc.Infernum().ExtraAI[1];

                // Don't do contact damage.
                npc.damage = 0;

                // Reset the flash effect.
                npc.ModNPC<Artemis>().ChargeFlash = 0f;

                // Simply hover in place if the laserbeams have not been fired.
                if (!deathraysHaveBeenFired && attackSubstate == 0f)
                {
                    Vector2 hoverDestination = target.Center + new Vector2(twinsHoverSide * 600f, -400f);
                    DoHoverMovement(npc, hoverDestination, 30f, 75f);

                    // Decide rotation.
                    npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;
                }
                else
                {
                    switch ((int)attackSubstate)
                    {
                        // Hover into position.
                        case 0:
                        default:
                            Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 800f, -400f);
                            Vector2 chargeVelocity = Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X) * artemisChargeSpeed;
                            DoHoverMovement(npc, hoverDestination, 20f, 60f);

                            // Determine rotation.
                            npc.rotation = chargeVelocity.ToRotation() + MathHelper.PiOver2;

                            // Prepare the charge.
                            if (generalAttackTimer > 45f && npc.WithinRange(hoverDestination, 40f))
                            {
                                generalAttackTimer = 0f;
                                attackSubstate = 1f;
                                npc.velocity = chargeVelocity;
                                npc.netUpdate = true;
                            }
                            break;

                        // Swoop down slightly and release lasers.
                        case 1:
                            npc.velocity.Y = CalamityUtils.Convert01To010(generalAttackTimer / artemisChargeTime) * 13.5f;
                            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

                            if (generalAttackTimer % artemisLaserReleaseRate == artemisLaserReleaseRate - 1f)
                            {
                                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LaserCannon"), npc.Center);

                                if (Main.netMode != NetmodeID.MultiplayerClient)
                                {
                                    float offsetAngle = Main.rand.NextFloat(MathHelper.Pi / artemisLaserBurstCount);
                                    for (int i = 0; i < artemisLaserBurstCount; i++)
                                    {
                                        Vector2 aimDestination = npc.Center + (MathHelper.TwoPi * i / artemisLaserBurstCount + offsetAngle).ToRotationVector2() * 1500f;
                                        Vector2 laserShootVelocity = npc.SafeDirectionTo(aimDestination) * 10.5f;
                                        int laser = Utilities.NewProjectileBetter(npc.Center, laserShootVelocity, ModContent.ProjectileType<ArtemisLaser>(), 580, 0f);
                                        if (Main.projectile.IndexInRange(laser))
                                        {
                                            Main.projectile[laser].ModProjectile<ArtemisLaser>().InitialDestination = aimDestination + laserShootVelocity.SafeNormalize(Vector2.UnitY) * 1600f;
                                            Main.projectile[laser].ai[1] = npc.whoAmI;
                                            Main.projectile[laser].netUpdate = true;
                                        }
                                    }
                                }
                            }

                            if (generalAttackTimer > artemisChargeTime)
                            {
                                generalAttackTimer = 0f;
                                attackSubstate = 0f;
                                npc.velocity *= 0.55f;
                                npc.netUpdate = true;
                            }
                            break;
                    }
                    generalAttackTimer++;
                }

                // Handle frames.
                npc.frameCounter++;
                frame = (int)Math.Round(MathHelper.Lerp(70f, 79f, (float)npc.frameCounter / 36f % 1f));
                if (attackSubstate >= 1f)
                    frame += 10f;
            }

            // Have Apollo do loops and carpet bombs with plasma missiles. This only happens after Ares has released the laserbeams.
            if (npc.type == ModContent.NPCType<Apollo>())
            {
                ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
                ref float generalAttackTimer = ref npc.Infernum().ExtraAI[1];

                // Reset contact damage.
                npc.damage = 0;

                // Reset the flash effect.
                npc.ModNPC<Apollo>().ChargeComboFlash = 0f;

                // Simply hover in place if the laserbeams have not been fired.
                if (!deathraysHaveBeenFired && attackSubstate == 0f)
                {
                    Vector2 hoverDestination = target.Center + new Vector2(twinsHoverSide * 600f, -400f);
                    DoHoverMovement(npc, hoverDestination, 30f, 75f);

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
                                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/ELRFire"), target.Center);
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
                            // Do contact damage.
                            npc.damage = npc.defDamage;

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
                                float adjustedTimer = generalAttackTimer - 50f;
                                if (adjustedTimer > 30f)
                                    npc.velocity = npc.velocity.RotatedBy(MathHelper.TwoPi / 90f);
                                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y - 2f, -42f, 42f);

                                // Release rockets.
                                if (adjustedTimer % 8f == 7f)
                                {
                                    Main.PlaySound(SoundID.Item36, target.Center);

                                    if (Main.netMode != NetmodeID.MultiplayerClient)
                                    {
                                        int type = ModContent.ProjectileType<ApolloRocket>();
                                        Vector2 rocketVelocity = npc.velocity.SafeNormalize(Vector2.UnitY) * 24f;
                                        Vector2 rocketSpawnPosition = npc.Center + npc.velocity.SafeNormalize(Vector2.Zero) * 70f;
                                        int rocket = Utilities.NewProjectileBetter(rocketSpawnPosition, rocketVelocity, type, 640, 0f, Main.myPlayer, 0f, target.Center.Y);
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
                frame = (int)Math.Round(MathHelper.Lerp(70f, 79f, (float)npc.frameCounter / 36f % 1f));
                if (wrappedAttackTimer > redirectTime + chargeupTime + laserTelegraphTime - 90f)
                    frame += 10f;

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

            // Have Ares pick a point to move to, move there, charge up, and release two lasers that spin around
            // and force smart movement when dealing with Artemis and Apollo's fast charges, spins, and other complex movements.
            if (npc.type == ModContent.NPCType<AresBody>())
            {
                ref float hoverDestinationX = ref npc.Infernum().ExtraAI[0];
                ref float hoverDestinationY = ref npc.Infernum().ExtraAI[1];
                ref float laserRotationalOffset = ref npc.Infernum().ExtraAI[2];
                ref float laserDirection = ref npc.Infernum().ExtraAI[3];

                Vector2 hoverDestination = new Vector2(hoverDestinationX, hoverDestinationY);

                // Define a hover destination if one is yet to be initialized.
                if (hoverDestinationX == 0f || hoverDestinationY == 0f || wrappedAttackTimer == 1f)
                {
                    laserDirection = 0f;
                    laserRotationalOffset = 0f;

                    int tries = 0;
                    do
                    {
                        hoverDestination = target.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(500f, 1200f);
                        tries++;

                        if (tries >= 1000)
                            break;
                    }
                    while (npc.WithinRange(hoverDestination, 800f) || Collision.SolidCollision(hoverDestination - Vector2.One * 200f, 400, 400));

                    hoverDestinationX = hoverDestination.X;
                    hoverDestinationY = hoverDestination.Y;
                    npc.netUpdate = true;
                }
                else if (wrappedAttackTimer < redirectTime)
                    DoHoverMovement(npc, hoverDestination, 25f, 50f);

                // Cease any movement once done redirecting.
                else
                    npc.velocity = npc.velocity.ClampMagnitude(0f, 15f) * 0.85f;

                // Create chargeup visuals before firing.
                if (aresSlowdownPreparationInProgress)
                {
                    float chargeupPower = Utils.InverseLerp(redirectTime + chargeupTime * 0.35f, redirectTime + chargeupTime, wrappedAttackTimer, true);
                    for (int i = 0; i < 1f + chargeupPower * 3f; i++)
                    {
                        Vector2 laserDustSpawnPosition = npc.Center + Vector2.UnitY * 26f + Main.rand.NextVector2CircularEdge(20f, 20f);
                        Dust laser = Dust.NewDustPerfect(laserDustSpawnPosition, 182);
                        laser.velocity = -Vector2.UnitY * Main.rand.NextFloat(1f, 3.5f) * MathHelper.Lerp(0.35f, 1f, chargeupPower);
                        laser.scale = MathHelper.Lerp(0.8f, 1.5f, chargeupPower) * Main.rand.NextFloat(0.75f, 1f);
                        laser.noGravity = true;

                        float secondaryDustSpeed = -laser.velocity.Length() * (1f + chargeupPower * 1.56f);
                        Dust.CloneDust(laser).velocity = (npc.Center - laser.position).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.4f) * secondaryDustSpeed;
                    }
                }

                // Create telegraphs prior to firing.
                if (Main.netMode != NetmodeID.MultiplayerClient && wrappedAttackTimer == redirectTime + chargeupTime)
                {
                    int type = ModContent.ProjectileType<AresBeamTelegraph>();
                    for (int b = 0; b < 7; b++)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            int beam = Projectile.NewProjectile(npc.Center, Vector2.Zero, type, 0, 0f, 255, npc.whoAmI);

                            // Determine the initial offset angle of telegraph. It will be smoothened to give a "stretch" effect.
                            if (Main.projectile.IndexInRange(beam))
                            {
                                float squishedRatio = (float)Math.Pow((float)Math.Sin(MathHelper.Pi * b / 7f), 2D);
                                float smoothenedRatio = MathHelper.SmoothStep(0f, 1f, squishedRatio);
                                Main.projectile[beam].ai[0] = npc.whoAmI;
                                Main.projectile[beam].localAI[0] = i == 0f ? MathHelper.PiOver2 : MathHelper.Pi * 1.5f;
                                Main.projectile[beam].ai[1] = MathHelper.Lerp(-0.55f, 0.55f, smoothenedRatio) + Main.projectile[beam].localAI[0];
                            }
                        }
                    }
                }

                // Release the laserbeams.
                if (wrappedAttackTimer == redirectTime + chargeupTime + laserTelegraphTime)
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/TeslaCannonFire"), target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        laserDirection = Main.rand.NextBool().ToDirectionInt();

                        int type = ModContent.ProjectileType<AresSpinningRedDeathray>();
                        int beam = Utilities.NewProjectileBetter(npc.Center, Vector2.UnitY, type, 1200, 0f);
                        if (Main.projectile.IndexInRange(beam))
                            Main.projectile[beam].ai[1] = npc.whoAmI;
                        beam = Utilities.NewProjectileBetter(npc.Center, -Vector2.UnitY, type, 1200, 0f);
                        if (Main.projectile.IndexInRange(beam))
                            Main.projectile[beam].ai[1] = npc.whoAmI;
                        npc.netUpdate = true;
                    }
                }

                // Make the lasers move.
                if (deathraysHaveBeenFired)
                {
                    float laserbeamRelativeTime = wrappedAttackTimer - (redirectTime + chargeupTime + laserTelegraphTime);
                    float deathraySpeed = Utils.InverseLerp(0f, 180f, laserbeamRelativeTime, true) * maxLaserTurnSpeed;
                    laserRotationalOffset += laserDirection * deathraySpeed;

                    // Get very pissed if the target leaves the deathray area.
                    if (!npc.WithinRange(target.Center, AresSpinningRedDeathray.LaserLength + 35f) && EnrageTimer <= 0f)
                    {
                        // Have Draedon comment on the player's attempts to escape.
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.DraedonAresEnrageText", DraedonNPC.TextColorEdgy);

                        EnrageTimer = 1500f;
                    }
                }

                // Decide rotation.
                npc.rotation = npc.velocity.X * 0.003f;

                // Handle frames.
                frame = (int)AresBodyFrameType.Normal;
                if (aresSlowdownPreparationInProgress)
                    frame = (int)AresBodyFrameType.Laugh;
            }

            return attackTimer >= (redirectTime + chargeupTime + laserTelegraphTime + laserSpinTime) * laserBurstCount;
        }

        public static bool DoBehavior_AresTwins_CircleAttack(NPC npc, Player target, float twinsHoverSide, ref float attackTimer, ref float frame)
        {
            int attackDelay = 120;
            int normalTwinsAttackTime = 360;
            int totalNormalShotsToDo = 8;
            float normalShotShootSpeed = 10f;

            if (CurrentTwinsPhase != 4)
            {
                normalShotShootSpeed += 3f;
                totalNormalShotsToDo++;
            }

            if (EnrageTimer > 0f)
            {
                normalShotShootSpeed += 6f;
                totalNormalShotsToDo += 3;
            }

            int normalShotShootRate = normalTwinsAttackTime / totalNormalShotsToDo;

            // Inherit the attack timer from the initial mech.
            attackTimer = FindInitialMech()?.ai[1] ?? attackTimer;

            // Hover over the target.
            // The plasma and tesla arms will do the attacking.
            if (npc.type == ModContent.NPCType<AresBody>())
            {
                ref float canFire = ref npc.Infernum().ExtraAI[0];

                // Decide whether to fire or not.
                canFire = (attackTimer > attackDelay).ToInt();

                Vector2 hoverDestination = target.Center - Vector2.UnitY * 435f;
                DoHoverMovement(npc, hoverDestination, 24f, 75f);

                // Decide the frame.
                frame = (int)AresBodyFrameType.Normal;
            }

            // Artemis and Apollo move in a circular formation at first, before creating the special attack.
            if (npc.type == ModContent.NPCType<Artemis>() || npc.type == ModContent.NPCType<Apollo>())
            {
                ref float normalShotCounter = ref npc.Infernum().ExtraAI[0];

                if (attackTimer < attackDelay + normalTwinsAttackTime)
                {
                    // Hover near the target and look at them.
                    float hoverOffsetAngle = MathHelper.TwoPi * normalShotCounter / totalNormalShotsToDo - MathHelper.PiOver2;
                    if (npc.type == ModContent.NPCType<Artemis>())
                    {
                        normalShotCounter = Main.npc[CalamityGlobalNPC.draedonExoMechTwinGreen].Infernum().ExtraAI[0];
                        hoverOffsetAngle += MathHelper.Pi;
                    }

                    Vector2 hoverDestination = target.Center + hoverOffsetAngle.ToRotationVector2() * new Vector2(700f, 380f);
                    DoHoverMovement(npc, hoverDestination, 40f, 95f);

                    Vector2 aimDestination = target.Center + target.velocity * 11.5f;
                    Vector2 aimDirection = npc.SafeDirectionTo(aimDestination);
                    npc.rotation = aimDirection.ToRotation() + MathHelper.PiOver2;

                    if (attackTimer % normalShotShootRate == normalShotShootRate - 1f && attackTimer >= attackDelay)
                    {
                        if (npc.type == ModContent.NPCType<Apollo>())
                        {
                            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PlasmaCasterFire"), npc.Center);

                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Vector2 plasmaShootVelocity = aimDirection * normalShotShootSpeed;
                                int plasma = Utilities.NewProjectileBetter(npc.Center + aimDirection * 70f, plasmaShootVelocity, ModContent.ProjectileType<ApolloPlasmaFireball>(), 550, 0f);
                                if (Main.projectile.IndexInRange(plasma))
                                    Main.projectile[plasma].ai[0] = Main.rand.NextBool().ToDirectionInt();
                            }
                        }
                        else
                        {
                            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LaserCannon"), npc.Center);

                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Vector2 laserShootVelocity = aimDirection * normalShotShootSpeed;
                                int laser = Utilities.NewProjectileBetter(npc.Center + aimDirection * 70f, laserShootVelocity, ModContent.ProjectileType<ArtemisLaser>(), 550, 0f);
                                if (Main.projectile.IndexInRange(laser))
                                {
                                    Main.projectile[laser].ModProjectile<ArtemisLaser>().InitialDestination = aimDestination;
                                    Main.projectile[laser].ai[1] = npc.whoAmI;
                                    Main.projectile[laser].netUpdate = true;
                                }
                            }
                        }

                        normalShotCounter++;
                        npc.netUpdate = true;
                    }

                    npc.frameCounter++;
                    frame = (int)Math.Round(MathHelper.Lerp(70f, 79f, (float)npc.frameCounter / 36f % 1f));
                }
            }
            return attackTimer > attackDelay + normalTwinsAttackTime;
        }

        public static bool DoBehavior_AresTwins_ElectromagneticPlasmaStar(NPC npc, Player target, float twinsHoverSide, ref float attackTimer, ref float frame)
        {
            int attackDelay = 60;
            int starCreationTime = ElectromagneticStar.ChargeupTime;
            int starAnimationTime = attackDelay + starCreationTime;
            int starAttackTime = ElectromagneticStar.AttackTime;

            if (CalamityGlobalNPC.draedonExoMechPrime == -1 || CalamityGlobalNPC.draedonExoMechTwinGreen == -1)
                return true;

            int potentialStarIndex = (int)Main.npc[CalamityGlobalNPC.draedonExoMechPrime].Infernum().ExtraAI[0];
            Projectile star = potentialStarIndex >= 0 && Main.projectile[potentialStarIndex].type == ModContent.ProjectileType<ElectromagneticStar>() ? Main.projectile[potentialStarIndex] : null;

            Vector2 hoverDestination = target.Center + new Vector2(twinsHoverSide * 600f, -425f);
            if (npc.type == ModContent.NPCType<AresBody>())
                hoverDestination = target.Center - Vector2.UnitY * 580f;

            // Determine rotation for the twins.
            else
                npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;

            // Do hover movement.
            DoHoverMovement(npc, hoverDestination, 25f, 75f);

            // All three mechs come together to form an electromagnetic star.
            // After the star is created, all three mechs will use energy to power it up. To keep the target occupied, Ares' core will release homing energy bolts during the chargeup period.
            if (attackTimer < starAnimationTime)
            {
                // Have ares prepare the star.
                if (Main.netMode != NetmodeID.MultiplayerClient && npc.type == ModContent.NPCType<AresBody>() && attackTimer == attackDelay)
                {
                    npc.Infernum().ExtraAI[0] = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<ElectromagneticStar>(), 1600, 0f);
                    npc.netUpdate = true;
                }

                // Start releasing cinders from the core.
                if (Main.netMode != NetmodeID.MultiplayerClient && npc.type == ModContent.NPCType<AresBody>() && attackTimer >= attackDelay && attackTimer % 12f == 11f && star != null)
                {
                    Vector2 coreSpawnPosition = npc.Center + Vector2.UnitY * 26f;
                    Utilities.NewProjectileBetter(coreSpawnPosition, Main.rand.NextVector2CircularEdge(12f, 12f), ModContent.ProjectileType<ExoburstSpark>(), 600, 0f);
                }

                NPC plasmaArm = Main.npc[NPC.FindFirstNPC(ModContent.NPCType<AresPlasmaFlamethrower>())];
                NPC teslaArm = Main.npc[NPC.FindFirstNPC(ModContent.NPCType<AresTeslaCannon>())];
                NPC artemis = Main.npc[CalamityGlobalNPC.draedonExoMechTwinRed];
                NPC apollo = Main.npc[CalamityGlobalNPC.draedonExoMechTwinGreen];
                List<NPC> thingsThatCanSpawnFuel = new List<NPC>()
                {
                    plasmaArm,
                    teslaArm,
                    artemis,
                    apollo
                };
                for (int i = 0; i < thingsThatCanSpawnFuel.Count; i++)
                {
                    if (star is null || npc.type != ModContent.NPCType<AresBody>())
                        break;

                    if (!Main.rand.NextBool(15))
                        continue;

                    Color color = Color.Lime;
                    if (thingsThatCanSpawnFuel[i].type == teslaArm.type)
                        color = Color.Cyan;
                    if (thingsThatCanSpawnFuel[i].type == artemis.type)
                        color = Color.Orange;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int fuel = Projectile.NewProjectile(thingsThatCanSpawnFuel[i].Center, thingsThatCanSpawnFuel[i].SafeDirectionTo(star.Center).RotatedByRandom(0.57f) * 9f, ModContent.ProjectileType<StarFuelProjectileThingIdk>(), 0, 0f);
                        if (Main.projectile.IndexInRange(fuel))
                            Main.projectile[fuel].ModProjectile<StarFuelProjectileThingIdk>().FuelColor = color;
                    }
                }

                // Hold the star in place above the player.
                if (star != null)
                    star.Center = target.Center - Vector2.UnitY * 350f;
            }

            // Release a border of lasers to prevent from the player from just RoD-ing away.
            float minHorizontalOffset = MathHelper.Lerp(1000f, 540f, Utils.InverseLerp(0f, attackDelay + 90f, attackTimer, true));
            if (npc.type == ModContent.NPCType<AresBody>())
            {
                for (int i = -1; i <= 1; i += 2)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        if (!Main.rand.NextBool(3))
                            break;

                        float horizontalOffset = Main.rand.NextFloat(minHorizontalOffset, 1900f) * i;
                        if (Main.rand.NextFloat() < 0.6f)
                            horizontalOffset = minHorizontalOffset * i + Main.rand.NextFloat(0f, 30f) * -i;

                        Vector2 laserSpawnPosition = new Vector2(npc.Center.X + horizontalOffset, target.Center.Y + Main.rand.NextBool().ToDirectionInt() * 1600f);
                        Vector2 laserShootVelocity = Vector2.UnitY * Math.Sign(target.Center.Y - laserSpawnPosition.Y) * Main.rand.NextFloat(7f, 8f);
                        if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(5))
                        {
                            int lightning = Utilities.NewProjectileBetter(laserSpawnPosition, laserShootVelocity, ModContent.ProjectileType<ExoLightning>(), 750, 0f);
                            if (Main.projectile.IndexInRange(lightning))
                            {
                                Main.projectile[lightning].ai[0] = Main.projectile[lightning].velocity.ToRotation();
                                Main.projectile[lightning].ai[1] = Main.rand.Next(100);
                            }
                        }
                    }
                }
                frame = (int)AresBodyFrameType.Laugh;
            }
            else
            {
                npc.frameCounter++;
                frame = (int)Math.Round(MathHelper.Lerp(70f, 79f, (float)npc.frameCounter / 36f % 1f));
            }

            return attackTimer >= attackDelay + starCreationTime + starAnimationTime + starAttackTime;
        }
    }
}
