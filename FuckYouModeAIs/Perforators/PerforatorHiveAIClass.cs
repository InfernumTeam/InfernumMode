using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Crabulon;
using CalamityMod.Projectiles.Boss;
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

		[OverrideAppliesTo("PerforatorHive", typeof(PerforatorHiveAIClass), "PerforatorHiveAI", EntityOverrideContext.NPCAI)]
        public static bool PerforatorHiveAI(NPC npc)
        {
            // Set a global whoAmI variable.
            CalamityGlobalNPC.perfHive = npc.whoAmI;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float time = ref npc.ai[0];
            ref float summonTimer = ref npc.ai[1];
            ref float phaseState = ref npc.ai[2];

            npc.TargetClosest();
            if (!Main.player.IndexInRange(npc.target) || !Main.player[npc.target].active || Main.player[npc.target].dead)
            {
                npc.TargetClosest();
                if (!Main.player.IndexInRange(npc.target) || !Main.player[npc.target].active || Main.player[npc.target].dead)
				{
                    DoDespawnEffects(npc);
                    return false;
				}
            }

            Player target = Main.player[npc.target];

            // Have a delay from summoning things and try to fly above the target.
            if (time % 960f > 640f)
            {
                DoHoverMovement(npc, target.Center - Vector2.UnitY * 300f);
                summonTimer = 0f;
            }
			else
			{
                Vector2 destination = target.Center - Vector2.UnitY * 270f;
                destination.X -= (target.Center.X - npc.Center.X < 0f).ToDirectionInt() * 425f;
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
                        SummonMonster(target, lifeRatio);

                    summonTimer = 0f;
                    npc.netUpdate = true;
                }

                summonTimer++;
            }

            npc.damage = 0;
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
            npc.rotation = MathHelper.Clamp(npc.velocity.X * 0.04f, -MathHelper.Pi / 6f, MathHelper.Pi / 6f);
        }

        public static int CountTotalMonsters()
		{
            return NPC.CountNPCS(NPCID.BigCrimera) + NPC.CountNPCS(NPCID.FaceMonster) + NPC.CountNPCS(NPCID.BloodCrawler) + NPC.CountNPCS(NPCID.BloodCrawlerWall);
		}

        public static void SummonMonster(Player target, float lifeRatio)
		{
            WeightedRandom<int> enemySelector = new WeightedRandom<int>();
            enemySelector.Add(NPCID.FaceMonster, 0.5);

            int typeToSummon = enemySelector.Get();
            if (typeToSummon == NPCID.FaceMonster)
            {
                Vector2 potentialSpawnPosition = target.Center;
                potentialSpawnPosition.X += Main.rand.NextFloat(300f, 750f) * Main.rand.NextBool(2).ToDirectionInt();
                potentialSpawnPosition.Y -= 900f;
                if (potentialSpawnPosition.Y < 180f)
                    potentialSpawnPosition.Y = 180f;

                WorldUtils.Find(potentialSpawnPosition.ToTileCoordinates(), Searches.Chain(new Searches.Down(900), new Conditions.IsSolid()), out Point result);
                potentialSpawnPosition = result.ToWorldCoordinates(8, 0);

                Utilities.NewProjectileBetter(potentialSpawnPosition, Vector2.Zero, ModContent.ProjectileType<FaceMonsterSpawner>(), 0, 0f);
            }
		}
        #endregion Specific Attacks

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
            if (enraged)
                DrawEnragedEffectOnEnemy(spriteBatch, npc);

            return true;
        }

        #endregion Frames and Drawcode
    }
}
