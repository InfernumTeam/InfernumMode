using CalamityMod;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.AttemptRecording;
using InfernumMode.Common.Worldgen;
using InfernumMode.Content.Credits;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using GreatSandSharkNPC = CalamityMod.NPCs.GreatSandShark.GreatSandShark;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.GreatSandShark
{
    public enum BereftVassalFightState
    {
        StartOfFightBereftVassal,
        BereftVassalAndGSS,
        EnragedBereftVassal
    }

    public enum BereftVassalComboAttackType
    {
        ParabolicLeaps = 100,
        HorizontalChargesAndLightningSpears,
        PerpendicularSandBursts,
        MantisLordCharges,
        SandstormBulletHell
    }

    public static class BereftVassalComboAttackManager
    {
        public static int SparkDamage => 185;

        public static int WaterSpearDamage => 190;

        public static int SandBlastDamage => 190;

        public static int SandBlobDamage => 190;

        public static int WaveDamage => 190;

        public static int DustDevilDamage => 195;

        public static int LightningDamage => 205;

        public static int SpearDamage => 215;

        public static int WaterSliceDamage => 230;

        public static int WaterBeamDamage => 275;

        public static int WaterTorrentDamage => 275;

        public static int PressureSandnadoDamage => 300;

        public static BereftVassalFightState FightState
        {
            get
            {
                int vassalIndex = NPC.FindFirstNPC(ModContent.NPCType<BereftVassal>());
                int gssIndex = NPC.FindFirstNPC(ModContent.NPCType<GreatSandSharkNPC>());
                NPC gss = gssIndex >= 0 ? Main.npc[gssIndex] : null;
                NPC vassal = vassalIndex >= 0 ? Main.npc[vassalIndex] : null;

                if (gss is null && vassal is not null && vassal.ModNPC<BereftVassal>().HasBegunSummoningGSS)
                    return BereftVassalFightState.EnragedBereftVassal;

                if (gss is not null)
                    return BereftVassalFightState.BereftVassalAndGSS;

                return BereftVassalFightState.StartOfFightBereftVassal;
            }
        }

        public static NPC Vassal => Main.npc[NPC.FindFirstNPC(ModContent.NPCType<BereftVassal>())];

        public static NPC GreatSandShark => Main.npc[NPC.FindFirstNPC(ModContent.NPCType<GreatSandSharkNPC>())];

        public static void InheritAttributesFromLeader(NPC npc)
        {
            // Inherit the attack state and timer. Also sync if the leader decides to.
            npc.ai[0] = Vassal.ai[0];
            npc.ai[1] = Vassal.ai[1];
            npc.target = Vassal.target;
            if (Vassal.netUpdate)
                npc.netUpdate = true;
        }

        public static void DoComboAttacksIfNecessary(NPC npc, Player target, ref float attackTimer)
        {
            if (FightState != BereftVassalFightState.BereftVassalAndGSS || NPC.FindFirstNPC(ModContent.NPCType<BereftVassal>()) < 0)
                return;

            // Ensure that a combo attack is always used.
            if (npc.ai[0] < 100f)
                SelectNextComboAttack(npc);

            switch ((int)npc.ai[0])
            {
                case (int)BereftVassalComboAttackType.ParabolicLeaps:
                    DoBehavior_ParabolicLeaps(npc, target, ref attackTimer);
                    break;
                case (int)BereftVassalComboAttackType.HorizontalChargesAndLightningSpears:
                    DoBehavior_HorizontalChargesAndLightningSpears(npc, target, ref attackTimer);
                    break;
                case (int)BereftVassalComboAttackType.PerpendicularSandBursts:
                    DoBehavior_PerpendicularSandBursts(npc, target, ref attackTimer);
                    break;
                case (int)BereftVassalComboAttackType.MantisLordCharges:
                    DoBehavior_MantisLordCharges(npc, target, ref attackTimer);
                    break;
                case (int)BereftVassalComboAttackType.SandstormBulletHell:
                    DoBehavior_SandstormBulletHell(npc, target, ref attackTimer);
                    break;
            }
        }

        public static void DoBehavior_ParabolicLeaps(NPC npc, Player target, ref float attackTimer)
        {
            int leapTime = 84;
            int sandVomitCount = 9;
            int leapCount = 3;
            int waterSpearReleaseRate = 5;
            float hoverVerticalOffset = 336f;
            float hoverRedirectSpeed = 22f;
            float hoverRedirectAcceleration = 0.8f;
            float leapHorizontalSpeed = 29f;
            float leapVerticalSpeed = 30f;
            float leapGravity = 0.65f;
            ref float hasLeapedYet = ref Vassal.Infernum().ExtraAI[0];
            ref float leapCounter = ref Vassal.Infernum().ExtraAI[1];
            ref float generalTimer = ref Vassal.Infernum().ExtraAI[2];
            ref float spearRotation = ref Vassal.ModNPC<BereftVassal>().SpearRotation;
            ref float spearOpacity = ref Vassal.ModNPC<BereftVassal>().SpearOpacity;

            // The bereft vassal rides atop the great sand shark at all times, releasing water spears into the air.
            if (npc.type == ModContent.NPCType<BereftVassal>())
            {
                // Ride the great sand shark.
                if (generalTimer >= 20f)
                    RideGreatSandShark(npc);
                generalTimer++;

                // Reset water spears into the air.
                if (hasLeapedYet == 1f && attackTimer % waterSpearReleaseRate == waterSpearReleaseRate - 1f && !Collision.SolidCollision(npc.TopLeft, npc.width, npc.height))
                {
                    SoundEngine.PlaySound(SoundID.AbigailAttack, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 shootPosition = npc.Center + (spearRotation - PiOver4).ToRotationVector2() * 12f;
                        Vector2 spearShootVelocity = new(npc.spriteDirection * -4f, -9f);

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(spear =>
                        {
                            spear.ModProjectile<WaterSpear>().StartingYPosition = target.Bottom.Y;
                        });
                        Utilities.NewProjectileBetter(shootPosition, spearShootVelocity, ModContent.ProjectileType<WaterSpear>(), WaterSpearDamage, 0f);
                    }
                }

                // Adjust the spear direction and rotation.
                if (hasLeapedYet == 1f)
                {
                    spearOpacity = Utils.GetLerpValue(0f, 20f, attackTimer) * Utils.GetLerpValue(leapTime, leapTime - 12f, attackTimer, true);
                    spearRotation = GreatSandShark.velocity.ToRotation() + PiOver4;
                }

                // Transition to the next attack once enough time has passed and within blocks.
                if (hasLeapedYet == 1f && attackTimer >= leapTime && npc.Center.Y >= target.Center.Y + 600f)
                {
                    GreatSandShark.velocity *= 0.3f;
                    leapCounter++;
                    hasLeapedYet = 0f;

                    if (leapCounter >= leapCount)
                        SelectNextComboAttack(npc);
                }

                return;
            }

            if (npc.Hitbox.Intersects(Vassal.Hitbox))
                generalTimer = 20f;

            if (generalTimer < 20f)
            {
                npc.velocity = -Vector2.UnitY * 19f;
                return;
            }

            // The great sand shark hovers to the bottom left/right of the target in anticipation of a leap.
            if (hasLeapedYet == 0f)
            {
                attackTimer = 0f;

                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 900f, hoverVerticalOffset);
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverRedirectSpeed, hoverRedirectAcceleration);
                npc.rotation = npc.velocity.X * 0.012f;
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

                // Begin the leap and have the sand shark vomit sand once sufficiently close to the hover destination.
                if (npc.WithinRange(hoverDestination, 56f))
                {
                    // Release the sand vomit.
                    SoundEngine.PlaySound(InfernumSoundRegistry.GreatSandSharkSuddenRoarSound, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < sandVomitCount; i++)
                        {
                            Vector2 sandVelocity = new(npc.spriteDirection * -Lerp(4f, 15f, i / (float)(sandVomitCount - 1f)), -16f - i * 1.36f);
                            Vector2 mouthPosition = npc.Center + npc.velocity.SafeNormalize(Vector2.UnitX * -npc.spriteDirection) * 108f;
                            sandVelocity += Main.rand.NextVector2Circular(0.5f, 0.5f);

                            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(blob =>
                            {
                                blob.ModProjectile<SandBlob>().StartingYPosition = target.Bottom.Y + 300f;
                            });
                            Utilities.NewProjectileBetter(mouthPosition, sandVelocity, ModContent.ProjectileType<SandBlob>(), SandBlobDamage, 0f);
                        }
                    }
                    CreditManager.StartRecordingFootageForCredits(ScreenCapturer.RecordingBoss.Vassal);

                    hasLeapedYet = 1f;
                    npc.velocity = new Vector2(npc.spriteDirection * -leapHorizontalSpeed, -leapVerticalSpeed);
                    npc.netUpdate = true;
                }
                return;
            }

            npc.velocity.X *= 0.99f;
            npc.rotation = npc.velocity.ToRotation();
            if (npc.spriteDirection == 1)
                npc.rotation += Pi;
            npc.velocity.Y += leapGravity;
        }

        public static void DoBehavior_HorizontalChargesAndLightningSpears(NPC npc, Player target, ref float attackTimer)
        {
            int chargeCount = 5;
            int chargeDelay = 23;
            int chargeTime = 26;
            int spearReleaseRate = 40;
            float chargeSpeed = 30f;
            float maxChargeSpeed = 50f;
            float hoverRedirectSpeed = 25f;
            float hoverRedirectAcceleration = 0.975f;
            float chargeHoverOffset = 540f;
            float spearShootSpeed = 13.5f;
            ref float hasReachedChargeDestination = ref Vassal.Infernum().ExtraAI[0];
            ref float chargeCounter = ref Vassal.Infernum().ExtraAI[1];
            ref float spearFadeBuffer = ref Vassal.Infernum().ExtraAI[2];
            ref float spearShootTimer = ref Vassal.Infernum().ExtraAI[3];
            ref float spearRotation = ref Vassal.ModNPC<BereftVassal>().SpearRotation;
            ref float spearOpacity = ref Vassal.ModNPC<BereftVassal>().SpearOpacity;

            // The bereft vassal rides atop the great sand shark at all times, shooting their spear at the ground.
            if (npc.type == ModContent.NPCType<BereftVassal>())
            {
                // Ride the great sand shark.
                RideGreatSandShark(npc);

                // Aim the spear at the target.
                spearRotation = npc.AngleTo(target.Center) + PiOver4;
                if (spearFadeBuffer > 0f)
                {
                    spearOpacity = 0f;
                    spearFadeBuffer--;
                }
                else
                    spearOpacity = Clamp(spearOpacity + 0.036f, 0f, 1f);

                // Release tridents. After enough time has passed, they will release lightning from the sky.
                spearShootTimer++;
                if (spearShootTimer >= spearReleaseRate && chargeCounter < chargeCount - 1f && !npc.WithinRange(target.Center, 336f) && !Collision.SolidCollision(npc.TopLeft, npc.width, npc.height))
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.MyrindaelThrowSound, npc.Center);

                    // Make the spear disappear the frame after it's fired.
                    spearFadeBuffer = 19f;
                    spearOpacity = 0f;
                    spearShootTimer = 0f;
                    npc.netUpdate = true;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(npc.Center, npc.SafeDirectionTo(target.Center) * spearShootSpeed, ModContent.ProjectileType<BereftVassalSpear>(), SpearDamage, 0f);
                }

                if (attackTimer >= chargeDelay + chargeTime)
                {
                    attackTimer = 0f;
                    chargeCounter++;
                    hasReachedChargeDestination = 0f;
                    if (chargeCounter >= chargeCount)
                    {
                        Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<BereftVassalSpear>());
                        SelectNextComboAttack(npc);
                    }
                    npc.netUpdate = true;
                }

                return;
            }

            // The ground sand shark charges horizontally at the target.
            // Hover into position.
            if (hasReachedChargeDestination == 0f)
            {
                // Hover into position and slow down once ready to charge.
                Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * chargeHoverOffset;
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverRedirectSpeed, hoverRedirectAcceleration);
                npc.rotation = npc.velocity.X * 0.012f;
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                if (npc.WithinRange(hoverDestination, 60f))
                {
                    hasReachedChargeDestination = 1f;
                    npc.velocity *= 0.4f;
                    npc.netUpdate = true;
                }

                // Disable natural time progression and contact damage.
                Vassal.damage = 0;
                npc.damage = 0;
                attackTimer = 0f;
                return;
            }

            // Prepare for the charge.
            if (attackTimer < chargeDelay)
            {
                npc.velocity *= 0.92f;
                npc.rotation = npc.velocity.X * 0.012f;

                // Roar before charging.
                if (attackTimer == chargeDelay - 25)
                    SoundEngine.PlaySound(InfernumSoundRegistry.GreatSandSharkChargeRoarSound, npc.Center);
            }

            // Accelerate after the charge has happened.
            else if (npc.velocity.Length() < maxChargeSpeed)
                npc.velocity *= 1.03f;

            // Do the charge.
            if (attackTimer == chargeDelay)
            {
                SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown, npc.Center);
                npc.velocity = Vector2.UnitX * (target.Center.X > npc.Center.X).ToDirectionInt() * chargeSpeed;
            }
        }

        public static void DoBehavior_PerpendicularSandBursts(NPC npc, Player target, ref float attackTimer)
        {
            int sandBlastReleaseRate = 10;
            int waterSpearReleaseRate = 7;
            int sandBlobCount = 20;
            int attackTransitionDelay = 160;
            float hoverRedirectSpeed = 24f;
            float hoverRedirectAcceleration = 0.96f;
            float leapVerticalSpeed = 18f;
            float hoverVerticalOffset = 250f;
            float sandSpeed = 14.5f;
            float sandBlobAngularArea = 0.73f;
            float sandBlobSpeed = 22f;
            ref float hasLeapedYet = ref Vassal.Infernum().ExtraAI[2];
            ref float hasGoneFarUpEnough = ref Vassal.Infernum().ExtraAI[3];
            ref float hasSlammedIntoGround = ref Vassal.Infernum().ExtraAI[4];
            ref float spearRotation = ref Vassal.ModNPC<BereftVassal>().SpearRotation;
            ref float spearOpacity = ref Vassal.ModNPC<BereftVassal>().SpearOpacity;

            // The bereft vassal rides atop the great sand shark at all times, releasing water spears into the air.
            if (npc.type == ModContent.NPCType<BereftVassal>())
            {
                // Ride the great sand shark.
                if (hasGoneFarUpEnough == 0f)
                    RideGreatSandShark(npc);
                else
                {
                    npc.rotation = 0f;
                    npc.ModNPC<BereftVassal>().DoBehavior_LongHorizontalCharges();
                }

                // Release water spears downward while the great sand shark is releasing sand blasts.
                if (hasLeapedYet == 1f && attackTimer % waterSpearReleaseRate == waterSpearReleaseRate - 1f && !Collision.SolidCollision(npc.TopLeft, npc.width, npc.height) && hasGoneFarUpEnough == 0f)
                {
                    SoundEngine.PlaySound(SoundID.AbigailAttack, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 shootPosition = npc.Center + (spearRotation - PiOver4).ToRotationVector2() * 12f;
                        Vector2 spearShootVelocity = new(npc.spriteDirection * -Main.rand.NextFloat(4f, 11f), -9f);

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(spear =>
                        {
                            spear.ModProjectile<WaterSpear>().StartingYPosition = target.Bottom.Y;
                        });
                        Utilities.NewProjectileBetter(shootPosition, spearShootVelocity, ModContent.ProjectileType<WaterSpear>(), WaterSpearDamage, 0f);
                    }
                }

                // Switch attacks after enough time has passed from the slam.
                if (attackTimer >= attackTransitionDelay && hasSlammedIntoGround == 1f)
                    SelectNextComboAttack(npc);

                return;
            }

            // The great sand shark hovers to the bottom left/right of the target in anticipation of a leap.
            if (hasLeapedYet == 0f)
            {
                attackTimer = 0f;

                // Disable contact damage.
                npc.damage = 0;

                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 600f, hoverVerticalOffset);
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverRedirectSpeed, hoverRedirectAcceleration);
                npc.rotation = npc.velocity.X * 0.012f;
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

                // Create a rumble where GSS is.
                if (WorldUtils.Find(npc.Center.ToTileCoordinates(), Searches.Chain(new Searches.Up(400), new CustomTileConditions.IsAir()), out Point result))
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(8))
                        Utilities.NewProjectileBetter(result.ToWorldCoordinates(), Vector2.UnitY * -4f, ProjectileID.DD2OgreSmash, 0, 0f);
                }

                // Begin the leap once sufficiently close to the hover destination.
                if (npc.WithinRange(hoverDestination, 56f))
                {
                    hasLeapedYet = 1f;
                    npc.netUpdate = true;
                }
                return;
            }
            else if (hasGoneFarUpEnough == 0f)
                npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * leapVerticalSpeed, 0.08f);

            // Release sand blasts.
            if (attackTimer % sandBlastReleaseRate == sandBlastReleaseRate - 1f && hasGoneFarUpEnough == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item89, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Utilities.NewProjectileBetter(npc.Center, -Vector2.UnitX * sandSpeed, ModContent.ProjectileType<GreatSandBlast>(), SandBlastDamage, 0f);
                    Utilities.NewProjectileBetter(npc.Center, Vector2.UnitX * sandSpeed, ModContent.ProjectileType<GreatSandBlast>(), SandBlastDamage, 0f);
                }

                // Fall back down once high enough.
                if (npc.Center.Y < target.Center.Y - 900f)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.GreatSandSharkMiscRoarSound, npc.Center);
                    npc.Center = target.Center - Vector2.UnitY * 900f;
                    hasGoneFarUpEnough = 1f;
                    npc.netUpdate = true;
                }
            }

            // Fall back down again once ready.
            if (hasGoneFarUpEnough == 1f)
            {
                // Disable contact damage to prevent cheap hits when falling back down.
                npc.damage = 0;

                if (hasSlammedIntoGround == 0f)
                {
                    npc.velocity.X = Lerp(npc.velocity.X, npc.SafeDirectionTo(target.Center).X * 13f, 0.05f);
                    npc.velocity.Y = Clamp(npc.velocity.Y + 0.9f, -18f, 31f);
                }

                if (hasSlammedIntoGround == 0f && Collision.SolidCollision(npc.TopLeft, npc.width, npc.height))
                {
                    SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, npc.Center);

                    // Slam into the ground and release sand blobs into the air if not too close to the target.
                    if (Main.netMode != NetmodeID.MultiplayerClient && !npc.WithinRange(target.Center, 350f))
                    {
                        for (int i = 0; i < sandBlobCount; i++)
                        {
                            float sandVelocityOffsetAngle = Lerp(-sandBlobAngularArea, sandBlobAngularArea, i / (float)(sandBlobCount - 1f));

                            // Add a small amount of variance to the sane velocity, to make it require a bit of dynamic reaction.
                            sandVelocityOffsetAngle += Main.rand.NextFloatDirection() * 0.11f;

                            Vector2 sandVelocity = -Vector2.UnitY.RotatedBy(sandVelocityOffsetAngle) * sandBlobSpeed;
                            Vector2 sandSpawnPosition = npc.Center + new Vector2(Main.rand.NextFloatDirection() * 6f, Main.rand.NextFloat(12f));

                            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(blob =>
                            {
                                blob.ModProjectile<SandBlob>().StartingYPosition = target.Bottom.Y;
                            });
                            Utilities.NewProjectileBetter(sandSpawnPosition, sandVelocity, ModContent.ProjectileType<SandBlob>(), SandBlobDamage, 0f);
                        }

                        Utilities.NewProjectileBetter(npc.Bottom, Vector2.UnitX * npc.spriteDirection * 8f, ProjectileID.DD2OgreSmash, 0, 0f);
                    }

                    attackTimer = 0f;
                    hasSlammedIntoGround = 1f;
                    npc.velocity *= 0.3f;
                    npc.netUpdate = true;
                }
            }

            // Rotate.
            npc.velocity.X *= 0.99f;
            npc.rotation = npc.velocity.ToRotation();
            if (npc.spriteDirection == 1)
                npc.rotation += Pi;
        }

        public static void DoBehavior_MantisLordCharges(NPC npc, Player target, ref float attackTimer)
        {
            int attakCycleCount = 3;
            int attackCycleTime = 266;
            int sharkChargeDelay = 18;
            int sharkChargeTime = 23;
            int vassalChargeDelay = 24;
            float vassalChargeSpeed = 43f;
            float wrappedAttackTimer = attackTimer % attackCycleTime;
            float hoverVerticalOffset = 240f;
            float hoverRedirectSpeed = 21f;
            float hoverRedirectAcceleration = 0.7f;
            float sharkChargeSpeed = 24f;
            float sharkMaxChargeSpeed = 35f;
            bool vassalShouldAttack = wrappedAttackTimer < attackCycleTime / 2;
            bool sharkShouldAttack = !vassalShouldAttack;
            ref float groundHitTimer = ref Vassal.Infernum().ExtraAI[0];
            ref float hasReachedChargeDestination = ref Vassal.Infernum().ExtraAI[1];
            ref float sharkAttackTimer = ref Vassal.Infernum().ExtraAI[2];
            ref float spearRotation = ref Vassal.ModNPC<BereftVassal>().SpearRotation;
            ref float spearOpacity = ref Vassal.ModNPC<BereftVassal>().SpearOpacity;
            ref float telegraphDirection = ref Vassal.ModNPC<BereftVassal>().LineTelegraphDirection;
            ref float telegraphIntensity = ref Vassal.ModNPC<BereftVassal>().LineTelegraphIntensity;

            // The bereft vassal performs a series of vertical dashes, while the great sand shark performs horizontal dashes, similar to the mantis lords.
            // It, like the great sand shark, waits its turn to attack, however.
            if (npc.type == ModContent.NPCType<BereftVassal>())
            {
                if (vassalShouldAttack)
                {
                    // Teleport above the player before charging downward.
                    float chargeTimer = wrappedAttackTimer % (vassalChargeDelay + 42f);
                    if (chargeTimer == 1f)
                        npc.ModNPC<BereftVassal>().TeleportToPosition(target.Center - Vector2.UnitY * 350f);

                    // Have the spear fade in.
                    if (chargeTimer <= vassalChargeDelay)
                    {
                        spearRotation = Pi - PiOver4;
                        spearOpacity = chargeTimer / vassalChargeDelay;
                        groundHitTimer = 0f;
                        npc.rotation = 0f;
                        npc.damage = 0;
                        npc.Opacity = 1f;
                        npc.velocity = Vector2.Zero;
                        telegraphDirection = PiOver2;
                        telegraphIntensity = chargeTimer / vassalChargeDelay;

                        // Play a slash sound before falling.
                        if (chargeTimer == vassalChargeDelay)
                        {
                            telegraphIntensity = 0f;
                            SoundEngine.PlaySound(InfernumSoundRegistry.VassalSlashSound, npc.Center);
                        }
                    }
                    else if (groundHitTimer == 0f)
                    {
                        // Create ground hit effects and cease moving once ground has been struck.
                        if (npc.velocity.Y == 0f && chargeTimer >= vassalChargeDelay + 6f)
                        {
                            groundHitTimer = 1f;

                            // Release a spread of waves.
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                for (int i = 0; i < 5; i++)
                                {
                                    float waveSpread = Lerp(-0.17f, 0.17f, i / 4f);
                                    Vector2 waveVelocity = waveSpread.ToRotationVector2() * 11f;
                                    Utilities.NewProjectileBetter(npc.Bottom, waveVelocity, ModContent.ProjectileType<TorrentWave>(), WaveDamage, 0f);
                                    Utilities.NewProjectileBetter(npc.Bottom, -waveVelocity, ModContent.ProjectileType<TorrentWave>(), WaveDamage, 0f);
                                }

                                Utilities.NewProjectileBetter(npc.Bottom, Vector2.UnitX * npc.spriteDirection * 8f, ProjectileID.DD2OgreSmash, 0, 0f);
                            }
                        }
                        npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * vassalChargeSpeed, 0.16f);
                        npc.noGravity = true;
                        npc.ModNPC<BereftVassal>().CreateMotionStreakParticles();
                    }
                    else
                    {
                        groundHitTimer++;

                        // Look at the target.
                        npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                        npc.rotation = 0f;

                        // Teleport out of view after on the ground for long enough.
                        if (groundHitTimer >= 16f)
                        {
                            if (npc.Opacity != 0f)
                            {
                                npc.Center = Vector2.One * 150f;
                                npc.Opacity = 0f;
                                npc.netUpdate = true;
                            }
                        }
                    }
                }
                else if (npc.Opacity != 0f)
                {
                    npc.Center = Vector2.One * 150f;
                    npc.Opacity = 0f;
                    npc.netUpdate = true;
                }

                sharkAttackTimer++;
                if (attackTimer >= attakCycleCount * attackCycleTime)
                {
                    npc.ModNPC<BereftVassal>().TeleportToPosition(target.Center - Vector2.UnitY * 700f);
                    SelectNextComboAttack(npc);
                }

                return;
            }

            if (!sharkShouldAttack)
            {
                sharkAttackTimer = 0f;
                hasReachedChargeDestination = 0f;
                if (npc.velocity.Length() > 16f)
                    npc.velocity *= 0.7f;
            }

            // Hover below the target if not supposed to attack.
            Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 500f, sharkShouldAttack ? -20f : hoverVerticalOffset);
            if (!sharkShouldAttack)
            {
                while (!CalamityUtils.ParanoidTileRetrieval((int)hoverDestination.X / 16, (int)hoverDestination.Y / 16).HasTile)
                    hoverDestination.Y += 64f;
            }

            if (hasReachedChargeDestination == 0f)
            {
                npc.damage = 0;
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * hoverRedirectSpeed;
                npc.SimpleFlyMovement(idealVelocity, hoverRedirectAcceleration);
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.1f);
                npc.rotation = npc.velocity.X * 0.01f;
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                if (sharkAttackTimer >= 12f && npc.WithinRange(hoverDestination, 66f) && wrappedAttackTimer < attackCycleTime - 54f)
                {
                    sharkAttackTimer = 0f;
                    hasReachedChargeDestination = 1f;
                    npc.velocity *= 0.75f;
                    npc.Center = Vector2.Lerp(npc.Center, hoverDestination, 0.56f);
                    npc.netUpdate = true;
                }

                return;
            }

            // Prepare for the charge.
            if (sharkAttackTimer < sharkChargeDelay)
            {
                npc.Center = Vector2.Lerp(npc.Center, hoverDestination, 0.1f);
                npc.velocity *= 0.86f;
                npc.rotation = npc.velocity.X * 0.012f;

                // Roar before charging.
                if (sharkAttackTimer == 1f)
                    SoundEngine.PlaySound(InfernumSoundRegistry.GreatSandSharkChargeRoarSound, npc.Center);
            }

            // Accelerate after the charge has happened.
            else if (npc.velocity.Length() < sharkMaxChargeSpeed)
                npc.velocity *= 1.04f;

            // Do the charge.
            if (sharkAttackTimer == sharkChargeDelay && hasReachedChargeDestination == 1f)
            {
                SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown, npc.Center);
                npc.velocity = Vector2.UnitX * (target.Center.X > npc.Center.X).ToDirectionInt() * sharkChargeSpeed;
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                npc.netUpdate = true;
            }

            if (sharkAttackTimer >= sharkChargeDelay + sharkChargeTime)
            {
                sharkAttackTimer = 0f;
                hasReachedChargeDestination = 0f;
            }
        }

        public static void DoBehavior_SandstormBulletHell(NPC npc, Player target, ref float attackTimer)
        {
            int shootTime = 420;
            int sandstormReleaseRate = 75;
            int myrindaelShootRate = 42;
            float hoverRedirectSpeed = 22f;
            float hoverRedirectAcceleration = 0.4f;
            float hoverVerticalOffset = -50f;
            float spearShootSpeed = 56f;
            ref float dustDevilDirection = ref Vassal.Infernum().ExtraAI[0];
            ref float spearRotation = ref Vassal.ModNPC<BereftVassal>().SpearRotation;
            ref float spearOpacity = ref Vassal.ModNPC<BereftVassal>().SpearOpacity;

            // The bereft vassal rides atop the great sand shark at all times, releasing water waves in bursts.
            if (npc.type == ModContent.NPCType<BereftVassal>())
            {
                // Create a sandstorm.
                for (int i = 0; i < 2; i++)
                    npc.ModNPC<BereftVassal>().CreateSandstormParticle(true);

                // Ride the great sand shark.
                RideGreatSandShark(npc);

                // Switch attacks after enough time has passed from the slam.
                if (attackTimer >= 9999f)
                    SelectNextComboAttack(npc);

                // Release dust devil barrges.
                int dustDevilID = ModContent.ProjectileType<DustDevil>();
                if (attackTimer % sandstormReleaseRate == sandstormReleaseRate - 1f && attackTimer < shootTime - 90f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int i = 0;
                        for (float dx = -700f; dx < 700f; dx += 174f)
                        {
                            Vector2 dustDevilSpawnPosition = target.Center + new Vector2(dx, -785f - (dx + 700f) * (dustDevilDirection == 1f).ToDirectionInt() * 0.1f);
                            Vector2 dustDevilVelocity = new((dustDevilDirection == 0f).ToDirectionInt() * 1.1f, 1.9f);
                            Utilities.NewProjectileBetter(dustDevilSpawnPosition, dustDevilVelocity, dustDevilID, DustDevilDamage, 0f, -1, i % 2f);

                            i++;
                        }
                        dustDevilDirection = (dustDevilDirection + 1f) % 2f;
                        npc.netUpdate = true;
                    }
                }

                // Look at a dust devil to target.
                Vector2 spearDirection = Vector2.UnitY;
                float idealRotation = npc.AngleTo(target.Center) + PiOver4;

                // Pick a dust devil that is near the target (but not too close) and above them to shoot at.
                Projectile[] dustDevils = Utilities.AllProjectilesByID(dustDevilID).OrderBy(p =>
                {
                    float score = p.Distance(target.Center);
                    if (score < 400f)
                        score += 9000f;
                    if (p.Center.Y > target.Center.Y - 336f)
                        score += 9000f;

                    return score;
                }).ToArray();

                // Constantly aim at the best devil.
                if (dustDevils.Length >= 1)
                {
                    spearDirection = CalamityUtils.CalculatePredictiveAimToTarget(npc.Center, dustDevils[0], spearShootSpeed, 8).SafeNormalize(Vector2.UnitY);
                    idealRotation = spearDirection.ToRotation() + PiOver4;
                    dustDevils[0].Infernum().ExtraAI[0] = 1;
                    for (int i = 1; i < dustDevils.Length; i++)
                        dustDevils[i].Infernum().ExtraAI[0] = 0;
                }
                spearRotation = spearRotation.AngleLerp(idealRotation, 0.15f);
                spearOpacity = Clamp(spearOpacity + 0.04f, 0f, 1f);

                // Sometimes shoot spears at a random dust devil near the target.
                if (attackTimer % myrindaelShootRate == myrindaelShootRate - 1f && dustDevils.Length >= 1)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.MyrindaelThrowSound, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 dustDevilVelocity = spearDirection * spearShootSpeed;
                        Utilities.NewProjectileBetter(npc.Center, dustDevilVelocity, ModContent.ProjectileType<BereftVassalSpear>(), 0, 0f);
                    }
                    spearOpacity = 0f;
                }

                if (attackTimer >= shootTime)
                {
                    Utilities.DeleteAllProjectiles(false, dustDevilID);
                    SelectNextComboAttack(npc);
                }

                return;
            }

            // Disable contact damage.
            npc.damage = 0;

            // The great sand shark hovers to the sides of the target with a slight vertical offset.
            Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 450f, hoverVerticalOffset);
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverRedirectSpeed, hoverRedirectAcceleration);
            npc.rotation = npc.velocity.X * 0.012f;
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
        }

        public static void RideGreatSandShark(NPC npc)
        {
            Vector2 gssDirection = GreatSandShark.rotation.ToRotationVector2() * -GreatSandShark.spriteDirection;
            Vector2 gssTop = GreatSandShark.Center + gssDirection * 76f + gssDirection.RotatedBy(PiOver2) * GreatSandShark.spriteDirection * 42f;

            npc.spriteDirection = GreatSandShark.spriteDirection;

            if (!npc.WithinRange(gssTop, 400f))
            {
                npc.ModNPC<BereftVassal>().TeleportToPosition(gssTop, false);
                npc.Opacity = 1f;
            }
            npc.Center = gssTop;
            npc.velocity = Vector2.Zero;
            npc.rotation = GreatSandShark.rotation;
            npc.noGravity = true;
            npc.noTileCollide = true;
        }

        public static void SelectNextComboAttack(NPC npc)
        {
            if (FightState != BereftVassalFightState.BereftVassalAndGSS || npc != Vassal)
                return;

            npc.Opacity = 1f;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            switch ((int)(npc.Infernum().ExtraAI[8] % 5))
            {
                case 0:
                    npc.ai[0] = (int)BereftVassalComboAttackType.ParabolicLeaps;
                    break;
                case 1:
                    npc.ai[0] = (int)BereftVassalComboAttackType.HorizontalChargesAndLightningSpears;
                    break;
                case 2:
                    npc.ai[0] = (int)BereftVassalComboAttackType.PerpendicularSandBursts;
                    break;
                case 3:
                    npc.ai[0] = (int)BereftVassalComboAttackType.MantisLordCharges;
                    break;
                case 4:
                    npc.ai[0] = (int)BereftVassalComboAttackType.SandstormBulletHell;
                    break;
            }
            npc.Infernum().ExtraAI[8]++;
            npc.netUpdate = true;
        }
    }
}
