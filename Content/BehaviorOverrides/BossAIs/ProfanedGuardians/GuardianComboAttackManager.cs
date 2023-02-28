using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.Particles;
using CalamityMod.Particles.Metaballs;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics;
using InfernumMode.Common.Graphics.Metaballs;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Providence;
using InfernumMode.Content.Projectiles.Wayfinder;
using InfernumMode.Core;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.GlobalInstances;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    public static class GuardianComboAttackManager
    {
        #region Enums
        public enum GuardiansAttackType
        {
            // Initial attacks.
            SpawnEffects,
            FlappyBird,

            // All 3 combo attacks.
            SoloHealer,
            SoloDefender,
            HealerAndDefender,

            HealerDeathAnimation,

            // Commander and Defender combo attacks
            SpearDashAndGroundSlam,
            LavaRaise,
            CrashRam,

            DefenderDeathAnimation,

            // Commander solo attacks.
            LargeGeyserAndFireCharge,
            ReleaseAimingFireballs,

            CommanderDeathAnimation
        }

        public enum DefenderShieldStatus
        {
            Inactive,
            ActiveAndAiming,
            ActiveAndStatic,
            MarkedForRemoval
        }
        #endregion

        #region Fields And Properties
        public static Vector2 CrystalPosition => WorldSaveSystem.ProvidenceArena.TopLeft() * 16f + new Vector2(6780, 1500);

        public static int CommanderType => ModContent.NPCType<ProfanedGuardianCommander>();
        public static int DefenderType => ModContent.NPCType<ProfanedGuardianDefender>();
        public static int HealerType => ModContent.NPCType<ProfanedGuardianHealer>();

        // The length of the phase 3 looping music section.
        public const int LoopingMusicLength = 2560;
        #endregion

        #region Indexes
        public const int DefenderFireSuckupWidthIndex = 10;
        public const int HealerConnectionsWidthScaleIndex = 11;
        public const int DefenderShouldGlowIndex = 12;
        public const int DefenderDrawDashTelegraphIndex = 13;
        public const int DefenderDashTelegraphOpacityIndex = 14;
        public const int CommanderMovedToTriplePositionIndex = 15;
        // 0 = shield needs to spawn, 1 = shield is spawned and should aim at the player, 2 = shield is spawned and should stop aiming, 3 = shield should die.
        public const int DefenderShieldStatusIndex = 16;
        public const int DefenderFireAfterimagesIndex = 17;
        public const int CommanderBlenderShouldFadeOutIndex = 18;
        public const int CommanderAngerGlowAmountIndex = 19;
        public const int CommanderSpearStatusIndex = 20;
        public const int CommanderSpearRotationIndex = 21;
        // Handled entirely by the commander.
        public const int CommanderSpearSmearOpacityIndex = 22;
        // Reset by the commander every frame.
        public const int CommanderDrawSpearSmearIndex = 22;
        public const int CommanderFireAfterimagesIndex = 23;
        public const int CommanderFireAfterimagesLengthIndex = 24;

        public const int CommanderDrawBlackBarsIndex = 25;
        public const int CommanderBlackBarsRotationIndex = 26;
        // Reset by the commander every frame based on the draw index.
        public const int CommanderBlackBarsOpacityIndex = 27;
        public const int CommanderHandsSpawnedIndex = 28;

        // Hand stuff
        public const int LeftHandIndex = 29;
        public const int RightHandIndex = 30;
        public const int LeftHandXIndex = 31;
        public const int LeftHandYIndex = 32;
        public const int RightHandXIndex = 33;
        public const int RightHandYIndex = 34;

        public const int DefenderHasBeenYeetedIndex = 35;

        public const int CommanderHasSpawnedBlenderAlreadyIndex = 36;

        public const int CommanderBrightnessWidthFactorIndex = 50;
        public const int MusicTimerIndex = 51;
        public const int MusicHasStartedIndex = 52;
        #endregion

        #region Commander + Defender + Healer Attacks
        public static void DoBehavior_SpawnEffects(NPC npc, Player target, ref float attackTimer)
        {
            float inertia = 20f;
            float flySpeed = 25f;

            // Do not take or deal damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            Vector2 positionToMoveTo = CrystalPosition;
            // If we are the commander, spawn in the pushback fire wall.
            if (npc.type == CommanderType)
            {
                positionToMoveTo += new Vector2(400, 0);
                if (attackTimer == 1 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 spawnPosition = new(target.Center.X + 800f, npc.Center.Y);
                    Vector2 finalPosition = new(WorldSaveSystem.ProvidenceDoorXPosition - 7104f, target.Center.Y);
                    float distance = (spawnPosition - finalPosition).Length();
                    float x = distance / HolyPushbackWall.Lifetime;
                    Vector2 velocity = new(-x, 0f);
                    Utilities.NewProjectileBetter(spawnPosition, velocity, ModContent.ProjectileType<HolyPushbackWall>(), 300, 0f);
                }
                if (npc.WithinRange(positionToMoveTo, 20f))
                {
                    npc.damage = npc.defDamage;
                    npc.dontTakeDamage = false;
                    // Go to the initial attack and reset the attack timer.
                    SelectNewAttack(npc, ref attackTimer);
                }
            }

            // This is the ideal velocity it would have
            Vector2 idealVelocity = npc.SafeDirectionTo(positionToMoveTo) * flySpeed;
            // And this is the actual velocity, using inertia and its existing one.
            npc.velocity = (npc.velocity * (inertia - 1f) + idealVelocity) / inertia;
        }

        public static void DoBehavior_FlappyBird(NPC npc,Player target, ref float attackTimer, NPC commander)
        {
            // This attack ends automatically when the crystal wall dies, it advances the attackers attack state, which the other
            // guardians check for and advance with it.

            // The commander bobs on the spot, pausing to aim and fire a fire beam at the player from afar.
            if (npc.type == CommanderType)
            {
                float deathrayFireRate = 150;
                float initialDelay = 460;
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
                        if (attackTimer % deathrayFireRate == 0 && target.WithinRange(npc.Center, 6200f) && attackTimer >= initialDelay && crystal.ai[0] == 0f)
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

                if (!Main.projectile.Any((Projectile p) => (p.type == ModContent.ProjectileType<HolyAimedDeathrayTelegraph>() || p.type == ModContent.ProjectileType<HolyAimedDeathray>()) && p.active))
                {
                    float sine = MathF.Sin(movementTimer * 0.05f);
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
                float wallCreationRate = 60f;
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
                        Vector2 hoverPosition = CrystalPosition + new Vector2(155f, 475f);
                        // Sit still behind and beneath the crystal.
                        if (npc.Distance(hoverPosition) > 7f && movedToPosition == 0f)
                            npc.velocity = npc.SafeDirectionTo(hoverPosition, Vector2.UnitY) * 5f;
                        else
                        {
                            npc.velocity.X = 0f;
                            movedToPosition = 1f;
                            float sine = -MathF.Sin(Main.GlobalTimeWrappedHourly * 2f);
                            npc.velocity.Y = sine * 1.5f;
                            npc.spriteDirection = -1;
                        }

                        // Create walls of fire with a random gap in them based off of the last one.
                        if (attackTimer % wallCreationRate == 0f && Main.netMode != NetmodeID.MultiplayerClient && crystal.ai[0] == 0f)
                        {
                            Vector2 velocity = -Vector2.UnitX * 10f;
                            Vector2 baseCenter = CrystalPosition + new Vector2(20f, 0f);
                            // Create a random offset.
                            float yRandomOffset;
                            Vector2 previousCenter = baseCenter + new Vector2(0f, lastOffsetY);
                            Vector2 newCenter;
                            int attempts = 0;
                            // Attempt to get one within a certain distance, but give up after 10 attempts.
                            do
                            {
                                yRandomOffset = Main.rand.NextFloat(-600f, 200f);
                                newCenter = baseCenter + new Vector2(0f, yRandomOffset);
                                attempts++;
                            }
                            while (newCenter.Distance(previousCenter) > 400f || attempts < 10);

                            // Set the new random offset as the last one.
                            lastOffsetY = yRandomOffset;
                            Utilities.NewProjectileBetter(newCenter, velocity, ModContent.ProjectileType<HolyFireWall>(), 300, 0);
                            npc.netUpdate = true;
                        }

                        // If the crystal is shattering, decrease the scale, else increase it.
                        if (crystal.ai[0] == 1f)
                            fireSuckupWidth = MathHelper.Clamp(fireSuckupWidth - 0.1f, 0f, 1f);
                        else
                            fireSuckupWidth = MathHelper.Clamp(fireSuckupWidth + 0.1f, 0f, 1f);
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

                        Vector2 hoverPosition = CrystalPosition + new Vector2(200f, -65f);
                        // Sit still behind the crystal.
                        if (npc.Distance(hoverPosition) > 7f && crystal.ai[0] == 0)
                            npc.velocity = npc.SafeDirectionTo(hoverPosition, Vector2.UnitY) * 5f;
                        else
                        {
                            npc.velocity *= 0.5f;
                            npc.spriteDirection = -1;
                        }

                        // If the crystal is shattering, decrease the scale, else increase it.
                        if (crystal.ai[0] == 1)
                            connectionsWidthScale = MathHelper.Clamp(connectionsWidthScale - 0.1f, 0f, 1f);
                        else
                            connectionsWidthScale = MathHelper.Clamp(connectionsWidthScale + 0.1f, 0f, 1f);
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

                Vector2 hoverPosition = CrystalPosition + new Vector2(-200f, 0);

                // Sit still in the middle of the area.
                if (npc.Distance(hoverPosition) > 15f && movedToPosition == 0f)
                {
                    // Do not increase until the lasers are present.
                    universalAttackTimer = 0;
                    npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverPosition) * MathHelper.Min(npc.Distance(hoverPosition), 18)) / 8f;
                }
                else
                {
                    if (movedToPosition == 0)
                        npc.Center = hoverPosition;
                    npc.velocity *= 0.9f;
                    movedToPosition = 1f;
                    float sine = -MathF.Sin(Main.GlobalTimeWrappedHourly * 2f);
                    npc.position.Y += sine * 0.5f;
                    npc.spriteDirection = -1;

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
                                Utilities.NewProjectileBetter(npc.Center, Vector2.UnitY, ModContent.ProjectileType<HolySpinningFireBeam>(), 700, 0f, -1, 0f, offsetAngleInterpolant);
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

                // Have very high DR.
                npc.Calamity().DR = 0.9999f;
                npc.lifeRegen = 1000000;
                npc.spriteDirection = MathF.Sign(npc.DirectionTo(target.Center).X);

                Vector2 hoverPosition = target.Center + new Vector2(500, -350);
                npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverPosition) * MathHelper.Min(npc.Distance(hoverPosition), flySpeed)) / 8f;

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
                        Vector2 rockPosition = npc.Center + (MathHelper.TwoPi * i / rockAmount).ToRotationVector2() * 100f;

                        int waitTimeToUse = Main.rand.Next(0, waitTimes.Count);
                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(rock => rock.ModProjectile<ProfanedCirclingRock>().WaitTime = waitTimes[waitTimeToUse]);
                        Utilities.NewProjectileBetter(rockPosition, Vector2.Zero, ModContent.ProjectileType<ProfanedCirclingRock>(), 120, 0f, Main.myPlayer, MathHelper.TwoPi * i / rockAmount, npc.whoAmI);
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
                float crystalAmount = 10f;
                float maxCrystalsFired = 4f;

                Vector2 hoverPosition = CrystalPosition - new Vector2(50f, 0f);
                // Sit still behind the commander
                if (npc.Distance(hoverPosition) > 5f && movedToPosition == 0f)
                     npc.velocity = (npc.velocity * 5f + npc.SafeDirectionTo(hoverPosition) * MathHelper.Min(npc.Distance(hoverPosition), 25)) / 8f;
                else
                {
                    drawShieldConnections = 1f;
                    npc.velocity.X *= 0.9f;
                    if (movedToPosition == 0)
                        npc.Center = hoverPosition;
                    movedToPosition = 1f;
                    float sine = -MathF.Sin(Main.GlobalTimeWrappedHourly * 2f);
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

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 projectileSpawnPosition = commander.Center;
                        for (int i = 0; i < crystalAmount; i++)
                        {
                            Vector2 shootVelocity = (MathHelper.TwoPi * i / crystalAmount).ToRotationVector2() * 15f;
                            Utilities.NewProjectileBetter(projectileSpawnPosition, shootVelocity, ModContent.ProjectileType<MagicSpiralCrystalShot>(), 230, 0f, -1, 0f, crystalsFired % 2f == 0f ? -1f : 1f);
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
                float sine = -MathF.Sin(Main.GlobalTimeWrappedHourly * 2f);
                npc.position.Y += sine * 0.5f;
                npc.spriteDirection = -1;
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
                ref float drawFireAfterimages = ref commander.Infernum().ExtraAI[DefenderFireAfterimagesIndex];

                float maxDashes = 4f;
                float waitTime = dashesCompleted == 0f ? 90f : 60f;
                float telegraphWaitTime = 20f;
                float dashTime = 30f;
                float dashSpeed = 35f;
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
                        npc.spriteDirection = MathF.Sign(npc.DirectionTo(target.Center).X);
                        drawDashTelegraph = 0;
                        Vector2 hoverOffset = Vector2.UnitY;
                        for (int i = 0; i < 100; i++)
                        {
                            float hoverOffsetAngle = Main.rand.Next(4) * MathHelper.TwoPi / 4f + MathHelper.PiOver4;
                            hoverOffset = hoverOffsetAngle.ToRotationVector2();

                            // Leave and use the current offset to see if the direction of the new offset is perpendicular or less to the previous one. This prevents going to opposite sides and
                            // moving thorugh the player's position to reach it. The 0.01 is added on top to ensure that tiny floating point imprecisions don't become a problem.
                            float angleBetween = oldHoverOffset.AngleBetween(hoverOffset);
                            if (angleBetween is < (MathHelper.PiOver2 + 0.01f) and not 0)
                                break;
                        }

                        hoverOffsetX = hoverOffset.X;
                        hoverOffsetY = hoverOffset.Y;
                        universalAttackTimer = 0;
                        substate++;
                        break;

                    // Get into position for the dash.
                    case 1:
                        shieldStatus = (float)DefenderShieldStatus.ActiveAndAiming;
                        npc.spriteDirection = MathF.Sign(npc.DirectionTo(target.Center).X);
                        float distance = 625f;
                        Vector2 position = target.Center + oldHoverOffset * distance;

                        if (npc.velocity.Length() < 2f)
                            npc.velocity = Vector2.UnitY * -2.4f;

                        npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(position) * MathHelper.Min(npc.Distance(position), 33f)) / 8f;

                        // Move out of the way of the target if going around them.
                        if (npc.WithinRange(target.Center, 150f))
                            npc.velocity.X += target.Center.DirectionTo(npc.Center).X * 10f;

                        // Initialize the dash telegraph.
                        if (universalAttackTimer >= telegraphWaitTime)
                            drawDashTelegraph = 1;

                        // Increase the opacity.
                        if (drawDashTelegraph == 1)
                            dashTelegraphOpacity = MathHelper.Clamp(dashTelegraphOpacity + 0.1f, 0f, 1f);


                        if ((npc.WithinRange(position, 25f) && universalAttackTimer >= waitTime) || universalAttackTimer >= 120f)
                        {
                            universalAttackTimer = 0;
                            substate++;
                        }
                        break;

                    // Charge
                    case 2:
                        drawFireAfterimages = 1;
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
                        substate++;
                        universalAttackTimer = 0;
                        break;

                    // After a set time, reset.
                    case 3:
                        // Decrease the dash telegraph.
                        drawDashTelegraph = 1;
                        dashTelegraphOpacity = MathHelper.Clamp(dashTelegraphOpacity - 0.2f, 0f, 1f);
                        commander.Infernum().ExtraAI[DefenderShouldGlowIndex] = 1;

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
                            drawFireAfterimages = 0;
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

                float flySpeed = MathHelper.Lerp(9f, 23f, Utils.GetLerpValue(50f, 270f, npc.Distance(hoverDestination), true));
                flySpeed *= Utils.GetLerpValue(0f, 50f, npc.Distance(hoverDestination), true);
                npc.velocity = npc.velocity * 0.85f + npc.SafeDirectionTo(hoverDestination) * flySpeed * 0.15f;
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(hoverDestination) * flySpeed, 4f);
                npc.spriteDirection = MathF.Sign(npc.DirectionTo(commander.Center).X);

                if (localAttackTimer % crystalShotReleaseRate == crystalShotReleaseRate - 1 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < crystalAmount; i++)
                    {
                        Vector2 fastShootVelocity = (MathHelper.TwoPi * i / crystalAmount).ToRotationVector2() * crystalShotSpeed;
                        Vector2 slowShootVelocity = (MathHelper.TwoPi * (i + 0.5f) / crystalAmount).ToRotationVector2() * (crystalShotSpeed / 3f);
                        Utilities.NewProjectileBetter(commander.Center, fastShootVelocity, ModContent.ProjectileType<MagicCrystalShot>(), 200, 0f);
                        Utilities.NewProjectileBetter(commander.Center, slowShootVelocity, ModContent.ProjectileType<MagicCrystalShot>(), 200, 0f);
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
                float sine = -MathF.Sin(Main.GlobalTimeWrappedHourly * 2f);
                npc.position.Y += sine * 0.5f;
                npc.spriteDirection = -1;
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
                ref float drawFireAfterimages = ref commander.Infernum().ExtraAI[DefenderFireAfterimagesIndex];

                float waitTime = dashesCompleted == 0f ? 60f : 30;
                float telegraphWaitTime = 20f;
                float dashTime = 25f;
                float dashSpeed = 35f;

                switch (substate)
                {
                    // Determine the best location to move to based on the Y position relative to the player.
                    case 0:
                        // If higher than the target.
                        if (npc.Center.Y < target.Center.Y)
                            hoverOffsetY = -500f;
                        else
                            hoverOffsetY = 500f;
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
                        npc.spriteDirection = MathF.Sign(npc.DirectionTo(target.Center).X);
                        Vector2 position = target.Center + new Vector2(xOffset, hoverOffsetY);

                        if (npc.velocity.Length() < 2f)
                            npc.velocity = Vector2.UnitY * -2.4f;

                        npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(position) * MathHelper.Min(npc.Distance(position), 33f)) / 8f;

                        // Move out of the way of the target if going around them.
                        if (npc.WithinRange(target.Center, 150f))
                            npc.velocity.X += target.Center.DirectionTo(npc.Center).X * 10f;

                        // Initialize the dash telegraph.
                        if (localAttackTimer >= telegraphWaitTime)
                            drawDashTelegraph = 1;

                        // Increase the opacity.
                        if (drawDashTelegraph == 1)
                            dashTelegraphOpacity = MathHelper.Clamp(dashTelegraphOpacity + 0.1f, 0f, 1f);


                        if ((npc.WithinRange(position, 25f) && localAttackTimer >= waitTime) || localAttackTimer >= 120f)
                        {
                            localAttackTimer = 0;
                            substate++;
                        }
                        break;

                    // Charge
                    case 2:
                        drawFireAfterimages = 1;
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
                        substate++;
                        localAttackTimer = 0;
                        break;

                    // After a set time, reset.
                    case 3:
                        // Decrease the dash telegraph.
                        drawDashTelegraph = 1;
                        dashTelegraphOpacity = MathHelper.Clamp(dashTelegraphOpacity - 0.2f, 0f, 1f);
                        commander.Infernum().ExtraAI[DefenderShouldGlowIndex] = 1;

                        // Create particles to indicate the sudden speed.
                        if (Main.rand.NextBool(2))
                        {
                            Vector2 energySpawnPosition = npc.Center + Main.rand.NextVector2Circular(30f, 20f) - npc.velocity;
                            Particle energyLeak = new SparkParticle(energySpawnPosition, npc.velocity * 0.3f, false, 30, Main.rand.NextFloat(0.9f, 1.4f), Color.Lerp(WayfinderSymbol.Colors[1], WayfinderSymbol.Colors[2], 0.75f));
                            GeneralParticleHandler.SpawnParticle(energyLeak);
                        }

                        if (localAttackTimer >= dashTime)
                        {
                            drawFireAfterimages = 0;
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
                float crystalLayerFireRate = 90f;
                float endOfAttackDelay = 120f;
                Vector2 hoverPosition = new(commander.Center.X, CrystalPosition.Y - 450);
                // Sit still behind the commander
                if (npc.Distance(hoverPosition) > 5f && movedToPosition == 0f)
                    npc.velocity = (npc.velocity * 5f + npc.SafeDirectionTo(hoverPosition) * MathHelper.Min(npc.Distance(hoverPosition), 18)) / 8f;
                else
                {
                    npc.velocity.X *= 0.9f;
                    if (movedToPosition == 0)
                        npc.Center = hoverPosition;
                    movedToPosition = 1f;
                    float sine = -MathF.Sin(Main.GlobalTimeWrappedHourly * 2f);
                    npc.velocity.Y = sine * 0.5f;
                    npc.spriteDirection = (npc.DirectionTo(target.Center).X > 0).ToInt();
                }

                if (universalAttackTimer % crystalLayerFireRate == crystalLayerFireRate - 1 && Main.netMode != NetmodeID.MultiplayerClient && completedCrystalLayers < totalCrystalLayers)
                {
                    SoundEngine.PlaySound(SoundID.Item109, target.Center);
                    //float xSpeedOffset = target.velocity.X + Main.rand.NextFloat(-5f, 5f);
                    for (int i = 0; i < totalCrystalsPerLayer; i++)
                    {
                        Vector2 shootVelocity = new(MathHelper.Lerp(-20f, 20f, i / (float)totalCrystalsPerLayer), -10.75f);
                        shootVelocity.X += Main.rand.NextFloatDirection() * 0.6f;
                        Utilities.NewProjectileBetter(npc.Center + -Vector2.UnitY * 20f, shootVelocity, ModContent.ProjectileType<FallingCrystalShard>(), 200, 0f);
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
                float sine = -MathF.Sin(Main.GlobalTimeWrappedHourly * 2f);
                npc.position.Y += sine * 0.5f;
                npc.spriteDirection = (npc.DirectionTo(target.Center).X > 0) ? 1 : -1;

                // This tells the blender lasers to fade out and disappear.
                npc.Infernum().ExtraAI[CommanderBlenderShouldFadeOutIndex] = 1f;

                if (universalAttackTimer >= angerDelay && universalAttackTimer < angerDelay + angerTime)
                {
                    float interlopant = MathF.Sin(MathF.PI * ((universalAttackTimer - angerDelay) / angerTime));
                    glowAmount = CalamityUtils.SineInOutEasing(interlopant, 0);
                }

                // Make the spear appear and spin.
                if (universalAttackTimer >= angerDelay + angerTime / 2f)
                {
                    if ((DefenderShieldStatus)spearStatus is DefenderShieldStatus.Inactive || !Main.projectile.Any((Projectile p) => p.active && p.type == ModContent.ProjectileType<CommanderSpear>()))
                    {
                        spearStatus = (float)DefenderShieldStatus.ActiveAndAiming;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<CommanderSpear>(), 300, 0f, -1, 0f, npc.whoAmI);
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

                Vector2 hoverDestination = commander.Center + commander.SafeDirectionTo(target.Center) * MathHelper.Lerp(25, 150, MathHelper.Clamp(target.Distance(commander.Center) / 850f, 0f, 1f));
                if (npc.velocity.Length() < 2f)
                    npc.velocity = Vector2.UnitY * -2.4f;

                float flySpeed = MathHelper.Lerp(9f, 23f, Utils.GetLerpValue(50f, 270f, npc.Distance(hoverDestination), true));
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
                    whiteGlowOpacity = CalamityUtils.ExpInEasing(MathHelper.Lerp(0f, 1f, universalAttackTimer / whiteGlowTime), 0);
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
                    npc.life = 0;
                    npc.active = false;
                }
            }
        }
        #endregion

        #region Commander + Defender Attacks
        public static void DoBehavior_SpearDashAndGroundSlam(NPC npc, Player target, ref float universalAttackTimer, NPC commander)
        {
            // The commander lines up horizontally, spins his spear around releasing spears in a circle before dashing at a rapid speed at the player.
            // The won't be ram-able due to the spear.
            if (npc.type == CommanderType)
            {
                ref float substate = ref npc.Infernum().ExtraAI[0];
                ref float localAttackTimer = ref npc.Infernum().ExtraAI[1];
                ref float xOffset = ref npc.Infernum().ExtraAI[2];
                ref float spearRotationStartingOffset = ref npc.Infernum().ExtraAI[3];

                ref float spearStatus = ref npc.Infernum().ExtraAI[CommanderSpearStatusIndex];
                ref float spearRotation = ref npc.Infernum().ExtraAI[CommanderSpearRotationIndex];
                ref float drawShieldSmear = ref npc.Infernum().ExtraAI[CommanderDrawSpearSmearIndex];
                ref float drawFireAfterimages = ref npc.Infernum().ExtraAI[CommanderFireAfterimagesIndex];

                float flySpeed = 20f;
                float spinLength = 20f;
                float recoilLength = 20f;
                float chargeLength = 25f;
                float chargeSpeed = 50f;
                float totalSpearsToRelease = 13f;
                int spearReleasePoint = (int)(spinLength / totalSpearsToRelease);

                drawFireAfterimages = 0f;
                npc.Infernum().ExtraAI[CommanderFireAfterimagesLengthIndex] = chargeLength;

                switch (substate)
                {
                    // The commander picks the location to hover and moves to it.
                    case 0:
                        xOffset = -700f * MathF.Sign(target.Center.X - npc.Center.X);
                        Vector2 hoverDestination = target.Center + new Vector2(xOffset, 0f);
                        npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), flySpeed)) / 8f;

                        // Spawn the spear if it does not exist.
                        if ((DefenderShieldStatus)spearStatus is DefenderShieldStatus.Inactive || !Main.projectile.Any((Projectile p) => p.active && p.type == ModContent.ProjectileType<CommanderSpear>()))
                        {
                            spearStatus = (float)DefenderShieldStatus.ActiveAndAiming;
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                                Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<CommanderSpear>(), 300, 0f, -1, 0f, npc.whoAmI);
                        }
                        spearStatus = (float)DefenderShieldStatus.ActiveAndAiming;
                        substate++;
                        localAttackTimer = 0f;
                        spearRotationStartingOffset = spearRotation;
                        return;

                    case 1:
                        hoverDestination = target.Center + new Vector2(xOffset, 0f);
                        npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), flySpeed)) / 8f;
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
                        npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), flySpeed)) / 8f;
                        drawShieldSmear = 1f;
                        npc.spriteDirection = (npc.DirectionTo(target.Center).X > 0f) ? 1 : -1;

                        if (localAttackTimer == 0)
                            SoundEngine.PlaySound(InfernumSoundRegistry.MyrindaelSpinSound with { Pitch = -0.1f, Volume = 0.9f }, target.Center);

                        if (localAttackTimer < spinLength)
                        {
                            spearRotation = MathF.Tau * CalamityUtils.SineInOutEasing(localAttackTimer / spinLength, 0) + spearRotationStartingOffset;
                            if (localAttackTimer % spearReleasePoint == spearReleasePoint - 1f && Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                float positionRotation = MathF.Tau * CalamityUtils.LinearEasing(localAttackTimer / spinLength, 0) + spearRotationStartingOffset;
                                Vector2 position = npc.Center + positionRotation.ToRotationVector2() * 75f;
                                Vector2 velocity = npc.SafeDirectionTo(position) * 5f;
                                Utilities.NewProjectileBetter(position, velocity, ModContent.ProjectileType<ProfanedSpearInfernum>(), 250, 0f);
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

                        npc.velocity = CalamityUtils.MoveTowards(Vector2.UnitX * -2.3f, npc.DirectionTo(target.Center) * -2.3f, 6f);

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

                        drawFireAfterimages = 1f;
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
                        npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), flySpeed)) / 8f;

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            List<int> waitTimes = new()
                            {
                                90,
                                180,
                            };

                            for (int i = 0; i < rockAmount; i++)
                            {
                                Vector2 rockPosition = npc.Center + (MathHelper.TwoPi * i / rockAmount).ToRotationVector2() * 100f;

                                int waitTimeToUse = Main.rand.Next(0, waitTimes.Count);
                                ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(rock => rock.ModProjectile<ProfanedCirclingRock>().WaitTime = waitTimes[waitTimeToUse]);
                                Utilities.NewProjectileBetter(rockPosition, Vector2.Zero, ModContent.ProjectileType<ProfanedCirclingRock>(), 120, 0f, Main.myPlayer, MathHelper.TwoPi * i / rockAmount, npc.whoAmI);
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
                        npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), flySpeed)) / 8f;

                        // Move out of the way of the target if going around them.
                        if (npc.WithinRange(target.Center, 200f))
                            npc.velocity.X += target.Center.DirectionTo(npc.Center).X * 5f;

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
                                    Utilities.NewProjectileBetter(position, Vector2.Zero, ModContent.ProjectileType<LavaEruptionPillar>(), 400, 0f);
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
                                // Switch to next attack. It will stall here if the commander is mid spin/charge until it is free. This is to avoid abruptly stopping mid spin/charge.
                                if (commander.Infernum().ExtraAI[0] is 0f or 1f && pillarsAreMostlyGone)
                                    SelectNewAttack(commander, ref universalAttackTimer);
                            }
                            else
                            {
                                substate = 0f;
                                universalAttackTimer = 0f;
                            }
                        }
                        break;
                }
            }
        }

        public static void DoBehavior_LavaRaise(NPC npc, Player target, ref float universalAttackTimer, NPC commander)
        {
            float attackLength = 120f;
            if (npc.type == CommanderType)
            {
                ref float lavaSpawned = ref npc.Infernum().ExtraAI[0];
                ref float localAttackTimer = ref npc.Infernum().ExtraAI[1];

                if (lavaSpawned == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    lavaSpawned = 1;
                    Vector2 position = new(WorldSaveSystem.ProvidenceDoorXPosition + 1500f, (Main.maxTilesY * 16f) - 50f);
                    Utilities.NewProjectileBetter(position, Vector2.Zero, ModContent.ProjectileType<ProfanedLavaWave>(), 500, 0f);
                }

                Vector2 hoverDestination = target.Center + new Vector2(0, -400f);

                npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), 19f)) / 8f;

                if (localAttackTimer >= attackLength)
                    SelectNewAttack(commander, ref universalAttackTimer);

                localAttackTimer++;
            }

            // The defender dives onto the lava, and surfs along it on its shield, jumping out at the player before falling back down towards it.
            else if (npc.type == DefenderType)
            {
                ref float substate = ref npc.Infernum().ExtraAI[0];
                ref float initialDirection = ref npc.Infernum().ExtraAI[1];
                ref float surfSpeed = ref npc.Infernum().ExtraAI[2];
                ref float surfSubstate = ref npc.Infernum().ExtraAI[3];

                ref float shieldStatus = ref npc.Infernum().ExtraAI[DefenderShieldStatusIndex];

                float lavaRisingDelay = ProfanedLavaWave.MoveTime + ProfanedLavaWave.TelegraphTime;
                float recoilWait = 10f;
                float recoilLength = 30f;
                float diveSpeed = 30f;
                float jumpTime = 90f;

                float initialFlySpeed = 19f;

                npc.spriteDirection = (npc.DirectionTo(target.Center).X > 0f) ? 1 : -1;
                switch (substate)
                {
                    // Hover to the right of the target.
                    case 0:                       
                        Vector2 hoverDestination = target.Center + new Vector2(600, 0);
                        npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), initialFlySpeed)) / 8f;

                        if (universalAttackTimer >= lavaRisingDelay)
                        {
                            universalAttackTimer = 0;
                            substate++;
                        }
                        break;

                    // Raise up slightly.
                    case 1:
                        npc.velocity.X *= 0.8f;

                        if (universalAttackTimer == recoilWait)
                            npc.velocity.Y = -3.2f;

                        else if (universalAttackTimer >= recoilWait + recoilLength)
                        {
                            universalAttackTimer = 0f;
                            substate++;
                        }
                        break;

                    // Dive down into the lava.
                    case 2:
                        Vector2 hoverPosition = new(npc.Center.X, GetLavaWaveHeightFromWorldBottom(npc) - npc.height);
                        npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverPosition) * MathHelper.Min(npc.Distance(hoverPosition), diveSpeed)) / 8f;

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

                        if (npc.WithinRange(hoverPosition, 30f))
                        {
                            universalAttackTimer = 0f;
                            substate++;
                        }
                        break;

                    // Surf along the surface of the lava.
                    case 3:
                        float currentLavaHeight = GetLavaWaveHeightFromWorldBottom(npc);

                        npc.Center = new(npc.Center.X, currentLavaHeight - npc.height);
                        if (initialDirection == 0)
                            initialDirection = npc.Center.X > target.Center.X ? 1 : -1;

                        float currentDirection = npc.Center.X > target.Center.X ? 1 : -1;

                        //if (currentDirection != initialDirection)
                        //{
                        //    npc.velocity.X -= npc.velocity.X > 0f ? 0.01f : -0.01f;

                        //    if (Math.Abs(npc.velocity.X) <= 3f)
                        //        initialDirection = npc.Center.X > target.Center.X ? 1 : -1;
                        //}
                        switch (surfSubstate)
                        {
                            case 0:
                                if (currentDirection != initialDirection)
                                    surfSpeed = MathHelper.Clamp(surfSpeed - 0.15f, 0f, 17f);
                                else
                                    surfSpeed = MathHelper.Clamp(surfSpeed + 0.2f, 0f, 17f);

                                if (surfSpeed <= 0.5f)
                                {
                                    surfSubstate++;
                                    initialDirection = npc.Center.X > target.Center.X ? 1 : -1;
                                }
                                break;

                            case 1:
                                surfSpeed = MathHelper.Clamp(surfSpeed + 0.2f, 0f, 17f);
                                if (surfSpeed >= 17f)
                                {
                                    surfSubstate = 0f;
                                    //initialDirection = npc.Center.X > target.Center.X ? 1 : -1;
                                }
                                break;
                        }

                        npc.velocity.X = -initialDirection * surfSpeed;

                        if (MathF.Abs(npc.Center.X - target.Center.X) < 200f && universalAttackTimer > jumpTime)
                        {
                            universalAttackTimer = 0f;
                            substate++;
                        }
                        break;

                    // Leap out at the target.
                    case 4:
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

            float maxRamsToComplete = 3f;
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
                ref float drawFireAfterimages = ref npc.Infernum().ExtraAI[CommanderFireAfterimagesIndex];
                npc.Infernum().ExtraAI[CommanderFireAfterimagesLengthIndex] = 10f;
                switch (substate)
                {
                    // Get an offset from the player.
                    case 0:
                        commanderOffset += Main.rand.NextFloat(0f, MathHelper.TwoPi) * Main.rand.NextFromList(-1f, 1f);
                        if (universalAttackTimer <= fadeInTime)
                            npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.0625f, 0f, 1f);
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
                            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.0625f, 0f, 1f);

                        npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), flySpeed)) / 8f;

                        // Move out of the way of the target if going around them.
                        if (npc.WithinRange(target.Center, 150f))
                            npc.velocity.X += target.Center.DirectionTo(npc.Center).X * 10f;

                        npc.spriteDirection = (npc.DirectionTo(target.Center).X > 0f) ? 1 : -1;

                        // Spawn the spear if it does not exist.
                        if ((DefenderShieldStatus)spearStatus is DefenderShieldStatus.Inactive || !Main.projectile.Any((Projectile p) => p.active && p.type == ModContent.ProjectileType<CommanderSpear>()))
                        {
                            spearStatus = (float)DefenderShieldStatus.ActiveAndAiming;
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                                Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<CommanderSpear>(), 300, 0f, -1, 0f, npc.whoAmI);
                        }

                        spearStatus = (float)DefenderShieldStatus.ActiveAndAiming;
                        // Make the spear rotation point to the player.
                        float idealRotation = npc.SafeDirectionTo(target.Center).ToRotation();
                        spearRotation = spearRotation.AngleTowards(idealRotation, 0.2f);

                        // If close enough, and the defender is ready.
                        if (npc.WithinRange(hoverDestination, 50f) && universalAttackTimer >= hoverWaitTime && defenderIsReady == 1f)
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

                        drawFireAfterimages = 1f;
                        // Reset the defender being ready.
                        defenderIsReady = 0f;
                        substate++;
                        universalAttackTimer = 0f;
                        break;

                    // If close enough to the defender, or if enough time has passed if they missed.
                    case 3:
                        drawFireAfterimages = 1f;

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
                            float size = MathF.Pow(Main.rand.NextFloat(), 2f);
                            FusableParticleManager.GetParticleSetByType<ProfanedLavaParticleSet>()?.SpawnParticle(npc.Center - (npc.velocity * 0.5f) + (Main.rand.NextVector2Circular(npc.width * 0.5f, npc.height * 0.5f) * size),
                                Main.rand.NextFloat(15f, 20f));
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
                                Particle rock = new ProfanedRockParticle(impactCenter, -Vector2.UnitY.RotatedByRandom(MathF.Tau) * Main.rand.NextFloat(6f, 9f),
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

                                    Vector2 direction = impactCenter + ((MathHelper.TwoPi * i / rockAmount) + Main.rand.NextFloat(-0.4f, 0.4f)).ToRotationVector2();
                                    Vector2 velocity = impactCenter.DirectionTo(direction) * Main.rand.NextFloat(5f, 7f);

                                    // Aim one directly at the player and make it slightly faster.
                                    if (i == rockAmount - 1f)
                                    {
                                        direction = impactCenter + impactCenter.DirectionTo(target.Center);
                                        velocity = impactCenter.DirectionTo(direction) * 7.5f;
                                    }
                                    Utilities.NewProjectileBetter(impactCenter, velocity, ModContent.ProjectileType<ProfanedRock>(), 200, 0f, -1, 0f, npc.whoAmI);
                                }
                            }

                            // Play a loud explosion + hitbox sound and screenshake to give the impact power.
                            SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Pitch = 0.35f, Volume = 1.6f }, target.Center);
                            SoundEngine.PlaySound(npc.HitSound.Value with { Volume = 3f }, target.Center);

                            if (CalamityConfig.Instance.Screenshake)
                                ScreenEffectSystem.SetBlurEffect(npc.Center, 0.1f, 25);

                            // Damage the defender slightly, as it has been stabbed by the spear.
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

                switch (substate)
                {
                    case 0:
                        if (universalAttackTimer <= fadeInTime)
                            npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.0625f, 0f, 1f);
                        break;
                    // Move to said offset.
                    case 1:
                        Vector2 hoverDestination = target.Center + (commanderOffset + MathF.PI).ToRotationVector2() * defenderHoverDistance;

                        if (universalAttackTimer == 1)
                        {
                            npc.Center = hoverDestination;
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
                            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.0625f, 0f, 1f);

                        npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), flySpeed)) / 8f;

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
                        if (npc.WithinRange(hoverDestination, 50f))
                            defenderIsReady = 1f;
                        else
                            defenderIsReady = 0f;
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
                            float size = MathF.Pow(Main.rand.NextFloat(), 2f);
                            FusableParticleManager.GetParticleSetByType<ProfanedLavaParticleSet>()?.SpawnParticle(npc.Center - (npc.velocity * 0.5f) + (Main.rand.NextVector2Circular(npc.width * 0.5f, npc.height * 0.5f) * size),
                                Main.rand.NextFloat(15f, 20f));
                        }
                        break;
                }
            }
        }

        public static void DoBehavior_DefenderDeathAnimation(NPC npc, Player target, ref float universalAttackTimer, NPC commander)
        {
            ref float substate = ref commander.Infernum().ExtraAI[0];
            ref float catchingLeftHandXOG = ref commander.Infernum().ExtraAI[1];
            ref float catchingLeftHandYOG = ref commander.Infernum().ExtraAI[2];
            ref float catchingRightHandXOG = ref commander.Infernum().ExtraAI[3];
            ref float catchingRightHandYOG = ref commander.Infernum().ExtraAI[4];

            ref float drawBlackBars = ref commander.Infernum().ExtraAI[CommanderDrawBlackBarsIndex];
            ref float drawBlackRotation = ref commander.Infernum().ExtraAI[CommanderBlackBarsRotationIndex];
            ref float handsSpawned = ref commander.Infernum().ExtraAI[CommanderHandsSpawnedIndex];
            ref float leftHandIndex = ref commander.Infernum().ExtraAI[LeftHandIndex];
            ref float rightHandIndex = ref commander.Infernum().ExtraAI[RightHandIndex];
            ref float leftHandX = ref commander.Infernum().ExtraAI[LeftHandXIndex];
            ref float leftHandY = ref commander.Infernum().ExtraAI[LeftHandYIndex];
            ref float rightHandX = ref commander.Infernum().ExtraAI[RightHandXIndex];
            ref float rightHandY = ref commander.Infernum().ExtraAI[RightHandYIndex];

            NPC leftHand = null;
            NPC rightHand = null;

            if (Main.npc.IndexInRange((int)leftHandIndex) && Main.npc.IndexInRange((int)rightHandIndex) && (rightHandIndex != 0 && leftHandIndex != 0))
            {
                leftHand = Main.npc[(int)leftHandIndex];
                rightHand = Main.npc[(int)rightHandIndex];
            }
            else
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    leftHandIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X - 160, (int)npc.Center.Y, ModContent.NPCType<EtherealHand>(), 0, -1);
                    rightHandIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X + 160, (int)npc.Center.Y, ModContent.NPCType<EtherealHand>(), 0, 1);
                    npc.netUpdate = true;
                }
            }

            // The commander spawns in the hands, moves towards your top right, pulls the defender into its hands, spins it around itself twice then lobs it at the player at mach 10.
            if (npc.type == CommanderType)
            {
                ref float spearStatus = ref commander.Infernum().ExtraAI[CommanderSpearStatusIndex];

                float flySpeed = 24f;
                float symbolSpawnRate = 15f;

                Vector2 focusPosition = target.Center + new Vector2(0f, target.gfxOffY) + (-0.4f).ToRotationVector2() * 70f;


                if (substate < 6f)
                {
                    target.Infernum_Camera().ScreenFocusInterpolant = 3f;
                    target.Infernum_Camera().ScreenFocusPosition = focusPosition;
                    target.Infernum_Camera().CurrentScreenShakePower = 1f;
                    drawBlackBars = 1f;

                    // Do not take damage.
                    npc.dontTakeDamage = true;
                    // Do not deal damage.
                    npc.damage = 0;
                    // Hide UI.
                    if (Main.myPlayer == npc.target)
                        Main.hideUI = true;
                }

                // Spawn cool symbols.
                if (universalAttackTimer % symbolSpawnRate == symbolSpawnRate - 1f)
                {
                    Vector2 position = npc.Center  + npc.DirectionTo(target.Center).SafeNormalize(Vector2.UnitY) * 50f + Main.rand.NextVector2Circular(250f, 250f);
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
                            npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), flySpeed)) / 8f;
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

                npc.Infernum().ShouldUseSaturationBlur = true;
                switch (substate)
                {
                    // Hover to the right of the target.
                    case 0:
                        Vector2 hoverDestination = target.Center + new Vector2(1200f, 0f);

                        if (localAttackTimer == 1)
                            npc.Center = hoverDestination;
                        npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), flySpeed)) / 8f;
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

                        if (InfernumConfig.Instance.FlashbangOverlays && localAttackTimer == 0)
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
                        Vector2 recievingHandPos = new(-150f, -75f);
                        Vector2 aimingHandPos = new(-40f, 25f);

                        // Right hand. This hurts to try and read.
                        rightHandX = recievingHandPos.X;
                        catchingRightHandXOG = recievingHandPos.X;
                        rightHandY = recievingHandPos.Y;
                        catchingRightHandYOG = recievingHandPos.Y;

                        leftHandX = aimingHandPos.X;
                        catchingLeftHandXOG = aimingHandPos.X;
                        leftHandY = aimingHandPos.Y;
                        catchingLeftHandYOG = aimingHandPos.Y;

                        if (localAttackTimer == 1)
                            npc.velocity = npc.SafeDirectionTo(new Vector2(rightHandX, rightHandY) + commander.Center) * (npc.Distance(new Vector2(rightHandX, rightHandY) + commander.Center) / pullBackTime);

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

                        npc.velocity = Vector2.Zero;
                        npc.Center = rightHand.Center;

                        // Right hand.
                        recievingHandPos = originalRightHandPos.RotatedBy(3f * rightHandMoveInterpolant);
                        // Left hand.
                        aimingHandPos = Vector2.Lerp(originalLeftHandPos, new Vector2(-125f, 70f), leftHandMoveInterpolant);

                        leftHandX = aimingHandPos.X;
                        leftHandY = aimingHandPos.Y;
                        rightHandX = recievingHandPos.X;
                        rightHandY = recievingHandPos.Y;

                        if (localAttackTimer >= reelbackTime)
                        {
                            // Reset the default positions for the next attack.
                            catchingRightHandXOG = recievingHandPos.X;
                            catchingRightHandYOG = recievingHandPos.Y;

                            catchingLeftHandXOG = aimingHandPos.X;
                            catchingLeftHandYOG = aimingHandPos.Y;

                            localAttackTimer = 0;
                            substate++;
                            return;
                        }
                        break;

                    // Continue to stick to the hands. They will stop at the correct position.
                    case 5:
                        rightHandMoveInterpolant = CalamityUtils.ExpInEasing(localAttackTimer / launchTime, 0);
                        leftHandMoveInterpolant = CalamityUtils.SineInOutEasing(localAttackTimer / launchTime, 0);

                        recievingHandPos = originalRightHandPos.RotatedBy(-4f * rightHandMoveInterpolant);
                        aimingHandPos = Vector2.Lerp(originalLeftHandPos, new Vector2(-55f, 40f), leftHandMoveInterpolant);

                        leftHandX = aimingHandPos.X;
                        leftHandY = aimingHandPos.Y;
                        rightHandX = recievingHandPos.X;
                        rightHandY = recievingHandPos.Y;

                        npc.Center = rightHand.Center;

                        if (localAttackTimer >= launchTime)
                        {
                            leftHandX = 0f;
                            leftHandY = 0f;
                            rightHandX = 0f;
                            rightHandY = 0f;

                            // Launch at the target.
                            npc.velocity = npc.DirectionTo(target.Center) * yeetSpeed;
                            npc.damage = 300;

                            localAttackTimer = 0;
                            substate++;
                            commander.Infernum().ExtraAI[DefenderHasBeenYeetedIndex] = 1f;
                            //SelectNewAttack(commander, ref universalAttackTimer);
                            return;
                        }
                        break;
                }

                localAttackTimer++;
            }
        }
        #endregion

        #region Commander Attacks
        public static void DoBehavior_LargeGeyserAndFireCharge(NPC npc, Player target, ref float universalAttackTimer)
        {
            ref float substate = ref npc.Infernum().ExtraAI[0];

            float moveUnderAndWaitTime = 60f;
            float maxMoveUnderAndWaitTime = 120f;
            float recoilDownwardsTime = 30f;
            float flyUpwardsDelay = 60f;
            float flyUpwardsSpeed = 60f;
            float curveTowardsTargetDelay = 40f;
            float completeSlowdownLength = 20f + curveTowardsTargetDelay;
            float aimTime = 60f + completeSlowdownLength;
            float aimedChargeSpeed = 70f;
            float maxChargeLength = 85f;
            float afterImpactWaitLength = 20f;

            switch (substate)
            {
                // Move under the target and telegraph where the geyser will spawn.
                case 0:
                    float flySpeed = 25f;
                    Vector2 hoverDestination = target.Center + new Vector2(0, 400f);

                    npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), flySpeed)) / 8f;

                    // Move out of the way of the target if going around them.
                    if (npc.WithinRange(target.Center, 150f))
                        npc.velocity.X += target.Center.DirectionTo(npc.Center).X * 10f;

                    // If close enough.
                    if (npc.WithinRange(hoverDestination, 100f))
                    {
                        // Create a bunch of lava particles under the commander on the players bottom of the screen.
                        for (int i = 0; i < 6; i++)
                        {
                            Vector2 position = new(npc.Center.X + Main.rand.NextFloat(-200f, 200f), Main.screenHeight + - 100f + Main.screenPosition.Y + Main.rand.NextFloat(-70f, 70f));
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
                            Utilities.NewProjectileBetter(center, Vector2.Zero, ModContent.ProjectileType<LavaEruptionPillar>(), 500, 0f);

                            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(pillar => pillar.ModProjectile<LavaEruptionPillar>().BigVersion = true);
                            Utilities.NewProjectileBetter(center + new Vector2(-2700f, 0f), Vector2.Zero, ModContent.ProjectileType<LavaEruptionPillar>(), 500, 0f);
                            ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(pillar => pillar.ModProjectile<LavaEruptionPillar>().BigVersion = true);
                            Utilities.NewProjectileBetter(center + new Vector2(2700f, 0f), Vector2.Zero, ModContent.ProjectileType<LavaEruptionPillar>(), 500, 0f);
                        }
                    }

                    if (universalAttackTimer >= recoilDownwardsTime + flyUpwardsDelay)
                    {
                        float lerpValue = MathHelper.Clamp(Utils.GetLerpValue(300, 500, npc.Center.Y - target.Center.Y, false), 0f, 0.35f);
                        npc.velocity = -Vector2.UnitY * flyUpwardsSpeed * (1 + lerpValue);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 center = new(npc.Center.X, (Main.maxTilesY * 16f) - 50f);
                            int rockAmount = 33;
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
                                Utilities.NewProjectileBetter(center, velocity, ModContent.ProjectileType<ProfanedRock>(), 200, 0f, -1, 0f, npc.whoAmI);
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
                    if (universalAttackTimer > curveTowardsTargetDelay && universalAttackTimer <= completeSlowdownLength)
                    {
                        float interpolant = (universalAttackTimer - curveTowardsTargetDelay) / (completeSlowdownLength - curveTowardsTargetDelay);
                        float xAmount = MathHelper.Lerp(1.2f, 0f, interpolant);
                        npc.velocity.X += npc.SafeDirectionTo(target.Center).X * xAmount;
                        npc.velocity.Y += 0.35f;

                        // Set the aim telegraph.
                    }
                    else if (universalAttackTimer >= aimTime)
                    {
                        npc.velocity = npc.SafeDirectionTo(target.Center) * aimedChargeSpeed;
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

                        // Spawn a large crack.
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            Utilities.NewProjectileBetter(impactCenter, Vector2.Zero, ModContent.ProjectileType<ProfanedCrack>(), 0, 0f);

                        // Recoil backwards.
                        npc.velocity = -npc.velocity.SafeNormalize(Vector2.UnitY) * 3.5f;

                        substate++;
                        universalAttackTimer = 0f;
                    }
                    break;

                case 4:
                    npc.velocity *= 0.98f;
                    if (universalAttackTimer >= afterImpactWaitLength)
                        SelectNewAttack(npc, ref universalAttackTimer);
                    break;
            }

        }

        public static void DoBehavior_ReleaseAimingFireballs(NPC npc, Player target, ref float universalAttackTimer)
        {
            float initialDelay = 90f;
            float releaseRate = 30f;
            float releaseTime = initialDelay + releaseRate * 4f;
            float attackLength = releaseTime + 100f;
            float fireballCount = 5f;
            float fireballSpeed = 6f;
            float flySpeed = 19f;

            Vector2 hoverPosition = target.Center + new Vector2(0f, -400f);
            npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverPosition) * MathHelper.Min(npc.Distance(hoverPosition), flySpeed)) / 8f;

            if (universalAttackTimer >= initialDelay && universalAttackTimer % releaseRate == 0 && universalAttackTimer < releaseTime)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < fireballCount; i++)
                    {
                        Vector2 velocity = npc.SafeDirectionTo(target.Center).RotatedBy(Main.rand.NextFloat(1.1f, 2.5f) * Main.rand.NextFromList(-1f, 1f)) * fireballSpeed;
                        Utilities.NewProjectileBetter(npc.Center, velocity, ModContent.ProjectileType<HolyAimingFireballs>(), 300, 0f);
                    }
                }
            }

            if (universalAttackTimer >= attackLength)
            {
                SelectNewAttack(npc, ref universalAttackTimer);
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
                    Main.rand.NextBool() ? WayfinderSymbol.Colors[1] : Color.OrangeRed, 60, Main.rand.NextFloat(0.85f, 1.15f) * scaleModifier, Main.rand.NextFloat(0.95f, 1.05f), true);
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
        public static float GetLavaWaveHeightFromWorldBottom(NPC defender)
        {
            ProfanedLavaWave lavaWave = null;
            for (int i = 0; i < Main.projectile.Length; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.ModProjectile is null)
                    continue;

                if (proj.type == ModContent.ProjectileType<ProfanedLavaWave>() && proj.active && proj.ModProjectile is ProfanedLavaWave lava)
                {
                    lavaWave = lava;
                    break;
                }
            }

            if (lavaWave is null)
                return 0f;
            else
            {
                // Get both bounds.
                float rightMostX = lavaWave.Projectile.Center.X;
                float leftMostX = rightMostX - lavaWave.Length;
                // Make them go from 0 at the left to the max length at the right.
                rightMostX -= leftMostX;
                leftMostX -= leftMostX;
                // Get the npcs x position with the same offset.
                float npcX = defender.Center.X - leftMostX;
                // Get the 0-1 ratio of it between the bounds and clamp it.
                float xPositionRatio = Math.Clamp(npcX - rightMostX, 0f, 1f);
                return lavaWave.OffsetFunction(xPositionRatio).Y + ((Main.maxTilesY * 16f) - 50f) - lavaWave.WaveHeight;
            }
        }

        public static void HandleMusicSyncStuff(NPC commander)
        {
            ref float musicTimer = ref commander.Infernum().ExtraAI[MusicTimerIndex];

            // If the timer has reached the end, reset it.
            if (musicTimer >= LoopingMusicLength)
                musicTimer = 0f;
            // Else, advance the timer by one.
            else
                musicTimer++;
        }

        public static bool IsLoopingMusicPlaying(NPC commander)
        {
            if (commander.Infernum().ExtraAI[MusicHasStartedIndex] == 1f)
                return true;
            return false;
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
                }
            }

            if (Main.npc.IndexInRange(CalamityGlobalNPC.doughnutBossHealer))
            {
                if (Main.npc[CalamityGlobalNPC.doughnutBossHealer].active)
                {
                    NPC healer = Main.npc[CalamityGlobalNPC.doughnutBossHealer];
                    for (int i = 0; i < aiSlotsToClear; i++)
                        healer.Infernum().ExtraAI[i] = 0f;
                }
            }

            // Reset the universal attack timer.
            universalAttackTimer = 0f;

            // Swap to a specific attack if one is specified.
            if (specificAttackToSwapTo != -1f)
            {
                commander.ai[0] = specificAttackToSwapTo;
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
            else if ((GuardiansAttackType)commander.ai[0] == GuardiansAttackType.CrashRam)
                commander.ai[0] = (float)GuardiansAttackType.SpearDashAndGroundSlam;

            // Else if its in the commander + defender combos, remain in them.
            else if ((GuardiansAttackType)commander.ai[0] < GuardiansAttackType.CrashRam)
                commander.ai[0]++;

            // Else if its at the end of the commander solo attacks, reset back to the start.
            else if ((GuardiansAttackType)commander.ai[0] == GuardiansAttackType.ReleaseAimingFireballs)
                commander.ai[0] = (float)GuardiansAttackType.LargeGeyserAndFireCharge;

            // Else if its in the commander solo attacks, remain in them.
            else if ((GuardiansAttackType)commander.ai[0] < GuardiansAttackType.ReleaseAimingFireballs)
                commander.ai[0]++;
        }

        public static void DespawnTransitionProjectiles()
        {
            Utilities.DeleteAllProjectiles(true,
                ModContent.ProjectileType<ProfanedCirclingRock>(),
                ModContent.ProjectileType<ProfanedRock>(),
                ModContent.ProjectileType<MagicCrystalShot>(),
                ModContent.ProjectileType<MagicSpiralCrystalShot>(),
                ModContent.ProjectileType<DefenderShield>(),
                ModContent.ProjectileType<LavaEruptionPillar>(),
                ModContent.ProjectileType<ProfanedSpearInfernum>()
                );
        }
        #endregion
    }
}
