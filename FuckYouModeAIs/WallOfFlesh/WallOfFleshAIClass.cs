using CalamityMod;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.World.Generation;

namespace InfernumMode.FuckYouModeAIs.WallOfFlesh
{
	public class WallOfFleshAIClass
    {
        #region AI

        #region Body
        [OverrideAppliesTo(NPCID.WallofFlesh, typeof(WallOfFleshAIClass), "WoFAI", EntityOverrideContext.NPCAI)]
        public static bool WoFAI(NPC npc)
        {
            npc.Calamity().DR = 0.2f;

            ref float initialized01Flag = ref npc.localAI[0];
            ref float attackTimer = ref npc.ai[3];

            // Determine a target.
            if (!Main.player.IndexInRange(npc.target) || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            Player target = Main.player[npc.target];
            float lifeRatio = MathHelper.Clamp(npc.life / (float)npc.lifeMax, 0f, 1f);
            
            Main.wof = npc.whoAmI;

            if (npc.Center.X <= 160f || npc.Center.X >= Main.maxTilesX * 16f - 160f)
			{
                npc.active = false;
                npc.netUpdate = true;
			}

            if (initialized01Flag == 0f)
            {
                Main.wofB = -1;
                Main.wofT = -1;

                SetEyePositions(npc);
                SummonEyes(npc);
                initialized01Flag = 1f;
            }

            SetEyePositions(npc);
            PerformMouthMotion(npc, lifeRatio);
            AngerEffects(npc, target);
            DetermineMouthRotation(npc, target);

            attackTimer++;
            if (attackTimer % 120f == 119f)
                SpitBlood(npc, target);

            int scytheCeilingShootRate = lifeRatio < 0.5f ? 300 : 180;
            if (attackTimer % scytheCeilingShootRate == scytheCeilingShootRate - 1f)
                DoCeilingAndFloorAttack(npc, target, lifeRatio < 0.5f);

            int beamShootRate = 440;
            if (!NPC.AnyNPCs(NPCID.WallofFleshEye) && attackTimer % beamShootRate == beamShootRate - 1f)
                PrepareFireBeam(npc, target);

            return false;
		}

        internal static void SetEyePositions(NPC npc)
        {
            int left = (int)(npc.Left.X / 16f);
            int right = (int)(npc.Right.X / 16f);
            int tries = 0;
            int y = (int)(npc.Center.Y / 16f) + 7;
            while (tries < 15 && y > Main.maxTilesY - 200)
            {
                y++;
                for (int x = left; x <= right; x++)
                {
                    try
                    {
                        if (WorldGen.SolidTile(x, y) || Main.tile[x, y].liquid > 0)
                            tries++;
                    }
                    catch
                    {
                        tries += 15; 
                    }
                }
            }
            y += 4;
            if (Main.wofB == -1)
                Main.wofB = y * 16;
            else if (Main.wofB > y * 16)
            {
                Main.wofB--;
                if (Main.wofB < y * 16)
                    Main.wofB = y * 16;
            }
            else if (Main.wofB < y * 16)
            {
                Main.wofB++;
                if (Main.wofB > y * 16)
                    Main.wofB = y * 16;
            }

            tries = 0;
            y = (int)(npc.Center.Y / 16f) - 7;
            while (tries < 15 && y < Main.maxTilesY - 10)
            {
                y--;
                for (int x = left; x <= right; x++)
                {
                    try
                    {
                        if (WorldGen.SolidTile(x, y) || Main.tile[x, y].liquid > 0)
                            tries++;
                    }
                    catch
                    {
                        tries += 15; 
                    }
                }
            }
            y -= 4;

            if (Main.wofT == -1)
                Main.wofT = y * 16;
            else if (Main.wofT > y * 16)
            {
                Main.wofT--;
                if (Main.wofT < y * 16)
                    Main.wofT = y * 16;
            }
            else if (Main.wofT < y * 16)
            {
                Main.wofT++;
                if (Main.wofT > y * 16)
                    Main.wofT = y * 16;
            }
        }

        internal static void PerformMouthMotion(NPC npc, float lifeRatio)
		{
            float verticalDestination = (Main.wofB + Main.wofT) / 2 - npc.height / 2;
            float horizontalSpeed = MathHelper.Lerp(4f, 5.6f, 1f - lifeRatio);
            if (verticalDestination < (Main.maxTilesY - 180) * 16f)
                verticalDestination = (Main.maxTilesY - 180) * 16f;

            npc.position.Y = verticalDestination;

            if (npc.velocity.X == 0f)
                npc.velocity.X = npc.direction;

            if (npc.velocity.X < 0f)
            {
                npc.velocity.X = -horizontalSpeed;
                npc.direction = -1;
            }
            else
            {
                npc.velocity.X = horizontalSpeed;
                npc.direction = 1;
            }
        }

        internal static void SummonEyes(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 5; i++)
            {
                int hungry = NPC.NewNPC((int)npc.position.X, (int)npc.Center.Y, NPCID.TheHungry, npc.whoAmI, 0f, 0f, 0f, 0f, 255);
                Main.npc[hungry].ai[0] = i * 0.2f - 0.05f;
            }

            List<float> offsetFactors = new List<float>();
            for (int i = 0; i < 3; i++)
			{
                float potentialOffsetFactor = Main.rand.NextFloat();
                while (offsetFactors.Any(factor => MathHelper.Distance(factor, potentialOffsetFactor) < 0.25f))
				{
                    i--;
                    continue;
				}

                NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.WallofFleshEye, ai0: potentialOffsetFactor);
			}
        }

        internal static void AngerEffects(NPC npc, Player target)
        {
            ref float angerStrength = ref npc.ai[0];
            ref float timeSpentRunning = ref npc.ai[1];
            ref float enrageAttackCountdown = ref npc.ai[2];
            ref float roarTimer = ref npc.localAI[1];

            float idealAngerStrength = MathHelper.Lerp(0f, 0.8f, Utils.InverseLerp(800, 2300f, Math.Abs(target.Center.X - npc.Center.X), true));

            // Check if the player is running in one direction.
            if (Math.Abs(Vector2.Dot(target.velocity.SafeNormalize(Vector2.Zero), Vector2.UnitX)) > 0.74f && Math.Abs(target.Center.X - npc.Center.X) > 500f)
                timeSpentRunning++;
            else
                timeSpentRunning--;

            if (roarTimer > 0)
                roarTimer--;

            timeSpentRunning = MathHelper.Clamp(timeSpentRunning, 0f, 1200f);
            idealAngerStrength += MathHelper.Lerp(0f, 0.2f, Utils.InverseLerp(240f, 420f, timeSpentRunning, true));
            idealAngerStrength = MathHelper.Clamp(idealAngerStrength, 0f, 1f);
            angerStrength = MathHelper.Lerp(angerStrength, idealAngerStrength, 0.03f);

            if (enrageAttackCountdown > 0)
            {
                // Summon tentacles near the player.
                if (Main.netMode != NetmodeID.MultiplayerClient && enrageAttackCountdown % 30 == 29)
				{
                    Vector2 spawnPosition = target.Center + Main.rand.NextVector2CircularEdge(320f, 320f);
                    for (int tries = 0; tries < 2500; tries++)
					{
                        int checkArea = 30 + tries / 20;   
                        Vector2 potentialSpawnPosition = target.Center + target.velocity * 10f + Main.rand.NextVector2CircularEdge(checkArea, checkArea) * 16f;
                        Tile spawnTile = CalamityUtils.ParanoidTileRetrieval((int)potentialSpawnPosition.X / 16, (int)potentialSpawnPosition.Y / 16);
                        Tile aboveTile = CalamityUtils.ParanoidTileRetrieval((int)potentialSpawnPosition.X / 16, (int)potentialSpawnPosition.Y / 16 - 2);
                        Tile belowTile = CalamityUtils.ParanoidTileRetrieval((int)potentialSpawnPosition.X / 16, (int)potentialSpawnPosition.Y / 16 + 2);

                        bool aboveTileInvalid = aboveTile.active() && Main.tileSolid[aboveTile.type];
                        bool bottomTileInvalid = belowTile.active() && Main.tileSolid[belowTile.type];

                        if (spawnTile.nactive() && Main.tileSolid[spawnTile.type] && !aboveTileInvalid && !bottomTileInvalid)
                        {
                            spawnPosition = potentialSpawnPosition;
                            break;
                        }
					}

                    spawnPosition = spawnPosition.ToTileCoordinates().ToWorldCoordinates();
                    Vector2 tentacleDirection = target.DirectionFrom(spawnPosition);
                    Utilities.NewProjectileBetter(spawnPosition, tentacleDirection, ModContent.ProjectileType<TileTentacle>(), 65, 0f);
				}
                enrageAttackCountdown--;
            }

            // Roaring and preparing for enrage attack.
            if (roarTimer <= 0f && enrageAttackCountdown <= 0f && idealAngerStrength >= 0.5f && angerStrength < idealAngerStrength)
            {
                if (Main.netMode != NetmodeID.Server)
				{
                    roarTimer = 660f;

                    // Scream.
                    Main.PlaySound(SoundID.Roar, (int)target.Center.X, (int)target.Center.Y, 1, 1f, 0.3f);

                    // Roar.
                    Main.PlaySound(SoundID.NPCKilled, (int)target.Center.X, (int)target.Center.Y, 10, 1.2f, 0.3f);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
				{
                    enrageAttackCountdown = 300f;
                    npc.netUpdate = true;
				}
            }
        }

        internal static void DetermineMouthRotation(NPC npc, Player target)
        {
            npc.spriteDirection = npc.direction;
            if (Utilities.AnyProjectiles(ModContent.ProjectileType<FireBeamWoF>()))
                return;

            if (npc.direction > 0)
            {
                if (target.Center.X > npc.Center.X)
                    npc.rotation = npc.AngleTo(target.Center);
                else
                    npc.rotation = MathHelper.Pi;
            }
            else if (target.Center.X < npc.Center.X)
                npc.rotation = npc.AngleTo(target.Center) + MathHelper.Pi;
            else
                npc.rotation = 0f;
        }

        internal static void SpitBlood(NPC npc, Player target)
		{
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            float shootSpeed = 12f;
            Vector2 spawnPosition = npc.Center + Vector2.UnitX * npc.direction * 20f + npc.velocity;
            Vector2 targetDestination = target.Center + Vector2.UnitX * (target.velocity.X + (target.Center.X - spawnPosition.X > 0).ToDirectionInt() * 10f) * 16f;
            float distance = MathHelper.Distance(spawnPosition.X, targetDestination.X);

            for (int i = 0; i < 3; i++)
            {
                float angle = 0.5f * (float)Math.Asin(MathHelper.Clamp(BloodVomit.Gravity * distance / (float)Math.Pow(shootSpeed, 2), -1f, 1f));
                angle += MathHelper.Lerp(-0.5f, 0.5f, i / 3f);

                Vector2 velocity = new Vector2(0f, -shootSpeed).RotatedBy(angle);
                velocity.X *= (targetDestination.X - spawnPosition.X > 0).ToDirectionInt();

                Utilities.NewProjectileBetter(spawnPosition + velocity * 3f, velocity, ModContent.ProjectileType<BloodVomit>(), 60, 0f);
            }
        }

        internal static void DoCeilingAndFloorAttack(NPC npc, Player target, bool inPhase2)
        {
            if (Main.myPlayer != npc.target)
                return;

            for (float x = -1180f; x < 1180f; x += inPhase2 ? 200f : 256f)
            {
                Vector2 spawnPosition = target.Center + new Vector2(x, Main.screenHeight * -0.4f);
                int scythe = Utilities.NewProjectileBetter(spawnPosition, Vector2.UnitY * 2f, ProjectileID.DemonSickle, 60, 0f);
                Main.projectile[scythe].tileCollide = false;
            }

            try
            {
                Vector2 searchPosition = target.Center + new Vector2(target.velocity.X * 110f, -50f);
                WorldUtils.Find(searchPosition.ToTileCoordinates(), Searches.Chain(new Searches.Down(350), new Conditions.HasLava()), out Point result);
                result.Y += 4;

                if (inPhase2)
                    Utilities.NewProjectileBetter(result.ToWorldCoordinates(), Vector2.Zero, ModContent.ProjectileType<CursedGeyser>(), 60, 0f);
            }

            // Do nothing if no valid spawn solution is found.
            catch { }
        }

        internal static void PrepareFireBeam(NPC npc, Player target)
		{
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Vector2 aimDirection = npc.SafeDirectionTo(target.Center);
            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/CrystylCharge"), npc.Center);

            int fire = Utilities.NewProjectileBetter(npc.Center, aimDirection, ModContent.ProjectileType<FireBeamTelegraph>(), 0, 0f);
            if (Main.projectile.IndexInRange(fire))
                Main.projectile[fire].ai[1] = npc.whoAmI;
		}
        #endregion

        #region Eyes

        [OverrideAppliesTo(NPCID.WallofFleshEye, typeof(WallOfFleshAIClass), "WoFEyeAI", EntityOverrideContext.NPCAI)]
        public static bool WoFEyeAI(NPC npc)
		{
            ref float time = ref npc.ai[1];

            if (!Main.npc.IndexInRange(Main.wof))
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            Player target = Main.player[Main.npc[Main.wof].target];
            float destinationOffset = MathHelper.Clamp(npc.Distance(target.Center), 60f, 210f);
            destinationOffset += MathHelper.Lerp(0f, 215f, (float)Math.Sin(npc.whoAmI % 4f / 4f * MathHelper.Pi + time / 16f) * 0.5f + 0.5f);
            destinationOffset += npc.Distance(target.Center) * 0.1f;

            float destinationAngularOffset = MathHelper.Lerp(-1.5f, 1.5f, npc.ai[0]);
            destinationAngularOffset += (float)Math.Sin(time / 32f + npc.whoAmI % 4f / 4f * MathHelper.Pi) * 0.16f;

            Vector2 destination = Main.npc[Main.wof].Center;
            destination += Main.npc[Main.wof].velocity.SafeNormalize(Vector2.UnitX).RotatedBy(destinationAngularOffset) * destinationOffset;

            float maxSpeed = Utilities.AnyProjectiles(ModContent.ProjectileType<FireBeamWoF>()) ? 4f : 15f;

            npc.velocity = (destination - npc.Center).SafeNormalize(Vector2.Zero) * MathHelper.Min(npc.Distance(destination) * 0.5f, maxSpeed);
            if (!npc.WithinRange(Main.npc[Main.wof].Center, 750f))
                npc.Center = Main.npc[Main.wof].Center + Main.npc[Main.wof].DirectionTo(npc.Center) * 750f;

            npc.spriteDirection = 1;
            npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(target.Center), MathHelper.Pi * 0.1f);

            time++;

            int beamShootRate = 1600;
            if (time % beamShootRate == (beamShootRate + npc.whoAmI * 300) % beamShootRate)
                PrepareFireBeam(npc, target);

            return false;
		}
        #endregion

        #endregion

        #region Drawing

        [OverrideAppliesTo(NPCID.WallofFleshEye, typeof(WallOfFleshAIClass), "WoFEyePreDraw", EntityOverrideContext.NPCPreDraw)]
        public static bool WoFEyePreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
		{
            ref float verticalOffsetFactor = ref npc.ai[0];
            float yStart = MathHelper.Lerp(Main.wofB, Main.wofT, verticalOffsetFactor);
            Vector2 start = new Vector2(Main.npc[Main.wof].Center.X, yStart);

            Texture2D fleshRopeTexture = Main.chain12Texture;
            void drawChainFrom(Vector2 startingPosition)
            {
                Vector2 drawPosition = startingPosition;
                float rotation = npc.AngleFrom(drawPosition) - MathHelper.PiOver2;
                while (Vector2.Distance(drawPosition, npc.Center) > 40f)
                {
                    drawPosition += npc.DirectionFrom(drawPosition) * fleshRopeTexture.Height;
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 drawOffset = Vector2.UnitX.RotatedBy(rotation) * (float)Math.Cos(MathHelper.TwoPi * i / 4f) * 4f;
                        Color color = Lighting.GetColor((int)(drawPosition + drawOffset).X / 16, (int)(drawPosition + drawOffset).Y / 16);
                        spriteBatch.Draw(fleshRopeTexture, drawPosition + drawOffset - Main.screenPosition, null, color, rotation, fleshRopeTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
                    }
                }
            }

            drawChainFrom(start);
            for (int i = 0; i < Main.maxNPCs; i++)
			{
                if (Main.npc[i].type != NPCID.WallofFleshEye || !Main.npc[i].active || Main.npc[i].whoAmI == npc.whoAmI)
                    continue;

                // Draw order depends on index. Therefore, if the other index is greater than this one, that means it will draw
                // a chain of its own. This is done to prevent duplicates.
                if (Main.npc[i].whoAmI < npc.whoAmI)
                    drawChainFrom(Main.npc[i].Center);
			}
            return true;
		}
        #endregion
    }
}
