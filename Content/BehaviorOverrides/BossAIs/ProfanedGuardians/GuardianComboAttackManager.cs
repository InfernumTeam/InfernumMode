using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ProfanedGuardians;
using CalamityMod.Particles;
using InfernumMode.Assets.Sounds;
using InfernumMode.Content.Projectiles.Wayfinder;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.GlobalInstances;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public static class GuardianComboAttackManager
    {
        public enum GuardiansAttackType
        {
            // Initial attacks.
            SpawnEffects,
            FlappyBird,

            // All 3 combo attacks.
            SoloHealer,
            SoloDefender,
            HealerAndDefender,

            CommanderDeathAnimation
        }

        public static Vector2 CrystalPosition => WorldSaveSystem.ProvidenceArena.TopLeft() * 16f + new Vector2(6780, 1500);

        public const int DefenderFireSuckupWidthIndex = 10;
        public const int HealerConnectionsWidthScaleIndex = 11;
        public const int DefenderShouldGlowIndex = 12;
        public const int DefenderDrawDashTelegraphIndex = 13;
        public const int DefenderDashTelegraphOpacityIndex = 14;
        public const int CommanderMovedToTriplePosition = 15;

        public static int CommanderType => ModContent.NPCType<ProfanedGuardianCommander>();
        public static int DefenderType => ModContent.NPCType<ProfanedGuardianDefender>();
        public static int HealerType => ModContent.NPCType<ProfanedGuardianHealer>();


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
                    SelectNewAttack(npc, ref attackTimer, npc);
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
                npc.spriteDirection = 1;

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

                // Give the player infinite flight time.
                for (int i = 0; i < Main.player.Length; i++)
                {
                    Player player = Main.player[i];
                    if (player.active && !player.dead && player.Distance(npc.Center) <= 10000f)
                        player.wingTime = player.wingTimeMax;
                }

                // Do not take damage.
                npc.dontTakeDamage = true;

                // Safely check for the crystal. The defender should stop attacking if it is not present
                if (Main.npc.IndexInRange(GlobalNPCOverrides.ProfanedCrystal))
                {
                    if (Main.npc[GlobalNPCOverrides.ProfanedCrystal].active)
                    {
                        NPC crystal = Main.npc[GlobalNPCOverrides.ProfanedCrystal];
                        Vector2 hoverPosition = CrystalPosition + new Vector2(125f, 475f);
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
                            npc.spriteDirection = 1;
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
                ref float movedToPosition = ref npc.Infernum().ExtraAI[CommanderMovedToTriplePosition];
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
                    npc.velocity.X *= 0.9f;
                    movedToPosition = 1f;
                    float sine = -MathF.Sin(Main.GlobalTimeWrappedHourly * 2f);
                    npc.velocity.Y = sine * 0.5f;
                    npc.spriteDirection = 1;

                    if (spawnedLasers == 0)
                    {
                        spawnedLasers = 1;
                        for (int i = 0; i < 2; i++)
                        {
                            float offsetAngleInterpolant = (float)i / 2;
                            Utilities.NewProjectileBetter(npc.Center, Vector2.UnitY, ModContent.ProjectileType<HolySpinningFireBeam>(), 400, 0f, -1, 0f, offsetAngleInterpolant);

                            // Screenshake
                            if (CalamityConfig.Instance.Screenshake)
                                Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 3f;
                        }
                    }
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
                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(rock => rock.ModProjectile<ProfanedRock>().WaitTime = waitTimes[waitTimeToUse]);
                        Utilities.NewProjectileBetter(rockPosition, Vector2.Zero, ModContent.ProjectileType<ProfanedRock>(), 120, 0f, Main.myPlayer, MathHelper.TwoPi * i / rockAmount, npc.whoAmI);
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
                    npc.velocity = (npc.velocity * 5f + npc.SafeDirectionTo(hoverPosition) * MathHelper.Min(npc.Distance(hoverPosition), 18)) / 8f;
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
                            Utilities.NewProjectileBetter(projectileSpawnPosition, shootVelocity, ModContent.ProjectileType<MagicCrystalShot>(), 230, 0f, -1, 0f, crystalsFired % 2f == 0f ? -1f : 1f);
                        }
                        crystalsFired++;
                    }

                    // The attack should end.
                    if (crystalsFired >= maxCrystalsFired)
                    {
                        SelectNewAttack(commander, ref universalAttackTimer, npc);
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
                npc.velocity.X *= 0.9f;
                float sine = -MathF.Sin(Main.GlobalTimeWrappedHourly * 2f);
                npc.velocity.Y = sine * 0.5f;
                npc.spriteDirection = 1;
            }

            else if (npc.type == DefenderType)
            {
                ref float substate = ref npc.Infernum().ExtraAI[0];
                ref float hoverOffsetX = ref npc.Infernum().ExtraAI[1];
                ref float hoverOffsetY = ref npc.Infernum().ExtraAI[2];
                ref float dashesCompleted = ref npc.Infernum().ExtraAI[3];

                ref float drawDashTelegraph = ref commander.Infernum().ExtraAI[DefenderDrawDashTelegraphIndex];
                ref float dashTelegraphOpacity = ref commander.Infernum().ExtraAI[DefenderDashTelegraphOpacityIndex];

                float maxDashes = 4f;
                float waitTime = dashesCompleted == 0f ? 120f : 60f;
                float telegraphWaitTime = 20f;
                float dashTime = 30f;
                float dashSpeed = 35f;
                Vector2 oldHoverOffset = new(hoverOffsetX, hoverOffsetY);

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
                        npc.spriteDirection = MathF.Sign(npc.DirectionTo(target.Center).X);
                        float distance = 625f;
                        Vector2 position = target.Center + oldHoverOffset * distance;

                        //// Create some fire at the position.
                        //Particle fire = new HeavySmokeParticle(position + Main.rand.NextVector2Circular(30f, 30f), Vector2.Zero, WayfinderSymbol.Colors[1], 20, Main.rand.NextFloat(0.9f, 1.3f), 1f, glowing: true);
                        //Particle fire2 = new HeavySmokeParticle(position + Main.rand.NextVector2Circular(30f, 30f), Vector2.Zero, WayfinderSymbol.Colors[0], 20, Main.rand.NextFloat(0.5f, 0.9f), 1f, glowing: true);
                        //GeneralParticleHandler.SpawnParticle(fire);
                        //GeneralParticleHandler.SpawnParticle(fire2);

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
                        drawDashTelegraph = 1;
                        npc.velocity = npc.DirectionTo(target.Center) * dashSpeed;
                        commander.Infernum().ExtraAI[DefenderShouldGlowIndex] = 1;
                        SoundEngine.PlaySound(InfernumSoundRegistry.VassalJumpSound, target.Center);
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
                            
                            if (dashesCompleted >= maxDashes)
                                SelectNewAttack(commander, ref universalAttackTimer, npc);
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
                ref float localAttackTimer = ref npc.Infernum().ExtraAI[0];
                // Move around the commander.
                Vector2 hoverDestination = commander.Center + (localAttackTimer / 25f).ToRotationVector2() * 150f;
                if (npc.velocity.Length() < 2f)
                    npc.velocity = Vector2.UnitY * -2.4f;

                float flySpeed = MathHelper.Lerp(9f, 23f, Utils.GetLerpValue(50f, 270f, npc.Distance(hoverDestination), true));
                flySpeed *= Utils.GetLerpValue(0f, 50f, npc.Distance(hoverDestination), true);
                npc.velocity = npc.velocity * 0.85f + npc.SafeDirectionTo(hoverDestination) * flySpeed * 0.15f;
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(hoverDestination) * flySpeed, 4f);
                npc.spriteDirection = MathF.Sign(npc.DirectionTo(target.Center).X);
                localAttackTimer++;
            }
        }

        public static void DoBehavior_HealerAndDefender(NPC npc, Player target, ref float universalAttackTimer, NPC commander)
        {
            // Commander remains hovering still.
            if (npc.type == CommanderType)
            {
                //npc.velocity.X *= 0.9f;
                //float sine = -MathF.Sin(Main.GlobalTimeWrappedHourly * 2f);
                //npc.velocity.Y = sine * 0.5f;
                //npc.spriteDirection = 1;
            }

            else if (npc.type == DefenderType)
            {
            }

            else if (npc.type == HealerType)
            {
            }

            if (universalAttackTimer >= 1f)
                SelectNewAttack(commander, ref universalAttackTimer, npc);
        }

        public static void SelectNewAttack(NPC commander, ref float universalAttackTimer, NPC npc)
        {
            // Reset the first 5 extra ai slots. These are used for per attack information.
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0;

            if (Main.npc.IndexInRange(CalamityGlobalNPC.doughnutBossDefender))
            {
                if (Main.npc[CalamityGlobalNPC.doughnutBossDefender].active)
                {
                    NPC defender = Main.npc[CalamityGlobalNPC.doughnutBossDefender];
                    for (int i = 0; i < 5; i++)
                        defender.Infernum().ExtraAI[i] = 0;
                }
            }

            if (Main.npc.IndexInRange(CalamityGlobalNPC.doughnutBossHealer))
            {
                if (Main.npc[CalamityGlobalNPC.doughnutBossHealer].active)
                {
                    NPC healer = Main.npc[CalamityGlobalNPC.doughnutBossHealer];
                    for (int i = 0; i < 5; i++)
                        healer.Infernum().ExtraAI[i] = 0;
                }
            }

            // Reset the attack timer.
            universalAttackTimer = 0;
            // If not the final combo attack, advance the current attack.
            if (commander.ai[0] < 4)
                commander.ai[0]++;

            // Else, reset it back to the first combo attack.
            else if (commander.ai[0] == 4)
                commander.ai[0] = 2;
        }
    }
}
