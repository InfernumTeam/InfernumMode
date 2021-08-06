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

namespace InfernumMode.FuckYouModeAIs.MoonLord
{
    public class MoonLordCoreBehaviorOverride : NPCBehaviorOverride
    {
        public const int CoreLifeMax = 99990;
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
            // Adjust lifeMax
            CalamityMod.CalamityMod.StopRain();

            if (npc.lifeMax != CoreLifeMax)
            {
                npc.life = npc.lifeMax = CoreLifeMax;
                npc.netUpdate = true;
            }
            // Play a random Moon Lord sound
            if (npc.ai[0] != -1f && npc.ai[0] != 2f && Main.rand.NextBool(200))
                Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, Main.rand.Next(93, 100), 1f, 0f);

            if (npc.Infernum().arenaRectangle != null)
            {
                Rectangle rect = npc.Infernum().arenaRectangle;
                // 1 is normal. 0 is enraged.
                npc.Infernum().ExtraAI[0] =
                    Main.player[npc.target].Hitbox.Intersects(npc.Infernum().arenaRectangle).ToInt();
                npc.TargetClosest(false);
            }

            npc.dontTakeDamage = NPC.CountNPCS(NPCID.MoonLordFreeEye) >= 3;

            // Life Ratio
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Start the AI
            if (npc.localAI[3] == 0f)
            {
                npc.netUpdate = true;

                DeleteMLArena();
                Player closest = Main.player[Player.FindClosest(npc.Center, 1, 1)];
                if (npc.Infernum().arenaRectangle == null)
                {
                    npc.Infernum().arenaRectangle = default;
                }
                Point closestTileCoords = closest.Center.ToTileCoordinates();
                const int width = 200;
                const int height = 150;
                npc.Infernum().arenaRectangle = new Rectangle((int)closest.position.X - width * 8, (int)closest.position.Y - height * 8 + 20, width * 16, height * 16);
                const int standSpaceX = 70;
                const int standHeight = 19;
                for (int i = closestTileCoords.X - width / 2; i <= closestTileCoords.X + width / 2; i++)
                {
                    for (int j = closestTileCoords.Y - height / 2; j <= closestTileCoords.Y + height / 2; j++)
                    {
                        int iClipped = i - closestTileCoords.X + width / 2;
                        int jClipped = j - closestTileCoords.Y + height / 2;
                        bool withinArenaStand = iClipped > standSpaceX && iClipped < width - standSpaceX &&
                                                jClipped > height - standHeight;
                        if ((Math.Abs(closestTileCoords.X - i) == width / 2 ||
                            Math.Abs(closestTileCoords.Y - j) == height / 2 ||
                                withinArenaStand)
                            && !Main.tile[i, j].active())
                        {
                            Main.tile[i, j].type = (ushort)ModContent.TileType<Tiles.MoonlordArena>();
                            Main.tile[i, j].active(true);
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
                npc.localAI[3] = 1f;
                npc.ai[0] = -1f;
            }

            // Death effects (Early)
            if (npc.ai[0] == -2f)
            {
                npc.ai[1] += 1f;
                if (npc.ai[1] == 30f)
                    Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, 92, 1f, 0f);

                if (npc.ai[1] < 60f)
                    MoonlordDeathDrama.RequestLight(npc.ai[1] / 30f, npc.Center);

                if (npc.ai[1] == 60f)
                {
                    npc.ai[1] = 0f;
                    npc.ai[0] = 0f;
                    if (Main.netMode != NetmodeID.MultiplayerClient && npc.type == NPCID.MoonLordCore)
                    {
                        npc.netUpdate = true;
                    }
                }
            }

            // Spawn head and hands
            if (npc.ai[0] == -1f)
            {
                npc.dontTakeDamage = true;

                npc.ai[1] += 1f;
                if (npc.ai[1] == 30f)
                    Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, 92, 1f, 0f);

                if (npc.ai[1] < 60f)
                    MoonlordDeathDrama.RequestLight(npc.ai[1] / 30f, npc.Center);

                if (npc.ai[1] == 60f)
                {
                    npc.ai[1] = 0f;
                    npc.ai[0] = 0f;

                    if (Main.netMode != NetmodeID.MultiplayerClient && npc.type == NPCID.MoonLordCore)
                    {
                        // Let it be known that some fool did this shit in the moon lord code
                        // npc.ai[2] = (float)Main.rand.Next(3);
                        // npc.ai[2] = 0f;
                        npc.ai[2] = 0f;

                        npc.netUpdate = true;
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
                        {
                            Main.npc[bodyPartIndices[i]].ai[3] = npc.whoAmI;
                        }
                        for (int i = 0; i < 3; i++)
                        {
                            npc.localAI[i] = bodyPartIndices[i];
                        }

                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            if (Main.npc[i].type == NPCID.MoonLordHand && Main.npc[i].active)
                            {
                                Main.npc[i].ai[0] = 0f;
                            }
                        }
                    }
                }
            }

            Vector2 idealDistance = Main.player[npc.target].Center - npc.Center + new Vector2(0f, 130f);

            // Fly near target, don't take damage
            if (npc.ai[0] == 0f)
            {
                npc.dontTakeDamage = true;
                npc.TargetClosest(false);

                if (idealDistance.Length() > 20f)
                {
                    float velocity = 9f;
                    if (Main.npc[(int)npc.localAI[2]].ai[0] == 1f)
                        velocity = 7f;

                    Vector2 desiredVelocity = Vector2.Normalize(idealDistance - npc.velocity) * velocity;
                    Vector2 oldVelocity = npc.velocity;
                    npc.SimpleFlyMovement(desiredVelocity, 0.5f);
                    npc.velocity = Vector2.Lerp(npc.velocity, oldVelocity, 0.5f);
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // Despawn if other parts aren't there
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

                    // Take damage if other parts are down
                    bool takeDamage = true;
                    if (Main.npc[(int)npc.localAI[0]].Calamity().newAI[0] != 1f)
                        takeDamage = false;
                    if (Main.npc[(int)npc.localAI[1]].Calamity().newAI[0] != 1f)
                        takeDamage = false;
                    if (Main.npc[(int)npc.localAI[2]].Calamity().newAI[0] != 1f)
                        takeDamage = false;

                    if (takeDamage)
                    {
                        npc.ai[0] = 1f;
                        npc.dontTakeDamage = false;
                        npc.netUpdate = true;
                    }
                }
            }

            // Fly near target, take damage
            else if (npc.ai[0] == 1f)
            {
                npc.dontTakeDamage = false;
                npc.TargetClosest(false);

                if (idealDistance.Length() > 20f)
                {
                    float velocity = 8f;
                    if (Main.npc[(int)npc.localAI[2]].ai[0] == 1f)
                        velocity = 6f;

                    Vector2 desiredVelocity2 = Vector2.Normalize(idealDistance - npc.velocity) * velocity;
                    Vector2 oldVelocity = npc.velocity;
                    npc.SimpleFlyMovement(desiredVelocity2, 0.5f);
                    npc.velocity = Vector2.Lerp(npc.velocity, oldVelocity, 0.5f);
                }
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
            }

            // Death effects
            else if (npc.ai[0] == 2f)
            {
                npc.dontTakeDamage = true;
                Vector2 idealVelocity = new Vector2(npc.direction, -0.5f);
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.98f);

                npc.ai[1] += 1f;
                if (npc.ai[1] < 60f)
                    MoonlordDeathDrama.RequestLight(npc.ai[1] / 60f, npc.Center);

                if (npc.ai[1] == 60f)
                {
                    for (int k = 0; k < Main.maxProjectiles; k++)
                    {
                        Projectile projectile = Main.projectile[k];
                        if (projectile.active && (projectile.type == ProjectileID.MoonLeech || projectile.type == ProjectileID.PhantasmalBolt ||
                            projectile.type == ProjectileID.PhantasmalDeathray || projectile.type == ProjectileID.PhantasmalEye ||
                            projectile.type == ProjectileID.PhantasmalSphere || projectile.type == ModContent.ProjectileType<PhantasmalBlast>() ||
                            projectile.type == ModContent.ProjectileType<PhantasmalSpark>()))
                            projectile.Kill();
                    }
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (Main.npc[i].type == NPCID.MoonLordFreeEye)
                        {
                            Main.npc[i].active = false;
                        }
                    }
                }

                if (npc.ai[1] % 3f == 0f && npc.ai[1] < 580f && npc.ai[1] > 60f)
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

                    float dustCounter = Main.rand.Next(6, 19);
                    float angularChange = MathHelper.TwoPi / dustCounter;
                    float rand2pi = MathHelper.TwoPi * Main.rand.NextFloat();
                    float velocityMult = 1f + Main.rand.NextFloat() * 2f;
                    float scale = 1f + Main.rand.NextFloat();
                    float fadeIn = 0.4f + Main.rand.NextFloat();
                    int dustType = Utils.SelectRandom(Main.rand, new int[]
                    {
                        31,
                        229
                    });
                    float ai1 = npc.ai[1];
                    if (canSpawnDust)
                    {
                        // MoonlordDeathDrama.AddExplosion(dustSpawnPos);
                        for (float i = 0f; i < dustCounter * 2f; i = ai1 + 1f)
                        {
                            Dust dust = Main.dust[Dust.NewDust(dustSpawnPos, 0, 0, 229, 0f, 0f, 0, default, 1f)];
                            dust.noGravity = true;
                            dust.position = dustSpawnPos;
                            dust.velocity = Vector2.UnitY.RotatedBy(rand2pi + angularChange * i) * velocityMult * (Main.rand.NextFloat() * 1.6f + 1.6f);
                            dust.fadeIn = fadeIn;
                            dust.scale = scale;
                            ai1 = i;
                        }
                    }

                    for (float i = 0f; i < npc.ai[1] / 60f; i = ai1 + 1f)
                    {
                        spawnAdditive = Utils.RandomVector2(Main.rand, -1f, 1f);
                        if (spawnAdditive != Vector2.Zero)
                            spawnAdditive.Normalize();

                        spawnAdditive *= 20f + Main.rand.NextFloat() * 800f;
                        dustSpawnPos = npc.Center + spawnAdditive;
                        dustSpawnTileCoords = dustSpawnPos.ToTileCoordinates();

                        bool canSpawndust = true;
                        if (!WorldGen.InWorld(dustSpawnTileCoords.X, dustSpawnTileCoords.Y, 0))
                            canSpawndust = false;
                        if (canSpawndust && WorldGen.SolidTile(dustSpawnTileCoords.X, dustSpawnTileCoords.Y))
                            canSpawndust = false;

                        if (canSpawndust)
                        {
                            Dust dust = Main.dust[Dust.NewDust(dustSpawnPos, 0, 0, dustType, 0f, 0f, 0, default, 1f)];
                            dust.noGravity = true;
                            dust.position = dustSpawnPos;
                            dust.velocity = -Vector2.UnitY * velocityMult * (Main.rand.NextFloat() * 0.9f + 1.6f);
                            dust.fadeIn = fadeIn;
                            dust.scale = scale;
                        }

                        ai1 = i;
                    }
                }

                if (npc.ai[1] % 15f == 0f && npc.ai[1] < 480f && npc.ai[1] >= 90f && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 spawnAdditive = Utils.RandomVector2(Main.rand, -1f, 1f);
                    if (spawnAdditive != Vector2.Zero)
                        spawnAdditive.Normalize();

                    spawnAdditive *= 20f + Main.rand.NextFloat() * 400f;
                    bool canSpawnDust = true;
                    Vector2 dustSpawnPos = npc.Center + spawnAdditive;
                    Point dustSpawnTileCoords = dustSpawnPos.ToTileCoordinates();

                    if (!WorldGen.InWorld(dustSpawnTileCoords.X, dustSpawnTileCoords.Y, 0))
                        canSpawnDust = false;
                    if (canSpawnDust && WorldGen.SolidTile(dustSpawnTileCoords.X, dustSpawnTileCoords.Y))
                        canSpawnDust = false;

                    if (canSpawnDust)
                    {
                        float smokeProjIndex = (Main.rand.Next(4) < 2).ToDirectionInt() * (MathHelper.Pi / 8f + (MathHelper.PiOver4 * Main.rand.NextFloat()));
                        Vector2 smokeVelocity = new Vector2(0f, -Main.rand.NextFloat() * 0.5f - 0.5f).RotatedBy(smokeProjIndex) * 6f;
                        Utilities.NewProjectileBetter(dustSpawnPos, smokeVelocity, ProjectileID.BlowupSmokeMoonlord, 0, 0f, Main.myPlayer, 0f, 0f);
                    }
                }

                if (npc.ai[1] == 1f)
                    Main.PlaySound(SoundID.NPCDeath61, npc.Center);

                if (npc.ai[1] >= 480f)
                    MoonlordDeathDrama.RequestLight((npc.ai[1] - 480f) / 120f, npc.Center);

                if (npc.ai[1] >= 600f)
                {
                    DeleteMLArena();
                    npc.life = 0;
                    npc.HitEffect(0, 1337.0);
                    npc.checkDead();

                    if (!BossRushEvent.BossRushActive)
                        typeof(CalamityGlobalAI).GetMethod("MoonLordLoot", Utilities.UniversalBindingFlags).Invoke(null, new object[]
                            {
                                npc
                            });

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

                    npc.active = false;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI, 0f, 0f, 0f, 0, 0, 0);

                    return false;
                }
            }

            // Despawn effects
            else if (npc.ai[0] == 3f)
            {
                npc.dontTakeDamage = true;
                Vector2 idealVelocity = new Vector2(npc.direction, -0.5f);
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.98f);

                npc.ai[1] += 1f;
                if (npc.ai[1] < 60f)
                    MoonlordDeathDrama.RequestLight(npc.ai[1] / 40f, npc.Center);

                if (npc.ai[1] == 40f)
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

                if (npc.ai[1] >= 60f)
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

                    return false;
                }
            }

            // Waves of seals
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
            }
            if (NPC.AnyNPCs(ModContent.NPCType<EldritchSeal>()))
            {
                npc.dontTakeDamage = true;
            }

            // Despawn
            bool despawn = false;
            if (npc.ai[0] == -2f || npc.ai[0] == -1f || npc.ai[0] == -2f || npc.ai[0] == 3f)
                despawn = true;
            if (Main.player[npc.target].active && !Main.player[npc.target].dead)
                despawn = true;

            // If unsure on despawning, check
            if (!despawn)
            {
                for (int playerIdx = 0; playerIdx < 255; playerIdx++)
                {
                    if (Main.player[playerIdx].active && !Main.player[playerIdx].dead)
                    {
                        despawn = true;
                        break;
                    }
                }
            }
            if (!despawn)
            {
                npc.ai[0] = 3f;
                npc.ai[1] = 0f;
                npc.netUpdate = true;
            }

            // Teleport
            if (npc.ai[0] >= 0f && npc.ai[0] < 2f && Main.netMode != NetmodeID.MultiplayerClient && npc.Distance(Main.player[npc.target].Center) > 2300f)
            {
                npc.ai[0] = -2f;
                npc.netUpdate = true;
                Vector2 teleportDelta = Main.player[npc.target].Center - Vector2.UnitY * 150f - npc.Center;
                npc.position += teleportDelta;

                if (Main.npc[(int)npc.localAI[0]].active)
                {
                    NPC nPC6 = Main.npc[(int)npc.localAI[0]];
                    nPC6.position += teleportDelta;
                    Main.npc[(int)npc.localAI[0]].netUpdate = true;
                }
                if (Main.npc[(int)npc.localAI[1]].active)
                {
                    NPC nPC6 = Main.npc[(int)npc.localAI[1]];
                    nPC6.position += teleportDelta;
                    Main.npc[(int)npc.localAI[1]].netUpdate = true;
                }
                if (Main.npc[(int)npc.localAI[2]].active)
                {
                    NPC nPC6 = Main.npc[(int)npc.localAI[2]];
                    nPC6.position += teleportDelta;
                    Main.npc[(int)npc.localAI[2]].netUpdate = true;
                }

                AffectAllEyes(eye => eye.Center = npc.Center);
            }
            return false;
        }

        /// <summary>
        /// Causes a section of code to affect all true eyes.
        /// </summary>
        /// <param name="toExecute">The code to execute among all eyes</param>
        public static void AffectAllEyes(Action<NPC> toExecute)
        {
            foreach (var eye in MoonLordHandBehaviorOverride.GetTrueEyes)
                toExecute(eye);
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
