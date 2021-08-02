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

using HiveMindBoss = CalamityMod.NPCs.HiveMind.HiveMind;

namespace InfernumMode.FuckYouModeAIs.HiveMind
{
	public class HiveMindBehaviorOverrideP1 : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<HiveMindBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        // TODO: Refactor this.
        public override bool PreAI(NPC npc)
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
                npc.defense = 5;
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
    }
}
