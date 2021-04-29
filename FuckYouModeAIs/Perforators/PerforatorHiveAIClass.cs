using CalamityMod;
using CalamityMod.NPCs;
using InfernumMode.FuckYouModeAIs.BoC;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.World.Generation;

namespace InfernumMode.FuckYouModeAIs.Perforators
{
    public class PerforatorHiveAIClass
    {
		#region AI

		#region Main Boss
		[OverrideAppliesTo("PerforatorHive", typeof(PerforatorHiveAIClass), "PerforatorHiveAI", EntityOverrideContext.NPCAI)]
        public static bool PerforatorHiveAI(NPC npc)
        {
            // Set a global whoAmI variable.
            CalamityGlobalNPC.perfHive = npc.whoAmI;

            npc.damage = 0;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float time = ref npc.ai[0];
            ref float summonTimer = ref npc.ai[1];
            ref float phaseState = ref npc.ai[2];
            ref float specificAttackTimer = ref npc.ai[3];
            ref float specificAttackType = ref npc.Infernum().ExtraAI[0];
            ref float pulseTimer = ref npc.Infernum().ExtraAI[1];

            bool angy = lifeRatio < 0.25f;

            if (pulseTimer > 0f)
			{
                pulseTimer++;
                if (pulseTimer > 60f)
                    pulseTimer = 0f;
            }

            if (!Main.player.IndexInRange(npc.target) || !Main.player[npc.target].active || Main.player[npc.target].dead)
            {
                npc.TargetClosest(false);
                if (!Main.player.IndexInRange(npc.target) || !Main.player[npc.target].active || Main.player[npc.target].dead)
				{
                    DoDespawnEffects(npc);
                    return false;
				}
            }

            Player target = Main.player[npc.target];

            // Have a delay from summoning things and try to do other stuff.
            // This also happens at low life.
            if (time % 1560f > 640f || angy)
            {
                // If enough monsters are still present, just hover.
                if (CountTotalMonsters() > 2)
                {
                    Vector2 destination = target.Center - Vector2.UnitY * 270f;
                    destination.X += (target.Center.X - npc.Center.X < 0f).ToDirectionInt() * 160f;
                    DoHoverMovement(npc, destination);

                    specificAttackType = 0f;
                    specificAttackTimer = 0f;
                }
				else
				{
                    switch ((int)specificAttackType % 2)
					{
                        case 0:
                            DoAttack_SwoopTowardsPlayer(npc, target, angy, ref specificAttackTimer, ref specificAttackType);
                            break;
                        case 1:
                            DoAttack_HoverNearTarget(npc, target, angy, ref specificAttackTimer, ref specificAttackType);
                            break;
					}
                    specificAttackTimer++;
                }
                summonTimer = 0f;
            }
			else
			{
                Vector2 destination = target.Center - Vector2.UnitY * 270f;
                destination.X += (target.Center.X - npc.Center.X < 0f).ToDirectionInt() * 225f;
                DoHoverMovement(npc, destination);

                int summonRate = (int)MathHelper.SmoothStep(180f, 75f, 1f - lifeRatio);
                int maxMonsters = (int)Math.Round(MathHelper.SmoothStep(5f, 9f, 1f - lifeRatio));
                if (summonTimer >= summonRate && CountTotalMonsters() < maxMonsters)
				{
                    // Create a pulse sound to indicate that something has spawned.
                    var pulseSound = Main.PlaySound(SoundID.DD2_WitherBeastAuraPulse, target.Center);
                    if (pulseSound != null)
                        pulseSound.Volume = MathHelper.Clamp(pulseSound.Volume * 1.4f, -1f, 1f);

                    // And summon the thing in question.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        SummonMonster(target, lifeRatio);
                        pulseTimer = 1f;
                        summonTimer = 0f;
                        npc.netUpdate = true;
                    }
                }

                summonTimer++;
            }

            npc.rotation = MathHelper.Clamp(npc.velocity.X * 0.04f, -MathHelper.Pi / 6f, MathHelper.Pi / 6f);
            time++;
            return false;
		}

        #region Specific Attacks
        public static void DoDespawnEffects(NPC npc)
		{
            npc.damage = 0;
            npc.velocity = Vector2.Lerp(npc.Center, Vector2.UnitY * 21f, 0.08f);
            if (npc.timeLeft > 225)
                npc.timeLeft = 225;
        }

        public static void DoHoverMovement(NPC npc, Vector2 destination)
        {
            if (!npc.WithinRange(destination, 160f))
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(destination) * 14f, 0.05f);
        }

        public static int CountTotalMonsters()
		{
            return NPC.CountNPCS(ModContent.NPCType<FaceMonster>()) 
                + NPC.CountNPCS(ModContent.NPCType<BloodCrawler>()) 
                + NPC.CountNPCS(ModContent.NPCType<Crimera>()) 
                + CalamityUtils.CountProjectiles(ModContent.ProjectileType<FaceMonsterSpawner>());
		}

        public static void SummonMonster(Player target, float lifeRatio)
		{
            WeightedRandom<int> enemySelector = new WeightedRandom<int>();
            enemySelector.Add(ModContent.NPCType<FaceMonster>(), 0.5);
            if (lifeRatio < 0.8f)
                enemySelector.Add(ModContent.NPCType<Crimera>(), 0.45f);
            if (lifeRatio < 0.66f)
                enemySelector.Add(ModContent.NPCType<BloodCrawler>(), 0.55f);

            int typeToSummon = enemySelector.Get();
            if (typeToSummon == ModContent.NPCType<FaceMonster>() || typeToSummon == ModContent.NPCType<BloodCrawler>())
            {
                Vector2 potentialSpawnPosition = target.Center;
                potentialSpawnPosition.X += Main.rand.NextFloat(300f, 750f) * Main.rand.NextBool(2).ToDirectionInt();
                potentialSpawnPosition.Y -= 900f;
                if (potentialSpawnPosition.Y < 180f)
                    potentialSpawnPosition.Y = 180f;

                WorldUtils.Find(potentialSpawnPosition.ToTileCoordinates(), Searches.Chain(new Searches.Down(900), new Conditions.IsSolid()), out Point result);
                potentialSpawnPosition = result.ToWorldCoordinates(8, 0);

                if (typeToSummon == ModContent.NPCType<BloodCrawler>())
                    NPC.NewNPC((int)potentialSpawnPosition.X, (int)potentialSpawnPosition.Y - 8, ModContent.NPCType<BloodCrawler>());
                else
                    Utilities.NewProjectileBetter(potentialSpawnPosition, Vector2.Zero, ModContent.ProjectileType<FaceMonsterSpawner>(), 0, 0f);
            }

            if (typeToSummon == ModContent.NPCType<Crimera>())
			{
                for (int tries = 0; tries < 500; tries++)
				{
                    Vector2 potentialSpawnPosition = target.Center + new Vector2(Main.rand.NextFloat(680f, -800f));

                    // Try again if it's too horizontaly close to the target.
                    if (MathHelper.Distance(target.Center.X, potentialSpawnPosition.X) < 300f)
                        continue;

                    // Try again if the position has nearby tiles.
                    if (Collision.SolidCollision(potentialSpawnPosition - Vector2.One * 40f, 80, 80))
                        continue;

                    // Try again if the position has something in the way of the target.
                    if (!Collision.CanHit(target.Center, 1, 1, potentialSpawnPosition, 1, 1))
                        continue;

                    NPC.NewNPC((int)potentialSpawnPosition.X, (int)potentialSpawnPosition.Y - 8, ModContent.NPCType<Crimera>());
                    break;
                }
			}
		}

        public static void DoAttack_SwoopTowardsPlayer(NPC npc, Player target, bool angy, ref float attackTimer, ref float attackType)
		{
            // Hover above the target before swooping.
            if (attackTimer < 90f)
            {
                Vector2 destination = target.Center - Vector2.UnitY * 270f;
                destination.X += (target.Center.X - npc.Center.X < 0f).ToDirectionInt() * 360f;
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(destination) * 14f, 0.12f);

                if (npc.WithinRange(destination, 35f))
				{
                    attackTimer = 90;
                    npc.netUpdate = true;
				}
            }

            // Play a roar sound before swooping.
            if (attackTimer == 90f)
            {
                Main.PlaySound(SoundID.Roar, target.Center, 0);
                npc.velocity = npc.SafeDirectionTo(target.Center) * new Vector2(8f, 20f);
                if (angy)
                    npc.velocity *= new Vector2(1.2f, 1.3f);
                npc.netUpdate = true;

                npc.TargetClosest();
            }

            // Swoop.
            if (attackTimer >= 90f && attackTimer <= 180f)
            {
                npc.velocity = npc.velocity.RotatedBy(MathHelper.PiOver2 / 90f * -npc.direction);
                npc.damage = 72;
            }

            if (attackTimer > 180f)
                npc.velocity *= 0.97f;

            if (attackTimer >= 215f)
			{
                attackType++;
                attackTimer = 0f;
                npc.netUpdate = true;
			}
		}

        public static void DoAttack_HoverNearTarget(NPC npc, Player target, bool angy, ref float attackTimer, ref float attackType)
        {
            Vector2 offset = (MathHelper.TwoPi * 2f * attackTimer / 180f).ToRotationVector2() * 300f;

            if (attackTimer % 120f > 85f)
            {
                // Play a roar sound before swooping.
                if (attackTimer % 120f == 90f)
                    Main.PlaySound(SoundID.Roar, target.Center, 0);

                npc.velocity *= 0.97f;

                // Release ichor everywhere.
                int shootRate = angy ? 4 : 6;
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % shootRate == shootRate - 1f)
                {
                    Vector2 shootVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f, 8.4f);
                    Vector2 spawnPosition = npc.Center - Vector2.UnitY * 45f + Main.rand.NextVector2Circular(30f, 30f);

                    int ichor = Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ModContent.ProjectileType<IchorSpit>(), 65, 0f);
                    if (Main.projectile.IndexInRange(ichor))
                        Main.projectile[ichor].ai[1] = 1f;
                }
            }
            else
                npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center + offset) * 15f, angy ? 0.185f : 0.1f);

            if (attackTimer >= 240f)
            {
                attackType++;
                attackTimer = 0f;
                npc.netUpdate = true;
            }
        }

        #endregion Specific Attacks
        #endregion Main Boss

        #endregion AI

        #region Frames and Drawcode

        public static void DrawEnragedEffectOnEnemy(SpriteBatch spriteBatch, NPC npc)
		{
            Texture2D texture = Main.npcTexture[npc.type];
            Color drawColor = Color.Lerp(Color.Yellow, Color.Orange, 0.35f) * npc.Opacity * 0.375f;
            drawColor.A = 0;

            SpriteEffects direction = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            for (int i = 0; i < 8; i++)
			{
                Vector2 drawPosition = npc.Center + (MathHelper.TwoPi * i / 8f + Main.GlobalTime * 3.7f).ToRotationVector2() * 4f - Main.screenPosition;
                drawPosition.Y += npc.gfxOffY;
                spriteBatch.Draw(texture, drawPosition, npc.frame, drawColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);
			}
		}

        [OverrideAppliesTo("PerforatorHive", typeof(PerforatorHiveAIClass), "PerforatorPreDraw", EntityOverrideContext.NPCPreDraw)]
        public static bool PerforatorPreDraw(NPC npc, SpriteBatch spriteBatch, Color _)
        {
            bool enraged = npc.ai[2] >= 2f;
            float pulseTimer = npc.Infernum().ExtraAI[1];
            if (enraged)
                DrawEnragedEffectOnEnemy(spriteBatch, npc);

            if (pulseTimer > 0f)
			{
                float pulseFade = Utils.InverseLerp(60f, 1f, pulseTimer, true);
                float pulseScale = MathHelper.Lerp(1.6f, 1f, pulseFade);

                Texture2D texture = Main.npcTexture[npc.type];
                Color drawColor = Color.Lerp(Color.Red, Color.DarkRed, pulseFade) * npc.Opacity * pulseFade * 1.4f;
                drawColor.A = 0;

                Vector2 drawPosition = npc.Center - Main.screenPosition + Vector2.UnitY * npc.gfxOffY;

                spriteBatch.Draw(texture, drawPosition, npc.frame, drawColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale * pulseScale, SpriteEffects.None, 0f);
            }

            return true;
        }

        #endregion Frames and Drawcode
    }
}
