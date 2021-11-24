using CalamityMod;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod.NPCs;
using CalamityMod.Events;
using Terraria.GameContent.Events;
using System.Collections.Generic;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class MoonLordCoreBehaviorOverride : NPCBehaviorOverride
    {
        public enum MoonLordCoreAttackState
        {
            Teleport = -2,
            Initializations,
            InvulnerableFlyToTarget,
            VulnerableFlyToTarget,
            DeathEffects,
            Despawn
        }

        public const int ArenaWidth = 200;
        public const int ArenaHeight = 150;
        public const int ArenaHorizontalStandSpace = 70;
        public const int ArenaStandSpaceHeight = 19;
        public override int NPCOverrideType => NPCID.MoonLordCore;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        // ai[0] = ai state. -2 = early death state and effects. -1 = spawn body parts. 0 = fly near target (invulnerable). 1 = fly near target (vulnerable)
        //             2 = death state and effects. 3 = despawn
        // ai[1] = ai counter variable
        // ai[2] = appears to be unused
        // ai[3] = appears to be unused
        // localAI[0] = left hand npc index
        // localAI[1] = right hand npc index
        // localAI[2] = head npc index
        // localAI[3] = initialization value, spawning the arena, ect.
        // ExtraAI[0] = enrage flag. 1 is normal, 0 is enraged
        // ExtraAI[1] = counter for summoning seal waves

        public override bool PreAI(NPC npc)
        {
            // Stop rain.
            CalamityMod.CalamityMod.StopRain();

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            bool doingFlyAttack = attackState == (int)MoonLordCoreAttackState.InvulnerableFlyToTarget || attackState == (int)MoonLordCoreAttackState.VulnerableFlyToTarget;

            // Randomly play sounds.
            // This does not happen during initializations or the death animation.
            if (attackState != (int)MoonLordCoreAttackState.Initializations && attackState != (int)MoonLordCoreAttackState.DeathEffects && Main.rand.NextBool(151))
                Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, Main.rand.Next(93, 100), 1f, 0f);

            // Handle enrage checks.
            if (npc.Infernum().arenaRectangle != null)
            {
                Rectangle arena = npc.Infernum().arenaRectangle;

                // 1 is normal. 0 is enraged.
                npc.Infernum().ExtraAI[0] = Main.player[npc.target].Hitbox.Intersects(arena).ToInt();
                if (npc.Infernum().ExtraAI[0] == 0f)
                    npc.Calamity().CurrentlyEnraged = true;

                npc.TargetClosest(false);
            }

            // Player variable.
            Player target = Main.player[npc.target];

            // Reset invulnerability.
            npc.dontTakeDamage = NPC.CountNPCS(NPCID.MoonLordFreeEye) >= 3;

            // Life Ratio
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Start the AI up and create the arena.
            if (npc.localAI[3] == 0f)
            {
                npc.netUpdate = true;

                DeleteMLArena();

                Player closest = Main.player[Player.FindClosest(npc.Center, 1, 1)];
                if (npc.Infernum().arenaRectangle == null)
                    npc.Infernum().arenaRectangle = default;

                Point closestTileCoords = closest.Center.ToTileCoordinates();
                npc.Infernum().arenaRectangle = new Rectangle((int)closest.position.X - ArenaWidth * 8, (int)closest.position.Y - ArenaHeight * 8 + 20, ArenaWidth * 16, ArenaHeight * 16);
                for (int i = closestTileCoords.X - ArenaWidth / 2; i <= closestTileCoords.X + ArenaWidth / 2; i++)
                {
                    for (int j = closestTileCoords.Y - ArenaHeight / 2; j <= closestTileCoords.Y + ArenaHeight / 2; j++)
                    {
                        int relativeX = i - closestTileCoords.X + ArenaWidth / 2;
                        int relativeY = j - closestTileCoords.Y + ArenaHeight / 2;
                        bool withinArenaStand = relativeX > ArenaHorizontalStandSpace && relativeX < ArenaWidth - ArenaHorizontalStandSpace &&
                                                relativeY > ArenaHeight - ArenaStandSpaceHeight;

                        // Create arena tiles.
                        if ((Math.Abs(closestTileCoords.X - i) == ArenaWidth / 2 || Math.Abs(closestTileCoords.Y - j) == ArenaHeight / 2 || withinArenaStand) && !Main.tile[i, j].active())
                        {
                            Main.tile[i, j].type = (ushort)ModContent.TileType<Tiles.MoonlordArena>();
                            Main.tile[i, j].active(true);
                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendTileSquare(-1, i, j, 1, TileChangeType.None);
                            else
                                WorldGen.SquareTileFrame(i, j, true);
                        }
                    }
                }
                npc.localAI[3] = 1f;
                attackState = (int)MoonLordCoreAttackState.Initializations;
                npc.netUpdate = true;
            }

            switch ((MoonLordCoreAttackState)attackState)
            {
                case MoonLordCoreAttackState.Teleport:
                    DoBehavior_Teleport(npc, ref attackState, ref attackTimer);
                    break;
                case MoonLordCoreAttackState.Initializations:
                    DoBehavior_Initializations(npc, ref attackState, ref attackTimer);
                    break;
                case MoonLordCoreAttackState.InvulnerableFlyToTarget:
                    DoBehavior_InvulnerableFlyToTarget(npc, target, ref attackState);
                    break;
                case MoonLordCoreAttackState.VulnerableFlyToTarget:
                    DoBehavior_VulnerableFlyToTarget(npc, target, ref attackTimer);
                    break;
                case MoonLordCoreAttackState.DeathEffects:
                    DoBehavior_DeathEffects(npc, ref attackTimer);
                    return false;
                case MoonLordCoreAttackState.Despawn:
                    DoBehavior_DespawnEffects(npc, ref attackTimer);
                    break;
            }

            Vector2 idealDistance = target.Center - npc.Center + Vector2.UnitY * 130f;

            // Summon waves of eldritch seals at life thresholds.
            if (npc.Infernum().ExtraAI[1] == 0f && lifeRatio < 0.7f)
            {
                for (int i = 0; i < 6; i++)
                {
                    float angle = MathHelper.TwoPi / 6f * i;
                    // Will be 0 or 1, the designated AI types for this wave
                    float ai0 = i % 2f;
                    int idx = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<EldritchSeal>(), 0, ai0, i / 2 * -25, 0f, npc.whoAmI);
                    Main.npc[idx].Infernum().ExtraAI[0] = angle;
                }
                npc.Infernum().ExtraAI[1] = 1f;
                npc.netUpdate = true;
            }
            if (npc.Infernum().ExtraAI[1] == 1f && lifeRatio < 0.4f)
            {
                for (int i = 0; i < 9; i++)
                {
                    float angle = MathHelper.TwoPi / 9f * i;
                    float ai0 = i % 3;
                    int idx = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<EldritchSeal>(), 0, ai0, i / 2 * -30, 0f, npc.whoAmI);
                    Main.npc[idx].Infernum().ExtraAI[0] = angle;
                }
                npc.Infernum().ExtraAI[1] = 2f;
                npc.netUpdate = true;
            }
            if (npc.Infernum().ExtraAI[1] == 2f && lifeRatio < 0.15f)
            {
                for (int i = 0; i < 9; i++)
                {
                    float angle = MathHelper.TwoPi / 9f * i;
                    float ai0 = i % 3;
                    int idx = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<EldritchSeal>(), 0, ai0, i / 2 * -30, 0f, npc.whoAmI);
                    Main.npc[idx].Infernum().ExtraAI[0] = angle;
                }
                for (int i = 0; i < 3; i++)
                {
                    int idx = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<EldritchSeal>(), 0, 3f, 0f, 0f, npc.whoAmI);
                    Main.npc[idx].life = Main.npc[idx].lifeMax = 9800;
                }
                npc.Infernum().ExtraAI[1] = 3f;
                npc.netUpdate = true;
            }

            // Don't take damage if eldritch seals are present.
            if (NPC.AnyNPCs(ModContent.NPCType<EldritchSeal>()))
                npc.dontTakeDamage = true;

            // Do despawn checks.
            bool shouldBeginDespawnAnimation = true;
            if (!doingFlyAttack)
                shouldBeginDespawnAnimation = false;
            if (target.active && !target.dead)
                shouldBeginDespawnAnimation = false;

            // Check to see if all players are dead if considering despawning.
            if (shouldBeginDespawnAnimation)
            {
                for (int i = 0; i < 255; i++)
                {
                    if (Main.player[i].active && !Main.player[i].dead)
                    {
                        shouldBeginDespawnAnimation = false;
                        break;
                    }
                }
            }

            // Begin the despawn animation if all checks failed.
            if (shouldBeginDespawnAnimation)
            {
                attackState = (int)MoonLordCoreAttackState.Despawn;
                attackTimer = 0f;
                npc.netUpdate = true;
            }

            // Teleport if the target is notably far away.
            // This only happens if doing fly attacks.
            if (Main.netMode != NetmodeID.MultiplayerClient && doingFlyAttack && !npc.WithinRange(target.Center, 2300f))
            {
                attackState = (int)MoonLordCoreAttackState.Teleport;

                // A teleport offset is used to ensure that offsets are equal for all body parts when teleporting.
                Vector2 teleportOffset = target.Center - Vector2.UnitY * 150f - npc.Center;
                npc.position += teleportOffset;

                // Bring all eyes and body parts along.
                if (Main.npc[(int)npc.localAI[0]].active)
                {
                    Main.npc[(int)npc.localAI[0]].position += teleportOffset;
                    Main.npc[(int)npc.localAI[0]].netUpdate = true;
                }
                if (Main.npc[(int)npc.localAI[1]].active)
                {
                    Main.npc[(int)npc.localAI[1]].position += teleportOffset;
                    Main.npc[(int)npc.localAI[1]].netUpdate = true;
                }
                if (Main.npc[(int)npc.localAI[2]].active)
                {
                    Main.npc[(int)npc.localAI[2]].position += teleportOffset;
                    Main.npc[(int)npc.localAI[2]].netUpdate = true;
                }

                AffectAllEyes(eye => eye.Center = npc.Center);

                npc.netUpdate = true;
            }
            return false;
        }

        public static void DoBehavior_Teleport(NPC npc, ref float attackState, ref float attackTimer)
        {
            attackTimer++;

            // Roar after the teleport.
            if (attackTimer == 30f)
                Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, 92, 1f, 0f);

            if (attackTimer < 60f)
                MoonlordDeathDrama.RequestLight(attackTimer / 30f, npc.Center);

            if (attackTimer >= 60f)
            {
                attackState = (int)MoonLordCoreAttackState.InvulnerableFlyToTarget;
                attackTimer = 0f;
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_Initializations(NPC npc, ref float attackState, ref float attackTimer)
        {
            // Initially don't take damage.
            npc.dontTakeDamage = true;

            attackTimer++;

            // Roar after a bit of time has passed.
            if (attackTimer == 30f)
                Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, 92, 1f, 0f);

            // Create light.
            if (attackTimer < 60f)
                MoonlordDeathDrama.RequestLight(attackTimer / 30f, npc.Center);

            // Create arms/head and go to the next attack state.
            if (attackTimer >= 60f)
            {
                attackTimer = 0f;
                attackState = (int)MoonLordCoreAttackState.InvulnerableFlyToTarget;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int[] bodyPartIndices = new int[3];
                    for (int i = 0; i < 2; i++)
                    {
                        int handIndex = NPC.NewNPC((int)npc.Center.X + i * 800 - 400, (int)npc.Center.Y - 100, NPCID.MoonLordHand, npc.whoAmI, 0f, 0f, 0f, 0f, 255);
                        Main.npc[handIndex].ai[2] = i;
                        Main.npc[handIndex].netUpdate = true;
                        bodyPartIndices[i] = handIndex;
                    }

                    int headIndex = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y - 400, NPCID.MoonLordHead, npc.whoAmI, 0f, 0f, 0f, 0f, 255);
                    Main.npc[headIndex].netUpdate = true;
                    bodyPartIndices[2] = headIndex;

                    for (int i = 0; i < 3; i++)
                        Main.npc[bodyPartIndices[i]].ai[3] = npc.whoAmI;

                    for (int i = 0; i < 3; i++)
                        npc.localAI[i] = bodyPartIndices[i];

                    // Reset hand AIs.
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (Main.npc[i].type == NPCID.MoonLordHand && Main.npc[i].active)
                            Main.npc[i].ai[0] = 0f;
                    }
                }
            }
            npc.netUpdate = true;
        }

        public static void DoBehavior_InvulnerableFlyToTarget(NPC npc, Player target, ref float attackState)
        {
            npc.dontTakeDamage = true;
            npc.TargetClosest(false);

            Vector2 hoverDestination = target.Center + Vector2.UnitY * 130f;

            // Hover towards the target if not very close to them.
            if (!npc.WithinRange(hoverDestination, 20f))
            {
                float hoverSpeed = 9f;
                if (Main.npc[(int)npc.localAI[2]].ai[0] == 1f)
                    hoverSpeed = 7f;

                Vector2 desiredVelocity = npc.SafeDirectionTo(hoverDestination - npc.velocity) * hoverSpeed;
                Vector2 oldVelocity = npc.velocity;
                npc.SimpleFlyMovement(desiredVelocity, 0.5f);
                npc.velocity = Vector2.Lerp(npc.velocity, oldVelocity, 0.5f);
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Despawn if other parts aren't present or are incorrect.
                bool fuckingDie = false;
                if (npc.localAI[0] < 0f || npc.localAI[1] < 0f || npc.localAI[2] < 0f)
                    fuckingDie = true;
                else if (!Main.npc[(int)npc.localAI[0]].active || Main.npc[(int)npc.localAI[0]].type != NPCID.MoonLordHand)
                    fuckingDie = true;
                else if (!Main.npc[(int)npc.localAI[1]].active || Main.npc[(int)npc.localAI[1]].type != NPCID.MoonLordHand)
                    fuckingDie = true;
                else if (!Main.npc[(int)npc.localAI[2]].active || Main.npc[(int)npc.localAI[2]].type != NPCID.MoonLordHead)
                    fuckingDie = true;

                if (fuckingDie)
                {
                    npc.life = 0;
                    npc.HitEffect();
                    npc.active = false;
                }

                // Start taking damage if other parts are marked as dead.
                bool takeDamage = true;
                if (Main.npc[(int)npc.localAI[0]].Calamity().newAI[0] != 2f)
                    takeDamage = false;
                if (Main.npc[(int)npc.localAI[1]].Calamity().newAI[0] != 2f)
                    takeDamage = false;
                if (Main.npc[(int)npc.localAI[2]].Calamity().newAI[0] != 2f)
                    takeDamage = false;

                // Go to the other non-invulnerable move variant if the Moon Lord should take damage.
                if (takeDamage)
                {
                    attackState = (int)MoonLordCoreAttackState.VulnerableFlyToTarget;
                    npc.dontTakeDamage = false;
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_VulnerableFlyToTarget(NPC npc, Player target, ref float attackTimer)
        {
            npc.dontTakeDamage = false;
            npc.TargetClosest(false);
            Vector2 hoverDestination = target.Center + Vector2.UnitY * 130f;

            // Hover towards the target if not very close to them.
            if (!npc.WithinRange(hoverDestination, 20f))
            {
                Vector2 oldVelocity = npc.velocity;
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination - npc.velocity) * 8f, 0.5f);
                npc.velocity = Vector2.Lerp(npc.velocity, oldVelocity, 0.5f);
            }

            bool eyesButNoSeals = NPC.CountNPCS(NPCID.MoonLordFreeEye) >= 3 && !NPC.AnyNPCs(ModContent.NPCType<EldritchSeal>());
            if (Main.netMode != NetmodeID.MultiplayerClient && eyesButNoSeals && attackTimer % 90f == 89f)
            {
                for (int i = 0; i < 16; i++)
                {
                    Vector2 boltVelocity = (MathHelper.TwoPi * i / 16f).ToRotationVector2() * 2f;
                    Utilities.NewProjectileBetter(npc.Center, boltVelocity, ProjectileID.PhantasmalBolt, 180, 0f);
                }
            }

            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
            attackTimer++;
        }

        public static void DoBehavior_DeathEffects(NPC npc, ref float attackTimer)
        {
            npc.dontTakeDamage = true;
            npc.velocity = Vector2.Lerp(npc.velocity, new Vector2(npc.direction, -0.5f), 0.98f);

            attackTimer++;

            // Create light briefly at the start of the animation.
            // This is done in conjunction with clearing of entities, so it doesn't look odd when it happens.
            if (attackTimer < 60f)
                MoonlordDeathDrama.RequestLight(attackTimer / 60f, npc.Center);

            // Clear away leftover projectiles and free eyes after enough time has passed.
            if (attackTimer == 60f)
                ClearBattleElements();

            // Create dust.
            if (attackTimer % 3f == 0f && attackTimer < 580f && attackTimer > 60f)
            {
                Vector2 spawnAdditive = Utils.RandomVector2(Main.rand, -1f, 1f);
                if (spawnAdditive != Vector2.Zero)
                    spawnAdditive.Normalize();

                spawnAdditive *= 20f + Main.rand.NextFloat() * 400f;
                Vector2 dustSpawnPos = npc.Center + spawnAdditive;
                Point dustSpawnTileCoords = dustSpawnPos.ToTileCoordinates();

                bool canSpawnDust = true;
                if (!WorldGen.InWorld(dustSpawnTileCoords.X, dustSpawnTileCoords.Y, 0))
                    canSpawnDust = false;
                if (canSpawnDust && WorldGen.SolidTile(dustSpawnTileCoords.X, dustSpawnTileCoords.Y))
                    canSpawnDust = false;

                float dustCount = Main.rand.Next(12, 38);
                float ai1 = attackTimer;
                if (canSpawnDust)
                {
                    float initialAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                    for (float i = 0f; i < dustCount; i = ai1 + 1f)
                    {
                        Dust dust = Main.dust[Dust.NewDust(dustSpawnPos, 0, 0, 229, 0f, 0f, 0, default, 1f)];
                        dust.noGravity = true;
                        dust.position = dustSpawnPos;
                        dust.velocity = Vector2.UnitY.RotatedBy(initialAngle + MathHelper.TwoPi * i / dustCount) * Main.rand.NextFloat(1.6f, 9.6f);
                        dust.fadeIn = Main.rand.NextFloat(0.4f, 1.4f);
                        dust.scale = Main.rand.NextFloat(1f, 3f);
                        ai1 = i;
                    }
                }
            }

            // Create smoke effects periodically after a small amount of time has passed.
            // This does not happen while light is being emitted, which happens below.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 15f == 0f && attackTimer < 480f && attackTimer >= 90f)
            {
                bool validSmokeSpawnPosition = true;
                Vector2 smokeSpawnPosition = npc.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(20f, 420f);
                Point smokeSpawnTilePosition = smokeSpawnPosition.ToTileCoordinates();

                if (!WorldGen.InWorld(smokeSpawnTilePosition.X, smokeSpawnTilePosition.Y, 0))
                    validSmokeSpawnPosition = false;
                else if (WorldGen.SolidTile(smokeSpawnTilePosition.X, smokeSpawnTilePosition.Y))
                    validSmokeSpawnPosition = false;

                if (validSmokeSpawnPosition)
                {
                    float smokeDirectionRotation = Main.rand.NextBool(2).ToDirectionInt() * (MathHelper.Pi / 8f + (MathHelper.PiOver4 * Main.rand.NextFloat()));
                    Vector2 smokeVelocity = -Vector2.UnitY.RotatedBy(smokeDirectionRotation) * Main.rand.NextFloat(2f, 6f);
                    Utilities.NewProjectileBetter(smokeSpawnPosition, smokeVelocity, ProjectileID.BlowupSmokeMoonlord, 0, 0f, Main.myPlayer, 0f, 0f);
                }
            }

            // Play the death sound on the first frame of the death animation.
            if (attackTimer == 1f)
                Main.PlaySound(npc.DeathSound, npc.Center);

            // Create light before dying.
            if (attackTimer >= 480f)
                MoonlordDeathDrama.RequestLight(Utils.InverseLerp(480f, 600f, attackTimer, true), npc.Center);

            if (attackTimer >= 600f)
            {
                // Clear away the arena tiles.
                DeleteMLArena();

                npc.life = 0;
                npc.HitEffect(0, 1337.0);
                npc.checkDead();

                // Drop loot.
                if (!BossRushEvent.BossRushActive)
                    typeof(CalamityGlobalAI).GetMethod("MoonLordLoot", Utilities.UniversalBindingFlags)?.Invoke(null, new object[] { npc });

                // Make body parts disappear.
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n.active && (n.type == NPCID.MoonLordHand || n.type == NPCID.MoonLordHead))
                    {
                        n.active = false;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n.whoAmI, 0f, 0f, 0f, 0, 0, 0);
                    }
                }

                // And disappear as well.
                npc.active = false;
            }
        }

        public static void DoBehavior_DespawnEffects(NPC npc, ref float attackTimer)
        {
            npc.dontTakeDamage = true;
            Vector2 idealVelocity = new Vector2(npc.direction, -0.5f);
            npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.98f);

            attackTimer++;
            if (attackTimer < 60f)
                MoonlordDeathDrama.RequestLight(attackTimer / 40f, npc.Center);

            if (attackTimer == 40f)
            {
                for (int projectileIdx = 0; projectileIdx < 1000; projectileIdx++)
                {
                    Projectile projectile = Main.projectile[projectileIdx];
                    if (projectile.active && (projectile.type == ProjectileID.MoonLeech || projectile.type == ProjectileID.PhantasmalBolt ||
                        projectile.type == ProjectileID.PhantasmalDeathray || projectile.type == ProjectileID.PhantasmalEye ||
                        projectile.type == ProjectileID.PhantasmalSphere || projectile.type == ModContent.ProjectileType<PhantasmalBlast>() ||
                        projectile.type == ModContent.ProjectileType<PhantasmalSpark>()))
                    {
                        projectile.active = false;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, projectileIdx, 0f, 0f, 0f, 0, 0, 0);
                    }
                }
                for (int goreIdx = 0; goreIdx < 500; goreIdx++)
                {
                    Gore gore = Main.gore[goreIdx];
                    if (gore.active && gore.type >= 619 && gore.type <= 622)
                        gore.active = false;
                }
            }

            if (attackTimer >= 60f)
            {
                for (int npcIdx = 0; npcIdx < Main.maxNPCs; npcIdx++)
                {
                    NPC npcFromArray = Main.npc[npcIdx];
                    if (npcFromArray.active && (npcFromArray.type == NPCID.MoonLordHand || npcFromArray.type == NPCID.MoonLordHead))
                    {
                        npcFromArray.active = false;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npcFromArray.whoAmI, 0f, 0f, 0f, 0, 0, 0);
                    }
                }

                DeleteMLArena();
                npc.active = false;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI, 0f, 0f, 0f, 0, 0, 0);

                NPC.LunarApocalypseIsUp = false;
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.WorldData, -1, -1, null, 0, 0f, 0f, 0f, 0, 0, 0);
            }
        }

        public static void AffectAllEyes(Action<NPC> toExecute)
        {
            foreach (var eye in MoonLordHandBehaviorOverride.GetTrueEyes)
                toExecute(eye);
        }

        public static void ClearBattleElements()
        {
            List<int> clearableProjectiles = new List<int>()
            {
                ProjectileID.MoonLeech,
                ProjectileID.PhantasmalBolt,
                ProjectileID.PhantasmalDeathray,
                ProjectileID.PhantasmalEye,
                ProjectileID.PhantasmalSphere,
                ModContent.ProjectileType<PhantasmalBlast>(),
                ModContent.ProjectileType<PhantasmalSpark>()
            };

            // Clear away battle projectiles.
            for (int k = 0; k < Main.maxProjectiles; k++)
            {
                Projectile projectile = Main.projectile[k];
                if (projectile.active && clearableProjectiles.Contains(projectile.type))
                    projectile.Kill();
            }

            // Clear away free eyes.
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].type == NPCID.MoonLordFreeEye)
                {
                    Main.npc[i].active = false;
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i);
                }
            }
        }

        public static void DeleteMLArena()
        {
            int surface = (int)Main.worldSurface;
            for (int i = 0; i < Main.maxTilesX; i++)
            {
                for (int j = 0; j < surface; j++)
                {
                    if (Main.tile[i, j] != null)
                    {
                        if (Main.tile[i, j].type == ModContent.TileType<Tiles.MoonlordArena>())
                        {
                            Main.tile[i, j] = new Tile();
                            if (Main.netMode == NetmodeID.Server)
                            {
                                NetMessage.SendTileSquare(-1, i, j, 1, TileChangeType.None);
                            }
                            else
                            {
                                WorldGen.SquareTileFrame(i, j, true);
                            }
                        }
                    }
                }
            }
        }
    }
}
