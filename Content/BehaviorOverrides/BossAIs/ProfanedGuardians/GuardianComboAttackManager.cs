using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.Particles;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics.Metaballs;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Common.Graphics.ScreenEffects;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Providence;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Content.Projectiles.Wayfinder;
using InfernumMode.Core.GlobalInstances;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public static partial class GuardianComboAttackManager
    {
        #region Commander + Defender + Healer Attacks
        public static void DoBehavior_SpawnEffects(NPC npc, Player target, ref float attackTimer)
        {
            ref float lastOffsetY = ref npc.Infernum().ExtraAI[0];

            float wallCreationRate = 90f;
            int wallsToSimulate = 7;

            // Do not take or deal damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            // If we are the commander, set a flash etc. The guardians are spawned in at the correct position so will move on correctly.
            if (npc.type == CommanderType)
            {
                if (attackTimer == 1)
                {
                    ScreenEffectSystem.SetFlashEffect(target.Center, 0.8f, 45);
                    SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceBurnSound, target.Center);

                    // Create a bunch of pre-existing fire walls. This is so the player doesn't either sit around dawdling for
                    // ages waiting for them to cover the garden, or more likely, move right and negate half the phase.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        // The base velocity of the walls.
                        Vector2 velocity = -Vector2.UnitX * 8f;
                        // The distance between each wall.
                        float gapBetweenWalls = velocity.X * wallCreationRate;

                        // Loop through every wall to create.
                        for (int i = 0; i < wallsToSimulate; i++)
                        {
                            // Get the base center the same way as normal, but modify the x position by the wall we are using times the gap size.
                            Vector2 baseCenter = CrystalPosition + new Vector2(220f + (gapBetweenWalls * i), 0f);

                            // This is the same as making the normal walls.

                            // -->
                            // Create a random offset.
                            float yRandomOffset;
                            Vector2 previousCenter = baseCenter + new Vector2(0f, lastOffsetY);
                            Vector2 newCenter;
                            int attempts = 0;
                            // Attempt to get one within a certain distance, but give up after 30 attempts.
                            do
                            {
                                yRandomOffset = Main.rand.NextFloat(-600f, 200f);
                                newCenter = baseCenter + new Vector2(0f, yRandomOffset);
                                attempts++;
                            }
                            while (newCenter.Distance(previousCenter) > 400f || attempts < 30);

                            // Set the new random offset as the last one.
                            lastOffsetY = yRandomOffset;
                            // <--

                            // Modify the lifetime of the walls so they all end at the same point.
                            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(proj =>
                            {
                                proj.ModProjectile<HolyFireWall>().Lifetime = HolyFireWall.BaseLifetime - (int)(wallCreationRate * i);
                            });
                            // Spawn the wall.
                            Utilities.NewProjectileBetter(newCenter, velocity, ModContent.ProjectileType<HolyFireWall>(), HolyFireBeamDamage, 0);
                        }
                        SelectNewAttack(npc, ref attackTimer);
                    }
                }
            }
        }

        // I cannot take this anymore.
        public static void DoBehavior_FlappyBird(NPC npc, Player target, ref float attackTimer, NPC commander)
        {
            // This attack ends automatically when the crystal wall dies, it advances the attackers attack state, which the other
            // guardians check for and advance with it.

            // The commander bobs on the spot, pausing to aim and fire a fire beam at the player from afar.
            if (npc.type == CommanderType)
            {
                float deathrayFireRate = 235;
                float initialDelay = 120;
                ref float movementTimer = ref npc.Infernum().ExtraAI[0];

                // Do not take damage.
                npc.dontTakeDamage = true;
                npc.spriteDirection = -1;

                // Safely get the crystal. The commander should not attack if it is not present.
                if (Main.npc.IndexInRange(GlobalNPCOverrides.ProfanedCrystal))
                {
                    if (Main.npc[GlobalNPCOverrides.ProfanedCrystal].active)
                    {
                        NPC crystal = Main.npc[GlobalNPCOverrides.ProfanedCrystal];

                        // If time to fire, the target is close enough and the pushback wall is not present.
                        if (attackTimer % deathrayFireRate == 0 && target.WithinRange(npc.Center, 7200f) && attackTimer >= initialDelay && crystal.ai[0] == 0f)
                        {
                            // Fire deathray.
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Vector2 aimDirection = npc.SafeDirectionTo(target.Center);
                                Utilities.NewProjectileBetter(npc.Center, aimDirection, ModContent.ProjectileType<HolyAimedDeathrayTelegraph>(), 0, 0f, -1, 0f, npc.whoAmI);
                            }
                        }
                    }
                }

                if (attackTimer == initialDelay)
                    HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.ProfanedFlappyBirdTip");

                if (attackTimer >= initialDelay && !Main.projectile.Any((Projectile p) => (p.type == ModContent.ProjectileType<HolyAimedDeathrayTelegraph>() || p.type == ModContent.ProjectileType<HolyAimedDeathray>()) && p.active))
                {
                    float sine = Sin(movementTimer * 0.05f);
                    npc.velocity.Y = sine * 1.5f;
                    movementTimer++;
                    npc.velocity.X *= 0.8f;
                }
                else
                    npc.velocity.Y *= 0.97f;
            }

            // The defender summons fire walls that force you to go inbetween the gap.
            else if (npc.type == DefenderType)
            {
                // This is basically flappy bird, the attacker spawns fire walls like the pipes that move towards the entrance of the garden.
                ref float lastOffsetY = ref npc.Infernum().ExtraAI[0];
                ref float movedToPosition = ref npc.Infernum().ExtraAI[1];
                float wallCreationRate = 90f;
                ref float drawFireSuckup = ref npc.ai[2];
                drawFireSuckup = 1;
                ref float fireSuckupWidth = ref commander.Infernum().ExtraAI[DefenderFireSuckupWidthIndex];

                // Do not take damage.
                npc.dontTakeDamage = true;

                // Safely check for the crystal. The defender should stop attacking if it is not present
                if (Main.npc.IndexInRange(GlobalNPCOverrides.ProfanedCrystal))
                {
                    if (Main.npc[GlobalNPCOverrides.ProfanedCrystal].active)
                    {
                        NPC crystal = Main.npc[GlobalNPCOverrides.ProfanedCrystal];
                        // Sit still behind and beneath the crystal.
                        if (npc.Distance(DefenderStartingHoverPosition) > 7f && movedToPosition == 0f)
                            npc.velocity = npc.SafeDirectionTo(DefenderStartingHoverPosition, Vector2.UnitY) * 5f;
                        else
                        {
                            npc.velocity.X = 0f;
                            movedToPosition = 1f;
                            float sine = -Sin(Main.GlobalTimeWrappedHourly * 2f);
                            npc.velocity.Y = sine * 1.5f;
                            npc.spriteDirection = -1;
                        }

                        // Create walls of fire with a random gap in them based off of the last one.
                        if (attackTimer % wallCreationRate == 0f && Main.netMode != NetmodeID.MultiplayerClient && crystal.ai[0] == 0f)
                        {
                            Vector2 velocity = -Vector2.UnitX * 8f;
                            Vector2 baseCenter = CrystalPosition + new Vector2(220f, 0f);
                            // Create a random offset.
                            float yRandomOffset;
                            Vector2 previousCenter = baseCenter + new Vector2(0f, lastOffsetY);
                            Vector2 newCenter;
                            int attempts = 0;
                            // Attempt to get one within a certain distance, but give up after 100 attempts.
                            do
                            {
                                yRandomOffset = Main.rand.NextFloat(-600f, 200f);
                                newCenter = baseCenter + new Vector2(0f, yRandomOffset);
                                attempts++;
                            }
                            while (newCenter.Distance(previousCenter) > 400f || attempts < 100);

                            // Set the new random offset as the last one.
                            lastOffsetY = yRandomOffset;
                            Utilities.NewProjectileBetter(newCenter, velocity, ModContent.ProjectileType<HolyFireWall>(), HolyFireBeamDamage, 0);
                            npc.netUpdate = true;
                        }

                        // If the crystal is shattering, decrease the scale, else increase it.
                        if (crystal.ai[0] == 1f)
                            fireSuckupWidth = Clamp(fireSuckupWidth - 0.1f, 0f, 1f);
                        else
                            fireSuckupWidth = Clamp(fireSuckupWidth + 0.1f, 0f, 1f);
                    }
                }
            }

            // The healer sits behind the shield and visibly pours energy into it.
            else if (npc.type == HealerType)
            {
                ref float drawShieldConnections = ref npc.ai[2];
                ref float spawnedCrystal = ref npc.Infernum().ExtraAI[0];
                ref float connectionsWidthScale = ref npc.Infernum().ExtraAI[HealerConnectionsWidthScaleIndex];

                // Take no damage.
                npc.dontTakeDamage = true;

                // Spawn the shield if this is the first frame.
                if (spawnedCrystal == 0f)
                {
                    spawnedCrystal = 1;
                    NPC.NewNPCDirect(npc.GetSource_FromAI(), CrystalPosition, ModContent.NPCType<HealerShieldCrystal>(), target: target.whoAmI);
                }

                if (Main.npc.IndexInRange(GlobalNPCOverrides.ProfanedCrystal))
                {
                    if (Main.npc[GlobalNPCOverrides.ProfanedCrystal].active)
                    {
                        // Draw the shield connections.
                        drawShieldConnections = 1f;
                        NPC crystal = Main.npc[GlobalNPCOverrides.ProfanedCrystal];

                        // Sit still behind the crystal.
                        if (npc.Distance(HealerStartingHoverPosition) > 7f && crystal.ai[0] == 0)
                            npc.velocity = npc.SafeDirectionTo(HealerStartingHoverPosition, Vector2.UnitY) * 5f;
                        else
                        {
                            npc.velocity *= 0.5f;
                            npc.spriteDirection = -1;
                        }

                        // If the crystal is shattering, decrease the scale, else increase it.
                        if (crystal.ai[0] == 1)
                            connectionsWidthScale = Clamp(connectionsWidthScale - 0.1f, 0f, 1f);
                        else
                            connectionsWidthScale = Clamp(connectionsWidthScale + 0.1f, 0f, 1f);
                    }
                }
            }
        }

        public static void DoBehavior_SoloHealer(NPC npc, Player target, ref float universalAttackTimer, NPC commander)
        {
            // The commander remains in the center firing its two spinning fire beams.
            if (npc.type == CommanderType)
            {
                ref float movedToPosition = ref npc.Infernum().ExtraAI[CommanderMovedToTriplePositionIndex];
                ref float commanderHasAlreadyDoneBoom = ref npc.Infernum().ExtraAI[CommanderHasSpawnedBlenderAlreadyIndex];
                ref float spawnedLasers = ref npc.Infernum().ExtraAI[1];

                Vector2 hoverPosition = CrystalPosition;

                // Sit still in the middle of the area.
                if (npc.Distance(hoverPosition) > 15f && movedToPosition == 0f)
                {
                    // Do not increase until the lasers are present.
                    universalAttackTimer = 0;
                    npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverPosition) * MathF.Min(npc.Distance(hoverPosition), 18)) / 8f;
                }
                else
                {
                    if (movedToPosition == 0)
                        npc.Center = hoverPosition;
                    npc.velocity *= 0.9f;
                    movedToPosition = 1f;
                    float sine = -Sin(Main.GlobalTimeWrappedHourly * 2f);
                    npc.position.Y += sine * 0.5f;
                    npc.spriteDirection = -1;

                    // Do not deal contact damage.
                    npc.damage = 0;

                    if (Main.projectile.Any((Projectile proj) => proj.active && proj.type == ModContent.ProjectileType<HolySpinningFireBeam>()))
                        spawnedLasers = 1;

                    if (spawnedLasers == 0)
                    {
                        spawnedLasers = 1;
                        for (int i = 0; i < 2; i++)
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                float offsetAngleInterpolant = (float)i / 2;
                                Utilities.NewProjectileBetter(npc.Center, Vector2.UnitY, ModContent.ProjectileType<HolySpinningFireBeam>(), HolySpinningFireBeamDamage, 0f, -1, 0f, offsetAngleInterpolant);
                            }
                            // Screenshake
                            if (CalamityConfig.Instance.Screenshake)
                                Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 6f;
                        }
                    }
                }

                if (universalAttackTimer == HolySpinningFireBeam.TelegraphTime && commanderHasAlreadyDoneBoom == 0)
                {
                    // Check this here so that the flag gets set regardless, stopping it happening if the player enables screenshake after the first attack.
                    if (CalamityConfig.Instance.Screenshake)
                    {
                        Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 6f;
                        ScreenEffectSystem.SetBlurEffect(npc.Center, 2f, 45);
                    }
                    commanderHasAlreadyDoneBoom = 1;
                }
            }

            // The defender hovers to your top left, not dealing contact damage and occasionally firing rocks at you.
            else if (npc.type == DefenderType)
            {
                ref float spawnedRockRing = ref npc.Infernum().ExtraAI[0];
                float flySpeed = 19f;
                float rockSpawnDelay = 30;
                float rockAmount = 5;

                npc.spriteDirection = Sign(npc.DirectionTo(target.Center).X);

                //float xOffset = 500f * -Sign(npc.DirectionTo(target.Center).X);
                Vector2 hoverPosition = target.Center + new Vector2(500f, -350);
                npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverPosition) * MathF.Min(npc.Distance(hoverPosition), flySpeed)) / 8f;

                if (universalAttackTimer % rockSpawnDelay == 0 && spawnedRockRing == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    spawnedRockRing = 1;
                    List<int> waitTimes = new()
                    {
                        60,
                        150,
                        240,
                        330,
                        420
                    };

                    for (int i = 0; i < rockAmount; i++)
                    {
                        Vector2 rockPosition = npc.Center + (TwoPi * i / rockAmount).ToRotationVector2() * 100f;

                        int waitTimeToUse = Main.rand.Next(0, waitTimes.Count);
                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(rock => rock.ModProjectile<ProfanedCirclingRock>().WaitTime = waitTimes[waitTimeToUse]);
                        Utilities.NewProjectileBetter(rockPosition, Vector2.Zero, ModContent.ProjectileType<ProfanedCirclingRock>(), ProfanedRockDamage, 0f, Main.myPlayer, TwoPi * i / rockAmount, npc.whoAmI);
                        waitTimes.RemoveAt(waitTimeToUse);
                    }
                }
            }

            // The healer sits to the right of the commander and empowers its shield. This causes spirals of projectiles to shoot out from it.
            else if (npc.type == HealerType)
            {
                ref float drawShieldConnections = ref npc.ai[2];
                ref float movedToPosition = ref npc.Infernum().ExtraAI[0];
                ref float crystalsFired = ref npc.Infernum().ExtraAI[1];
                ref float connectionsWidthScale = ref commander.Infernum().ExtraAI[HealerConnectionsWidthScaleIndex];
                float crystalReleaseRate = 120f;
                float crystalAmount = 16f;
                float maxCrystalsFired = 4f;

                Vector2 hoverPosition = CrystalPosition + new Vector2(150f, 0f);

                // Sit still behind the commander
                if (npc.Distance(hoverPosition) > 5f && movedToPosition == 0f)
                    npc.velocity = (npc.velocity * 5f + npc.SafeDirectionTo(hoverPosition) * MathF.Min(npc.Distance(hoverPosition), 25)) / 8f;
                else
                {
                    drawShieldConnections = 1f;
                    npc.velocity.X *= 0.9f;
                    if (movedToPosition == 0)
                        npc.Center = hoverPosition;
                    movedToPosition = 1f;
                    float sine = -Sin(Main.GlobalTimeWrappedHourly * 2f);
                    npc.velocity.Y = sine * 0.5f;
                    npc.spriteDirection = -1;
                }

                if (universalAttackTimer % crystalReleaseRate == crystalReleaseRate - 1)
                {
                    if (Main.netMode != NetmodeID.Server)
                    {
                        SoundEngine.PlaySound(SoundID.DD2_PhantomPhoenixShot with { Volume = 4.6f }, target.Center);
                        SoundEngine.PlaySound(SoundID.DD2_DarkMageHealImpact with { Volume = 4.6f }, target.Center);
                    }

                    ScreenEffectSystem.SetFlashEffect(commander.Center, 0.8f, 45);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 projectileSpawnPosition = commander.Center;
                        for (int i = 0; i < crystalAmount; i++)
                        {
                            Vector2 shootVelocity = (TwoPi * i / crystalAmount).ToRotationVector2() * 15f;
                            Utilities.NewProjectileBetter(projectileSpawnPosition, shootVelocity, ModContent.ProjectileType<MagicSpiralCrystalShot>(), MagicShotDamage, 0f, -1, 0f, i % 2f == 0f ? -1f : 1f);
                        }
                        if (crystalsFired >= maxCrystalsFired)
                            universalAttackTimer = 0;
                        crystalsFired++;
                    }

                    // The attack should end.
                    if (crystalsFired >= maxCrystalsFired)
                    {
                        SelectNewAttack(commander, ref universalAttackTimer);
                        drawShieldConnections = 0;
                    }
                }
            }
        }

        public static void DoBehavior_SoloDefender(NPC npc, Player target, ref float universalAttackTimer, NPC commander)
        {
            // Commander remains hovering still.
            if (npc.type == CommanderType)
            {
                npc.velocity *= 0.9f;
                float sine = -Sin(Main.GlobalTimeWrappedHourly * 2f);
                npc.position.Y += sine * 0.5f;
                npc.spriteDirection = -1;

                // Do not deal contact damage.
                npc.damage = 0;
            }

            else if (npc.type == DefenderType)
            {
                ref float substate = ref npc.Infernum().ExtraAI[0];
                ref float hoverOffsetX = ref npc.Infernum().ExtraAI[1];
                ref float hoverOffsetY = ref npc.Infernum().ExtraAI[2];
                ref float dashesCompleted = ref npc.Infernum().ExtraAI[3];

                ref float shieldStatus = ref npc.Infernum().ExtraAI[DefenderShieldStatusIndex];
                ref float drawDashTelegraph = ref commander.Infernum().ExtraAI[DefenderDrawDashTelegraphIndex];
                ref float dashTelegraphOpacity = ref commander.Infernum().ExtraAI[DefenderDashTelegraphOpacityIndex];

                float maxDashes = 4f;
                float waitTime = dashesCompleted == 0f ? 90f : 60f;
                float telegraphWaitTime = 20f;
                float dashTime = 30f;
                float dashSpeed = 24f;
                float dashAcceleration = 1.35f;
                Vector2 oldHoverOffset = new(hoverOffsetX, hoverOffsetY);

                // Generate the shield if it is inactive.
                if ((DefenderShieldStatus)shieldStatus == DefenderShieldStatus.Inactive || !Main.projectile.Any((Projectile p) => p.active && p.type == ModContent.ProjectileType<DefenderShield>()))
                {
                    // Mark the shield as active.
                    shieldStatus = (float)DefenderShieldStatus.ActiveAndAiming;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(npc.Center + npc.velocity.SafeNormalize(Vector2.UnitY) * 75f, Vector2.Zero, ModContent.ProjectileType<DefenderShield>(), 0, 0f, -1, 0f, npc.whoAmI);
                }

                switch (substate)
                {
                    // Pick a location for the dash.
                    case 0:
                        npc.spriteDirection = Sign(npc.DirectionTo(target.Center).X);
                        drawDashTelegraph = 0;
                        Vector2 hoverOffset = Vector2.UnitY;
                        for (int i = 0; i < 100; i++)
                        {
                            float hoverOffsetAngle = Main.rand.Next(4) * TwoPi / 4f + PiOver4;
                            hoverOffset = hoverOffsetAngle.ToRotationVector2();

                            // Leave and use the current offset to see if the direction of the new offset is perpendicular or less to the previous one. This prevents going to opposite sides and
                            // moving thorugh the player's position to reach it. The 0.01 is added on top to ensure that tiny floating point imprecisions don't become a problem.
                            float angleBetween = oldHoverOffset.AngleBetween(hoverOffset);
                            if (angleBetween is < (PiOver2 + 0.01f) and not 0)
                                break;
                        }

                        hoverOffsetX = hoverOffset.X;
                        hoverOffsetY = hoverOffset.Y;
                        universalAttackTimer = 0;
                        npc.netUpdate = true;
                        substate++;
                        break;

                    // Get into position for the dash.
                    case 1:
                        shieldStatus = (float)DefenderShieldStatus.ActiveAndAiming;
                        npc.spriteDirection = Sign(npc.DirectionTo(target.Center).X);
                        float distance = 700f;
                        Vector2 position = target.Center + oldHoverOffset * distance;

                        if (npc.velocity.Length() < 2f)
                            npc.velocity = Vector2.UnitY * -2.4f;

                        npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(position) * MathF.Min(npc.Distance(position), 33f)) / 8f;

                        // Move out of the way of the target if going around them.
                        if (npc.WithinRange(target.Center, 150f))
                            npc.velocity.X += target.Center.DirectionTo(npc.Center).X * 10f;

                        // Initialize the dash telegraph.
                        if (universalAttackTimer >= telegraphWaitTime)
                            drawDashTelegraph = 1;

                        // Increase the opacity.
                        if (drawDashTelegraph == 1)
                            dashTelegraphOpacity = Clamp(dashTelegraphOpacity + 0.1f, 0f, 1f);

                        // Do not deal contact damage while positioning.
                        npc.damage = 0;

                        if ((npc.WithinRange(position, 25f) && universalAttackTimer >= waitTime) || universalAttackTimer >= 120f)
                        {
                            universalAttackTimer = 0;
                            substate++;
                        }
                        break;

                    // Charge
                    case 2:
                        shieldStatus = (float)DefenderShieldStatus.ActiveAndStatic;
                        drawDashTelegraph = 1;
                        npc.velocity = npc.DirectionTo(target.Center) * dashSpeed;
                        commander.Infernum().ExtraAI[DefenderShouldGlowIndex] = 1;
                        SoundEngine.PlaySound(InfernumSoundRegistry.VassalJumpSound, target.Center);
                        SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Pitch = 0.35f, Volume = 1.6f }, target.Center);
                        if (CalamityConfig.Instance.Screenshake)
                        {
                            target.Infernum_Camera().CurrentScreenShakePower = 3f;
                            ScreenEffectSystem.SetFlashEffect(npc.Center, 0.2f, 30);
                        }
                        npc.damage = npc.defDamage;
                        substate++;
                        universalAttackTimer = 0;
                        break;

                    // After a set time, reset.
                    case 3:
                        // Decrease the dash telegraph.
                        drawDashTelegraph = 1;
                        dashTelegraphOpacity = Clamp(dashTelegraphOpacity - 0.2f, 0f, 1f);
                        commander.Infernum().ExtraAI[DefenderShouldGlowIndex] = 1;
                        npc.damage = npc.defDamage;

                        // Accelerate after charging.
                        if (universalAttackTimer < dashTime && npc.velocity.Length() <= 60f)
                            npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * (npc.velocity.Length() + dashAcceleration);

                        // Create particles to indicate the sudden speed.
                        if (Main.rand.NextBool(2))
                        {
                            Vector2 energySpawnPosition = npc.Center + Main.rand.NextVector2Circular(30f, 20f) - npc.velocity;
                            Particle energyLeak = new SparkParticle(energySpawnPosition, npc.velocity * 0.3f, false, 30, Main.rand.NextFloat(0.9f, 1.4f), Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], 0.75f));
                            GeneralParticleHandler.SpawnParticle(energyLeak);
                        }

                        if (universalAttackTimer >= dashTime)
                        {
                            dashesCompleted++;
                            if (dashesCompleted >= maxDashes)
                                SelectNewAttack(commander, ref universalAttackTimer);
                            else
                            {
                                substate = 0;
                                universalAttackTimer = 0;
                            }
                        }
                        break;
                }
            }

            // The healer hovers around the commander, whos shield is toned down a bit in opacity.
            else if (npc.type == HealerType)
            {
                ref float drawShieldConnections = ref npc.ai[2];
                drawShieldConnections = 1f;
                ref float localAttackTimer = ref npc.Infernum().ExtraAI[0];

                float crystalShotReleaseRate = 90f;
                float crystalShotSpeed = 6f;
                float crystalAmount = 6f;

                // Move around the commander.
                Vector2 hoverDestination = commander.Center + (localAttackTimer / 25f).ToRotationVector2() * 200f;
                if (npc.velocity.Length() < 2f)
                    npc.velocity = Vector2.UnitY * -2.4f;

                float flySpeed = Lerp(9f, 23f, Utils.GetLerpValue(50f, 270f, npc.Distance(hoverDestination), true));
                flySpeed *= Utils.GetLerpValue(0f, 50f, npc.Distance(hoverDestination), true);
                npc.velocity = npc.velocity * 0.85f + npc.SafeDirectionTo(hoverDestination) * flySpeed * 0.15f;
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(hoverDestination) * flySpeed, 4f);
                npc.spriteDirection = Sign(npc.DirectionTo(commander.Center).X);

                if (localAttackTimer % crystalShotReleaseRate == crystalShotReleaseRate - 1 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < crystalAmount; i++)
                    {
                        Vector2 fastShootVelocity = (TwoPi * i / crystalAmount).ToRotationVector2() * crystalShotSpeed;
                        Vector2 slowShootVelocity = (TwoPi * (i + 0.5f) / crystalAmount).ToRotationVector2() * (crystalShotSpeed / 3f);
                        Utilities.NewProjectileBetter(commander.Center, fastShootVelocity, ModContent.ProjectileType<MagicCrystalShot>(), MagicShotDamage, 0f);
                        Utilities.NewProjectileBetter(commander.Center, slowShootVelocity, ModContent.ProjectileType<MagicCrystalShot>(), MagicShotDamage, 0f);
                    }
                }

                localAttackTimer++;
            }
        }

        public static void DoBehavior_HealerAndDefender(NPC npc, Player target, ref float universalAttackTimer, NPC commander)
        {
            // Commander remains hovering still.
            if (npc.type == CommanderType)
            {
                npc.velocity *= 0.9f;
                float sine = -Sin(Main.GlobalTimeWrappedHourly * 2f);
                npc.position.Y += sine * 0.5f;
                npc.spriteDirection = -1;

                // Do not deal contact damage.
                npc.damage = 0;
            }

            else if (npc.type == DefenderType)
            {
                ref float substate = ref commander.Infernum().ExtraAI[0];
                ref float localAttackTimer = ref npc.Infernum().ExtraAI[1];
                ref float hoverOffsetY = ref npc.Infernum().ExtraAI[2];
                ref float dashesCompleted = ref npc.Infernum().ExtraAI[3];
                ref float xOffset = ref npc.Infernum().ExtraAI[4];

                ref float shieldStatus = ref npc.Infernum().ExtraAI[DefenderShieldStatusIndex];
                ref float drawDashTelegraph = ref commander.Infernum().ExtraAI[DefenderDrawDashTelegraphIndex];
                ref float dashTelegraphOpacity = ref commander.Infernum().ExtraAI[DefenderDashTelegraphOpacityIndex];

                float waitTime = dashesCompleted == 0f ? 60f : 30;
                float telegraphWaitTime = 20f;
                float dashTime = 25f;
                float dashSpeed = 25f;
                float dashAcceleration = 1.55f;

                switch (substate)
                {
                    // Determine the best location to move to based on the Y position relative to the player.
                    case 0:
                        // If higher than the target.
                        if (npc.Center.Y < target.Center.Y)
                            hoverOffsetY = -550f;
                        else
                            hoverOffsetY = 550f;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            xOffset = Main.rand.NextFloat(100f, 200f) * Main.rand.NextFromList(-1f, 1f);
                            npc.netUpdate = true;
                        }
                        localAttackTimer = 0;
                        substate++;
                        break;

                    // Move to the dash starting location.
                    case 1:
                        shieldStatus = (float)DefenderShieldStatus.ActiveAndAiming;
                        npc.spriteDirection = Sign(npc.DirectionTo(target.Center).X);
                        Vector2 position = target.Center + new Vector2(xOffset, hoverOffsetY);

                        if (npc.velocity.Length() < 2f)
                            npc.velocity = Vector2.UnitY * -2.4f;

                        npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(position) * MathF.Min(npc.Distance(position), 33f)) / 8f;

                        // Move out of the way of the target if going around them.
                        if (npc.WithinRange(target.Center, 150f))
                            npc.velocity.X += target.Center.DirectionTo(npc.Center).X * 10f;

                        // Initialize the dash telegraph.
                        if (localAttackTimer >= telegraphWaitTime)
                            drawDashTelegraph = 1;

                        // Increase the opacity.
                        if (drawDashTelegraph == 1)
                            dashTelegraphOpacity = Clamp(dashTelegraphOpacity + 0.1f, 0f, 1f);

                        // Do not deal damage.
                        npc.damage = 0;

                        if ((npc.WithinRange(position, 25f) && localAttackTimer >= waitTime) || localAttackTimer >= 120f)
                        {
                            localAttackTimer = 0;
                            substate++;
                        }
                        break;

                    // Charge
                    case 2:
                        shieldStatus = (float)DefenderShieldStatus.ActiveAndStatic;
                        drawDashTelegraph = 1;
                        npc.velocity = npc.DirectionTo(target.Center) * dashSpeed;
                        commander.Infernum().ExtraAI[DefenderShouldGlowIndex] = 1;
                        SoundEngine.PlaySound(InfernumSoundRegistry.VassalJumpSound, target.Center);
                        SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Pitch = 0.35f, Volume = 1.6f }, target.Center);
                        if (CalamityConfig.Instance.Screenshake)
                        {
                            target.Infernum_Camera().CurrentScreenShakePower = 3f;
                            ScreenEffectSystem.SetFlashEffect(npc.Center, 0.2f, 30);
                        }
                        npc.damage = npc.defDamage;
                        substate++;
                        localAttackTimer = 0;
                        break;

                    // After a set time, reset.
                    case 3:
                        // Decrease the dash telegraph.
                        drawDashTelegraph = 1;
                        dashTelegraphOpacity = Clamp(dashTelegraphOpacity - 0.2f, 0f, 1f);
                        commander.Infernum().ExtraAI[DefenderShouldGlowIndex] = 1;
                        npc.damage = npc.defDamage;
                        // Create particles to indicate the sudden speed.
                        if (Main.rand.NextBool(2))
                        {
                            Vector2 energySpawnPosition = npc.Center + Main.rand.NextVector2Circular(30f, 20f) - npc.velocity;
                            Particle energyLeak = new SparkParticle(energySpawnPosition, npc.velocity * 0.3f, false, 30, Main.rand.NextFloat(0.9f, 1.4f), Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], 0.75f));
                            GeneralParticleHandler.SpawnParticle(energyLeak);
                        }

                        // Accelerate after charging.
                        if (localAttackTimer < dashTime && npc.velocity.Length() <= 60f)
                            npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * (npc.velocity.Length() + dashAcceleration);

                        if (localAttackTimer >= dashTime)
                        {
                            substate = 0;
                            localAttackTimer = 0;
                        }
                        break;
                }
                localAttackTimer++;
            }

            // The healer moves above the commander, and released crystal walls upwards.
            else if (npc.type == HealerType)
            {
                ref float movedToPosition = ref npc.Infernum().ExtraAI[0];
                ref float completedCrystalLayers = ref npc.Infernum().ExtraAI[1];
                float totalCrystalLayers = 6;
                float totalCrystalsPerLayer = 10;
                float crystalLayerFireRate = 45f;
                float endOfAttackDelay = 60f;
                Vector2 hoverPosition = new(commander.Center.X, CrystalPosition.Y - 550);

                // Sit still above the commander
                if (npc.Distance(hoverPosition) > 10f && movedToPosition == 0f)
                    npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverPosition) * MathF.Min(npc.Distance(hoverPosition), 18)) / 8f;
                else
                {
                    npc.velocity.X *= 0.9f;
                    if (movedToPosition == 0)
                        npc.Center = hoverPosition;
                    movedToPosition = 1f;
                    float sine = -Sin(Main.GlobalTimeWrappedHourly * 2f);
                    npc.velocity.Y = sine * 0.5f;
                    npc.spriteDirection = (npc.DirectionTo(target.Center).X > 0).ToInt();
                }

                if (universalAttackTimer % crystalLayerFireRate == crystalLayerFireRate - 1 && Main.netMode != NetmodeID.MultiplayerClient && completedCrystalLayers < totalCrystalLayers)
                {
                    SoundEngine.PlaySound(SoundID.Item109, target.Center);
                    //float xSpeedOffset = target.velocity.X + Main.rand.NextFloat(-5f, 5f);
                    for (int i = 0; i < totalCrystalsPerLayer; i++)
                    {
                        Vector2 shootVelocity = new(Lerp(-20f, 20f, i / (float)totalCrystalsPerLayer), -10.75f);
                        shootVelocity.X += Main.rand.NextFloatDirection() * 0.6f;
                        Utilities.NewProjectileBetter(npc.Center + -Vector2.UnitY * 20f, shootVelocity, ModContent.ProjectileType<FallingCrystalShard>(), MagicShotDamage, 0f);
                    }
                    completedCrystalLayers++;
                    if (completedCrystalLayers >= totalCrystalLayers)
                        universalAttackTimer = 0;
                }

                if (completedCrystalLayers >= totalCrystalLayers && commander.Infernum().ExtraAI[0] == 0 && universalAttackTimer >= endOfAttackDelay)
                    SelectNewAttack(commander, ref universalAttackTimer);
            }
        }

        public static void DoBehavior_HealerDeathAnimation(NPC npc, Player target, ref float universalAttackTimer, NPC commander)
        {
            // The commander stays in place still, and the lasers fade out.
            if (npc.type == CommanderType)
            {
                float angerDelay = 170;
                float angerTime = 45f;

                ref float glowAmount = ref npc.Infernum().ExtraAI[CommanderAngerGlowAmountIndex];
                ref float spearStatus = ref npc.Infernum().ExtraAI[CommanderSpearStatusIndex];
                ref float spearRotation = ref npc.Infernum().ExtraAI[CommanderSpearRotationIndex];

                npc.velocity *= 0.9f;
                float sine = -Sin(Main.GlobalTimeWrappedHourly * 2f);
                npc.position.Y += sine * 0.5f;
                npc.spriteDirection = (npc.DirectionTo(target.Center).X > 0) ? 1 : -1;

                // This tells the blender lasers to fade out and disappear.
                npc.Infernum().ExtraAI[CommanderBlenderShouldFadeOutIndex] = 1f;

                if (universalAttackTimer >= angerDelay && universalAttackTimer < angerDelay + angerTime)
                {
                    float interlopant = Sin(PI * ((universalAttackTimer - angerDelay) / angerTime));
                    glowAmount = CalamityUtils.SineInOutEasing(interlopant, 0);
                }

                // Do not deal contact damage.
                npc.damage = 0;

                // Make the spear appear and spin.
                if (universalAttackTimer >= angerDelay + angerTime / 2f)
                {
                    if ((DefenderShieldStatus)spearStatus is DefenderShieldStatus.Inactive || !Main.projectile.Any((Projectile p) => p.active && p.type == ModContent.ProjectileType<CommanderSpear>()))
                    {
                        spearStatus = (float)DefenderShieldStatus.ActiveAndAiming;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<CommanderSpear>(), CommanderSpearDamage, 0f, -1, 0f, npc.whoAmI);
                    }
                    float easingInterpolant = (universalAttackTimer - (angerDelay + angerTime / 2f)) / (angerTime / 2f);
                    spearRotation = Utilities.EaseInOutCubic(easingInterpolant);
                }

                if (universalAttackTimer >= angerDelay + angerTime)
                    SelectNewAttack(commander, ref universalAttackTimer);
            }

            // The defender rushes to the commander to shield it.
            else if (npc.type == DefenderType)
            {
                ref float shieldStatus = ref npc.Infernum().ExtraAI[DefenderShieldStatusIndex];

                Vector2 hoverDestination = commander.Center + commander.SafeDirectionTo(target.Center) * Lerp(25, 150, Clamp(target.Distance(commander.Center) / 850f, 0f, 1f));
                if (npc.velocity.Length() < 2f)
                    npc.velocity = Vector2.UnitY * -2.4f;

                float flySpeed = Lerp(9f, 23f, Utils.GetLerpValue(50f, 270f, npc.Distance(hoverDestination), true));
                flySpeed *= Utils.GetLerpValue(0f, 50f, npc.Distance(hoverDestination), true);
                npc.velocity = npc.velocity * 0.85f + npc.SafeDirectionTo(hoverDestination) * flySpeed * 0.15f;
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(hoverDestination) * flySpeed, 4f);
                npc.spriteDirection = (npc.DirectionTo(target.Center).X > 0) ? 1 : -1;

                // Generate the shield if it is inactive.
                if ((DefenderShieldStatus)shieldStatus == DefenderShieldStatus.Inactive || !Main.projectile.Any((Projectile p) => p.active && p.type == ModContent.ProjectileType<DefenderShield>()))
                {
                    // Mark the shield as active.
                    shieldStatus = (float)DefenderShieldStatus.ActiveAndAiming;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(npc.Center + npc.velocity.SafeNormalize(Vector2.UnitY) * 75f, Vector2.Zero, ModContent.ProjectileType<DefenderShield>(), 0, 0f, -1, 0f, npc.whoAmI);
                }
            }

            // The healer rapidly slows down, and glows white, before poofing.
            else if (npc.type == HealerType)
            {
                float whiteGlowTime = 120f;
                float ashesTime = 90f;
                ref float whiteGlowOpacity = ref npc.Infernum().ExtraAI[0];

                // Slow down rapidly.
                npc.velocity *= 0.97f;
                npc.damage = 0;
                npc.dontTakeDamage = true;
                npc.Calamity().ShouldCloseHPBar = true;

                if (universalAttackTimer <= whiteGlowTime)
                    whiteGlowOpacity = CalamityUtils.ExpInEasing(Lerp(0f, 1f, universalAttackTimer / whiteGlowTime), 0);

                else if (universalAttackTimer == whiteGlowTime + ashesTime - 5)
                {
                    for (int i = 0; i < 100; i++)
                    {
                        Vector2 position = npc.Center + Main.rand.NextVector2Circular(npc.width * 0.5f, npc.height * 0.5f);
                        Vector2 velocity = npc.SafeDirectionTo(position) * Main.rand.NextFloat(1.5f, 2f);
                        Particle ashes = new MediumMistParticle(position, velocity, WayfinderSymbol.Colors[1], Color.Gray, Main.rand.NextFloat(0.75f, 0.95f), 400, Main.rand.NextFloat(-0.05f, 0.05f));
                        GeneralParticleHandler.SpawnParticle(ashes);
                    }
                }
                else if (universalAttackTimer >= whiteGlowTime + ashesTime)
                {
                    // Die once the animation is complete.
                    HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.ProfanedHealerDeathTip");
                    npc.life = 0;
                    npc.active = false;
                    DoPhaseTransitionEffects(commander, 2);
                }
            }
        }
        #endregion

        #region Commander + Defender Attacks
        public static void DoBehavior_SpearDashAndGroundSlam(NPC npc, Player target, ref float universalAttackTimer, NPC commander)
        {
            // The commander lines up horizontally, spins his spear around releasing spears in a circle before dashing at a rapid speed at the player.
            // This won't be ramable due to the spear.
            if (npc.type == CommanderType)
            {
                ref float substate = ref npc.Infernum().ExtraAI[0];
                ref float localAttackTimer = ref npc.Infernum().ExtraAI[1];
                ref float xOffset = ref npc.Infernum().ExtraAI[2];
                ref float spearRotationStartingOffset = ref npc.Infernum().ExtraAI[3];

                ref float spearStatus = ref npc.Infernum().ExtraAI[CommanderSpearStatusIndex];
                ref float spearRotation = ref npc.Infernum().ExtraAI[CommanderSpearRotationIndex];
                ref float drawSpearSmear = ref npc.Infernum().ExtraAI[CommanderDrawSpearSmearIndex];

                float flySpeed = 20f;
                float spinLength = 20f;
                float recoilLength = 20f;
                float chargeLength = 25f;
                float chargeSpeed = 30f;
                float chargeAcceleration = 1.55f;
                float totalSpearsToRelease = 11f;
                int spearReleasePoint = (int)(spinLength / totalSpearsToRelease);

                switch (substate)
                {
                    // The commander picks the location to hover and moves to it.
                    case 0:
                        // Spawn the spear if it does not exist.
                        npc.CreateSpearAndAimAtTarget(ref spearStatus, ref spearRotation, target);
                        substate++;
                        localAttackTimer = 0f;
                        spearRotationStartingOffset = spearRotation;
                        return;

                    case 1:
                        xOffset = -700f * Sign(target.Center.X - npc.Center.X);
                        Vector2 hoverDestination = target.Center + new Vector2(xOffset, 0f);
                        npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathF.Min(npc.Distance(hoverDestination), flySpeed)) / 8f;
                        npc.spriteDirection = (npc.DirectionTo(target.Center).X > 0f) ? 1 : -1;

                        // Make the spear rotation point to the player.
                        float idealRotation = npc.SafeDirectionTo(target.Center).ToRotation();
                        spearRotation = spearRotation.AngleTowards(idealRotation, 0.2f);

                        // If close enough or enough time has passed, go to the next state.
                        if ((npc.Distance(hoverDestination) < 20f && localAttackTimer >= 30) || localAttackTimer > 120f)
                        {
                            substate++;
                            localAttackTimer = 0f;
                            spearRotationStartingOffset = spearRotation;
                            return;
                        }
                        break;

                    // The commander remains moving to the location, and spins his spear.
                    case 2:
                        hoverDestination = target.Center + new Vector2(xOffset, 0f);
                        npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathF.Min(npc.Distance(hoverDestination), flySpeed)) / 8f;
                        drawSpearSmear = 1f;
                        npc.spriteDirection = (npc.DirectionTo(target.Center).X > 0f) ? 1 : -1;

                        if (localAttackTimer == 0)
                            SoundEngine.PlaySound(InfernumSoundRegistry.MyrindaelSpinSound with { Pitch = -0.1f, Volume = 0.9f }, target.Center);

                        if (localAttackTimer < spinLength)
                        {
                            spearRotation = Tau * CalamityUtils.SineInOutEasing(localAttackTimer / spinLength, 0) + spearRotationStartingOffset;
                            if (localAttackTimer % spearReleasePoint == spearReleasePoint - 1f && Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                float positionRotation = Tau * CalamityUtils.LinearEasing(localAttackTimer / spinLength, 0) + spearRotationStartingOffset;
                                Vector2 position = npc.Center + positionRotation.ToRotationVector2() * 75f;
                                Vector2 velocity = npc.SafeDirectionTo(position) * 5f;
                                Utilities.NewProjectileBetter(position, velocity, ModContent.ProjectileType<ProfanedSpearInfernum>(), HolySpearDamage, 0f);
                            }
                        }
                        else
                        {
                            substate++;
                            localAttackTimer = 0f;
                            return;
                        }
                        break;

                    // The commander recoils backwards before launching.
                    case 3:

                        npc.velocity = Utils.MoveTowards(Vector2.UnitX * -2.3f, npc.DirectionTo(target.Center) * -2.3f, 4.5f);

                        // Make the spear rotation point to the player.
                        idealRotation = npc.SafeDirectionTo(target.Center).ToRotation();
                        spearRotation = spearRotation.AngleTowards(idealRotation, 0.2f);

                        npc.spriteDirection = (npc.DirectionTo(target.Center).X > 0f) ? 1 : -1;

                        if (localAttackTimer >= recoilLength)
                        {
                            substate++;
                            localAttackTimer = 0f;
                            return;
                        }
                        break;

                    // The commander charges at the target.
                    case 4:
                        SoundEngine.PlaySound(InfernumSoundRegistry.VassalSlashSound with { Pitch = -0.1f, Volume = 0.9f }, target.Center);
                        npc.velocity = -npc.velocity.SafeNormalize(Vector2.UnitY) * chargeSpeed;
                        substate++;
                        localAttackTimer = 0f;
                        return;

                    // After enough time, it resets the substate.
                    case 5:
                        // Create particles to indicate the sudden speed.
                        if (Main.rand.NextBool(2))
                        {
                            Vector2 energySpawnPosition = npc.Center + Main.rand.NextVector2Circular(30f, 20f) - npc.velocity;
                            Particle energyLeak = new SparkParticle(energySpawnPosition, npc.velocity * 0.3f, false, 30, Main.rand.NextFloat(0.9f, 1.4f), Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], 0.75f));
                            GeneralParticleHandler.SpawnParticle(energyLeak);
                        }

                        // Accelerate after charging.
                        if (localAttackTimer < chargeLength && npc.velocity.Length() <= 120f)
                            npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * (npc.velocity.Length() + chargeAcceleration);

                        if (localAttackTimer >= chargeLength)
                        {
                            substate = 0f;
                            localAttackTimer = 0f;
                            return;
                        }
                        break;
                }

                localAttackTimer++;
            }

            // The defender hovers above the target, gathering fire energy, before charging up and slamming downwards, spawning lava eruptions from the ground when colliding with the ground.
            else if (npc.type == DefenderType)
            {
                ref float substate = ref npc.Infernum().ExtraAI[0];
                ref float slamsPerformed = ref npc.Infernum().ExtraAI[1];
                ref float xOffset = ref npc.Infernum().ExtraAI[2];

                ref float shieldStatus = ref npc.Infernum().ExtraAI[DefenderShieldStatusIndex];

                float chargeUpLength = 180f;
                float recoilWait = 10f;
                float recoilLength = 30f;
                float slamLength = 150f;
                float afterSlamWaitLength = 60f;
                float slamSpeed = 35f;
                float flySpeed = 19f;
                float maxSlams = 2f;
                float pillarAmount = 12f;
                float rockAmount = 2f;

                switch (substate)
                {
                    // The defender hovers above you, charging up energy.
                    case 0:
                        xOffset = Main.rand.NextFloat(25f, 50f) * Main.rand.NextFromList(-1f, 1f);
                        Vector2 hoverDestination = target.Center + new Vector2(xOffset, -400f);
                        npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathF.Min(npc.Distance(hoverDestination), flySpeed)) / 8f;

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            List<int> waitTimes = new()
                            {
                                90,
                                180,
                            };

                            for (int i = 0; i < rockAmount; i++)
                            {
                                Vector2 rockPosition = npc.Center + (TwoPi * i / rockAmount).ToRotationVector2() * 100f;

                                int waitTimeToUse = Main.rand.Next(0, waitTimes.Count);
                                ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(rock => rock.ModProjectile<ProfanedCirclingRock>().WaitTime = waitTimes[waitTimeToUse]);
                                Utilities.NewProjectileBetter(rockPosition, Vector2.Zero, ModContent.ProjectileType<ProfanedCirclingRock>(), ProfanedRockDamage, 0f, Main.myPlayer, TwoPi * i / rockAmount, npc.whoAmI);
                                waitTimes.RemoveAt(waitTimeToUse);
                            }
                        }

                        if ((DefenderShieldStatus)shieldStatus != DefenderShieldStatus.Inactive)
                            shieldStatus = (float)DefenderShieldStatus.MarkedForRemoval;

                        substate++;
                        universalAttackTimer = 0;
                        return;

                    case 1:
                        hoverDestination = target.Center + new Vector2(xOffset, -400f);
                        npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathF.Min(npc.Distance(hoverDestination), flySpeed)) / 8f;

                        // Move out of the way of the target if going around them.
                        if (npc.WithinRange(target.Center, 200f))
                            npc.velocity.X += target.Center.DirectionTo(npc.Center).X * 5f;

                        if (npc.Opacity < 1f)
                        {
                            if (npc.Opacity == 0f)
                                CreateFireExplosion(npc.Center, true);
                            npc.Opacity = Clamp(npc.Opacity + 0.1f, 0f, 1f);
                            npc.dontTakeDamage = false;
                        }

                        npc.spriteDirection = (npc.DirectionTo(target.Center).X > 0f) ? 1 : -1;

                        if (universalAttackTimer >= chargeUpLength && npc.Distance(hoverDestination) < 75f)
                        {
                            substate++;
                            universalAttackTimer = 0;
                            return;
                        }
                        break;

                    // The defender stops folowing for half a second and recoils upwards.
                    case 2:
                        npc.velocity.X *= 0.8f;

                        if (universalAttackTimer == recoilWait)
                            npc.velocity.Y = -3.2f;

                        npc.spriteDirection = (npc.DirectionTo(target.Center).X > 0f) ? 1 : -1;

                        // Generate the shield if it is inactive.
                        if ((DefenderShieldStatus)shieldStatus == DefenderShieldStatus.Inactive || !Main.projectile.Any((Projectile p) => p.active && p.type == ModContent.ProjectileType<DefenderShield>()))
                        {
                            // Mark the shield as active.
                            shieldStatus = (float)DefenderShieldStatus.ActiveAndStatic;
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(shield => shield.ModProjectile<DefenderShield>().PositionOffset = Vector2.UnitY * 60f);
                                Utilities.NewProjectileBetter(npc.Center + npc.velocity.SafeNormalize(Vector2.UnitY) * 75f, Vector2.Zero, ModContent.ProjectileType<DefenderShield>(), 0, 0f, -1, 0f, npc.whoAmI);
                            }
                        }

                        else if (universalAttackTimer >= recoilWait + recoilLength)
                        {
                            substate++;
                            universalAttackTimer = 0f;
                            return;
                        }
                        break;

                    // The defender slams downwards rapidly.
                    case 3:
                        SoundEngine.PlaySound(InfernumSoundRegistry.VassalJumpSound with { Pitch = -0.2f, Volume = 0.9f }, target.Center);
                        npc.velocity = Vector2.UnitY * slamSpeed;
                        npc.damage = npc.defDamage;

                        universalAttackTimer = 0f;
                        substate++;
                        return;

                    // Upon hitting a tile, or enough time passing, stop and create lava pillars.
                    case 4:
                        // Create particles to indicate the sudden speed.
                        if (Main.rand.NextBool(2))
                        {
                            Vector2 energySpawnPosition = npc.Center + Main.rand.NextVector2Circular(30f, 20f) - npc.velocity;
                            Particle energyLeak = new SparkParticle(energySpawnPosition, npc.velocity * 0.3f, false, 30, Main.rand.NextFloat(0.9f, 1.4f), Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], 0.75f));
                            GeneralParticleHandler.SpawnParticle(energyLeak);
                        }

                        npc.damage = npc.defDamage;

                        if ((Collision.SolidCollision(npc.Center, (int)(npc.width * 0.85f), (int)(npc.height * 0.85f)) && npc.Center.Y > target.Center.Y) || universalAttackTimer >= slamLength)
                        {
                            SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Pitch = 0.35f, Volume = 1.6f }, target.Center);
                            SoundEngine.PlaySound(npc.HitSound.Value with { Volume = 3f }, target.Center);

                            if (CalamityConfig.Instance.Screenshake)
                            {
                                //target.Infernum_Camera().CurrentScreenShakePower = 12f;
                                ScreenEffectSystem.SetBlurEffect(npc.Center, 2f, 45);
                                ScreenEffectSystem.SetFlashEffect(npc.Center, 1f, 45);
                            }

                            if (npc.life > (npc.lifeMax * 0.1f))
                                npc.life -= (int)(npc.lifeMax * 0.01f);
                            npc.velocity = -npc.velocity.SafeNormalize(Vector2.UnitY) * 4.6f;

                            float yPos = (Main.maxTilesY * 16f) - 50f;
                            Vector2 start = new(npc.Center.X - 3000f, yPos);
                            Vector2 end = new(npc.Center.X + 3000f, yPos);

                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                for (int i = 0; i < pillarAmount; i++)
                                {
                                    Vector2 position = Vector2.Lerp(start, end, (float)i / pillarAmount);
                                    Utilities.NewProjectileBetter(position, Vector2.Zero, ModContent.ProjectileType<LavaEruptionPillar>(), LavaPillarDamage, 0f);
                                }
                            }
                            for (int j = 0; j < 40; j++)
                            {
                                Particle rock = new ProfanedRockParticle(npc.Bottom, -Vector2.UnitY.RotatedBy(Main.rand.NextFloat(-0.9f, 0.9f)) * Main.rand.NextFloat(6f, 9f), Color.White, Main.rand.NextFloat(0.85f, 1.15f), 60, Main.rand.NextFloat(0f, 0.2f));
                                GeneralParticleHandler.SpawnParticle(rock);
                            }
                            substate++;
                            universalAttackTimer = 0f;
                            return;
                        }
                        break;

                    case 5:
                        npc.velocity *= 0.975f;
                        if (universalAttackTimer > afterSlamWaitLength)
                        {
                            shieldStatus = (float)DefenderShieldStatus.MarkedForRemoval;
                            slamsPerformed++;
                            if (slamsPerformed >= maxSlams)
                            {
                                bool pillarsAreMostlyGone = !Main.projectile.Any(proj => proj.active && proj.type == ModContent.ProjectileType<LavaEruptionPillar>() && proj.timeLeft >= 30);

                                // Fade out rapidly.
                                if (npc.Opacity == 1f)
                                    CreateFireExplosion(npc.Center, true);

                                npc.Opacity = Clamp(npc.Opacity - 0.1f, 0f, 1f);
                                npc.dontTakeDamage = true;

                                // Switch to next attack. It will stall here if the commander is mid spin/charge until it is free. This is to avoid abruptly stopping mid spin/charge.
                                if (commander.Infernum().ExtraAI[0] is 0f or 1f && pillarsAreMostlyGone)
                                    SelectNewAttack(commander, ref universalAttackTimer);
                            }
                            else
                            {
                                npc.dontTakeDamage = false;
                                substate = 0f;
                                universalAttackTimer = 0f;
                            }
                        }
                        break;
                }
            }
        }

        public static void DoBehavior_CrashRam(NPC npc, Player target, ref float universalAttackTimer, NPC commander)
        {
            // They both use the same substate due to needing to be synced.
            ref float substate = ref commander.Infernum().ExtraAI[0];
            ref float commanderOffset = ref commander.Infernum().ExtraAI[1];
            ref float defenderIsReady = ref commander.Infernum().ExtraAI[2];
            ref float ramsCompleted = ref commander.Infernum().ExtraAI[3];

            float maxRamsToComplete = 4f;
            float hoverWaitTime = 35f;
            float flySpeed = 25f;
            float fadeInTime = 16f;
            // Get faster if taking too long to get into position.
            //float flySpeedScaled = universalAttackTimer > maxMoveTime ? flySpeed + (universalAttackTimer - maxMoveTime) : flySpeed;

            float commanderHoverDistance = 600f;
            float defenderHoverDistance = commanderHoverDistance * 0.75f;
            float defenderRamSpeed = 35f;
            float commanderRamSpeed = defenderRamSpeed * 1.25f;

            float recoilTime = 30f;
            float rockAmount = 9f;

            NPC defender;

            if (CalamityGlobalNPC.doughnutBossDefender == -1)
                return;

            if (!Main.npc.IndexInRange(CalamityGlobalNPC.doughnutBossDefender))
                return;

            defender = Main.npc[CalamityGlobalNPC.doughnutBossDefender];

            if (npc.type == CommanderType)
            {
                ref float spearStatus = ref npc.Infernum().ExtraAI[CommanderSpearStatusIndex];
                ref float spearRotation = ref npc.Infernum().ExtraAI[CommanderSpearRotationIndex];
                switch (substate)
                {
                    // Get an offset from the player.
                    case 0:
                        commanderOffset += Main.rand.NextFloat(0f, TwoPi) * Main.rand.NextFromList(-1f, 1f);
                        npc.netUpdate = true;
                        if (universalAttackTimer <= fadeInTime)
                            npc.Opacity = Clamp(npc.Opacity - 0.0625f, 0f, 1f);
                        else
                        {
                            substate++;
                            universalAttackTimer = 0f;
                        }
                        break;

                    // Teleport and stick to said offset.
                    case 1:
                        Vector2 hoverDestination = target.Center + commanderOffset.ToRotationVector2() * commanderHoverDistance;

                        if (universalAttackTimer == 1)
                        {
                            if (CalamityConfig.Instance.Screenshake)
                                ScreenEffectSystem.SetFlashEffect(target.Center, 1f, 30);

                            for (int i = 0; i < 75; i++)
                            {
                                Particle fire = new HeavySmokeParticle(hoverDestination + Main.rand.NextVector2Circular(npc.width * 0.75f, npc.height * 0.75f), Vector2.Zero,
                                    Main.rand.NextBool() ? WayfinderSymbol.Colors[1] : WayfinderSymbol.Colors[2], 60, Main.rand.NextFloat(0.75f, 1f), 1f, glowing: true,
                                    rotationSpeed: Main.rand.NextFromList(-1, 1) * 0.01f);
                                GeneralParticleHandler.SpawnParticle(fire);
                            }

                            CreateFireExplosion(hoverDestination);
                            npc.Center = hoverDestination;
                        }

                        if (universalAttackTimer <= fadeInTime)
                            npc.Opacity = Clamp(npc.Opacity + 0.0625f, 0f, 1f);

                        if (universalAttackTimer <= hoverWaitTime * 0.9f)
                            npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathF.Min(npc.Distance(hoverDestination), flySpeed)) / 8f;
                        else
                            npc.velocity *= 0.93f;

                        // Move out of the way of the target if going around them.
                        if (npc.WithinRange(target.Center, 150f))
                            npc.velocity.X += target.Center.DirectionTo(npc.Center).X * 10f;

                        npc.spriteDirection = (npc.DirectionTo(target.Center).X > 0f) ? 1 : -1;

                        // Spawn the spear if it does not exist.
                        npc.CreateSpearAndAimAtTarget(ref spearStatus, ref spearRotation, target);

                        // If close enough, and the defender is ready.
                        if (/*npc.WithinRange(hoverDestination, 50f) &&*/ universalAttackTimer >= hoverWaitTime && defenderIsReady == 1f)
                        {
                            substate++;
                            universalAttackTimer = 0;
                        }
                        break;

                    // Charge towards the defender.
                    case 2:
                        npc.velocity = npc.DirectionTo(defender.Center) * commanderRamSpeed;
                        spearStatus = (float)DefenderShieldStatus.ActiveAndStatic;
                        // Cause the defender to charge as well.
                        defender.velocity = defender.DirectionTo(npc.Center) * defenderRamSpeed;
                        SoundEngine.PlaySound(InfernumSoundRegistry.VassalJumpSound with { Pitch = -0.2f, Volume = 0.9f }, target.Center);

                        // Reset the defender being ready.
                        defenderIsReady = 0f;
                        substate++;
                        universalAttackTimer = 0f;
                        break;

                    // If close enough to the defender, or if enough time has passed if they missed.
                    case 3:

                        // Create particles to indicate the sudden speed.
                        if (Main.rand.NextBool())
                        {
                            Vector2 energySpawnPosition = npc.Center + Main.rand.NextVector2Circular(30f, 20f) - npc.velocity;
                            Particle energyLeak = new SparkParticle(energySpawnPosition, npc.velocity * 0.3f, false, 30, Main.rand.NextFloat(0.9f, 1.4f), Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], 0.75f));
                            GeneralParticleHandler.SpawnParticle(energyLeak);
                        }

                        for (int i = 0; i < 30; i++)
                        {
                            // Bias towards lower values. 
                            float size = Pow(Main.rand.NextFloat(), 2f);
                            ModContent.GetInstance<ProfanedLavaMetaball>().SpawnParticle(npc.Center - (npc.velocity * 0.5f) + (Main.rand.NextVector2Circular(npc.width * 0.5f, npc.height * 0.5f) * size),
                                Vector2.Zero, new(Main.rand.NextFloat(15f, 20f)), 0.93f);
                        }

                        if (npc.WithinRange(defender.Center, 230f) || universalAttackTimer >= 240f)
                        {
                            substate++;
                            universalAttackTimer = 0f;
                        }
                        break;

                    // Create an explosion and recoil backwards.
                    case 4:
                        if (universalAttackTimer == 1f)
                        {
                            npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * -5f;
                            defender.velocity = defender.velocity.SafeNormalize(Vector2.UnitY) * -3.75f;

                            // Create a bunch of rock particles to indicate a heavy impact.
                            Vector2 impactCenter = (npc.Center + defender.Center) / 2f;
                            for (int i = 0; i < 20; i++)
                            {
                                Particle rock = new ProfanedRockParticle(impactCenter, -Vector2.UnitY.RotatedByRandom(Tau) * Main.rand.NextFloat(6f, 9f),
                                    Color.White, Main.rand.NextFloat(0.85f, 1.15f), 60, Main.rand.NextFloat(0f, 0.2f), false);
                                GeneralParticleHandler.SpawnParticle(rock);
                            }

                            CreateFireExplosion(impactCenter);

                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {

                                // Also spawn a bunch of rocks with slightly random direction and speed.
                                for (int i = 0; i < rockAmount; i++)
                                {

                                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(rock =>
                                    {
                                        rock.ModProjectile<ProfanedRock>().RockTypeVarient = (int)ProfanedRock.RockType.Accelerating;
                                    });

                                    Vector2 direction = impactCenter + ((TwoPi * i / rockAmount) + Main.rand.NextFloat(-0.4f, 0.4f)).ToRotationVector2();
                                    Vector2 velocity = impactCenter.DirectionTo(direction) * Main.rand.NextFloat(5f, 7f);

                                    // Aim one directly at the player and make it slightly faster.
                                    if (i == rockAmount - 1f)
                                    {
                                        direction = impactCenter + impactCenter.DirectionTo(target.Center);
                                        velocity = impactCenter.DirectionTo(direction) * 7.5f;
                                    }
                                    Utilities.NewProjectileBetter(impactCenter, velocity, ModContent.ProjectileType<ProfanedRock>(), ProfanedRockDamage, 0f, -1, 0f, npc.whoAmI);
                                }
                            }

                            // Play a loud explosion + hitbox sound and screenshake to give the impact power.
                            SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Pitch = 0.35f, Volume = 1.6f }, target.Center);
                            SoundEngine.PlaySound(npc.HitSound.Value with { Volume = 3f }, target.Center);

                            if (CalamityConfig.Instance.Screenshake)
                                ScreenEffectSystem.SetBlurEffect(npc.Center, 0.1f, 25);

                            // Damage the defender slightly, as it has been stabbed by the spear.
                            if (defender.life > defender.lifeMax * 0.01f)
                                defender.life -= (int)(defender.lifeMax * 0.001f);
                        }

                        npc.velocity *= 0.99f;
                        defender.velocity *= 0.99f;

                        // If enough rams have happened, select a new attack, else reset the substate.
                        if (universalAttackTimer >= recoilTime)
                        {
                            ramsCompleted++;
                            if (ramsCompleted >= maxRamsToComplete)
                            {
                                SelectNewAttack(commander, ref universalAttackTimer);
                                return;
                            }

                            substate = 0f;
                            universalAttackTimer = 0f;
                        }
                        break;
                }
            }

            // The defender has most of its stuff controlled by the commander here.
            else if (npc.type == DefenderType)
            {
                ref float shieldStatus = ref npc.Infernum().ExtraAI[DefenderShieldStatusIndex];

                npc.damage = npc.Opacity >= 0.95f ? npc.defDamage : 0;

                switch (substate)
                {
                    case 0:
                        if (universalAttackTimer <= fadeInTime)
                            npc.Opacity = Clamp(npc.Opacity - 0.0625f, 0f, 1f);
                        break;
                    // Move to said offset.
                    case 1:
                        Vector2 hoverDestination = target.Center + (commanderOffset + PI).ToRotationVector2() * defenderHoverDistance;

                        if (universalAttackTimer == 1)
                        {
                            npc.Center = hoverDestination;
                            npc.netUpdate = true;
                            for (int i = 0; i < 75; i++)
                            {
                                Particle fire = new HeavySmokeParticle(hoverDestination + Main.rand.NextVector2Circular(npc.width * 0.5f, npc.height * 0.5f), Vector2.Zero,
                                    Main.rand.NextBool() ? WayfinderSymbol.Colors[1] : WayfinderSymbol.Colors[2], 60, Main.rand.NextFloat(0.75f, 1f), 1f, glowing: true,
                                    rotationSpeed: Main.rand.NextFromList(-1, 1) * 0.01f);
                                GeneralParticleHandler.SpawnParticle(fire);
                            }
                            CreateFireExplosion(hoverDestination);
                        }

                        if (universalAttackTimer <= (int)(fadeInTime))
                            npc.Opacity = Clamp(npc.Opacity + 0.0625f, 0f, 1f);

                        if (universalAttackTimer <= hoverWaitTime * 0.5f)
                            npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathF.Min(npc.Distance(hoverDestination), flySpeed)) / 8f;
                        else
                            npc.velocity *= 0.93f;

                        // Move out of the way of the target if going around them.
                        if (npc.WithinRange(target.Center, 150f))
                            npc.velocity.X += target.Center.DirectionTo(npc.Center).X * 10f;

                        npc.spriteDirection = (npc.DirectionTo(target.Center).X > 0f) ? 1 : -1;

                        // Generate the shield if it is inactive.
                        if ((DefenderShieldStatus)shieldStatus == DefenderShieldStatus.Inactive || !Main.projectile.Any((Projectile p) => p.active && p.type == ModContent.ProjectileType<DefenderShield>()))
                        {
                            // Mark the shield as active.
                            shieldStatus = (float)DefenderShieldStatus.ActiveAndAiming;
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                                Utilities.NewProjectileBetter(npc.Center + npc.velocity.SafeNormalize(Vector2.UnitY) * 75f, Vector2.Zero, ModContent.ProjectileType<DefenderShield>(), 0, 0f, -1, 0f, npc.whoAmI);
                        }
                        shieldStatus = (float)DefenderShieldStatus.ActiveAndAiming;

                        // If close enough, mark us as ready.
                        defenderIsReady = 1f;
                        break;

                    // Mark the shield as static.
                    case 3:
                        shieldStatus = (float)DefenderShieldStatus.ActiveAndStatic;

                        // Create particles to indicate the sudden speed.
                        if (Main.rand.NextBool())
                        {
                            Vector2 energySpawnPosition = npc.Center + Main.rand.NextVector2Circular(30f, 20f) - npc.velocity;
                            Particle energyLeak = new SparkParticle(energySpawnPosition, npc.velocity * 0.3f, false, 30, Main.rand.NextFloat(0.9f, 1.4f), Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], 0.75f));
                            GeneralParticleHandler.SpawnParticle(energyLeak);
                        }

                        for (int i = 0; i < 30; i++)
                        {
                            float size = Pow(Main.rand.NextFloat(), 2f);
                            ModContent.GetInstance<ProfanedLavaMetaball>().SpawnParticle(npc.Center - (npc.velocity * 0.5f) + (Main.rand.NextVector2Circular(npc.width * 0.5f, npc.height * 0.5f) * size),
                                Vector2.Zero, new(Main.rand.NextFloat(15f, 20f)), 0.93f);
                        }
                        break;
                }
            }
        }

        public static void DoBehavior_FireballBulletHell(NPC npc, Player target, ref float universalAttackTimer, NPC commander)
        {
            ref float fireballRingsReleased = ref commander.Infernum().ExtraAI[1];
            float maxFireballRingsToRelease = 11f;

            // The commander moves to the center of the arena, creating a border of fire around the himself and periodically releasing
            // near solid rings of fireballs that move outwards to the border.
            if (npc.type == CommanderType)
            {
                ref float substate = ref npc.Infernum().ExtraAI[0];
                ref float xPosToMoveTo = ref npc.Infernum().ExtraAI[2];

                ref float spearStatus = ref npc.Infernum().ExtraAI[CommanderSpearStatusIndex];
                ref float drawBorder = ref npc.Infernum().ExtraAI[FireBorderShouldDrawIndex];

                float flySpeed = 25f;
                float maxMoveToCenterTime = 120f;
                float fireballCount = 20f;
                float fireballSpeed = 1.25f;
                float fireballReleaseRate = 35;
                float endOfAttackWait = 160f;

                npc.spriteDirection = (npc.DirectionTo(target.Center).X > 0f) ? 1 : -1;

                // Despawn the spear if it is active.
                if ((DefenderShieldStatus)spearStatus != DefenderShieldStatus.Inactive || Main.projectile.Any((Projectile p) => p.active && p.type == ModContent.ProjectileType<CommanderSpear>()))
                    // Mark the spear for removal.
                    spearStatus = (float)DefenderShieldStatus.MarkedForRemoval;

                if (universalAttackTimer <= fireballReleaseRate && substate == 2)
                {
                    // Create convergence particles.
                    if (universalAttackTimer < fireballReleaseRate)
                    {
                        int lightLifetime = Main.rand.Next(20, 24);
                        float squishFactor = 1.5f;
                        float scale = 0.4f;
                        Vector2 lightSpawnPosition = npc.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(240f, 630f);
                        Vector2 lightVelocity = (npc.Center - lightSpawnPosition) / lightLifetime * 1.1f;
                        Color lightColor = Color.Lerp(WayfinderSymbol.Colors[0], WayfinderSymbol.Colors[1], Main.rand.NextFloat(0f, 0.5f));
                        if (Main.rand.NextBool())
                            lightColor = Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], 0.6f);

                        SquishyLightParticle light = new(lightSpawnPosition, lightVelocity, scale, lightColor, lightLifetime, 1f, squishFactor, squishFactor * 4f);
                        GeneralParticleHandler.SpawnParticle(light);

                        // Create pulse rungs and bloom periodically.
                        if (universalAttackTimer % 15f == 0f)
                        {
                            PulseRing ring = new(npc.Center, Vector2.Zero, lightColor, 3.6f, 0f, 60);
                            GeneralParticleHandler.SpawnParticle(ring);

                            StrongBloom bloom = new(npc.Center, Vector2.Zero, lightColor, 1f, 15);
                            GeneralParticleHandler.SpawnParticle(bloom);
                        }

                        // Create energy sparks at the center.
                        CritSpark spark = new(npc.Center, Main.rand.NextVector2Circular(8f, 8f), Color.OrangeRed, Color.Gold, 5f, 6, 0.01f, 7.5f);
                        GeneralParticleHandler.SpawnParticle(spark);
                    }
                    else if (substate == 2)
                    {
                        float lightAmount = 25f;
                        for (int i = 0; i < lightAmount; i++)
                        {
                            Vector2 lightSpawnPosition = npc.Center;
                            Vector2 lightVelocity = npc.Center.DirectionTo(npc.Center + Main.rand.NextVector2Unit()) * Main.rand.NextFloat(5f, 10f);
                            int lightLifetime = Main.rand.Next(30, 54);
                            float squishFactor = 2f;
                            float scale = 0.8f;
                            Color lightColor = Color.Lerp(WayfinderSymbol.Colors[0], WayfinderSymbol.Colors[1], Main.rand.NextFloat(0f, 0.5f));
                            if (Main.rand.NextBool())
                                lightColor = Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], 0.6f);

                            SquishyLightParticle light = new(lightSpawnPosition, lightVelocity, scale, lightColor, lightLifetime, 1f, squishFactor, squishFactor * 4f);
                            GeneralParticleHandler.SpawnParticle(light);
                            if (i == 0)
                            {
                                PulseRing ring = new(npc.Center, Vector2.Zero, lightColor, 0f, 3.6f, 30);
                                GeneralParticleHandler.SpawnParticle(ring);

                                StrongBloom bloom = new(npc.Center, Vector2.Zero, lightColor, 1f, 15);
                                GeneralParticleHandler.SpawnParticle(bloom);
                            }
                        }

                        // Release a fast but less dense ring of fireballs.
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 0; i < 12; i++)
                            {
                                Vector2 fireballVelocity = (TwoPi * i / 12f).ToRotationVector2() * fireballSpeed * 4.5f;
                                ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(f =>
                                {
                                    f.ModProjectile<HolyBasicFireball>().GuardiansType = true;
                                });
                                Utilities.NewProjectileBetter(npc.Center, fireballVelocity, ModContent.ProjectileType<HolyBasicFireball>(), HolySpearDamage, 0f);
                            }
                            fireballRingsReleased++;
                        }
                    }
                }

                switch (substate)
                {
                    // The commander chooses a location to go.
                    case 0:

                        float maxXPos = CrystalPosition.X;
                        float minXPos = CenterOfGarden.X - 2370f;
                        xPosToMoveTo = Clamp(npc.Center.X, minXPos, maxXPos);

                        if (target.HasShieldBash())
                            HatGirl.SayThingWhileOwnerIsAlive(target, "Mods.InfernumMode.PetDialog.ProfanedDefenderTip");

                        // Don't deal damage while moving.
                        npc.damage = 0;

                        universalAttackTimer = 0f;
                        substate++;
                        break;

                    // The commander moves to the center of the garden.
                    case 1:
                        // Don't deal damage while moving.
                        npc.damage = 0;
                        Vector2 hoverDestination = new(xPosToMoveTo, CenterOfGarden.Y);
                        if (npc.Distance(hoverDestination) > 100f || universalAttackTimer < 10f)
                            npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathF.Min(npc.Distance(hoverDestination), flySpeed)) / 8f;
                        else
                            npc.velocity *= 0.9f;
                        if (npc.WithinRange(hoverDestination, flySpeed) || universalAttackTimer >= maxMoveToCenterTime)
                        {
                            universalAttackTimer = 0f;
                            substate++;
                        }
                        break;

                    // The commander sits still, and periodially creates rings of fireballs.
                    case 2:
                        npc.velocity *= 0.8f;

                        drawBorder = 1f;

                        if (universalAttackTimer % fireballReleaseRate == fireballReleaseRate - 1f && fireballRingsReleased < maxFireballRingsToRelease)
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                float offset = (fireballRingsReleased % 2 == 0 ? 0f : 0.5f);
                                for (int i = 0; i < fireballCount; i++)
                                {
                                    Vector2 fireballVelocity = (TwoPi * i / fireballCount + offset).ToRotationVector2() * fireballSpeed;
                                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(f =>
                                    {
                                        f.ModProjectile<HolyBasicFireball>().GuardiansType = true;
                                    });
                                    Utilities.NewProjectileBetter(npc.Center, fireballVelocity, ModContent.ProjectileType<HolyBasicFireball>(), HolySpearDamage, 0f);
                                }
                            }
                            SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceBurnSound with { PitchVariance = 0.2f, Volume = 0.75f }, target.Center);

                            // Create a fire explosion.
                            for (int i = 0; i < 30; i++)
                            {
                                Vector2 position = npc.Center + Main.rand.NextVector2Circular(50f, 50f);
                                CloudParticle fireExplosion = new(position, npc.Center.DirectionTo(position) * Main.rand.NextFloat(1f, 2f),
                                    Main.rand.NextBool() ? WayfinderSymbol.Colors[0] : WayfinderSymbol.Colors[1],
                                    Color.Gray, 50, Main.rand.NextFloat(0.45f, 1.05f), true);
                                GeneralParticleHandler.SpawnParticle(fireExplosion);
                            }

                            fireballRingsReleased++;
                            if (fireballRingsReleased >= maxFireballRingsToRelease)
                            {
                                universalAttackTimer = 0f;
                                substate++;
                            }
                        }
                        break;

                    case 3:

                        if (universalAttackTimer >= endOfAttackWait)
                        {
                            drawBorder = 0f;
                            SelectNewAttack(commander, ref universalAttackTimer);
                        }
                        break;
                }
            }

            // The defender dashes at you. Very eventful.
            else if (npc.type == DefenderType)
            {
                ref float substate = ref npc.Infernum().ExtraAI[0];
                ref float localAttackTimer = ref npc.Infernum().ExtraAI[1];
                ref float hoverOffsetX = ref npc.Infernum().ExtraAI[2];
                ref float hoverOffsetY = ref npc.Infernum().ExtraAI[3];

                ref float shieldStatus = ref npc.Infernum().ExtraAI[DefenderShieldStatusIndex];
                ref float drawDashTelegraph = ref commander.Infernum().ExtraAI[DefenderDrawDashTelegraphIndex];
                ref float dashTelegraphOpacity = ref commander.Infernum().ExtraAI[DefenderDashTelegraphOpacityIndex];

                float telegraphWaitTime = 10f;
                float dashPositioningWaitTime = 45f;
                float flySpeed = 33f;
                float dashTime = 40f;
                float dashSpeed = 16f;
                float dashAcceleration = 1.15f;

                Vector2 oldHoverOffset = new(hoverOffsetX, hoverOffsetY);

                // Generate the shield if it is inactive.
                if ((DefenderShieldStatus)shieldStatus == DefenderShieldStatus.Inactive || !Main.projectile.Any((Projectile p) => p.active && p.type == ModContent.ProjectileType<DefenderShield>()))
                {
                    // Mark the shield as active.
                    shieldStatus = (float)DefenderShieldStatus.ActiveAndAiming;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(npc.Center + npc.velocity.SafeNormalize(Vector2.UnitY) * 75f, Vector2.Zero, ModContent.ProjectileType<DefenderShield>(), 0, 0f, -1, 0f, npc.whoAmI);
                }

                switch (substate)
                {
                    // Decide the position to hover at.
                    case 0:
                        Vector2 hoverOffset = Vector2.UnitY;
                        for (int i = 0; i < 100; i++)
                        {
                            float hoverOffsetAngle = Main.rand.Next(4) * TwoPi / 4f + PiOver4;
                            hoverOffset = hoverOffsetAngle.ToRotationVector2();

                            // Leave and use the current offset to see if the direction of the new offset is perpendicular or less to the previous one. This prevents going to opposite sides and
                            // moving thorugh the player's position to reach it. The 0.01 is added on top to ensure that tiny floating point imprecisions don't become a problem.
                            float angleBetween = oldHoverOffset.AngleBetween(hoverOffset);
                            if (angleBetween is < (PiOver2 + 0.01f) and not 0)
                                break;
                        }
                        shieldStatus = (float)DefenderShieldStatus.ActiveAndAiming;
                        hoverOffsetX = hoverOffset.X;
                        hoverOffsetY = hoverOffset.Y;
                        localAttackTimer = 0;
                        npc.netUpdate = true;
                        substate++;
                        break;

                    // Hover at the position.
                    case 1:
                        if (npc.Opacity < 1f)
                        {
                            if (npc.Opacity == 0f)
                                CreateFireExplosion(npc.Center, true);
                            npc.Opacity = Clamp(npc.Opacity + 0.1f, 0f, 1f);
                            npc.dontTakeDamage = false;
                        }
                        float distance = 700f;
                        Vector2 hoverDestination = target.Center + oldHoverOffset * distance;

                        npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathF.Min(npc.Distance(hoverDestination), flySpeed)) / 8f;

                        npc.spriteDirection = (npc.DirectionTo(target.Center).X > 0f) ? 1 : -1;

                        // Move out of the way of the target if going around them.
                        if (npc.WithinRange(target.Center, 150f))
                            npc.velocity.X += target.Center.DirectionTo(npc.Center).X * 10f;

                        // Initialize the dash telegraph.
                        if (universalAttackTimer >= telegraphWaitTime && fireballRingsReleased > 0)
                            drawDashTelegraph = 1;

                        // Increase the opacity.
                        if (drawDashTelegraph == 1)
                            dashTelegraphOpacity = Clamp(dashTelegraphOpacity + 0.1f, 0f, 1f);

                        // Do not deal contact damage while positioning.
                        npc.damage = 0;

                        if ((npc.WithinRange(hoverDestination, 25f) && localAttackTimer >= dashPositioningWaitTime && fireballRingsReleased > 0f) || localAttackTimer >= 180f)
                        {
                            localAttackTimer = 0;
                            substate++;
                        }
                        break;

                    // Charge
                    case 2:
                        drawDashTelegraph = 1;
                        shieldStatus = (float)DefenderShieldStatus.ActiveAndStatic;
                        npc.velocity = npc.DirectionTo(target.Center) * dashSpeed;
                        commander.Infernum().ExtraAI[DefenderShouldGlowIndex] = 1;
                        SoundEngine.PlaySound(InfernumSoundRegistry.VassalJumpSound, target.Center);
                        SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Pitch = 0.35f, Volume = 1.6f }, target.Center);

                        if (CalamityConfig.Instance.Screenshake)
                        {
                            target.Infernum_Camera().CurrentScreenShakePower = 3f;
                            ScreenEffectSystem.SetFlashEffect(npc.Center, 0.4f, 30);
                        }

                        npc.damage = npc.defDamage;
                        substate++;
                        localAttackTimer = 0;
                        break;

                    // After a set time, reset.
                    case 3:
                        // Decrease the dash telegraph.
                        drawDashTelegraph = 1;
                        dashTelegraphOpacity = Clamp(dashTelegraphOpacity - 0.2f, 0f, 1f);
                        commander.Infernum().ExtraAI[DefenderShouldGlowIndex] = 1;
                        npc.damage = npc.defDamage;

                        // Accelerate after charging.
                        if (localAttackTimer < dashTime && npc.velocity.Length() <= 60f)
                            npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * (npc.velocity.Length() + dashAcceleration);

                        // Create particles to indicate the sudden speed.
                        if (Main.rand.NextBool(2))
                        {
                            Vector2 energySpawnPosition = npc.Center + Main.rand.NextVector2Circular(30f, 20f) - npc.velocity;
                            Particle energyLeak = new SparkParticle(energySpawnPosition, npc.velocity * 0.3f, false, 30, Main.rand.NextFloat(0.9f, 1.4f), Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], 0.75f));
                            GeneralParticleHandler.SpawnParticle(energyLeak);
                        }

                        if (localAttackTimer >= dashTime)
                        {
                            if (fireballRingsReleased >= maxFireballRingsToRelease)
                            {
                                // Fade out rapidly.
                                if (npc.Opacity == 1f)
                                    CreateFireExplosion(npc.Center, true);
                                npc.velocity *= 0.8f;
                                npc.Opacity = Clamp(npc.Opacity - 0.1f, 0f, 1f);
                                npc.dontTakeDamage = true;
                            }
                            else
                            {
                                shieldStatus = (float)DefenderShieldStatus.ActiveAndAiming;
                                substate = 0;
                                localAttackTimer = 0;
                            }
                        }
                        break;
                }
                if (fireballRingsReleased > 0)
                    localAttackTimer++;
            }
        }

        public static void DoBehavior_DefenderDeathAnimation(NPC npc, Player target, ref float universalAttackTimer, NPC commander)
        {
            ref float substate = ref commander.Infernum().ExtraAI[0];
            ref float catchingLeftHandXOG = ref commander.Infernum().ExtraAI[1];
            ref float catchingLeftHandYOG = ref commander.Infernum().ExtraAI[2];
            ref float catchingRightHandXOG = ref commander.Infernum().ExtraAI[3];
            ref float catchingRightHandYOG = ref commander.Infernum().ExtraAI[4];

            ref float leftHandIndex = ref commander.Infernum().ExtraAI[LeftHandIndex];
            ref float rightHandIndex = ref commander.Infernum().ExtraAI[RightHandIndex];
            ref float moveHands = ref commander.Infernum().ExtraAI[HandsShouldUseNotDefaultPositionIndex];

            if ((rightHandIndex == 0 && leftHandIndex == 0))
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    leftHandIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X - 160, (int)npc.Center.Y, ModContent.NPCType<EtherealHand>(), 0, -1);
                    rightHandIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X + 160, (int)npc.Center.Y, ModContent.NPCType<EtherealHand>(), 0, 1);
                    npc.netUpdate = true;
                }
            }

            // Ensure they are fully faded in.
            npc.Opacity = 1f;
            // Stop this drawing.
            commander.Infernum().ExtraAI[FireBorderShouldDrawIndex] = 0;

            // The commander spawns in the hands, moves towards your top right, pulls the defender into its hands, spins it around itself twice then lobs it at the player at mach 10.
            if (npc.type == CommanderType)
            {
                ref float spearStatus = ref commander.Infernum().ExtraAI[CommanderSpearStatusIndex];

                float flySpeed = 24f;
                float symbolSpawnRate = 15f;

                Vector2 focusPosition = target.Center + new Vector2(0f, target.gfxOffY) + (-0.4f).ToRotationVector2() * 70f;
                target.Infernum_Camera().ScreenFocusInterpolant = 1f;
                target.Infernum_Camera().ScreenFocusPosition = focusPosition;

                if (substate < 6f)
                {
                    target.Infernum_Camera().CurrentScreenShakePower = 2f;

                    // Do not take damage.
                    npc.dontTakeDamage = true;
                    // Do not deal damage.
                    npc.damage = 0;
                }

                // Spawn cool symbols.
                if (universalAttackTimer % symbolSpawnRate == symbolSpawnRate - 1f)
                {
                    Vector2 position = npc.Center + npc.DirectionTo(target.Center).SafeNormalize(Vector2.UnitY) * 50f + Main.rand.NextVector2Circular(250f, 250f);
                    Vector2 velocity = -Vector2.UnitY.RotatedBy(Main.rand.NextFloat(0.1f, 0.4f)) * Main.rand.NextFloat(1.5f, 2f);
                    Color color = Color.Lerp(WayfinderSymbol.Colors[0], WayfinderSymbol.Colors[1], Main.rand.NextFloat(1f));
                    Particle jojo = new ProfanedSymbolParticle(position, velocity, color, 0.8f, 120);
                    GeneralParticleHandler.SpawnParticle(jojo);
                }

                switch (substate)
                {
                    // Move to the top right of the target.
                    case 0:
                    case 1:
                    case 2:
                    case 4:
                    case 5:
                    case 6:
                        Vector2 hoverDestination = target.Center + new Vector2(800f, -325f);
                        if (universalAttackTimer == 1)
                            npc.Center = hoverDestination;

                        if (npc.Distance(hoverDestination) > 2f)
                            npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathF.Min(npc.Distance(hoverDestination), flySpeed)) / 8f;
                        else
                            npc.Center = hoverDestination;
                        npc.spriteDirection = (npc.DirectionTo(target.Center).X > 0f) ? 1 : -1;

                        // Despawn the spear if it is active.
                        if ((DefenderShieldStatus)spearStatus != DefenderShieldStatus.Inactive || Main.projectile.Any((Projectile p) => p.active && p.type == ModContent.ProjectileType<CommanderSpear>()))
                            // Mark the spear for removal.
                            spearStatus = (float)DefenderShieldStatus.MarkedForRemoval;
                        break;

                    case 3:
                        npc.velocity *= 0.9f;
                        break;
                }
            }

            // The defender begins to ram at you from the left vertically, but is pulled up by the commander before reaching you. It then glues to the commanders hands, while squirming around
            // on the spot and changing its sprite direction to indicate struggling.
            else if (npc.type == DefenderType)
            {
                ref float localAttackTimer = ref npc.Infernum().ExtraAI[0];

                ref float shieldStatus = ref npc.Infernum().ExtraAI[DefenderShieldStatusIndex];

                Vector2 originalRightHandPos = new(catchingRightHandXOG, catchingRightHandYOG);
                Vector2 originalLeftHandPos = new(catchingLeftHandXOG, catchingLeftHandYOG);
                float flySpeed = 20f;
                float chargeDelay = 60f;
                float chargeSpeed = 35f;
                float pullbackDelay = 25f;
                float pullBackTime = 30f;
                float reelbackTime = 50f;
                float launchTime = 30f;
                float yeetSpeed = 40f;

                // Close the HP bar.
                npc.Calamity().ShouldCloseHPBar = true;
                // Do not take damage.
                npc.dontTakeDamage = true;
                // Do not deal damage either.
                npc.damage = 0;

                switch (substate)
                {
                    // Hover to the right of the target.
                    case 0:
                        Vector2 hoverDestination = target.Center + new Vector2(1200f, 0f);

                        if (localAttackTimer == 1)
                        {
                            npc.Center = hoverDestination;
                            if (Main.myPlayer == npc.target && Main.netMode == NetmodeID.SinglePlayer)
                            {
                                BlockerSystem.Start(false, true, () =>
                                {
                                    return NPC.AnyNPCs(DefenderType);
                                });
                            }
                        }
                        npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathF.Min(npc.Distance(hoverDestination), flySpeed)) / 8f;
                        npc.spriteDirection = (npc.DirectionTo(target.Center).X > 0f) ? 1 : -1;

                        // Generate the shield if it is inactive.
                        if ((DefenderShieldStatus)shieldStatus == DefenderShieldStatus.Inactive || !Main.projectile.Any((Projectile p) => p.active && p.type == ModContent.ProjectileType<DefenderShield>()))
                        {
                            // Mark the shield as active.
                            shieldStatus = (float)DefenderShieldStatus.ActiveAndAiming;
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                                Utilities.NewProjectileBetter(npc.Center + npc.velocity.SafeNormalize(Vector2.UnitY) * 75f, Vector2.Zero, ModContent.ProjectileType<DefenderShield>(), 0, 0f, -1, 0f, npc.whoAmI);
                        }
                        shieldStatus = (float)DefenderShieldStatus.ActiveAndAiming;

                        if (localAttackTimer == 0)
                            typeof(MoonlordDeathDrama).GetField("whitening", Utilities.UniversalBindingFlags).SetValue(null, 1f);//MoonlordDeathDrama.RequestLight(1f/*1f - Utils.GetLerpValue(15f, 45f, universalAttackTimer, true)*/, target.Center);

                        if (npc.WithinRange(hoverDestination, 20f) && localAttackTimer >= chargeDelay)
                        {
                            substate++;
                            localAttackTimer = 0;
                            return;
                        }
                        break;

                    // Charge.
                    case 1:
                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                        substate++;
                        localAttackTimer = 0;
                        return;

                    // Halfway through, advance the main substate.
                    case 2:
                        if (localAttackTimer >= pullbackDelay)
                        {
                            substate++;
                            localAttackTimer = 0;
                            return;
                        }
                        break;

                    // Gets pulled back to the hand by the commander.
                    case 3:
                        moveHands = 1f;

                        RightHandPosition = new(-150f, -75f);
                        LeftHandPosition = new(-40f, 25f);

                        catchingRightHandXOG = RightHandPosition.X;
                        catchingRightHandYOG = RightHandPosition.Y;

                        catchingLeftHandXOG = LeftHandPosition.X;
                        catchingLeftHandYOG = LeftHandPosition.Y;

                        if (localAttackTimer == 1)
                            npc.velocity = npc.SafeDirectionTo(RightHandPosition + commander.Center) * (npc.Distance(RightHandPosition + commander.Center) / pullBackTime);

                        if (localAttackTimer >= pullBackTime)
                        {
                            localAttackTimer = 0;
                            substate++;
                            return;
                        }
                        break;

                    // Stick to the hand. The hands reel back.
                    case 4:
                        float rightHandMoveInterpolant = Utilities.EaseInOutCubic(localAttackTimer / reelbackTime);
                        float leftHandMoveInterpolant = CalamityUtils.SineInOutEasing(localAttackTimer / reelbackTime, 0);
                        moveHands = 1f;

                        npc.velocity = Vector2.Zero;
                        npc.Center = RightHandPosition + commander.Center;

                        // Right hand.
                        RightHandPosition = originalRightHandPos.RotatedBy(3f * rightHandMoveInterpolant);
                        // Left hand.
                        LeftHandPosition = Vector2.Lerp(originalLeftHandPos, new Vector2(-125f, 70f), leftHandMoveInterpolant);

                        if (localAttackTimer >= reelbackTime)
                        {
                            // Reset the default positions for the next attack.
                            catchingRightHandXOG = RightHandPosition.X;
                            catchingRightHandYOG = RightHandPosition.Y;

                            catchingLeftHandXOG = LeftHandPosition.X;
                            catchingLeftHandYOG = LeftHandPosition.Y;

                            localAttackTimer = 0;
                            substate++;
                            return;
                        }
                        break;

                    // Continue to stick to the hands. They will stop at the correct position.
                    case 5:
                        rightHandMoveInterpolant = CalamityUtils.ExpInEasing(localAttackTimer / launchTime, 0);
                        leftHandMoveInterpolant = CalamityUtils.SineInOutEasing(localAttackTimer / launchTime, 0);
                        moveHands = 1f;

                        RightHandPosition = originalRightHandPos.RotatedBy(-4f * rightHandMoveInterpolant);
                        LeftHandPosition = Vector2.Lerp(originalLeftHandPos, new Vector2(-55f, 40f), leftHandMoveInterpolant);

                        npc.Center = RightHandPosition + commander.Center;

                        if (localAttackTimer >= launchTime)
                        {
                            RightHandPosition = Vector2.Zero;
                            LeftHandPosition = Vector2.Zero;

                            // Launch at the target.
                            npc.velocity = npc.DirectionTo(target.Center) * yeetSpeed;
                            npc.damage = 300;

                            localAttackTimer = 0;
                            substate++;
                            commander.Infernum().ExtraAI[DefenderHasBeenYeetedIndex] = 1f;
                            return;
                        }
                        break;
                }

                localAttackTimer++;
            }
        }
        #endregion

        #region Commander Attacks
        public static void DoBehavior_LargeGeyserAndCharge(NPC npc, Player target, ref float universalAttackTimer)
        {
            ref float substate = ref npc.Infernum().ExtraAI[0];

            ref float spearStatus = ref npc.Infernum().ExtraAI[CommanderSpearStatusIndex];
            ref float spearRotation = ref npc.Infernum().ExtraAI[CommanderSpearRotationIndex];
            // The commander will check this as well, and draw it instead if the defender is not present.
            ref float drawDashTelegraph = ref npc.Infernum().ExtraAI[DefenderDrawDashTelegraphIndex];

            float moveUnderAndWaitTime = 20f;
            float maxMoveUnderAndWaitTime = 90f;
            float recoilDownwardsTime = 20f;
            float flyUpwardsDelay = 60f;
            float flyUpwardsSpeed = 60f;
            int rockAmount = 23;
            float curveTowardsTargetDelay = 30f;
            float completeSlowdownLength = 25f + curveTowardsTargetDelay;
            float aimTime = 30f + completeSlowdownLength;
            float aimedChargeSpeed = 40f;
            float chargeAcceleration = 1.5f;
            float maxChargeLength = 85f;
            float afterImpactWaitLength = 10f;

            switch (substate)
            {
                // Move under the target and telegraph where the geyser will spawn.
                case 0:
                    float flySpeed = 25f;
                    Vector2 hoverDestination = target.Center + new Vector2(0, 400f);

                    npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathF.Min(npc.Distance(hoverDestination), flySpeed)) / 8f;

                    npc.spriteDirection = (npc.DirectionTo(target.Center).X > 0f) ? 1 : -1;

                    // Move out of the way of the target if going around them.
                    if (npc.WithinRange(target.Center, 150f))
                        npc.velocity.X += target.Center.DirectionTo(npc.Center).X * 10f;

                    // Do not deal contact damage.
                    npc.damage = 0;

                    // Despawn the spear if it is active.
                    if ((DefenderShieldStatus)spearStatus != DefenderShieldStatus.Inactive || Main.projectile.Any((Projectile p) => p.active && p.type == ModContent.ProjectileType<CommanderSpear>()))
                        // Mark the spear for removal.
                        spearStatus = (float)DefenderShieldStatus.MarkedForRemoval;

                    // If close enough.
                    if (npc.WithinRange(hoverDestination, 100f))
                    {
                        // Create a bunch of lava particles under the commander on the players bottom of the screen.
                        for (int i = 0; i < 6; i++)
                        {
                            Vector2 position = new(npc.Center.X + Main.rand.NextFloat(-250f, 250f), Main.screenHeight + -100f + Main.screenPosition.Y + Main.rand.NextFloat(-70f, 70f));
                            Particle lavaParticle = new GlowyLightParticle(position, -Vector2.UnitY.RotatedBy(Main.rand.NextFloat(-0.5f, 0.5f)) * Main.rand.NextFloat(6f, 9f),
                                Main.rand.NextBool() ? WayfinderSymbol.Colors[2] : Color.OrangeRed, 60, Main.rand.NextFloat(0.75f, 1.25f), Main.rand.NextFloat(0.9f, 1.1f), true);
                            GeneralParticleHandler.SpawnParticle(lavaParticle);
                        }

                        if (universalAttackTimer >= moveUnderAndWaitTime)
                        {
                            universalAttackTimer = 0;
                            substate++;
                        }
                    }
                    // If it takes too long, move onto the next phase.
                    else if (universalAttackTimer >= maxMoveUnderAndWaitTime)
                    {
                        universalAttackTimer = 0;
                        substate++;
                    }
                    break;

                // Prepare to launch upwards alongside a huge lava geyser.
                case 1:
                    npc.spriteDirection = (npc.DirectionTo(target.Center).X > 0f) ? 1 : -1;

                    if (universalAttackTimer < recoilDownwardsTime)
                    {
                        npc.velocity.X *= 0.85f;
                        npc.velocity.Y = 3.4f;
                    }
                    else if (universalAttackTimer == recoilDownwardsTime)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(pillar => pillar.ModProjectile<LavaEruptionPillar>().BigVersion = true);
                            Vector2 center = new(npc.Center.X, (Main.maxTilesY * 16f) - 50f);
                            Utilities.NewProjectileBetter(center, Vector2.Zero, ModContent.ProjectileType<LavaEruptionPillar>(), LavaPillarDamage, 0f);

                            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(pillar => pillar.ModProjectile<LavaEruptionPillar>().BigVersion = true);
                            Utilities.NewProjectileBetter(center + new Vector2(-2700f, 0f), Vector2.Zero, ModContent.ProjectileType<LavaEruptionPillar>(), LavaPillarDamage, 0f);
                            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(pillar => pillar.ModProjectile<LavaEruptionPillar>().BigVersion = true);
                            Utilities.NewProjectileBetter(center + new Vector2(2700f, 0f), Vector2.Zero, ModContent.ProjectileType<LavaEruptionPillar>(), LavaPillarDamage, 0f);
                        }
                    }

                    // Create a bunch of lava particles under the commander on the players bottom of the screen.
                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 position = new(npc.Center.X + Main.rand.NextFloat(-250f, 250f), Main.screenHeight + -100f + Main.screenPosition.Y + Main.rand.NextFloat(-70f, 70f));
                        Particle lavaParticle = new GlowyLightParticle(position, -Vector2.UnitY.RotatedBy(Main.rand.NextFloat(-0.5f, 0.5f)) * Main.rand.NextFloat(6f, 9f),
                            Main.rand.NextBool() ? WayfinderSymbol.Colors[2] : Color.OrangeRed, 60, Main.rand.NextFloat(0.75f, 1.25f), Main.rand.NextFloat(0.9f, 1.1f), true);
                        GeneralParticleHandler.SpawnParticle(lavaParticle);
                    }

                    if (universalAttackTimer >= recoilDownwardsTime + flyUpwardsDelay)
                    {
                        SoundEngine.PlaySound(InfernumSoundRegistry.VassalJumpSound with { Pitch = -0.2f, Volume = 1.4f }, target.Center);
                        float lerpValue = Clamp(Utils.GetLerpValue(300, 500, npc.Center.Y - target.Center.Y, false), 0f, 0.35f);
                        npc.velocity = -Vector2.UnitY * flyUpwardsSpeed * (1 + lerpValue);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 center = new(npc.Center.X, (Main.maxTilesY * 16f) - 50f);
                            // Also spawn a bunch of rocks with slightly random direction and speed.
                            for (int i = 0; i < rockAmount; i++)
                            {

                                ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(rock =>
                                {
                                    rock.ModProjectile<ProfanedRock>().RockTypeVarient = (int)ProfanedRock.RockType.Gravity;
                                });

                                Vector2 direction = center - Vector2.UnitY.RotatedBy(Main.rand.NextFloat(-0.7f, 0.7f));
                                Vector2 velocity = center.DirectionTo(direction) * Main.rand.NextFloat(34f, 40f);
                                velocity.Y *= 1.16f;
                                Utilities.NewProjectileBetter(center, velocity, ModContent.ProjectileType<ProfanedRock>(), ProfanedRockDamage, 0f, -1, 0f, npc.whoAmI);
                            }
                        }
                        if (CalamityConfig.Instance.Screenshake)
                        {
                            target.Infernum_Camera().CurrentScreenShakePower = 6f;
                            ScreenEffectSystem.SetBlurEffect(npc.Center, 0.75f, 45);
                        }
                        universalAttackTimer = 0;
                        substate++;
                    }
                    break;

                // Start to curve towards the target, before slowing to a halt and aiming at them, and then charging rapidly.
                case 2:
                    if (universalAttackTimer < curveTowardsTargetDelay)
                        npc.velocity.Y *= 0.96f;

                    // Create the spear, and make it aim upwards.
                    // Spawn the spear if it does not exist.
                    npc.CreateSpearAndAimAtTarget(ref spearStatus, ref spearRotation, target);

                    npc.spriteDirection = (npc.DirectionTo(target.Center).X > 0f) ? 1 : -1;

                    // Draw an aimed telegraph.
                    drawDashTelegraph = 1f;

                    if (universalAttackTimer > curveTowardsTargetDelay && universalAttackTimer <= completeSlowdownLength)
                    {
                        float interpolant = (universalAttackTimer - curveTowardsTargetDelay) / (completeSlowdownLength - curveTowardsTargetDelay);
                        float xAmount = Lerp(1.2f, 0f, interpolant);
                        npc.velocity.X += npc.SafeDirectionTo(target.Center).X * xAmount;
                        npc.velocity.Y += 0.35f;
                    }

                    // Charge at the target.
                    else if (universalAttackTimer >= aimTime)
                    {
                        npc.velocity = npc.SafeDirectionTo(target.Center) * aimedChargeSpeed;
                        SoundEngine.PlaySound(InfernumSoundRegistry.VassalJumpSound with { Pitch = -0.2f, Volume = 1.4f }, target.Center);
                        SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceBurnSound, target.Center);

                        universalAttackTimer = 0;
                        substate++;
                    }
                    break;

                case 3:
                    // Create particles to indicate the sudden speed.
                    if (Main.rand.NextBool())
                    {
                        Vector2 energySpawnPosition = npc.Center + Main.rand.NextVector2Circular(30f, 20f) - npc.velocity;
                        Particle energyLeak = new SparkParticle(energySpawnPosition, npc.velocity * 0.3f, false, 30, Main.rand.NextFloat(0.9f, 1.4f), Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], 0.75f));
                        GeneralParticleHandler.SpawnParticle(energyLeak);
                    }

                    for (int i = 0; i < 50; i++)
                    {
                        // Bias towards lower values. 
                        float size = Pow(Main.rand.NextFloat(), 2f);
                        ModContent.GetInstance<ProfanedLavaMetaball>().SpawnParticle(npc.Center - (npc.velocity * 0.5f) + (Main.rand.NextVector2Circular(npc.width * 0.5f, npc.height * 0.5f) * size), Vector2.Zero, new(Main.rand.NextFloat(25f, 35f)));
                    }

                    drawDashTelegraph = 0f;

                    // Accelerate after charging.
                    if (universalAttackTimer < maxChargeLength && npc.velocity.Length() <= 180f)
                        npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * (npc.velocity.Length() + chargeAcceleration);

                    // Stop the spear aiming.
                    spearStatus = (float)DefenderShieldStatus.ActiveAndStatic;

                    // Leave a trail of fire.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(npc.Center + Main.rand.NextVector2Circular(16f, 16f), npc.velocity * 0.3f, ModContent.ProjectileType<LingeringHolyFire>(), LingeringFireDamage, 0f);

                    if ((Collision.SolidCollision(npc.Center, (int)(npc.width * 0.85f), (int)(npc.height * 0.85f)) && npc.Center.Y > target.Center.Y) || universalAttackTimer >= maxChargeLength)
                    {
                        SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Pitch = 0.35f, Volume = 1.6f }, target.Center);
                        SoundEngine.PlaySound(npc.HitSound.Value with { Volume = 3f }, target.Center);

                        if (CalamityConfig.Instance.Screenshake)
                        {
                            target.Infernum_Camera().CurrentScreenShakePower = 12f;
                            ScreenEffectSystem.SetFlashEffect(npc.Center, 0.5f, 45);
                        }
                        // Hurt itself.
                        //npc.life -= (int)(npc.lifeMax * 0.002f);

                        npc.velocity = -npc.velocity.SafeNormalize(Vector2.UnitY) * 4.6f;
                        Vector2 impactCenter = npc.Center + new Vector2(0f, 55f);

                        for (int j = 0; j < 40; j++)
                        {
                            Particle rock = new ProfanedRockParticle(impactCenter, -Vector2.UnitY.RotatedBy(Main.rand.NextFloat(-0.9f, 0.9f)) * Main.rand.NextFloat(6f, 9f), Color.White, Main.rand.NextFloat(0.85f, 1.15f), 60, Main.rand.NextFloat(0f, 0.2f));
                            GeneralParticleHandler.SpawnParticle(rock);
                        }

                        // Spawn a bunch of light particles.
                        for (int i = 0; i < 20; i++)
                        {
                            Vector2 position = impactCenter + Main.rand.NextVector2Circular(20f, 20f);
                            Particle light = new GlowyLightParticle(position, impactCenter.DirectionTo(position) * Main.rand.NextFloat(3f, 5f),
                                Main.rand.NextBool() ? WayfinderSymbol.Colors[1] : Color.OrangeRed, 60, Main.rand.NextFloat(0.85f, 1.15f), Main.rand.NextFloat(0.95f, 1.05f), true);
                            GeneralParticleHandler.SpawnParticle(light);
                        }

                        // Create a fire explosion.
                        for (int i = 0; i < 30; i++)
                        {
                            MediumMistParticle fireExplosion = new(impactCenter + Main.rand.NextVector2Circular(80f, 80f), Vector2.Zero,
                                Main.rand.NextBool() ? WayfinderSymbol.Colors[0] : WayfinderSymbol.Colors[1],
                                Color.Gray, Main.rand.NextFloat(0.85f, 1.15f), Main.rand.NextFloat(220f, 250f));
                            GeneralParticleHandler.SpawnParticle(fireExplosion);
                        }


                        for (int i = 0; i < 70; i++)
                            ModContent.GetInstance<ProfanedLavaMetaball>().SpawnParticle(npc.Center + Main.rand.NextVector2Circular(150f, 150f), Vector2.Zero, new(Main.rand.NextFloat(52f, 85f)));

                        // Spawn crosses/rocks.
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float crossCount = 24f;
                            for (int j = 0; j < crossCount; j++)
                            {
                                Vector2 crossVelocity = (TwoPi * j / crossCount).ToRotationVector2() * 7f;
                                int type = ModContent.ProjectileType<HolyCross>();
                                if (Main.rand.NextBool(4))
                                {
                                    type = ModContent.ProjectileType<ProfanedRock>();
                                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(r =>
                                    {
                                        r.ModProjectile<ProfanedRock>().RockTypeVarient = (int)ProfanedRock.RockType.Accelerating;
                                    });
                                }
                                Utilities.NewProjectileBetter(impactCenter + crossVelocity, crossVelocity, type, ProfanedRockDamage, 0f);
                            }
                        }

                        // Recoil backwards.
                        npc.velocity = -npc.velocity.SafeNormalize(Vector2.UnitY) * 3.5f;

                        universalAttackTimer = 0;
                        substate++;
                    }
                    break;

                case 4:
                    npc.velocity *= 0.98f;
                    if (universalAttackTimer >= afterImpactWaitLength)
                    {
                        spearStatus = (float)DefenderShieldStatus.MarkedForRemoval;
                        SelectNewAttack(npc, ref universalAttackTimer);
                    }
                    break;
            }

        }

        public static void DoBehavior_DogmaLaserBall(NPC npc, Player target, ref float universalAttackTimer)
        {
            ref float substate = ref npc.Infernum().ExtraAI[0];

            ref float spearStatus = ref npc.Infernum().ExtraAI[CommanderSpearStatusIndex];
            ref float spearRotation = ref npc.Infernum().ExtraAI[CommanderSpearRotationIndex];
            ref float fireballLaunchStatus = ref npc.Infernum().ExtraAI[CommanderDogmaFireballHasBeenYeetedIndex];

            float chargeUpTime = 25f;
            float minDistanceToMove = 800f;
            float flySpeed = 16f;

            // Face the target.
            npc.spriteDirection = (npc.DirectionTo(target.Center).X > 0f) ? 1 : -1;
            Vector2 spearTip = npc.Center + spearRotation.ToRotationVector2() * 195f;

            switch (substate)
            {
                // Move upwards.
                case 0:

                    // Aim towards the target if far enough.
                    npc.velocity = npc.SafeDirectionTo(target.Center) * (flySpeed * npc.Distance(target.Center) / minDistanceToMove);

                    if (npc.Center.Y > CrystalPosition.Y)
                        npc.velocity.Y = -13f;
                    else
                        npc.velocity.Y = 0f;

                    // Create the spear, and make it aim upwards.
                    // Spawn the spear if it does not exist.
                    npc.CreateSpearAndAimAtTarget(ref spearStatus, ref spearRotation, target);


                    spearStatus = (float)DefenderShieldStatus.ActiveAndAiming;
                    substate++;
                    universalAttackTimer = 0;
                    return;
                // Slow down and point the spear upwards.
                case 1:
                    // Slow down.
                    npc.velocity *= 0.94f;

                    // Make the spear rotation point upwards.
                    float idealRotation = -PiOver2;
                    spearRotation = spearRotation.AngleTowards(idealRotation, 0.2f);

                    for (int i = 0; i < 1; i++)
                    {

                        Color energyColor = Color.Lerp(Color.Pink, Color.Yellow, Main.rand.NextFloat(0.7f));
                        Vector2 energySpawnPosition = spearTip + Main.rand.NextVector2Unit() * Main.rand.NextFloat(116f, 166f);
                        Vector2 energyVelocity = (spearTip - energySpawnPosition) * 0.032f;
                        SquishyLightParticle laserEnergy = new(energySpawnPosition, energyVelocity, 1.5f, energyColor, 32, 1f, 4f);
                        GeneralParticleHandler.SpawnParticle(laserEnergy);
                    }

                    if (npc.velocity.Length() < 2f && spearRotation - -PiOver2 <= 0.3f && !Main.projectile.Any(p => p.type == ModContent.ProjectileType<HolyDogmaFireball>() && p.active))
                    {
                        npc.velocity = Vector2.Zero;
                        universalAttackTimer = 0;
                        substate++;
                        return;
                    }
                    break;

                // Charge and create a large fireball at the spear position.
                case 2:
                    // Make the spear rotation point upwards.
                    idealRotation = -PiOver2;
                    spearRotation = spearRotation.AngleTowards(idealRotation, 0.2f);

                    if (universalAttackTimer == 1)
                        Utilities.NewProjectileBetter(spearTip, Vector2.Zero, ModContent.ProjectileType<HolyDogmaFireball>(), HolyFireBeamDamage, 0f);

                    if (universalAttackTimer <= chargeUpTime)
                    {
                        float chargeUpInterpolant = Utils.GetLerpValue(0f, chargeUpTime, universalAttackTimer, true);
                        for (int i = 0; i < 2; i++)
                        {
                            if (Main.rand.NextFloat() > chargeUpInterpolant)
                                continue;

                            Color energyColor = Color.Lerp(Color.Pink, Color.Yellow, Main.rand.NextFloat(0.7f));
                            Vector2 energySpawnPosition = spearTip + Main.rand.NextVector2Unit() * Main.rand.NextFloat(116f, 166f);
                            Vector2 energyVelocity = (spearTip - energySpawnPosition) * 0.032f;
                            SquishyLightParticle laserEnergy = new(energySpawnPosition, energyVelocity, 1.5f, energyColor, 32, 1f, 4f);
                            GeneralParticleHandler.SpawnParticle(laserEnergy);
                        }
                    }
                    else
                    {
                        universalAttackTimer = 0;
                        substate++;
                    }
                    break;

                // Wait until the fireball informs the commander that it has launched itself.
                case 3:
                    // If it has, or a set amount of time has passed to avoid softlocking the fight, proceede to the next attack.
                    // The fireball will fire one more set of lasers at consistent angles, to add extra difficulty.
                    if (fireballLaunchStatus == 1 || universalAttackTimer >= 300f)
                    {
                        fireballLaunchStatus = 0;
                        SelectNewAttack(npc, ref universalAttackTimer);
                    }
                    break;
            }
        }

        public static void DoBehavior_BerdlySpears(NPC npc, Player target, ref float universalAttackTimer)
        {
            ref float substate = ref npc.Infernum().ExtraAI[0];

            ref float spearStatus = ref npc.Infernum().ExtraAI[CommanderSpearStatusIndex];
            ref float spearRotation = ref npc.Infernum().ExtraAI[CommanderSpearRotationIndex];

            float hoverDistance = 700f;
            float flySpeed = 18f;
            float shootDelay = 90f;
            float totalSpearThrows = 3f;
            float totalShootLength = shootDelay * totalSpearThrows;
            float spearSpeed = 20f;
            float minDistanceToSpeedUpAt = 700f;

            float endOfAttackDelay = 45f;

            npc.spriteDirection = (npc.DirectionTo(target.Center).X > 0f) ? 1 : -1;

            // Spawn the spear if it does not exist.
            npc.CreateSpearAndAimAtTarget(ref spearStatus, ref spearRotation, target);

            Vector2 spearTip = npc.Center + spearRotation.ToRotationVector2() * 195f;

            switch (substate)
            {
                case 0:
                    // Speed up if far enough away.
                    if (!npc.WithinRange(target.Center, minDistanceToSpeedUpAt))
                        flySpeed = Clamp(SmoothStep(flySpeed, flySpeed * 2f, npc.Distance(target.Center) / minDistanceToSpeedUpAt * 2f), 0f, flySpeed * 2f);
                    Vector2 hoverDestination = target.Center + Vector2.UnitX * (hoverDistance * Math.Sign(npc.Center.X - target.Center.X));
                    npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathF.Min(npc.Distance(hoverDestination), flySpeed)) / 8f;

                    if (npc.WithinRange(hoverDestination, 50f) || universalAttackTimer >= 45f)
                    {
                        substate++;
                        universalAttackTimer = 0;
                        return;
                    }
                    break;

                case 1:
                    // Hover to the side.
                    hoverDestination = target.Center + Vector2.UnitX * (hoverDistance * Math.Sign(npc.Center.X - target.Center.X));
                    npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathF.Min(npc.Distance(hoverDestination), flySpeed)) / 8f;

                    if (universalAttackTimer % shootDelay >= shootDelay * 0.60f)
                    {
                        Color energyColor = Color.Lerp(Color.Pink, Color.Yellow, Main.rand.NextFloat(0.7f));
                        Vector2 energySpawnPosition = spearTip + Main.rand.NextVector2Unit() * Main.rand.NextFloat(56f, 116f);
                        Vector2 energyVelocity = (spearTip - energySpawnPosition) * 0.12f;
                        SparkParticle laserEnergy = new(energySpawnPosition, energyVelocity, false, 15, 1f, energyColor);
                        GeneralParticleHandler.SpawnParticle(laserEnergy);
                    }

                    if (universalAttackTimer % shootDelay == 0f && universalAttackTimer <= totalShootLength)
                    {
                        SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceSpearHitSound with { PitchVariance = 0.4f }, target.Center);
                        SoundEngine.PlaySound(SoundID.DD2_LightningBugZap, target.Center);

                        ScreenEffectSystem.SetFlashEffect(spearTip, 0.5f, 45);

                        if (CalamityConfig.Instance.Screenshake)
                            target.Infernum_Camera().CurrentScreenShakePower = 4f;

                        Vector2 spearVelocity = npc.SafeDirectionTo(target.Center) * Main.rand.NextFloat(spearSpeed * 0.9f, spearSpeed * 1.1f);
                        // Recoil back slightly.
                        npc.velocity = -spearVelocity.SafeNormalize(Vector2.UnitY) * 10f;

                        CreateFireExplosion(spearTip, false);
                        for (int i = 0; i < 8; i++)
                        {
                            Vector2 sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f, 11f);
                            Color sparkColor = Color.Lerp(WayfinderSymbol.Colors[0], WayfinderSymbol.Colors[1], Main.rand.NextFloat(0.4f, 1f));
                            GeneralParticleHandler.SpawnParticle(new SparkParticle(spearTip, sparkVelocity, false, 60, 2f, sparkColor));

                            sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 10f);
                            Color arcColor = Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], Main.rand.NextFloat(0.3f, 1f));
                            GeneralParticleHandler.SpawnParticle(new ElectricArc(spearTip, sparkVelocity, arcColor, 0.84f, 30));
                        }


                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(s =>
                            {
                                s.ModProjectile<HolySineSpear>().WaveOffset = Main.rand.NextFloat(0f, Tau);
                            });
                            Utilities.NewProjectileBetter(spearTip, spearVelocity, ModContent.ProjectileType<HolySineSpear>(), CommanderSpearDamage, 0f, ai1: 1f);
                        }
                    }
                    if (universalAttackTimer >= totalShootLength + endOfAttackDelay)
                        SelectNewAttack(npc, ref universalAttackTimer);
                    break;
            }
        }

        public static void DoBehavior_SpearSpinThrow(NPC npc, Player target, ref float universalAttackTimer)
        {
            ref float substate = ref npc.Infernum().ExtraAI[0];
            ref float angleToStab = ref npc.Infernum().ExtraAI[1];
            ref float exitPosX = ref npc.Infernum().ExtraAI[2];
            ref float exitPosY = ref npc.Infernum().ExtraAI[3];
            ref float spearRotationStartingOffset = ref npc.Infernum().ExtraAI[4];
            ref float hoverXOffset = ref npc.Infernum().ExtraAI[5];

            ref float spearStatus = ref npc.Infernum().ExtraAI[CommanderSpearStatusIndex];
            ref float spearRotation = ref npc.Infernum().ExtraAI[CommanderSpearRotationIndex];
            ref float spearPosOffset = ref npc.Infernum().ExtraAI[CommanderSpearPositionOffsetIndex];

            float flySpeed = 26f;
            float spearSpinTime = 30f;
            float spearReelbackTime = spearSpinTime + 20f;
            float spearStabTime = spearReelbackTime + 10f;
            float spearThrowSpeed = 12f;
            float afterAttackWaitTime = 65f;
            float spearsAmount = 8f;
            float spearDistance = 550f;
            float minDistanceToSpeedUpAt = 1000f;

            npc.spriteDirection = (npc.DirectionTo(target.Center).X > 0f) ? 1 : -1;

            switch (substate)
            {
                // Choose the angle to aim at, as well as the position for the exit portal.
                case 0:
                    angleToStab = (TwoPi + Main.rand.NextFloat(0f, TwoPi)) * Main.rand.NextFromList(-1f, 1f);
                    hoverXOffset = 400f * -Sign(npc.SafeDirectionTo(target.Center).X);
                    exitPosX = Main.rand.NextFloat(0f, 400f) * Main.rand.NextFromList(-1f, 1f);
                    exitPosY = Main.rand.NextFloat(-300f, -400f);
                    npc.netUpdate = true;

                    // Spawn the spear if it does not exist.
                    npc.CreateSpearAndAimAtTarget(ref spearStatus, ref spearRotation, target);

                    substate++;
                    universalAttackTimer = 0f;
                    return;

                // Move to the offset.
                case 1:
                    // Speed up if far enough away.
                    if (!npc.WithinRange(target.Center, minDistanceToSpeedUpAt))
                        flySpeed = Clamp(SmoothStep(flySpeed, flySpeed * 2f, npc.Distance(target.Center) / minDistanceToSpeedUpAt * 2f), 0f, flySpeed * 2f);

                    // Hover to the side.
                    Vector2 hoverDestination = target.Center + new Vector2(hoverXOffset, -350f);
                    npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathF.Min(npc.Distance(hoverDestination), flySpeed)) / 8f;

                    // Make the spear rotation point to the player.
                    float idealRotation = npc.SafeDirectionTo(target.Center).ToRotation();
                    spearRotation = spearRotation.AngleTowards(idealRotation, 0.2f);

                    if (npc.WithinRange(hoverDestination, 30f) || universalAttackTimer >= 60f)
                    {
                        SoundEngine.PlaySound(InfernumSoundRegistry.MyrindaelSpinSound with { Pitch = -0.1f, Volume = 1.4f }, target.Center);
                        ScreenEffectSystem.SetFlashEffect(target.Center, 0.4f, 30);
                        spearRotationStartingOffset = spearRotation;
                        substate++;
                        universalAttackTimer = 0f;
                    }
                    break;

                // Spin the spear around til it reaches the desired angle
                case 2:
                    npc.velocity *= 0.9f;
                    if (universalAttackTimer <= spearSpinTime)
                    {
                        spearRotation = angleToStab * CalamityUtils.SineInOutEasing(universalAttackTimer / spearSpinTime, 0) + spearRotationStartingOffset;
                    }
                    else if (universalAttackTimer <= spearReelbackTime)
                    {
                        // Move the spear backwards.
                        float interpolant = (universalAttackTimer - spearSpinTime) / (spearReelbackTime - spearSpinTime);
                        spearPosOffset = Lerp(0f, -50f, CalamityUtils.SineInOutEasing(interpolant, 0));
                    }
                    else if (universalAttackTimer <= spearStabTime)
                    {
                        float interpolant = (universalAttackTimer - spearReelbackTime) / (spearStabTime - spearReelbackTime);
                        spearPosOffset = Lerp(-50f, 30f, CalamityUtils.SineInOutEasing(interpolant, 0));
                    }
                    else
                    {
                        Vector2 spearTip = npc.Center + spearRotation.ToRotationVector2() * (120f * 1.5f);
                        for (int i = 0; i < 100; i++)
                            ModContent.GetInstance<ProfanedLavaMetaball>().SpawnParticle(spearTip + Main.rand.NextVector2Circular(50f, 50f), Vector2.Zero, new(Main.rand.NextFloat(52f, 85f)));

                        spearPosOffset = 0f;
                        spearStatus = (float)DefenderShieldStatus.MarkedForRemoval;
                        substate++;
                        universalAttackTimer = 0;
                    }
                    break;

                case 3:
                    if (universalAttackTimer == 1)
                    {
                        Vector2 spawnPosition = new Vector2(exitPosX, exitPosY) + target.Center;
                        for (int i = 0; i < 100; i++)
                            ModContent.GetInstance<ProfanedLavaMetaball>().SpawnParticle(spawnPosition + Main.rand.NextVector2Circular(75f, 75f), Vector2.Zero, new(Main.rand.NextFloat(52f, 85f)));

                        SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceBurnSound, target.Center);
                        SoundEngine.PlaySound(SoundID.DD2_LightningBugZap, target.Center);

                        ScreenEffectSystem.SetFlashEffect(target.Center, 0.5f, 45);

                        if (CalamityConfig.Instance.Screenshake)
                            target.Infernum_Camera().CurrentScreenShakePower = 4f;

                        CreateFireExplosion(spawnPosition, false);

                        float baseRotation = TwoPi / spearsAmount;

                        // Create the spears.
                        for (int i = 0; i < spearsAmount; i++)
                        {
                            // Create the exploding one above the target.
                            if (i == 0)
                            {
                                Vector2 explodyVelocity = spawnPosition.DirectionTo(target.Center).SafeNormalize(Vector2.UnitY) * (spearThrowSpeed * 0.8f);
                                if (Main.netMode != NetmodeID.MultiplayerClient)
                                {
                                    Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<HolyFireRift>(), 0, 0f, ai0: 1f);
                                    Utilities.NewProjectileBetter(spawnPosition, explodyVelocity, ModContent.ProjectileType<CommanderSpearThrown>(), CommanderSpearDamage, 0f, -1, 0f, 1f);
                                }
                            }
                            else
                            {
                                if (Main.netMode != NetmodeID.MultiplayerClient)
                                {
                                    float rotation = baseRotation * i;
                                    spawnPosition = target.Center + Main.rand.NextFloat(rotation * 0.85f, rotation * 1.15f).ToRotationVector2() * spearDistance;
                                    Vector2 velocity = spawnPosition.DirectionTo(target.Center).SafeNormalize(Vector2.UnitY) * spearThrowSpeed;
                                    Utilities.NewProjectileBetter(spawnPosition, Vector2.Zero, ModContent.ProjectileType<HolyFireRift>(), 0, 0f, ai0: 1f);
                                    Utilities.NewProjectileBetter(spawnPosition, velocity, ModContent.ProjectileType<CommanderSpearThrown>(), CommanderSpearDamage, 0f);
                                }
                            }
                        }
                    }
                    substate++;
                    universalAttackTimer = 0;
                    break;

                case 4:
                    if (universalAttackTimer >= afterAttackWaitTime)
                        SelectNewAttack(npc, ref universalAttackTimer);
                    break;
            }
        }

        public static void DoBehavior_RiftFireCharges(NPC npc, Player target, ref float universalAttackTimer)
        {
            ref float substate = ref npc.Infernum().ExtraAI[0];
            ref float chargesPerformed = ref npc.Infernum().ExtraAI[1];
            ref float sittingInPortalIndex = ref npc.Infernum().ExtraAI[2];

            // The commander will check this as well, and draw it instead if the defender is not present.
            ref float drawDashTelegraph = ref npc.Infernum().ExtraAI[DefenderDrawDashTelegraphIndex];
            ref float spearStatus = ref npc.Infernum().ExtraAI[CommanderSpearStatusIndex];
            ref float spearRotation = ref npc.Infernum().ExtraAI[CommanderSpearRotationIndex];

            float maxCharges = 4f;
            float chargeSpeed = 21f;
            float chargeAcceleration = 1.5f;
            float chargeLength = 45f;
            float fadeOutTime = 10f;
            float initialWaitTime = 75f;
            float normalWaitTime = 40f;
            float spearSpeed = 9f;
            float spearAmount = 4f;
            switch (substate)
            {
                // Enter a rift, create an exit one, and aim at the target.
                case 0:
                    // Fade out.
                    if (universalAttackTimer <= fadeOutTime)
                        npc.Opacity = Clamp(npc.Opacity - 1f / fadeOutTime, 0f, 1f);

                    // Slow down rapidly.
                    npc.velocity *= 0.92f;

                    //// Create metaballs at the location to indicate that the commander has entered a rift.
                    //for (int i = 0; i < 50; i++)
                    //    FusableParticleManager.GetParticleSetByType<ProfanedLavaParticleSet>()?.SpawnParticle(npc.Center + Main.rand.NextVector2Circular(npc.width * 0.5f, npc.height * 0.5f), 
                    //        Main.rand.NextFloat(52f, 85f));

                    // Look at the target.
                    npc.spriteDirection = (npc.DirectionTo(target.Center).X > 0f) ? 1 : -1;

                    if (universalAttackTimer == fadeOutTime && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        // Create the exit rift, and move to it.                       
                        Vector2 riftPosition = target.Center + Main.rand.NextVector2CircularEdge(750f, 650f);
                        npc.Center = riftPosition;
                        npc.velocity = Vector2.Zero;
                        sittingInPortalIndex = Utilities.NewProjectileBetter(riftPosition, Vector2.Zero, ModContent.ProjectileType<HolyFireRift>(), 0, 0f);
                        npc.netUpdate = true;
                    }

                    else if (universalAttackTimer > fadeOutTime)
                    {
                        drawDashTelegraph = 1f;
                        // Ensure the spear exists, and make it point towards the target.
                        npc.CreateSpearAndAimAtTarget(ref spearStatus, ref spearRotation, target);
                    }

                    npc.dontTakeDamage = true;
                    npc.damage = 0;

                    float waitTime = chargesPerformed == 0 ? initialWaitTime : normalWaitTime;

                    if (universalAttackTimer >= waitTime)
                    {
                        universalAttackTimer = 0f;
                        substate++;
                    }
                    break;

                // Shoot out from the rift at the target.
                case 1:
                    if (universalAttackTimer == 1f)
                    {
                        npc.damage = npc.defDamage;
                        npc.dontTakeDamage = false;

                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;

                        drawDashTelegraph = 0f;

                        CreateFireExplosion(npc.Center, true);
                        SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceBurnSound with { Pitch = 1.2f }, target.Center);
                        SoundEngine.PlaySound(InfernumSoundRegistry.VassalJumpSound with { Pitch = -0.2f, Volume = 1.4f }, target.Center);
                        if (CalamityConfig.Instance.Screenshake)
                        {
                            ScreenEffectSystem.SetBlurEffect(npc.Center, 1f, 30);
                            target.Infernum_Camera().CurrentScreenShakePower = 6f;
                        }

                        spearStatus = (float)DefenderShieldStatus.ActiveAndStatic;

                        // Failsafe check, shouldn't ever be the case, mark the portal for death that the commander has just left.
                        if (Main.projectile.IndexInRange((int)sittingInPortalIndex))
                            Main.projectile[(int)sittingInPortalIndex].ai[1] = 1f;

                        // Spawn two spears.
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float rotation = 0.4f;
                            Vector2 velocity = npc.velocity.RotatedBy(-rotation * spearAmount * 0.5f).SafeNormalize(Vector2.UnitY);
                            for (int i = 0; i < spearAmount; i++)
                            {
                                int index = i;
                                if (index >= 2)
                                    index++;
                                Utilities.NewProjectileBetter(npc.Center, velocity.RotatedBy(rotation * index) * spearSpeed, ModContent.ProjectileType<ProfanedSpearInfernum>(), HolySpearDamage, 0f);
                            }
                        }
                    }

                    // Fade in.
                    if (universalAttackTimer <= fadeOutTime)
                        npc.Opacity = Clamp(npc.Opacity + 1f / fadeOutTime, 0f, 1f);

                    // Accelerate after charging.
                    if (universalAttackTimer < chargeLength && npc.velocity.Length() <= 140f)
                        npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * (npc.velocity.Length() + chargeAcceleration);

                    // Leave a trail of fire.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        Utilities.NewProjectileBetter(npc.Center + Main.rand.NextVector2Circular(16f, 16f) + npc.velocity * 2f, npc.velocity * 0.1f, ModContent.ProjectileType<LingeringHolyFire>(), LingeringFireDamage, 0f);

                    if (universalAttackTimer >= chargeLength)
                    {
                        chargesPerformed++;

                        if (chargesPerformed >= maxCharges)
                        {
                            SelectNewAttack(npc, ref universalAttackTimer);
                            return;
                        }
                        universalAttackTimer = 0f;
                        substate = 0f;
                    }
                    break;
            }
        }
        #endregion

        #region AI Utilities
        public static void CreateFireExplosion(Vector2 impactCenter, bool bigExplosion = true)
        {
            float scaleModifier = bigExplosion ? 1f : 0.75f;
            // Spawn a bunch of light particles.
            for (int i = 0; i < 20; i++)
            {
                Vector2 position = impactCenter + Main.rand.NextVector2Circular(20f, 20f);
                Particle light = new GlowyLightParticle(position, impactCenter.DirectionTo(position) * Main.rand.NextFloat(3f, 5f),
                    Main.rand.NextBool() ? WayfinderSymbol.Colors[1] : Color.OrangeRed, 60, Main.rand.NextFloat(0.85f, 1.15f) * scaleModifier, Main.rand.NextFloat(0.95f, 1.05f), false);
                GeneralParticleHandler.SpawnParticle(light);
            }

            // Create a fire explosion.
            for (int i = 0; i < 30; i++)
            {
                MediumMistParticle fireExplosion = new(impactCenter + Main.rand.NextVector2Circular(80f, 80f), Vector2.Zero,
                    Main.rand.NextBool() ? WayfinderSymbol.Colors[0] : WayfinderSymbol.Colors[1],
                    Color.Gray, Main.rand.NextFloat(0.85f, 1.15f) * scaleModifier, Main.rand.NextFloat(220f, 250f));
                GeneralParticleHandler.SpawnParticle(fireExplosion);
            }
        }

        public static void CreateSpearAndAimAtTarget(this NPC npc, ref float spearStatus, ref float spearRotation, Player target)
        {
            // Spawn the spear if it does not exist.
            if ((DefenderShieldStatus)spearStatus is DefenderShieldStatus.Inactive || !Main.projectile.Any((Projectile p) => p.active && p.type == ModContent.ProjectileType<CommanderSpear>()))
            {
                spearStatus = (float)DefenderShieldStatus.ActiveAndAiming;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<CommanderSpear>(), CommanderSpearDamage, 0f, -1, 0f, npc.whoAmI);
            }

            spearStatus = (float)DefenderShieldStatus.ActiveAndAiming;
            // Make the spear rotation point to the player.
            float idealRotation = npc.SafeDirectionTo(target.Center).ToRotation();
            spearRotation = spearRotation.AngleTowards(idealRotation, 0.2f);
        }

        public static void DoPhaseTransitionEffects(NPC commander, int phaseTransition)
        {
            commander.Infernum().ExtraAI[GuardianSkyExtraIntensityIndex] = phaseTransition switch
            {
                1 => 0.6f,
                2 => 0.8f,
                _ => 1f
            };

            SoundStyle soundToPlay = phaseTransition switch
            {
                1 => InfernumSoundRegistry.GuardianSpawnSound,
                2 => InfernumSoundRegistry.GuardiansPhaseTwoTransition,
                _ => InfernumSoundRegistry.GuardiansPhaseThreeTransition
            };
            SoundEngine.PlaySound(soundToPlay);

            if (Main.player.IndexInRange(commander.target))
            {
                Main.player[commander.target].Infernum_Camera().CurrentScreenShakePower = commander.Infernum().ExtraAI[GuardianSkyExtraIntensityIndex] * 12f;
                ScreenEffectSystem.SetBlurEffect(commander.Center, 2f, 90);
            }
            commander.netUpdate = true;
        }

        public static void SelectNewAttack(NPC commander, ref float universalAttackTimer, float specificAttackToSwapTo = -1)
        {
            // Reset the first few extra ai slots. These are used for per attack information.
            int aiSlotsToClear = 7;
            for (int i = 0; i < aiSlotsToClear; i++)
                commander.Infernum().ExtraAI[i] = 0f;

            if (Main.npc.IndexInRange(CalamityGlobalNPC.doughnutBossDefender))
            {
                if (Main.npc[CalamityGlobalNPC.doughnutBossDefender].active)
                {
                    NPC defender = Main.npc[CalamityGlobalNPC.doughnutBossDefender];
                    for (int i = 0; i < aiSlotsToClear; i++)
                        defender.Infernum().ExtraAI[i] = 0f;

                    // If the next attack is the healer solo, mark the defending shield as no longer needed.
                    // This gets reset back to zero by the shield before removing itself.
                    if ((GuardiansAttackType)commander.ai[0] == GuardiansAttackType.HealerAndDefender)
                    {
                        if ((DefenderShieldStatus)defender.Infernum().ExtraAI[DefenderShieldStatusIndex] is DefenderShieldStatus.ActiveAndAiming or DefenderShieldStatus.ActiveAndStatic)
                            defender.Infernum().ExtraAI[DefenderShieldStatusIndex] = (float)DefenderShieldStatus.MarkedForRemoval;
                        else
                            defender.Infernum().ExtraAI[DefenderShieldStatusIndex] = (float)DefenderShieldStatus.Inactive;
                    }

                    defender.netUpdate = true;
                }
            }

            if (Main.npc.IndexInRange(CalamityGlobalNPC.doughnutBossHealer))
            {
                if (Main.npc[CalamityGlobalNPC.doughnutBossHealer].active)
                {
                    NPC healer = Main.npc[CalamityGlobalNPC.doughnutBossHealer];
                    for (int i = 0; i < aiSlotsToClear; i++)
                        healer.Infernum().ExtraAI[i] = 0f;

                    healer.netUpdate = true;
                }
            }

            // Reset the universal attack timer.
            universalAttackTimer = 0f;

            commander.netUpdate = true;

            // Swap to a specific attack if one is specified.
            if (specificAttackToSwapTo != -1f)
            {
                commander.ai[0] = specificAttackToSwapTo;
                return;
            }

            // If in phase 3
            if (AttackerGuardianBehaviorOverride.TotalRemaininGuardians == 1)
            {
                commander.ai[0] = (int)CommanderAttackCycle[(int)commander.Infernum().ExtraAI[CommanderAttackCyclePositionIndex] % CommanderAttackCycle.Count];
                commander.Infernum().ExtraAI[CommanderAttackCyclePositionIndex]++;
                return;
            }

            // If not the final combo attack, advance the current attack.
            if ((GuardiansAttackType)commander.ai[0] < GuardiansAttackType.HealerAndDefender)
                commander.ai[0]++;

            // Else, if its the final combo attack reset it back to the first combo attack.
            else if ((GuardiansAttackType)commander.ai[0] == GuardiansAttackType.HealerAndDefender)
                commander.ai[0] = (float)GuardiansAttackType.SoloHealer;

            // Else if its the healer death animation, enter the next set of combo attacks.
            else if ((GuardiansAttackType)commander.ai[0] == GuardiansAttackType.HealerDeathAnimation)
                commander.ai[0] = (float)GuardiansAttackType.SpearDashAndGroundSlam;

            // If its at the end of the final double combo attack, reset back to the start.
            else if ((GuardiansAttackType)commander.ai[0] == GuardiansAttackType.FireballBulletHell)
                commander.ai[0] = (float)GuardiansAttackType.SpearDashAndGroundSlam;

            else if ((GuardiansAttackType)commander.ai[0] < GuardiansAttackType.FireballBulletHell)
                commander.ai[0]++;
        }

        public static void DespawnTransitionProjectiles()
        {
            Utilities.DeleteAllProjectiles(true,
                ModContent.ProjectileType<ProfanedCirclingRock>(),
                ModContent.ProjectileType<ProfanedRock>(),
                ModContent.ProjectileType<MagicCrystalShot>(),
                ModContent.ProjectileType<MagicSpiralCrystalShot>(),
                ModContent.ProjectileType<FallingCrystalShard>(),
                ModContent.ProjectileType<DefenderShield>(),
                ModContent.ProjectileType<LavaEruptionPillar>(),
                ModContent.ProjectileType<ProfanedSpearInfernum>(),
                ModContent.ProjectileType<HolyCross>(),
                ModContent.ProjectileType<HolyMagicLaserbeam>(),
                ModContent.ProjectileType<HolyDogmaFireball>(),
                ModContent.ProjectileType<CommanderSpearThrown>(),
                ModContent.ProjectileType<HolySineSpear>(),
                ModContent.ProjectileType<HolyFireRift>(),
                ModContent.ProjectileType<HolyBasicFireball>()
                );
        }
        #endregion
    }
}
