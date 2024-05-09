using System;
using CalamityMod;
using CalamityMod.NPCs.Abyss;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.NPCs.NormalNPCs;
using CalamityMod.NPCs.SulphurousSea;
using CalamityMod.NPCs.SunkenSea;
using CalamityMod.Projectiles.Enemy;
using CalamityMod.World;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Ares;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode
{
    public static partial class Utilities
    {
        public const float DefaultTargetRedecideThreshold = 4000f;

        public static void TargetClosestIfTargetIsInvalid(this NPC npc, float distanceThreshold = DefaultTargetRedecideThreshold)
        {
            bool invalidTargetIndex = npc.target is < 0 or >= 255;
            if (invalidTargetIndex)
            {
                npc.TargetClosest();
                return;
            }

            Player target = Main.player[npc.target];
            bool invalidTarget = target.dead || !target.active || target.Infernum().GetValue<int>("EelSwallowIndex") >= 0;
            if (invalidTarget)
                npc.TargetClosest();

            if (distanceThreshold >= 0f && !npc.WithinRange(target.Center, distanceThreshold - target.aggro))
                npc.TargetClosest();
        }


        public static NPC CurrentlyFoughtBoss
        {
            get
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].IsABoss())
                        return Main.npc[i].realLife >= 0 ? Main.npc[Main.npc[i].realLife] : Main.npc[i];
                }
                return null;
            }
        }

        public static bool IsExoMech(NPC npc)
        {
            // Thanatos.
            if (npc.type == ModContent.NPCType<ThanatosHead>() ||
                npc.type == ModContent.NPCType<ThanatosBody1>() ||
                npc.type == ModContent.NPCType<ThanatosBody2>() ||
                npc.type == ModContent.NPCType<ThanatosTail>())
            {
                return true;
            }

            // Ares.
            if (npc.type == ModContent.NPCType<AresBody>() ||
                npc.type == ModContent.NPCType<AresLaserCannon>() ||
                npc.type == ModContent.NPCType<AresTeslaCannon>() ||
                npc.type == ModContent.NPCType<AresPlasmaFlamethrower>() ||
                npc.type == ModContent.NPCType<AresGaussNuke>() ||
                npc.type == ModContent.NPCType<AresPulseCannon>() ||
                npc.type == ModContent.NPCType<AresEnergyKatana>())
            {
                return true;
            }

            // Artemis and Apollo.
            if (npc.type == ModContent.NPCType<Artemis>() ||
                npc.type == ModContent.NPCType<Apollo>())
            {
                return true;
            }

            return false;
        }

        public static string GetNPCNameFromID(int id)
        {
            if (id < NPCID.Count)
                return id.ToString();

            return NPCLoader.GetNPC(id).FullName;
        }

        public static string GetNPCFullNameFromID(int id)
        {
            if (id < NPCID.Count)
                return NPC.GetFullnameByID(id);

            return NPCLoader.GetNPC(id).DisplayName.Value;
        }

        public static int GetNPCIDFromName(string name)
        {
            if (int.TryParse(name, out int id))
                return id;

            string[] splitName = name.Split('/');
            if (ModContent.TryFind(splitName[0], splitName[1], out ModNPC modNpc))
                return modNpc.Type;

            return NPCID.None;
        }

        public static T BehaviorOverride<T>(this NPC npc) where T : NPCBehaviorOverride
        {
            var container = NPCBehaviorOverride.BehaviorOverrideSet[npc.type];
            if (container is not null && container.BehaviorOverride is T t)
                return t;

            return null;
        }

        public static void PassiveSwimmingAI(this NPC npc, int passiveness, float detectRange, float xSpeed, float ySpeed, float speedLimitX, float speedLimitY, float rotation, bool spriteFacesLeft = true)
        {
            if (spriteFacesLeft)
                npc.spriteDirection = (npc.direction > 0) ? 1 : -1;
            else
                npc.spriteDirection = (npc.direction > 0) ? -1 : 1;

            npc.noGravity = true;
            if (npc.direction == 0)
            {
                npc.TargetClosest(true);
            }

            if (npc.justHit && passiveness != 3)
            {
                npc.chaseable = true;
            }
            if (npc.wet)
            {
                bool hasWetTarget = npc.chaseable;
                npc.TargetClosest(false);
                Player target = Main.player[npc.target];
                // Player detection behavior
                if (passiveness != 2)
                {
                    if (npc.type == ModContent.NPCType<Frogfish>())
                    {
                        if (target.wet && !target.dead)
                        {
                            hasWetTarget = true;
                            npc.chaseable = true; //once the enemy has detected the player, let minions fuck it up
                        }
                    }
                    if (npc.type == ModContent.NPCType<Sulflounder>())
                    {
                        if (!target.dead)
                        {
                            hasWetTarget = true;
                            npc.chaseable = true; //once the enemy has detected the player, let minions fuck it up
                        }
                    }
                    else if (target.wet && !target.dead && (target.Center - npc.Center).Length() < detectRange &&
                        Collision.CanHit(npc.position, npc.width, npc.height, target.position, target.width, target.height))
                    {
                        hasWetTarget = true;
                        npc.chaseable = true; //once the enemy has detected the player, let minions fuck it up
                    }
                    else
                    {
                        if (passiveness == 1)
                        {
                            hasWetTarget = false;
                        }
                    }
                }
                if ((target.dead || !Collision.CanHit(npc.position, npc.width, npc.height, target.position, target.width, target.height)) && hasWetTarget)
                {
                    hasWetTarget = false;
                }

                // Swim back and forth
                if (!hasWetTarget || passiveness == 2)
                {
                    if (passiveness == 0)
                        npc.TargetClosest(true);
                    if (npc.collideX)
                    {
                        npc.velocity.X *= -1f;
                        npc.direction *= -1;
                        npc.netUpdate = true;
                    }
                    if (npc.collideY)
                    {
                        npc.netUpdate = true;
                        if (npc.velocity.Y > 0f)
                        {
                            npc.velocity.Y = Math.Abs(npc.velocity.Y) * -1f;
                            npc.directionY = -1;
                            npc.ai[0] = -1f;
                        }
                        else if (npc.velocity.Y < 0f)
                        {
                            npc.velocity.Y = Math.Abs(npc.velocity.Y);
                            npc.directionY = 1;
                            npc.ai[0] = 1f;
                        }
                    }
                }

                if (hasWetTarget && passiveness != 2)
                {
                    npc.TargetClosest(true);
                    target = Main.player[npc.target];
                    // Swim away from the player
                    if (passiveness == 3)
                    {
                        npc.velocity.X -= npc.direction * xSpeed;
                        npc.velocity.Y -= npc.directionY * ySpeed;
                    }
                    // Swim toward the player
                    else
                    {
                        npc.velocity.X += npc.direction * (CalamityWorld.death ? 2f * xSpeed : CalamityWorld.revenge ? 1.5f * xSpeed : xSpeed);
                        npc.velocity.Y += npc.directionY * (CalamityWorld.death ? 2f * ySpeed : CalamityWorld.revenge ? 1.5f * ySpeed : ySpeed);
                    }
                    float velocityCapX = CalamityWorld.death && passiveness != 3 ? 2f * speedLimitX : CalamityWorld.revenge ? 1.5f * speedLimitX : speedLimitX;
                    float velocityCapY = CalamityWorld.death && passiveness != 3 ? 2f * speedLimitY : CalamityWorld.revenge ? 1.5f * speedLimitY : speedLimitY;
                    npc.velocity.X = MathHelper.Clamp(npc.velocity.X, -velocityCapX, velocityCapX);
                    npc.velocity.Y = Clamp(npc.velocity.Y, -velocityCapY, velocityCapY);

                    if (npc.justHit)
                        npc.localAI[0] = 0f;

                    // Laserfish shoot the player
                    if (npc.type == ModContent.NPCType<Laserfish>())
                    {
                        npc.localAI[0] += (CalamityWorld.death ? 2f : CalamityWorld.revenge ? 1.5f : 1f);
                        if (Main.netMode != NetmodeID.MultiplayerClient && npc.localAI[0] >= 120f)
                        {
                            npc.localAI[0] = 0f;
                            if (Collision.CanHit(npc.position, npc.width, npc.height, target.position, target.width, target.height))
                            {
                                float speed = 5f;
                                Vector2 vector = new(npc.position.X + npc.width * 0.5f, npc.position.Y + npc.height / 2);
                                float velX = target.Center.X - vector.X + Main.rand.NextFloat(-20f, 20f);
                                float velY = target.Center.Y - vector.Y + Main.rand.NextFloat(-20f, 20f);
                                float dist = (float)Math.Sqrt((double)(velX * velX + velY * velY));
                                dist = speed / dist;
                                velX *= dist;
                                velY *= dist;
                                int damage = Main.expertMode ? 40 : 50;
                                int beam = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center.X + (npc.spriteDirection == 1 ? 25f : -25f), npc.Center.Y + (target.position.Y > npc.Center.Y ? 5f : -5f), velX, velY, ProjectileID.EyeBeam, damage, 0f, Main.myPlayer);
                                Main.projectile[beam].tileCollide = true;
                            }
                        }
                    }

                    // Flounder shoot Sulphuric Mist at the player
                    if (npc.type == ModContent.NPCType<Sulflounder>())
                    {
                        if ((target.Center - npc.Center).Length() < 350f)
                        {
                            npc.localAI[0] += (CalamityWorld.death ? 3f : CalamityWorld.revenge ? 2f : 1f);
                            if (Main.netMode != NetmodeID.MultiplayerClient && npc.localAI[0] >= 180f)
                            {
                                npc.localAI[0] = 0f;
                                if (Collision.CanHit(npc.position, npc.width, npc.height, target.position, target.width, target.height))
                                {
                                    float speed = 4f;
                                    Vector2 vector = new(npc.position.X + npc.width * 0.5f, npc.position.Y + npc.height / 2);
                                    float velX = target.Center.X - vector.X + Main.rand.NextFloat(-20f, 20f);
                                    float velY = target.Center.Y - vector.Y + Main.rand.NextFloat(-20f, 20f);
                                    float dist = (float)Math.Sqrt((double)(velX * velX + velY * velY));
                                    dist = speed / dist;
                                    velX *= dist;
                                    velY *= dist;
                                    int damage = Main.expertMode ? 25 : 35;
                                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center.X + (npc.spriteDirection == 1 ? 10f : -10f), npc.Center.Y, velX, velY, ModContent.ProjectileType<SulphuricAcidMist>(), damage, 0f, Main.myPlayer);
                                }
                            }
                        }
                    }

                    // Sea Minnows face away from the player
                    if (npc.type == ModContent.NPCType<SeaMinnow>())
                    {
                        npc.direction *= -1;
                    }
                }
                else
                {
                    // No target behavior
                    npc.velocity.X += npc.direction * 0.1f;
                    if (npc.velocity.X < -2.5f || npc.velocity.X > 2.5f)
                    {
                        npc.velocity.X *= 0.95f;
                    }
                    if (npc.ai[0] == -1f)
                    {
                        npc.velocity.Y -= 0.01f;
                        if (npc.velocity.Y < -0.3f)
                        {
                            npc.ai[0] = 1f;
                        }
                    }
                    else
                    {
                        npc.velocity.Y += 0.01f;
                        if (npc.velocity.Y > 0.3f)
                        {
                            npc.ai[0] = -1f;
                        }
                    }
                }
                int npcTileX = (int)(npc.position.X + npc.width / 2) / 16;
                int npcTileY = (int)(npc.position.Y + npc.height / 2) / 16;
                if (Main.tile[npcTileX, npcTileY - 1].LiquidAmount > 128)
                {
                    if (Main.tile[npcTileX, npcTileY + 1].HasTile)
                    {
                        npc.ai[0] = -1f;
                    }
                    else if (Main.tile[npcTileX, npcTileY + 2].HasTile)
                    {
                        npc.ai[0] = -1f;
                    }
                }
                if (npc.velocity.Y > 0.4f || npc.velocity.Y < -0.4f)
                {
                    npc.velocity.Y *= 0.95f;
                }
            }
            else
            {
                // Out of water behavior
                if (npc.velocity.Y == 0f)
                {
                    npc.velocity.X *= 0.94f;
                    if (npc.velocity.X > -0.2f && npc.velocity.X < 0.2f)
                    {
                        npc.velocity.X = 0f;
                    }
                }
                npc.velocity.Y += 0.4f;
                if (npc.velocity.Y > 12f)
                {
                    npc.velocity.Y = 12f;
                }
                npc.ai[0] = 1f;
            }
            npc.rotation = npc.velocity.Y * npc.direction * rotation;
            float rotationLimit = 2f * rotation;
            npc.rotation = MathHelper.Clamp(npc.rotation, -rotationLimit, rotationLimit);
        }
    }
}
