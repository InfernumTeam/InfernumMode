using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares.AresBodyBehaviorOverride;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.ExoMechManagement;
using ArtemisLaserInfernum = InfernumMode.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo.ArtemisLaser;
using DraedonNPC = CalamityMod.NPCs.ExoMechs.Draedon;

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
                    initialMech.Infernum().ExtraAI[Twins_ComplementMechEnrageTimerIndex] = 0f;

                return initialMech.Infernum().ExtraAI[Twins_ComplementMechEnrageTimerIndex];
            }
            set
            {
                NPC initialMech = FindInitialMech();
                if (initialMech is null)
                    return;

                initialMech.Infernum().ExtraAI[Twins_ComplementMechEnrageTimerIndex] = value;
            }
        }

        public static bool UseTwinsAresComboAttack(NPC npc, float twinsHoverSide, ref float attackTimer, ref float frameType)
        {
            NPC initialMech = FindInitialMech();
            if (initialMech is null)
                return false;

            Player target = Main.player[initialMech.target];
            switch ((ExoMechComboAttackType)initialMech.ai[0])
            {
                case ExoMechComboAttackType.AresTwins_DualLaserCharges:
                    return DoBehavior_AresTwins_DualLaserCharges(npc, target, twinsHoverSide, ref attackTimer, ref frameType);
                case ExoMechComboAttackType.AresTwins_CircleAttack:
                    return DoBehavior_AresTwins_CircleAttack(npc, target, ref attackTimer, ref frameType);
            }
            return false;
        }

        public static bool DoBehavior_AresTwins_DualLaserCharges(NPC npc, Player target, float twinsHoverSide, ref float attackTimer, ref float frame)
        {
            int laserBurstCount = 2;
            int redirectTime = 195;
            int chargeupTime = 40;
            int laserTelegraphTime = AresBeamTelegraph.Lifetime;
            int laserSpinTime = AresSpinningRedDeathray.Lifetime;
            float wrappedAttackTimer = attackTimer % (redirectTime + chargeupTime + laserTelegraphTime + laserSpinTime);
            bool deathraysHaveBeenFired = wrappedAttackTimer >= redirectTime + chargeupTime + laserTelegraphTime;
            float apolloChargeSpeed = 31f;
            float artemisChargeSpeed = 30f;
            int artemisChargeTime = 64;
            int artemisLaserReleaseRate = 34;
            int artemisLaserBurstCount = 8;
            float maxLaserTurnSpeed = MathHelper.TwoPi / 276f;

            if (CurrentTwinsPhase != 4)
            {
                apolloChargeSpeed += 5f;
                artemisLaserReleaseRate -= 12;
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

            // Have Artemis attempt to do a horizontal sweep while releasing lasers in bursts. This only happens after Ares has released the laserbeams.
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
                    ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 30f, 75f);

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
                            Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 850f, -500f);
                            Vector2 chargeVelocity = Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X) * artemisChargeSpeed;
                            ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 20f, 60f);

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

                            if (generalAttackTimer % artemisLaserReleaseRate == artemisLaserReleaseRate - 1f && !npc.WithinRange(target.Center, 270f))
                            {
                                Main.PlaySound(InfernumMode.CalamityMod.GetSoundSlot(SoundType.Item, "Sounds/Item/LaserCannon"), npc.Center);

                                if (Main.netMode != NetmodeID.MultiplayerClient)
                                {
                                    float offsetAngle = Main.rand.NextFloat(MathHelper.Pi / artemisLaserBurstCount);
                                    for (int i = 0; i < artemisLaserBurstCount; i++)
                                    {
                                        Vector2 aimDestination = npc.Center + (MathHelper.TwoPi * i / artemisLaserBurstCount + offsetAngle).ToRotationVector2() * 1500f;
                                        Vector2 laserShootVelocity = npc.SafeDirectionTo(aimDestination) * 7.25f;
                                        int laser = Utilities.NewProjectileBetter(npc.Center, laserShootVelocity, ModContent.ProjectileType<ArtemisLaserInfernum>(), 500, 0f);
                                        if (Main.projectile.IndexInRange(laser))
                                        {
                                            Main.projectile[laser].ModProjectile<ArtemisLaserInfernum>().InitialDestination = aimDestination + laserShootVelocity.SafeNormalize(Vector2.UnitY) * 1600f;
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
                frame = (int)Math.Round(MathHelper.Lerp(10f, 19f, (float)npc.frameCounter / 36f % 1f));
                if (attackSubstate >= 1f)
                    frame += 10f;
                if (ExoTwinsAreInSecondPhase)
                    frame += 60f;
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
                                Main.PlaySound(InfernumMode.CalamityMod.GetSoundSlot(SoundType.Item, "Sounds/Item/ELRFire"), target.Center);
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
                                    Main.PlaySound(SoundID.Item36, target.Center);

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
                if (wrappedAttackTimer > redirectTime + chargeupTime + laserTelegraphTime - 90f)
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

            // Have Ares pick a point to move to, move there, charge up, and release two lasers that spin around
            // and force smart movement when dealing with Artemis and Apollo's fast charges, spins, and other complex movements.
            if (npc.type == ModContent.NPCType<AresBody>())
            {
                ref float hoverDestinationX = ref npc.Infernum().ExtraAI[0];
                ref float hoverDestinationY = ref npc.Infernum().ExtraAI[1];
                ref float laserRotationalOffset = ref npc.Infernum().ExtraAI[2];
                ref float laserDirection = ref npc.Infernum().ExtraAI[3];
                ref float hasInitialized = ref npc.Infernum().ExtraAI[4];

                Vector2 hoverDestination = new Vector2(hoverDestinationX, hoverDestinationY);

                // Define a hover destination if one is yet to be initialized.
                if (hoverDestinationX == 0f || hoverDestinationY == 0f || wrappedAttackTimer == 1f)
                {
                    laserDirection = 0f;
                    laserRotationalOffset = 0f;

                    int tries = 0;
                    do
                    {
                        hoverDestination = target.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(500f, 900f);
                        tries++;

                        if (tries >= 1000)
                            break;
                    }
                    while (npc.WithinRange(hoverDestination, 700f) || Collision.SolidCollision(hoverDestination - Vector2.One * 200f, 400, 400));

                    if (hasInitialized == 0f)
                    {
                        hoverDestination = target.Center - Vector2.UnitY * 450f;
                        hasInitialized = 1f;
                    }

                    hoverDestinationX = hoverDestination.X;
                    hoverDestinationY = hoverDestination.Y;
                    npc.netUpdate = true;
                }
                else if (wrappedAttackTimer < redirectTime)
                    ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 25f, 50f);

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
                    Main.PlaySound(InfernumMode.CalamityMod.GetSoundSlot(SoundType.Item, "Sounds/Item/TeslaCannonFire"), target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        laserDirection = Main.rand.NextBool().ToDirectionInt();

                        int type = ModContent.ProjectileType<AresSpinningRedDeathray>();
                        int beam = Utilities.NewProjectileBetter(npc.Center, Vector2.UnitY, type, 960, 0f);
                        if (Main.projectile.IndexInRange(beam))
                            Main.projectile[beam].ai[1] = npc.whoAmI;
                        beam = Utilities.NewProjectileBetter(npc.Center, -Vector2.UnitY, type, 960, 0f);
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

        public static bool DoBehavior_AresTwins_CircleAttack(NPC npc, Player target, ref float attackTimer, ref float frame)
        {
            int attackDelay = 120;
            int normalTwinsAttackTime = 360;
            int totalNormalShotCount = 11;
            float normalShotShootSpeed = 7.25f;

            if (CurrentTwinsPhase != 4)
            {
                normalShotShootSpeed += 3f;
                totalNormalShotCount += 3;
            }

            if (EnrageTimer > 0f)
            {
                normalShotShootSpeed += 6f;
                totalNormalShotCount += 3;
            }

            int normalShotShootRate = normalTwinsAttackTime / totalNormalShotCount;

            // Inherit the attack timer from the initial mech.
            attackTimer = FindInitialMech()?.ai[1] ?? attackTimer;

            // Hover over the target.
            // The plasma and tesla arms will do the attacking.
            if (npc.type == ModContent.NPCType<AresBody>())
            {
                ref float canFire = ref npc.Infernum().ExtraAI[0];

                // Decide whether to fire or not.
                canFire = (attackTimer > attackDelay).ToInt();

                Vector2 hoverDestination = target.Center - Vector2.UnitY * 485f;
                ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 24f, 75f);

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
                    float hoverOffsetAngle = MathHelper.TwoPi * normalShotCounter / totalNormalShotCount - MathHelper.PiOver2;
                    if (npc.type == ModContent.NPCType<Artemis>())
                    {
                        normalShotCounter = Main.npc[CalamityGlobalNPC.draedonExoMechTwinGreen].Infernum().ExtraAI[0];
                        hoverOffsetAngle += MathHelper.Pi;
                    }

                    Vector2 hoverDestination = target.Center + hoverOffsetAngle.ToRotationVector2() * new Vector2(800f, 575f);
                    ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 40f, 95f);

                    Vector2 aimDestination = target.Center + target.velocity * 11.5f;
                    Vector2 aimDirection = npc.SafeDirectionTo(aimDestination);
                    npc.rotation = aimDirection.ToRotation() + MathHelper.PiOver2;

                    if (attackTimer % normalShotShootRate == normalShotShootRate - 1f && attackTimer >= attackDelay)
                    {
                        if (npc.type == ModContent.NPCType<Apollo>())
                        {
                            Main.PlaySound(InfernumMode.CalamityMod.GetSoundSlot(SoundType.Item, "Sounds/Item/PlasmaCasterFire"), npc.Center);

                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Vector2 plasmaShootVelocity = aimDirection * normalShotShootSpeed;
                                int plasma = Utilities.NewProjectileBetter(npc.Center + aimDirection * 70f, plasmaShootVelocity, ModContent.ProjectileType<ApolloPlasmaFireball>(), 500, 0f);
                                if (Main.projectile.IndexInRange(plasma))
                                    Main.projectile[plasma].ai[0] = Main.rand.NextBool().ToDirectionInt();
                            }
                        }
                        else
                        {
                            Main.PlaySound(InfernumMode.CalamityMod.GetSoundSlot(SoundType.Item, "Sounds/Item/LaserCannon"), npc.Center);

                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Vector2 laserShootVelocity = aimDirection * normalShotShootSpeed;
                                int laser = Utilities.NewProjectileBetter(npc.Center + aimDirection * 70f, laserShootVelocity, ModContent.ProjectileType<ArtemisLaserInfernum>(), 500, 0f);
                                if (Main.projectile.IndexInRange(laser))
                                {
                                    Main.projectile[laser].ModProjectile<ArtemisLaserInfernum>().InitialDestination = aimDestination;
                                    Main.projectile[laser].ai[1] = npc.whoAmI;
                                    Main.projectile[laser].netUpdate = true;
                                }
                            }
                        }

                        normalShotCounter++;
                        npc.netUpdate = true;
                    }

                    npc.frameCounter++;
                    frame = (int)Math.Round(MathHelper.Lerp(10f, 19f, (float)npc.frameCounter / 36f % 1f));
                    if (ExoTwinsAreInSecondPhase)
                        frame += 60f;
                }
            }
            return attackTimer > attackDelay + normalTwinsAttackTime;
        }
    }
}
