using CalamityMod.NPCs;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.World.Generation;

namespace InfernumMode.FuckYouModeAIs.HiveMind
{
	public class HiveMindAIClass
    {

        internal const float HiveMindFadeoutTime = 25f;

        [OverrideAppliesTo("HiveMind", typeof(HiveMindAIClass), "HiveMindAI", EntityOverrideContext.NPCAI)]
        public static bool HiveMindAI(NPC npc)
        {
            // npc.Infernum().ExtraAI[0] = Countdown variable (for teleporting, shooting blobs, ect.)
            npc.TargetClosest(true);
            Player player = Main.player[npc.target];
            float lifeRatio = npc.life / (float)npc.lifeMax;
            if (!player.active || player.dead)
            {
                if (npc.timeLeft > 60)
                    npc.timeLeft = 60;
                if (npc.localAI[3] < 120f)
                {
                    float[] aidistances = npc.localAI;
                    int number = 3;
                    float num244 = aidistances[number];
                    aidistances[number] = num244 + 1f;
                }
                if (npc.localAI[3] > 60f)
                {
                    npc.velocity.Y += (npc.localAI[3] - 60f) * 0.5f;
                    npc.noGravity = true;
                    npc.noTileCollide = true;
                    if (npc.Infernum().ExtraAI[0] > 30)
                        npc.Infernum().ExtraAI[0] = 30;
                }
                return false;
            }
            if (npc.localAI[3] > 0f)
            {
                npc.localAI[3] -= 1f;
                return false;
            }
            npc.noGravity = false;
            npc.noTileCollide = false;
            CalamityGlobalNPC.hiveMind = npc.whoAmI;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                npc.localAI[1] += 1f;
                if (npc.localAI[1] >= 600f)
                {
                    npc.localAI[1] = 0f;
                    NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, InfernumMode.CalamityMod.NPCType("HiveBlob"), npc.whoAmI, 0f, 0f, 0f, 0f, 255);
                }
                if (npc.localAI[0] == 0f)
                {
                    npc.localAI[0] = 1f;
                    for (int num723 = 0; num723 < 5; num723++)
                    {
                        NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, InfernumMode.CalamityMod.NPCType("HiveBlob"), npc.whoAmI, 0f, 0f, 0f, 0f, 255);
                    }
                }
            }
            bool flag100 = false;
            int num568 = 0;
            for (int num569 = 0; num569 < Main.maxNPCs; num569++)
            {
                if (Main.npc[num569].active && Main.npc[num569].type == InfernumMode.CalamityMod.NPCType("DankCreeper"))
                {
                    flag100 = true;
                    num568++;
                }
            }
            npc.defense += num568 * 25;
            if (!flag100)
            {
                npc.defense = 10;
            }
            if (npc.ai[3] == 0f && npc.life > 0)
            {
                npc.ai[3] = npc.lifeMax;
            }
            if (npc.life > 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int num660 = (int)(npc.lifeMax * 0.25);
                    if (npc.life + num660 < npc.ai[3])
                    {
                        npc.ai[3] = npc.life;
                        int num661 = Main.rand.Next(3, 6);
                        for (int num662 = 0; num662 < num661; num662++)
                        {
                            int x = (int)(npc.position.X + Main.rand.Next(npc.width - 32));
                            int y = (int)(npc.position.Y + Main.rand.Next(npc.height - 32));
                            int num663 = InfernumMode.CalamityMod.NPCType("HiveBlob");
                            if (Main.rand.NextBool(3))
                            {
                                num663 = InfernumMode.CalamityMod.NPCType("DankCreeper");
                            }
                            int num664 = NPC.NewNPC(x, y, num663, 0, 0f, 0f, 0f, 0f, 255);
                            Main.npc[num664].SetDefaults(num663, -1f);
                            Main.npc[num664].velocity.X = Main.rand.Next(-15, 16) * 0.1f;
                            Main.npc[num664].velocity.Y = Main.rand.Next(-30, 1) * 0.1f;
                            if (Main.netMode == NetmodeID.Server && num664 < Main.maxNPCs)
                            {
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, num664, 0f, 0f, 0f, 0, 0, 0);
                            }
                        }
                        return false;
                    }
                }
            }
            npc.Infernum().ExtraAI[0]--;
            float spitFrames = 260f;
            float spawnMirageFrames = 300f;
            if (lifeRatio < 0.25f)
            {
                spitFrames = 240f;
            }
            if (lifeRatio < 0.5f)
            {
                spawnMirageFrames = 180f;
            }
            if (npc.Infernum().ExtraAI[0] > 100 &&
                npc.Infernum().ExtraAI[0] % spitFrames == 0f)
            {
                for (int i = 0; i < Main.rand.Next(4, 6); i++)
                {
                    Projectile.NewProjectile(npc.Center, npc.DirectionTo(player.Center).RotatedByRandom(MathHelper.ToRadians(15f)) * 8f, InfernumMode.CalamityMod.ProjectileType("VileClot"), 16, 1f);
                }
            }
            if (npc.Infernum().ExtraAI[0] > 50 &&
                npc.Infernum().ExtraAI[0] % spawnMirageFrames == 0f &&
                lifeRatio < 0.75f)
            {
                Projectile.NewProjectile(player.Center - new Vector2(0f, 2300f), Vector2.Zero, ModContent.ProjectileType<HiveMindMirage>(), 20, 3f, player.whoAmI, 1f);
            }
            if (npc.Infernum().ExtraAI[0] < -120)
            {
                npc.Infernum().ExtraAI[0] = 600;
                npc.scale = 1f;
                npc.alpha = 0;
                npc.dontTakeDamage = false;
                npc.damage = npc.defDamage;
            }
            else if (npc.Infernum().ExtraAI[0] < -60)
            {
                npc.scale += 0.0165f;
                npc.alpha -= 4;
                int num622 = Dust.NewDust(new Vector2(npc.position.X, npc.Center.Y), npc.width, npc.height / 2, 14, 0f, -3f, 100, default, 2.5f * npc.scale);
                Main.dust[num622].velocity *= 2f;
                if (Main.rand.NextBool(2))
                {
                    Main.dust[num622].scale = 0.5f;
                    Main.dust[num622].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
                for (int i = 0; i < 2; i++)
                {
                    int num624 = Dust.NewDust(new Vector2(npc.position.X, npc.Center.Y), npc.width, npc.height / 2, 14, 0f, -3f, 100, default, 3.5f * npc.scale);
                    Main.dust[num624].noGravity = true;
                    Main.dust[num624].velocity *= 3.5f;
                    num624 = Dust.NewDust(new Vector2(npc.position.X, npc.Center.Y), npc.width, npc.height / 2, 14, 0f, -3f, 100, default, 2.5f * npc.scale);
                    Main.dust[num624].velocity *= 1f;
                }
            }
            else if (npc.Infernum().ExtraAI[0] == -60)
            {
                npc.scale = 0.01f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.Center = player.Center;
                    npc.position.Y = player.position.Y - npc.height;
                    int tilePosX = (int)npc.Center.X / 16;
                    int tilePosY = (int)(npc.position.Y + npc.height) / 16 + 1;
                    if (Main.tile[tilePosX, tilePosY] == null)
                        Main.tile[tilePosX, tilePosY] = new Tile();
                    while (!(Main.tile[tilePosX, tilePosY].nactive() && Main.tileSolid[Main.tile[tilePosX, tilePosY].type]))
                    {
                        tilePosY++;
                        npc.position.Y += 16;
                        if (Main.tile[tilePosX, tilePosY] == null)
                            Main.tile[tilePosX, tilePosY] = new Tile();
                    }
                }
                npc.netUpdate = true;
            }
            else if (npc.Infernum().ExtraAI[0] < 0)
            {
                npc.scale -= 0.0165f;
                npc.alpha += 4;
                int num622 = Dust.NewDust(new Vector2(npc.position.X, npc.Center.Y), npc.width, npc.height / 2, 14, 0f, -3f, 100, default, 2.5f * npc.scale);
                Main.dust[num622].velocity *= 2f;
                if (Main.rand.NextBool(2))
                {
                    Main.dust[num622].scale = 0.5f;
                    Main.dust[num622].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
                for (int i = 0; i < 2; i++)
                {
                    int num624 = Dust.NewDust(new Vector2(npc.position.X, npc.Center.Y), npc.width, npc.height / 2, 14, 0f, -3f, 100, default, 3.5f * npc.scale);
                    Main.dust[num624].noGravity = true;
                    Main.dust[num624].velocity *= 3.5f;
                    num624 = Dust.NewDust(new Vector2(npc.position.X, npc.Center.Y), npc.width, npc.height / 2, 14, 0f, -3f, 100, default, 2.5f * npc.scale);
                    Main.dust[num624].velocity *= 1f;
                }
            }
            else if (npc.Infernum().ExtraAI[0] == 0)
            {
                if (!player.active || player.dead)
                {
                    npc.Infernum().ExtraAI[0] = 30;
                }
                else
                {
                    npc.dontTakeDamage = true;
                    npc.damage = 0;
                }
            }
            return false;
        }

        [OverrideAppliesTo("HiveMindP2", typeof(HiveMindAIClass), "HiveMindP2AI", EntityOverrideContext.NPCAI)]
        public static bool HiveMindP2AI(NPC npc)
        {
            //npc.Infernum().angleTarget = player Center
            //npc.Infernum().ExtraAI[0] = current AI state
            //npc.Infernum().ExtraAI[1-3] = varies by AI state
            //npc.Infernum().ExtraAI[4] = slowdown time
            //npc.Infernum().ExtraAI[5] = new AI state
            //npc.Infernum().ExtraAI[6] = fade out timer
            //npc.Infernum().ExtraAI[7] = fade out incrementer
            //npc.Infernum().ExtraAI[8] = fade out incrementer
            //npc.Infernum().ExtraAI[9] = old AI when used in the suspension state
            //npc.Infernum().ExtraAI[10] = <20% invincibility flag
            //npc.Infernum().ExtraAI[11] = <20% invincibility time
            //npc.Infernum().ExtraAI[12] = fire column shoot countdown
            const int spinRadius = 300;
            const int dashDistance = 300;
            const int slowdownTime = 60;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool below20 = lifeRatio < 0.2f || npc.Infernum().ExtraAI[10] == 1f;
            Player player = Main.player[npc.target];
            npc.defense = player.ZoneCorrupt ? 5 : 9999;
            CalamityGlobalNPC.hiveMind = npc.whoAmI;
            if (npc.Infernum().angleTarget == null)
            {
                npc.Infernum().angleTarget = default;
            }
            if (below20 && npc.Infernum().ExtraAI[10] == 0f)
            {
                npc.Infernum().ExtraAI[10] = 1f;
                npc.Infernum().ExtraAI[11] = 300f;
            }
            if (npc.Infernum().ExtraAI[11] == 60f)
            {
                npc.velocity = Vector2.UnitY * -12f;
            }
            if (below20)
            {
                player.GetModPlayer<CalamityMod.CalPlayer.CalamityPlayer>().rage =
                player.GetModPlayer<CalamityMod.CalPlayer.CalamityPlayer>().adrenaline = 0;
            }
            if (npc.Infernum().ExtraAI[6] > 0f)
            {
                npc.Infernum().ExtraAI[6] -= 1f;
            }
            if (npc.Infernum().ExtraAI[11] > 0f)
            {
                if (npc.Infernum().ExtraAI[11] % 20 == 0f)
                {
                    npc.ai[1] = 1f;
                }
                npc.life = (int)MathHelper.Lerp(npc.lifeMax * 0.2f, npc.lifeMax * 0.5f, 1f - npc.Infernum().ExtraAI[11] / 300f);
                npc.Infernum().ExtraAI[11] -= 1f;
                npc.velocity *= 0.94f;
                npc.defense = 9999;
                npc.Infernum().ExtraAI[7] += 0.24f;
                return false;
            }

            if (npc.Infernum().ExtraAI[12] > 0f)
            {
                if (npc.Infernum().ExtraAI[12] % 60f == 30f - 1f)
                {
                    WorldUtils.Find((player.Top - Vector2.UnitY * 320f).ToTileCoordinates(), Searches.Chain(new Searches.Down(200), new Conditions.IsSolid()), out Point result);
                    if (Math.Abs(result.X) > 10000)
                        result = (player.Bottom + Vector2.UnitY * 120f).ToTileCoordinates();
                    Utilities.NewProjectileBetter(result.ToWorldCoordinates(), Vector2.Zero, ModContent.ProjectileType<ShadeFireColumn>(), 50, 0f);
                }
                npc.Infernum().ExtraAI[12]--;
            }

            npc.defense = below20 ? 12 : 7;
            // Suspension state drift
            if (npc.Infernum().ExtraAI[0] == -1f)
            {
                npc.ai[0] += 1f;
                float driftTime = lifeRatio < 0.2 ? 30f : 90f;
                if (npc.alpha > 0)
                {
                    npc.alpha -= 9;
                    if (npc.alpha <= 0f)
                    {
                        npc.alpha = 0;
                    }
                }
                if (npc.ai[0] == 1f)
                {
                    npc.velocity = npc.DirectionTo(player.Center) * 4f;
                }
                if (npc.knockBackResist == 0f)
                {
                    npc.knockBackResist = 1f;
                }
                if (npc.justHit && npc.ai[0] < driftTime - 15f)
                {
                    npc.ai[0] = driftTime - 15f;
                    npc.ai[2] = 1f;
                }
                if (npc.ai[0] > driftTime)
                {
                    npc.knockBackResist = 0f;
                    if (npc.Infernum().ExtraAI[5] == 3f ||
                        npc.Infernum().ExtraAI[5] == 4f ||
                        npc.Infernum().ExtraAI[5] == 6f)
                    {
                        if (npc.Infernum().ExtraAI[5] == 6f)
                        {
                            npc.Center = player.Center - Vector2.UnitY * 350f;
                        }
                        npc.alpha = 255;
                    }
                    npc.Infernum().ExtraAI[0] = npc.Infernum().ExtraAI[5];
                }
            }

            // Reset AI
            if (npc.Infernum().ExtraAI[0] == 0f)
            {
                npc.TargetClosest(false);
                float oldAI = npc.Infernum().ExtraAI[9];
                bool initialCheck = false;
                while (npc.Infernum().ExtraAI[5] == oldAI || !initialCheck)
                {
                    npc.Infernum().ExtraAI[5] = Main.rand.Next(1, 4);
                    if ((npc.Infernum().ExtraAI[5] == 1f && lifeRatio < 0.8f) ||
                        (npc.Infernum().ExtraAI[5] == 2f && lifeRatio < 0.6f) ||
                        (npc.Infernum().ExtraAI[5] == 3f && lifeRatio < 0.4f))
                    {
                        // Shift to the special/new attacks
                        npc.Infernum().ExtraAI[5] += 3f;
                    }
                    initialCheck = true;
                }

                if (below20 && Main.rand.NextBool(4) && oldAI != 8f)
                {
                    npc.Infernum().ExtraAI[5] = 8f;
                }

                npc.ai = new float[] { 0f, 0f, 0f, 0f };
                npc.Infernum().ExtraAI[1] =
                    npc.Infernum().ExtraAI[2] =
                    npc.Infernum().ExtraAI[3] = 0f;
                npc.Infernum().ExtraAI[0] = -1f;
            }
            // Spawn npcs
            if (npc.Infernum().ExtraAI[0] == 1f)
            {
                npc.Infernum().ExtraAI[9] = 1f;
                const float spinTime = 45f;
                const float initialVelocity = MathHelper.Pi / spinTime;
                npc.ai[0] += 1f;
                if (npc.alpha >= 0 && npc.ai[3] == 0f)
                {
                    npc.alpha -= 7;
                    npc.Center = player.Center + Vector2.UnitY * spinRadius;
                    npc.velocity = Vector2.Zero;
                    if (npc.alpha <= 0f)
                    {
                        // Weird roar
                        npc.ai[2] = 1f;
                        npc.Infernum().ExtraAI[6] = HiveMindFadeoutTime;
                        npc.Infernum().ExtraAI[1] = Main.rand.NextBool(2).ToDirectionInt();
                        npc.velocity = Vector2.UnitX * MathHelper.Pi * spinRadius / spinTime
                            * npc.Infernum().ExtraAI[1];
                        npc.alpha = 0;
                        npc.ai[3] = 1f;
                    }
                }
                // Spin
                if (npc.ai[3] == 1f)
                {
                    npc.velocity = npc.velocity.RotatedBy(initialVelocity * -npc.Infernum().ExtraAI[1]);
                    if (npc.ai[0] % (int)Math.Ceiling(spinTime / 6) == (int)Math.Ceiling(spinTime / 6) - 1)
                    {
                        npc.Infernum().ExtraAI[2]++;
                        if (Main.netMode != NetmodeID.MultiplayerClient && Collision.CanHit(npc.Center, 1, 1, player.position, player.width, player.height)) // draw line of sight
                        {
                            if (npc.Infernum().ExtraAI[2] == 2 || npc.Infernum().ExtraAI[2] == 4)
                            {
                                if (!NPC.AnyNPCs(InfernumMode.CalamityMod.NPCType("DarkHeart")))
                                {
                                    NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, InfernumMode.CalamityMod.NPCType("DarkHeart"));
                                }
                            }
                            else if (NPC.CountNPCS(NPCID.EaterofSouls) < 2)
                            {
                                NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.EaterofSouls);
                            }
                        }
                        if (npc.Infernum().ExtraAI[2] >= 6)
                        {
                            // Reset
                            npc.Infernum().ExtraAI[4] = slowdownTime;
                            npc.Infernum().ExtraAI[0] = 7f;
                        }
                    }
                }
            }
            // Spin around player and lunge inward
            if (npc.Infernum().ExtraAI[0] == 2f)
            {
                npc.Infernum().ExtraAI[9] = 2f;
                const float spinTime = 130f;
                const float lungeTime = 20f;
                const float waitTime = 20f;
                if (npc.ai[0] == 0f)
                {
                    npc.velocity = Vector2.Zero;
                    npc.alpha = 255;
                    npc.Infernum().ExtraAI[6] = HiveMindFadeoutTime;
                    npc.Center = player.Center + new Vector2(0f, spinRadius);
                    npc.ai[0] = 1f;
                }
                if (npc.alpha > 0)
                {
                    npc.alpha -= 7;
                    if (npc.alpha <= 0f)
                    {
                        npc.alpha = 0;
                    }
                }
                npc.ai[3] += 1f;
                npc.Infernum().ExtraAI[3] += npc.ai[3] > spinTime ? 0.97f / (npc.ai[3] - spinTime + 2f) : 1f;
                while (npc.Infernum().ExtraAI[1] == 0f)
                {
                    npc.Infernum().ExtraAI[1] = Main.rand.NextBool(2).ToDirectionInt();
                }
                if (npc.ai[3] == spinTime + waitTime)
                {
                    npc.velocity = npc.DirectionTo(player.Center) * spinRadius / slowdownTime * 4f;
                    npc.Infernum().ExtraAI[6] = HiveMindFadeoutTime;
                    // Roar
                    npc.ai[1] = 1f;
                }
                else if (npc.ai[3] < spinTime + waitTime)
                {
                    float angle = 0.0739198272f * npc.Infernum().ExtraAI[3] * 1.5f * npc.Infernum().ExtraAI[1];
                    npc.velocity = Vector2.Zero;
                    npc.Center = player.Center + new Vector2(0f, spinRadius).RotatedBy(angle);
                }
                if (npc.ai[3] > spinTime + lungeTime + waitTime * 0.44f)
                {
                    npc.Infernum().ExtraAI[4] = slowdownTime;
                    npc.Infernum().ExtraAI[0] = 7f;
                }
            }
            // Cloud dash
            if (npc.Infernum().ExtraAI[0] == 3f)
            {
                if (lifeRatio < 0.4f || below20)
				{
                    npc.Infernum().ExtraAI[0] = 6f;
                    return false;
                }
                npc.Infernum().ExtraAI[9] = 3f;
                if (npc.ai[0] == 0f)
                {
                    npc.alpha = 255;

                    npc.ai[0] = 1f;
                }
                npc.ai[3] += 1f;
                if (npc.alpha > 0)
                {
                    npc.alpha -= 5;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        npc.Center = player.Center;
                        while (npc.Infernum().ExtraAI[78] == 0f)
                        {
                            npc.Infernum().ExtraAI[78] = Main.rand.NextBool(2).ToDirectionInt();
                        }
                        npc.position.Y -= dashDistance;
                        npc.position.X += dashDistance * npc.Infernum().ExtraAI[78];
                    }
                    if (npc.alpha <= 0)
                    {
                        npc.velocity = Vector2.UnitX * -11f * npc.Infernum().ExtraAI[78];
                        npc.ai[2] = 1f;
                    }
                    npc.netUpdate = true;
                }
                else if (npc.ai[3] % 2 == 0f)
                {
                    const int damage = 16;
                    Projectile.NewProjectile(npc.position.X + Main.rand.Next(npc.width), npc.position.Y + Main.rand.Next(npc.height), 0, 0,
                        InfernumMode.CalamityMod.ProjectileType("ShadeNimbusHostile"), damage, 0, Main.myPlayer, 11, 0);
                    npc.Infernum().ExtraAI[1] += 1f;
                }
                if (npc.Infernum().ExtraAI[1] >= 10f)
                {
                    // Slow down
                    npc.alpha = 255;
                    npc.Infernum().ExtraAI[78] *= -1f;
                    npc.Infernum().ExtraAI[4] = slowdownTime / 2;
                    npc.Infernum().ExtraAI[0] = 7f;
                }
                int num414 = (int)(npc.position.X + 14f + Main.rand.Next(npc.width - 28));
                int num415 = (int)(npc.position.Y + npc.height + 4f);
                Projectile.NewProjectile(num414, num415, 0f, 5f, InfernumMode.CalamityMod.ProjectileType("ShaderainHostile"), 18, 0f, Main.myPlayer, 0f, 0f);
            }
            // Eater Wall
            if (npc.Infernum().ExtraAI[0] == 4f)
            {
                npc.Infernum().ExtraAI[9] = 4f;
                const float upwardMovementTime = 40;
                const float wallCreationTime = 40f;
                const float wallHeight = 1900f;
                if (npc.ai[0] == 0f)
                {
                    npc.velocity = Vector2.UnitY * -12f;
                    npc.ai[0] = 1f;
                }
                npc.ai[3] += 1f;
                if (npc.ai[3] < upwardMovementTime)
                {
                    npc.velocity *= 0.95f;
                }
                else if (npc.ai[3] == upwardMovementTime)
                {
                    npc.velocity = Vector2.Zero;
                    // Roar
                    npc.ai[1] = 1f;
                    npc.Infernum().ExtraAI[2] = Main.rand.Next(0, 35);
                }
                else
                {
                    npc.Infernum().ExtraAI[1] += wallHeight / wallCreationTime * (below20 ? 2.8f : 3.1f);
                    Projectile.NewProjectile(player.Center + new Vector2(-1200f, npc.Infernum().ExtraAI[1] - wallHeight / 2f + npc.Infernum().ExtraAI[2]),
                        new Vector2(10f, 0f).RotatedBy(below20 ? MathHelper.ToRadians(10f) : 0f), ModContent.ProjectileType<EaterOfSouls>(),
                        17, 1f);
                    Projectile.NewProjectile(player.Center + new Vector2(1200f, npc.Infernum().ExtraAI[1] - wallHeight / 2f + npc.Infernum().ExtraAI[2]),
                        new Vector2(-10f, 0f).RotatedBy(below20 ? MathHelper.ToRadians(-10f) : 0f), ModContent.ProjectileType<EaterOfSouls>(),
                        17, 1f);
                    if (npc.ai[3] > upwardMovementTime + wallCreationTime)
                    {
                        npc.alpha = 255;
                        npc.Infernum().ExtraAI[4] = slowdownTime / 2;
                        npc.Infernum().ExtraAI[0] = 7f;
                    }
                }
            }
            // Fire dash underground
            if (npc.Infernum().ExtraAI[0] == 5f)
            {
                npc.Infernum().ExtraAI[9] = 5f;
                if (npc.Infernum().ExtraAI[77] == 0f)
                {
                    npc.velocity = Vector2.Zero;
                    npc.Infernum().ExtraAI[1] = Main.rand.NextBool(2).ToDirectionInt();

                    float xOffset = 600f - 160f * (1f - lifeRatio);
                    if (below20)
                        xOffset -= 100f;

                    npc.position = player.Center + new Vector2(xOffset * npc.Infernum().ExtraAI[1], 350f);
                    npc.Infernum().ExtraAI[77] = 1f;
                }
                float waitTime = below20 ? 96f : 75f;
                float moveTime = below20 ? 50f : 90f;
                Projectile.NewProjectile(npc.Center, Vector2.UnitY * -7.4f, ModContent.ProjectileType<ShadeFire>(), 17, 0f);
                npc.ai[3] += 1f;
                if (npc.ai[3] == waitTime)
                {
                    // Roar
                    npc.ai[1] = 1f;
                    npc.velocity = Vector2.UnitX * npc.Infernum().ExtraAI[1] * (below20 ? -21f : -19f);
                }
                if (npc.ai[3] > waitTime && below20 && npc.ai[3] % 10f == 9f)
                {
                    int vileClot = Projectile.NewProjectile(npc.Center, Vector2.UnitY.RotatedByRandom(0.4f) * Main.rand.NextFloat(-7f, -5.25f), ModContent.ProjectileType<VileClot>(), 17, 0f);
                    Main.projectile[vileClot].tileCollide = false;
                }

                if (npc.ai[3] == waitTime + moveTime)
                {
                    npc.alpha = 255;
                    npc.Infernum().ExtraAI[77] = 0f;
                    npc.Infernum().ExtraAI[0] = 7f;
                }
            }
            // Rain, cursed fire, and fire from the ground
            if (npc.Infernum().ExtraAI[0] == 6f)
            {
                npc.Infernum().ExtraAI[9] = 6f;
                npc.velocity = Vector2.Zero;
                if (npc.ai[0] == 0f)
                {
                    npc.alpha = 255;
                    npc.ai[0] = 1f;
                }
                if (npc.alpha > 0)
                {
                    npc.alpha -= 9;
                    if (npc.alpha <= 0)
                    {
                        // Roar
                        npc.ai[1] = 1f;
                        npc.alpha = 0;
                    }
                    npc.netUpdate = true;
                }
                else
                {
                    npc.ai[3] += 1f;
                    if ((npc.ai[3] % 15 == 14f && !below20) ||
                        (npc.ai[3] % 12 == 11f && below20))
                    {
                        Projectile.NewProjectile(player.Center + new Vector2(Main.rand.NextFloat(-400f, 400f), -570f + Main.rand.NextFloat(-35f, 35f)),
                            Vector2.UnitY.RotatedByRandom(MathHelper.ToRadians(36f)) * 10f, InfernumMode.CalamityMod.ProjectileType("VileClot"),
                            16, 1f);
                    }
                    if ((npc.ai[3] % 45 == 44f && !below20) ||
                        (npc.ai[3] % 35 == 34f && below20))
                    {
                        Projectile.NewProjectile(player.Center + new Vector2(Main.rand.NextFloat(-400f, 400f), -570f + Main.rand.NextFloat(-35f, 35f)),
                            Vector2.Zero, InfernumMode.CalamityMod.ProjectileType("ShadeNimbusHostile"),
                            16, 1f);
                    }

                    if ((int)npc.ai[3] == 160f)
                        npc.Infernum().ExtraAI[12] = (below20 ? 4f : 3f) * 60f;
                    if (npc.ai[3] >= 210f - (below20 ? 30f : 0f))
                    {
                        npc.Infernum().ExtraAI[0] = 0f;
                    }
                }
            }
            // Slow down
            if (npc.Infernum().ExtraAI[0] == 7f)
            {
                npc.Infernum().ExtraAI[9] = 7f;
                if (npc.alpha > 0)
                {
                    npc.alpha -= 17;
                    if (npc.alpha < 0)
                    {
                        npc.alpha = 0;
                    }
                }
                if (npc.Infernum().ExtraAI[4] > 0f)
                {
                    npc.velocity *= 0.92f;
                    npc.Infernum().ExtraAI[4] -= 1f;
                }
                else
                {
                    // Go back to picking a new AI
                    npc.Infernum().ExtraAI[0] = 0f;
                }
            }
            // Blob sniping
            if (npc.Infernum().ExtraAI[0] == 8f)
            {
                npc.Infernum().ExtraAI[9] = 8f;
                npc.ai[0] += 1f;
                npc.ai[3] += MathHelper.ToRadians(5f);
                if ((int)npc.ai[0] == 120f)
                {
                    Projectile.NewProjectile(npc.Center, Vector2.Zero, ModContent.ProjectileType<HiveMindWave>(), 0, 0f);
                    Main.PlaySound(SoundID.Roar, (int)npc.Center.X, (int)npc.Center.Y, 0);
                    var explosionSound = Main.PlaySound(SoundID.DD2_BetsyFireballImpact, npc.Center);
                    if (explosionSound != null)
					{
                        explosionSound.Volume = 0.2f;
                        explosionSound.Pitch = -0.4f;
					}
                }

                if (npc.ai[0] > 120f && npc.ai[0] % 60f > 30f)
                {
                    npc.velocity *= 0.95f;
                    if (npc.ai[0] % 60f == 55f)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            float offsetAngle = i == 0 ? 0f : Main.rand.NextFloat(-0.53f, 0.53f);
                            float shootSpeed = i == 0f ? 14f : Main.rand.NextFloat(7.75f, 11f);
                            if (lifeRatio < 0.25f)
                                shootSpeed *= 1.35f;
                            Projectile.NewProjectile(npc.Center, npc.DirectionTo(player.Center).RotatedBy(offsetAngle) * shootSpeed, ModContent.ProjectileType<BlobProjectile>(), 16, 0f);
                        }
                        Main.PlaySound(SoundID.Roar, (int)npc.Center.X, (int)npc.Center.Y, 0);
                    }
                }
                else
                {
                    npc.SimpleFlyMovement((npc.velocity * 7f + npc.DirectionTo(player.Center + npc.ai[3].ToRotationVector2() * 300f) * 15f) / 8f, 0.1f);
                }
                if (npc.ai[0] > 120f + 60f * 4f)
                {
                    // Go back to picking a new AI
                    npc.Infernum().ExtraAI[4] = slowdownTime;
                    npc.Infernum().ExtraAI[0] = 7f;
                }
            }

            // Roar
            if (npc.ai[1] == 1f)
            {
                for (int i = 0; i < 72; i++)
                {
                    float angle = MathHelper.TwoPi / 72f * i;
                    int idx = Dust.NewDust(npc.Center, 1, 1, 157, (float)Math.Cos(angle) * 15f, (float)Math.Sin(angle) * 15f);
                    Main.dust[idx].noGravity = true;
                }
                Main.PlaySound(SoundID.Roar, (int)npc.Center.X, (int)npc.Center.Y, 0);
                npc.ai[1] = 0f;
            }

            // Weird roar
            if (npc.ai[2] == 1f)
            {
                Main.PlaySound(SoundID.ForceRoar, (int)npc.Center.X, (int)npc.Center.Y, -1, 1f, 0f);
                npc.ai[2] = 0f;
            }

            // For afterimage
            if (npc.Infernum().ExtraAI[6] <= 0f ||
                (npc.Infernum().ExtraAI[0] >= 4 &&
                npc.Infernum().ExtraAI[0] <= 7) ||
                below20 ||
                npc.Infernum().ExtraAI[6] > 0f)
            {
                npc.Infernum().ExtraAI[7] += 0.14f;
            }
            return false;
        }


        [OverrideAppliesTo("HiveMindP2", typeof(HiveMindAIClass), "HiveMindPreDraw", EntityOverrideContext.NPCPreDraw)]
        public static bool HiveMindPreDraw(NPC npc, SpriteBatch spriteBatch, Color drawColor)
        {
            NPCID.Sets.TrailingMode[npc.type] = 1;
            NPCID.Sets.TrailCacheLength[npc.type] = 8;

            for (int i = 1; i < npc.oldPos.Length; i++)
            {
                if (npc.Infernum().ExtraAI[10] == 0f)
                    break;

                float scale = npc.scale * MathHelper.Lerp(0.9f, 0.45f, i / (float)npc.oldPos.Length);
                float trailLength = MathHelper.Lerp(70f, 195f, Utils.InverseLerp(3f, 7f, npc.velocity.Length(), true));
                if (npc.velocity.Length() < 1.8f)
                    trailLength = 8f;

                Color lightColor = Color.MediumPurple * (1f - i / (float)npc.oldPos.Length);
                lightColor.A = 0;
                lightColor *= npc.Opacity;

                Vector2 drawPosition = npc.Center + npc.velocity.SafeNormalize(Vector2.Zero) * -MathHelper.Lerp(8f, trailLength, i / (float)npc.oldPos.Length);
                spriteBatch.Draw(ModContent.GetTexture(npc.modNPC.Texture), drawPosition - Main.screenPosition + new Vector2(0, npc.gfxOffY),
                    npc.frame, lightColor, npc.rotation, npc.frame.Size() / 2f, scale, SpriteEffects.None, 0f);
            }

            // If performing the blob snipe attack
            if (npc.Infernum().ExtraAI[0] == 8f)
            {
                spriteBatch.Draw(ModContent.GetTexture(npc.modNPC.Texture), npc.Center - Main.screenPosition + new Vector2(0, npc.gfxOffY),
                    npc.frame, Color.White, npc.rotation, npc.frame.Size() / 2f, npc.scale, SpriteEffects.None, 0f);
            }
            // If in the middle of a special attack (such as the Eater of Soul wall), or in the middle of its invincibility period after
            // going below 20% life
            if (npc.Infernum().ExtraAI[0] >= 4f ||
                npc.Infernum().ExtraAI[11] > 0f)
            {
                spriteBatch.Draw(ModContent.GetTexture(npc.modNPC.Texture), npc.Center - Main.screenPosition + new Vector2(0, npc.gfxOffY),
                    npc.frame, new Color(91f / 255f, 71f / 255f, 127f / 255f, 0.3f * npc.Opacity), npc.rotation, npc.frame.Size() / 2f, Utilities.AngularSmoothstep(npc.Infernum().ExtraAI[7], 1f, 1.5f), SpriteEffects.None, 0f);
                npc.Infernum().ExtraAI[6] = HiveMindFadeoutTime;
            }
            // If fadeout timer is greater than 0
            else if (npc.Infernum().ExtraAI[6] > 0f)
            {
                float scale = npc.Infernum().ExtraAI[6] / HiveMindFadeoutTime / Utilities.AngularSmoothstep(npc.Infernum().ExtraAI[7], 1f, 1.5f);
                spriteBatch.Draw(ModContent.GetTexture(npc.modNPC.Texture), npc.Center - Main.screenPosition + new Vector2(0, npc.gfxOffY), npc.frame, new Color(91f / 255f, 71f / 255f, 127f / 255f, 0.3f * npc.Opacity), npc.rotation, npc.frame.Size() / 2f, MathHelper.Clamp(scale, 1f, 1000f), SpriteEffects.None, 0f);
                npc.Infernum().ExtraAI[6] -= 1f;
            }

            return true;
        }
    }
}
