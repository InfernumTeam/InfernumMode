using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.HiveMind;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using InfernumMode.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using HiveMindBoss = CalamityMod.NPCs.HiveMind.HiveMind;

namespace InfernumMode.BehaviorOverrides.BossAIs.HiveMind
{
    public class HiveMindBehaviorOverrideP1 : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<HiveMindBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public const float Phase2LifeRatio = 0.8f;

        public const float Phase3LifeRatio = 0.2f;

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio,
            Phase3LifeRatio
        };

        public override bool PreAI(NPC npc)
        {
            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Do defense damage on contact.
            npc.Calamity().canBreakPlayerDefense = true;

            ref float summonThresholdByLife = ref npc.ai[3];
            ref float hasSummonedInitialBlobsFlag = ref npc.localAI[0];
            ref float hiveBlobSummonTimer = ref npc.localAI[1];
            ref float digTime = ref npc.localAI[3];
            ref float shootTimer = ref npc.Infernum().ExtraAI[0];
            ref float phase2 = ref npc.Infernum().ExtraAI[20];

            // Kill debuffs.
            if (target.HasBuff(BuffID.CursedInferno))
                target.ClearBuff(BuffID.CursedInferno);
            if (target.HasBuff(ModContent.BuffType<Shadowflame>()))
                target.ClearBuff(ModContent.BuffType<Shadowflame>());

            if (lifeRatio < Phase2LifeRatio)
            {
                if (phase2 == 0f)
                {
                    for (int i = 0; i < 4; i++)
                        npc.ai[i] = 0f;
                    for (int i = 0; i < 20; i++)
                        npc.Infernum().ExtraAI[i] = 0f;
                    phase2 = 1f;
                    npc.netUpdate = true;
                }
                return HiveMindBehaviorOverrideP2.PreAI(npc);
            }

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
                if (hiveBlobSummonTimer >= 540f)
                {
                    hiveBlobSummonTimer = 0f;
                    NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, InfernumMode.CalamityMod.Find<ModNPC>("HiveBlob").Type, npc.whoAmI, 0f, 0f, 0f, 0f, 255);
                }
                if (hasSummonedInitialBlobsFlag == 0f)
                {
                    hasSummonedInitialBlobsFlag = 1f;
                    for (int i = 0; i < 7; i++)
                        NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, InfernumMode.CalamityMod.Find<ModNPC>("HiveBlob").Type, npc.whoAmI, 0f, 0f, 0f, 0f, 255);
                }
            }

            // Gain a massive defense boost if a dank meme is alive.
            npc.defense = NPC.AnyNPCs(ModContent.NPCType<DankCreeper>()) ? 45 : -5;

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

                            int summonedThing = NPC.NewNPC(npc.GetSource_FromAI(), x, y, thingToSummon, 0, 0f, 0f, 0f, 0f, 255);
                            Main.npc[summonedThing].SetDefaults(thingToSummon);
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
            float mirageSummonRate = 400f;

            if (lifeRatio < 0.92f)
                mirageSummonRate = 270f;
            if (lifeRatio < 0.85f)
                clotShootRate = 145f;

            if (shootTimer > 100 && shootTimer % clotShootRate == 0f)
            {
                int clotCount = Main.rand.Next(4, 6);
                for (int i = 0; i < clotCount; i++)
                {
                    Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedByRandom(MathHelper.ToRadians(15f)) * 8f;
                    if (BossRushEvent.BossRushActive)
                        shootVelocity *= 2.25f;

                    Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<VileClot>(), 80, 1f);
                }
            }
            if (shootTimer > 50 && shootTimer % mirageSummonRate == 0f && lifeRatio < 0.95f)
            {
                Vector2 mirageSummonPosition = target.Center - Vector2.UnitY * 2300f;
                Utilities.NewProjectileBetter(mirageSummonPosition, Vector2.Zero, ModContent.ProjectileType<HiveMindMirage>(), 105, 3f, target.whoAmI, 1f);
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
                    while (!(Main.tile[tilePosX, tilePosY].HasUnactuatedTile && Main.tileSolid[Main.tile[tilePosX, tilePosY].TileType]))
                    {
                        tilePosY++;
                        npc.position.Y += 16;
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

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            if (npc.Infernum().ExtraAI[20] == 1f)
                return HiveMindBehaviorOverrideP2.PreDraw(npc, lightColor);
            return true;
        }

        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "This is the time you would want to learn the opponents moves, use their tells to get the upper hand!";
            yield return n => "Try to push his Rain Charge away by running towards the Hive Mind, this can help keep your arena clean!";
            yield return n => "The Hive Mind begins its next attack early if you attack it; wait until it's on cooldown before you shoot!";
            yield return n =>
            {
                if (HatGirlTipsManager.ShouldUseJokeText)
                    return "That didn't work, but dont worry! Hive got a plan!";
                return string.Empty;
            };
            yield return n =>
            {
                if (HatGirlTipsManager.ShouldUseJokeText)
                    return "I would make a snarky comment right now, but I probably should Mind my own business...";
                return string.Empty;
            };
        }
    }
}
