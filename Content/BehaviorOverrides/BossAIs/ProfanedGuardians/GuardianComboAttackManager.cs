using CalamityMod;
using CalamityMod.NPCs.ProfanedGuardians;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.GlobalInstances;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
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

        public static void DoBehavior_SpawnEffects(NPC npc, Player target, ref float attackTimer)
        {
            float inertia = 20f;
            float flySpeed = 25f;

            // Do not take or deal damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            Vector2 positionToMoveTo = CrystalPosition;
            // If we are the commander, spawn in the pushback fire wall.
            if (npc.type == ModContent.NPCType<ProfanedGuardianCommander>())
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
            }

            // This is the ideal velocity it would have
            Vector2 idealVelocity = npc.SafeDirectionTo(positionToMoveTo) * flySpeed;
            // And this is the actual velocity, using inertia and its existing one.
            npc.velocity = (npc.velocity * (inertia - 1f) + idealVelocity) / inertia;

            if (npc.WithinRange(positionToMoveTo, 20f))
            {
                npc.damage = npc.defDamage;
                npc.dontTakeDamage = false;
                // Go to the initial attack and reset the attack timer.
                SelectNewAttack(npc, ref attackTimer);
            }
        }

        public static void DoBehavior_FlappyBird(NPC npc,Player target, ref float attackTimer, NPC commander)
        {
            // This attack ends automatically when the crystal wall dies, it advances the attackers attack state, which the other
            // guardians check for and advance with it.

            // The commander bobs on the spot, pausing to aim and fire a fire beam at the player from afar.
            if (npc.type == ModContent.NPCType<ProfanedGuardianCommander>())
            {
                float deathrayFireRate = 150;
                float initialDelay = 460;
                ref float movementTimer = ref npc.Infernum().ExtraAI[0];

                // Do not take damage.
                npc.dontTakeDamage = true;

                // If time to fire, the target is close enough and the pushback wall is not present.
                if (attackTimer % deathrayFireRate == 0 && target.WithinRange(npc.Center, 6200f) && attackTimer >= initialDelay)
                {
                    // Fire deathray.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 aimDirection = npc.SafeDirectionTo(target.Center);
                        Utilities.NewProjectileBetter(npc.Center, aimDirection, ModContent.ProjectileType<HolyAimedDeathrayTelegraph>(), 0, 0f, -1, 0f, npc.whoAmI);
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
            else if (npc.type == ModContent.NPCType<ProfanedGuardianDefender>())
            {
                // This is basically flappy bird, the attacker spawns fire walls like the pipes that move towards the entrance of the garden.
                ref float lastOffsetY = ref npc.Infernum().ExtraAI[0];
                ref float movedToPosition = ref npc.Infernum().ExtraAI[1];
                float wallCreationRate = 60f;
                ref float drawFireSuckup = ref npc.ai[2];
                drawFireSuckup = 1;
                ref float fireSuckupWidth = ref npc.Infernum().ExtraAI[DefenderFireSuckupWidthIndex];

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
                        if (attackTimer % wallCreationRate == 0f && Main.netMode != NetmodeID.MultiplayerClient)
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

                            // Reset the attack timer, for drawing.
                            attackTimer = 0f;
                        }

                        // If the crystal is shattering, decrease the scale, else increase it.
                        if (crystal.ai[0] == 1f)
                            fireSuckupWidth = MathHelper.Clamp(fireSuckupWidth - 0.1f, 0f, 1f);
                        else
                            fireSuckupWidth = MathHelper.Clamp(fireSuckupWidth + 0.1f, 0f, 1f);
                    }
                }

                // If the commander has gone onto the next attack.
                if ((GuardiansAttackType)commander.ai[0] == GuardiansAttackType.SoloHealer)
                {
                    // Enter the looping attack section of the pattern, and reset the attack timer.
                    SelectNewAttack(npc, ref attackTimer);
                    drawFireSuckup = 0f;
                }
            }

            // The healer sits behind the shield and visibly pours energy into it.
            else if (npc.type == ModContent.NPCType<ProfanedGuardianHealer>())
            {
                ref float drawShieldConnections = ref npc.ai[2];
                ref float connectionsWidthScale = ref npc.Infernum().ExtraAI[HealerConnectionsWidthScaleIndex];

                // Take no damage.
                npc.dontTakeDamage = true;

                // Spawn the shield if this is the first frame.
                if (attackTimer == 1f)
                    NPC.NewNPCDirect(npc.GetSource_FromAI(), CrystalPosition, ModContent.NPCType<HealerShieldCrystal>(), target: target.whoAmI);

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

                // Check if the commander is on the next attack, if so, join it.
                if ((GuardiansAttackType)commander.ai[0] == GuardiansAttackType.SoloHealer)
                {
                    SelectNewAttack(npc, ref attackTimer);
                    drawShieldConnections = 0f;
                }
            }
        }

        public static void DoBehavior_SoloHealer(NPC npc, Player target, ref float attackTimer)
        {
            // The commander remains in the center firing its two spinning fire beams.
            if (npc.type == ModContent.NPCType<ProfanedGuardianCommander>())
            {
                ref float movedToPosition = ref npc.Infernum().ExtraAI[0];
                ref float spawnedLasers = ref npc.Infernum().ExtraAI[1];

                Vector2 hoverPosition = CrystalPosition + new Vector2(-200f, 0);

                // Sit still in the middle of the area.
                if (npc.Distance(hoverPosition) > 7f && movedToPosition == 0f)
                    npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverPosition) * MathHelper.Min(npc.Distance(hoverPosition), 18)) / 8f;
                else
                {
                    npc.velocity.X *= 0.9f;
                    movedToPosition = 1f;
                    float sine = -MathF.Sin(Main.GlobalTimeWrappedHourly * 2f);
                    npc.velocity.Y = sine * 0.5f;
                    npc.spriteDirection = -1;

                    if (spawnedLasers == 0)
                    {
                        spawnedLasers = 1;
                        for (int i = 0; i < 2; i++)
                        {
                            float offsetAngleInterpolant = (float)i / 2;
                            Utilities.NewProjectileBetter(npc.Center, Vector2.UnitY, ModContent.ProjectileType<HolySpinningFireBeam>(), 400, 0f, -1, 0f, offsetAngleInterpolant);
                        }
                    }
                }
            }
            // The defender hovers to your top left, not dealing contact damage and occasionally firing rocks at you.
            if (npc.type == ModContent.NPCType<ProfanedGuardianDefender>())
            {
                float flySpeed = 19f;
                float rockChuckRate = 180;

                // Have very high DR.
                npc.Calamity().DR = 0.9999f;
                npc.lifeRegen = 1000000;

                Vector2 hoverPosition = target.Center + new Vector2(600, -400);
                npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverPosition) * MathHelper.Min(npc.Distance(hoverPosition), flySpeed)) / 8f;

                if (attackTimer % rockChuckRate == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 rockVelocity = npc.SafeDirectionTo(target.Center, Vector2.UnitY);
                    Utilities.NewProjectileBetter(npc.Center, rockVelocity * 70f, ModContent.ProjectileType<ProfanedRock>(), 120, 0f, Main.myPlayer, 0, npc.whoAmI);
                }
            }
        }

        public static void DoBehavior_SoloDefender(NPC npc, Player target, ref float attackTimer)
        {

        }

        public static void DoBehavior_HealerAndDefender(NPC npc, Player target, ref float attackTimer)
        {

        }

        public static void SelectNewAttack(NPC npc, ref float attackTimer)
        {
            // Reset the first 5 extra ai slots. These are used for per attack information.
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0;

            // Reset the attack timer.
            attackTimer = 0;
            // If not the final combo attack, advance the current attack.
            if (npc.ai[0] < 4)
                npc.ai[0]++;
            // Else, reset it back to the first combo attack.
            else if (npc.ai[0] == 4)
                npc.ai[0] = 2;
        }
    }
}
