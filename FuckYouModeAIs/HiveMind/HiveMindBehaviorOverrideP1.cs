using CalamityMod.NPCs;
using CalamityMod.NPCs.HiveMind;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using HiveMindBoss = CalamityMod.NPCs.HiveMind.HiveMind;

namespace InfernumMode.FuckYouModeAIs.HiveMind
{
	public class HiveMindBehaviorOverrideP1 : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<HiveMindBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            npc.TargetClosest(true);
            Player target = Main.player[npc.target];
            float lifeRatio = npc.life / (float)npc.lifeMax;

            ref float summonThresholdByLife = ref npc.ai[3];
            ref float hasSummonedInitialBlobsFlag = ref npc.localAI[0];
            ref float hiveBlobSummonTimer = ref npc.localAI[1];
            ref float digTime = ref npc.localAI[3];
            ref float shootTimer = ref npc.Infernum().ExtraAI[0];

            // Despawn if the target is dead or gone.
            if (!target.active || target.dead)
            {
                if (npc.timeLeft > 60)
                    npc.timeLeft = 60;
                if (digTime < 120f)
                    digTime++;

                if (digTime > 60f)
                {
                    npc.velocity.Y += (digTime - 60f) * 0.5f;
                    npc.noGravity = true;
                    npc.noTileCollide = true;
                    if (shootTimer > 30)
                        shootTimer = 30;
                }
                return false;
            }

            CalamityGlobalNPC.hiveMind = npc.whoAmI;

            // Don't do anything beyond this point if busy digging.
            if (digTime > 0f)
            {
                digTime--;
                return false;
            }
            npc.noGravity = false;
            npc.noTileCollide = false;

            // Idly summon blobs.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                hiveBlobSummonTimer++;
                if (hiveBlobSummonTimer >= 600f)
                {
                    hiveBlobSummonTimer = 0f;
                    NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, InfernumMode.CalamityMod.NPCType("HiveBlob"), npc.whoAmI, 0f, 0f, 0f, 0f, 255);
                }
                if (hasSummonedInitialBlobsFlag == 0f)
                {
                    hasSummonedInitialBlobsFlag = 1f;
                    for (int i = 0; i < 7; i++)
                        NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, InfernumMode.CalamityMod.NPCType("HiveBlob"), npc.whoAmI, 0f, 0f, 0f, 0f, 255);
                }
            }

            // Gain a massive defense boost if a dank meme is alive.
            npc.defense = NPC.AnyNPCs(ModContent.NPCType<DankCreeper>()) ? 45 : 5;

            if (summonThresholdByLife == 0f && npc.life > 0)
                summonThresholdByLife = npc.lifeMax;

            if (npc.life > 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (npc.life + (int)(npc.lifeMax * 0.2) < summonThresholdByLife)
                    {
                        summonThresholdByLife = npc.life;
                        int enemySummonCount = Main.rand.Next(3, 6);
                        for (int i = 0; i < enemySummonCount; i++)
                        {
                            int x = (int)(npc.position.X + Main.rand.Next(npc.width - 32));
                            int y = (int)(npc.position.Y + Main.rand.Next(npc.height - 32));
                            int thingToSummon = ModContent.NPCType<HiveBlob>();
                            if (Main.rand.NextBool(3))
                                thingToSummon = ModContent.NPCType<DankCreeper>();

                            int summonedThing = NPC.NewNPC(x, y, thingToSummon, 0, 0f, 0f, 0f, 0f, 255);
                            Main.npc[summonedThing].SetDefaults(thingToSummon, -1f);
                            Main.npc[summonedThing].velocity = -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver2) * 3f;
                            if (Main.netMode == NetmodeID.Server && summonedThing < Main.maxNPCs)
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, summonedThing, 0f, 0f, 0f, 0, 0, 0);
                        }
                        npc.netUpdate = true;
                        return false;
                    }
                }
            }

            shootTimer--;

            float clotShootRate = 260f;
            float mirageSummonRate = 300f;

            if (lifeRatio < 0.5f)
                mirageSummonRate = 180f;
            if (lifeRatio < 0.25f)
                clotShootRate = 145f;

            if (shootTimer > 100 && shootTimer % clotShootRate == 0f)
            {
                int clotCount = Main.rand.Next(4, 6);
                for (int i = 0; i < clotCount; i++)
                {
                    Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedByRandom(MathHelper.ToRadians(15f)) * 8f;
                    Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<VileClot>(), 65, 1f);
                }
            }
            if (shootTimer > 50 && shootTimer % mirageSummonRate == 0f && lifeRatio < 0.75f)
            {
                Vector2 mirageSummonPosition = target.Center - Vector2.UnitY * 2300f;
                Utilities.NewProjectileBetter(mirageSummonPosition, Vector2.Zero, ModContent.ProjectileType<HiveMindMirage>(), 28, 3f, target.whoAmI, 1f);
            }
            if (shootTimer < -120)
            {
                shootTimer = 600;
                npc.scale = 1f;
                npc.alpha = 0;
                npc.dontTakeDamage = false;
                npc.damage = npc.defDamage;
            }

            // Fade out and do dig effects.
            else if (shootTimer < -60)
            {
                npc.scale += 0.0165f;
                npc.alpha -= 4;
                Dust digDust = Dust.NewDustDirect(new Vector2(npc.position.X, npc.Center.Y), npc.width, npc.height / 2, 14, 0f, -3f, 100, default, 2.5f * npc.scale);
                digDust.velocity *= 2f;
                if (Main.rand.NextBool(2))
                {
                    digDust.scale = 0.5f;
                    digDust.fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
                for (int i = 0; i < 2; i++)
                {
                    digDust = Dust.NewDustDirect(new Vector2(npc.position.X, npc.Center.Y), npc.width, npc.height / 2, 14, 0f, -3f, 100, default, 3.5f * npc.scale);
                    digDust.noGravity = true;
                    digDust.velocity *= 3.5f;
                    digDust = Dust.NewDustDirect(new Vector2(npc.position.X, npc.Center.Y), npc.width, npc.height / 2, 14, 0f, -3f, 100, default, 2.5f * npc.scale);
                    digDust.velocity *= 1f;
                }
            }

            // Do the dig teleport.
            else if (shootTimer == -60)
            {
                npc.scale = 0.01f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.Center = target.Center;
                    npc.position.Y = target.position.Y - npc.height;
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
            else if (shootTimer < 0)
            {
                npc.scale -= 0.0165f;
                npc.alpha += 4;
                Dust digDust = Dust.NewDustDirect(new Vector2(npc.position.X, npc.Center.Y), npc.width, npc.height / 2, 14, 0f, -3f, 100, default, 2.5f * npc.scale);
                digDust.velocity *= 2f;
                if (Main.rand.NextBool(2))
                {
                    digDust.scale = 0.5f;
                    digDust.fadeIn = 1f + Main.rand.Next(10) * 0.1f;
                }
                for (int i = 0; i < 2; i++)
                {
                    digDust = Dust.NewDustDirect(new Vector2(npc.position.X, npc.Center.Y), npc.width, npc.height / 2, 14, 0f, -3f, 100, default, 3.5f * npc.scale);
                    digDust.noGravity = true;
                    digDust.velocity *= 3.5f;
                    digDust = Dust.NewDustDirect(new Vector2(npc.position.X, npc.Center.Y), npc.width, npc.height / 2, 14, 0f, -3f, 100, default, 2.5f * npc.scale);
                    digDust.velocity *= 1f;
                }
            }
            else if (shootTimer == 0)
            {
                if (!target.active || target.dead)
                    shootTimer = 30;

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
