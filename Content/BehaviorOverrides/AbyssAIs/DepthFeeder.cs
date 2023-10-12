using CalamityMod;
using CalamityMod.BiomeManagers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Utilities;

namespace InfernumMode.Content.BehaviorOverrides.AbyssAIs
{
    public class DepthFeeder : ModNPC
    {
        public bool HasCreatedSchool
        {
            get => NPC.ai[0] == 1f;
            set => NPC.ai[0] = value.ToInt();
        }

        public Player NearestPlayer => Main.player[NPC.target];

        public const int MinSchoolSize = 4;

        public const int MaxSchoolSize = 10;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Depth Feeder");
            Main.npcFrameCount[NPC.type] = 6;
            NPCID.Sets.CountsAsCritter[NPC.type] = true;
            NPCID.Sets.TakesDamageFromHostilesWithoutBeingFriendly[Type] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 0.1f;
            NPC.noGravity = true;
            NPC.lavaImmune = true;
            NPC.damage = 0;
            NPC.width = 24;
            NPC.height = 24;
            NPC.lifeMax = 15;
            NPC.aiStyle = -1;
            AIType = -1;
            NPC.value = 0;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.9f;
            NPC.chaseable = false;
            SpawnModBiomes = new int[3] { ModContent.GetInstance<AbyssLayer1Biome>().Type, ModContent.GetInstance<AbyssLayer2Biome>().Type, ModContent.GetInstance<AbyssLayer3Biome>().Type };
            NPC.waterMovementSpeed = 0f;
            NPC.Infernum().IsAbyssPrey = true;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement("Mods.InfernumMode.Bestiary.DepthFeeder")
            });
        }

        public override void AI()
        {
            NPC.noGravity = true;
            NPC.TargetClosest();
            NPC.Infernum().IsAbyssPrey = true;

            // Choose a direction.
            NPC.spriteDirection = (NPC.velocity.X > 0f).ToDirectionInt();

            // Create an initial school of fish if in water.
            // Fish spawned by this cannot create more fish.
            if (Main.netMode != NetmodeID.MultiplayerClient && !HasCreatedSchool && NPC.wet)
            {
                // Larger schools are made rarer by this exponent by effectively "squashing" randomness.
                float fishInterpolant = Pow(Main.rand.NextFloat(), 4f);
                int fishCount = (int)Lerp(MinSchoolSize, MaxSchoolSize, fishInterpolant);

                for (int i = 0; i < fishCount; i++)
                    NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, NPC.type, NPC.whoAmI, 1f);

                HasCreatedSchool = true;
                NPC.netUpdate = true;
                return;
            }

            // Sit helplessly if not in water.
            if (!NPC.wet)
            {
                if (Math.Abs(NPC.velocity.Y) < 0.45f)
                {
                    NPC.velocity.X *= 0.95f;
                    NPC.rotation = NPC.rotation.AngleLerp(0f, 0.15f).AngleTowards(0f, 0.15f);
                }
                NPC.noGravity = false;
                return;
            }

            Vector2 ahead = NPC.Center + NPC.velocity * 40f;
            bool aboutToLeaveWorld = ahead.X >= Main.maxTilesX * 16f - 700f || ahead.X < 700f;
            bool shouldTurnAround = aboutToLeaveWorld;
            for (float x = -0.47f; x < 0.47f; x += 0.06f)
            {
                Vector2 checkDirection = NPC.velocity.SafeNormalize(Vector2.Zero).RotatedBy(x);
                if (!Collision.CanHit(NPC.Center, 1, 1, NPC.Center + checkDirection * 125f, 1, 1) ||
                    !Collision.WetCollision(NPC.Center + checkDirection * 50f, NPC.width, NPC.height))
                {
                    shouldTurnAround = true;
                    break;
                }
            }

            // Avoid walls and exiting water.
            NPC closestPredator = NPC.FindClosestAbyssPredator(out _);
            if (shouldTurnAround && (closestPredator is null || !NPC.WithinRange(closestPredator.Center, 400f)))
            {
                float distanceToTileOnLeft = CalamityUtils.DistanceToTileCollisionHit(NPC.Center, NPC.velocity.RotatedBy(-PiOver2)) ?? 999f;
                float distanceToTileOnRight = CalamityUtils.DistanceToTileCollisionHit(NPC.Center, NPC.velocity.RotatedBy(PiOver2)) ?? 999f;
                float turnDirection = distanceToTileOnLeft > distanceToTileOnRight ? -1f : 1f;
                Vector2 idealVelocity = NPC.velocity.RotatedBy(PiOver2 * turnDirection);
                if (aboutToLeaveWorld)
                    idealVelocity = ahead.X >= Main.maxTilesX * 16f - 700f ? -Vector2.UnitX * 4f : Vector2.UnitX * 4f;

                NPC.velocity = NPC.velocity.MoveTowards(idealVelocity, 0.15f);
                NPC.velocity = Vector2.Lerp(NPC.velocity, idealVelocity, 0.15f);
            }
            else
                DoSchoolingMovement();

            // Move in some random direction if stuck.
            if (NPC.velocity == Vector2.Zero)
            {
                NPC.velocity = Main.rand.NextVector2CircularEdge(4f, 4f);
                NPC.netUpdate = true;
            }

            // Clamp velocities.
            NPC.velocity = NPC.velocity.ClampMagnitude(1.6f, 7f);
            if (NPC.velocity.Length() < 3.45f)
                NPC.velocity *= 1.024f;

            // Define rotation.
            NPC.rotation = NPC.velocity.ToRotation();
            if (NPC.spriteDirection == -1)
                NPC.rotation += Pi;
        }

        // Does schooling movement in conjunction with other sea minnows.
        // This is largely based on the boids algorithm.
        public void DoSchoolingMovement()
        {
            List<NPC> otherFish = Main.npc.Take(Main.maxNPCs).Where(n =>
            {
                bool nearbyAndInRange = n.WithinRange(NPC.Center, 1350f) && Collision.CanHitLine(NPC.Center, 1, 1, n.Center, 1, 1);
                return n.type == NPC.type && n.whoAmI != NPC.whoAmI && nearbyAndInRange;
            }).ToList();

            // Get the center of the flock position and move towards it.
            List<NPC> flockNeighbors = otherFish.Where(n => n.WithinRange(NPC.Center, 300f)).ToList();
            Vector2 centerOfFlock;
            if (flockNeighbors.Count > 0)
            {
                centerOfFlock = Vector2.Zero;
                foreach (NPC neighbor in flockNeighbors)
                    centerOfFlock += neighbor.Center;
                centerOfFlock /= flockNeighbors.Count;
            }
            else
                centerOfFlock = NPC.Center;

            float clockCenterMoveInterpolant = Utils.GetLerpValue(0f, 40f, NPC.Distance(centerOfFlock), true);
            NPC.velocity += NPC.SafeDirectionTo(centerOfFlock, -Vector2.UnitY) * clockCenterMoveInterpolant * 0.1f;

            // Align with other fish.
            List<NPC> alignmentNeighbors = otherFish.Where(n => n.WithinRange(NPC.Center, 300f)).ToList();
            Vector2 flockDirection;
            if (flockNeighbors.Count > 0)
            {
                flockDirection = Vector2.Zero;
                foreach (NPC neighbor in flockNeighbors)
                    flockDirection += neighbor.velocity;
                flockDirection /= flockNeighbors.Count;
            }
            else
                flockDirection = NPC.velocity.RotatedBy(Pi * 0.012f);

            // Angle towards the flock's current direction.
            NPC.velocity = NPC.velocity.ToRotation().AngleLerp(flockDirection.ToRotation(), 0.04f).ToRotationVector2() * NPC.velocity.Length();

            // Avoid close fish.
            List<NPC> closeNeighbors = otherFish.Where(n => n.WithinRange(NPC.Center, 100f)).ToList();
            if (flockNeighbors.Count > 0)
            {
                Vector2 avoidVelocity = Vector2.Zero;
                foreach (NPC neighbor in flockNeighbors)
                {
                    float avoidFactor = Utils.GetLerpValue(150f, 0f, NPC.Distance(neighbor.Center), true);
                    avoidVelocity -= NPC.SafeDirectionTo(neighbor.Center, Vector2.UnitX) * avoidFactor * 0.74f;
                }
                avoidVelocity /= flockNeighbors.Count;
                NPC.velocity += avoidVelocity;
            }

            // Avoid the closest player.
            if (NearestPlayer.active && !NearestPlayer.dead)
            {
                float playerAvoidanceInterpolant = Utils.GetLerpValue(250f, 100f, NPC.Distance(NearestPlayer.Center), true);
                NPC.velocity += NPC.SafeDirectionTo(NearestPlayer.Center) * playerAvoidanceInterpolant * -0.85f;
            }

            // Avoid predators.
            // This effect is dampened significantly the more transparent the predator is.
            NPC closestPredator = NPC.FindClosestAbyssPredator(out _);

            // If there was no predator NPC, avoid players, since they're big and scary.
            if (closestPredator != null)
            {
                float predatorAvoidanceInterpolant = Utils.GetLerpValue(400f, 250f, NPC.Distance(NearestPlayer.Center), true);
                predatorAvoidanceInterpolant *= Lerp(0.1f, 1f, closestPredator.Opacity);
                NPC.velocity += NPC.SafeDirectionTo(NearestPlayer.Center) * predatorAvoidanceInterpolant * -1.3f;
            }

            // Swim around idly.
            NPC.velocity = NPC.velocity.RotatedBy(Pi * (NPC.whoAmI % 2f == 0f).ToDirectionInt() * 0.004f);
        }

        public override void FindFrame(int frameHeight)
        {
            if (!NPC.wet && !NPC.IsABestiaryIconDummy)
            {
                NPC.frameCounter = 0.0;
                return;
            }
            NPC.frameCounter += 0.15f;
            NPC.frameCounter %= Main.npcFrameCount[NPC.type] - 2f;
            int frame = (int)NPC.frameCounter;

            if (!NPC.wet && !NPC.IsABestiaryIconDummy)
                frame = Main.npcFrameCount[NPC.type] - 2;

            NPC.frame.Y = frame * frameHeight;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glowmask = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/AbyssAIs/DepthFeederGlow").Value;
            Vector2 drawPosition = NPC.Center - screenPos;
            SpriteEffects direction = NPC.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Main.spriteBatch.Draw(texture, drawPosition, NPC.frame, NPC.GetAlpha(lightColor), NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, direction, 0f);
            Main.spriteBatch.Draw(glowmask, drawPosition, NPC.frame, NPC.GetAlpha(Color.White), NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, direction, 0f);
            return false;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            var calPlayer = spawnInfo.Player.Calamity();
            bool inFirst3Layers = calPlayer.ZoneAbyssLayer1 || calPlayer.ZoneAbyssLayer2 || calPlayer.ZoneAbyssLayer3;
            if (inFirst3Layers && spawnInfo.Water)
                return SpawnCondition.CaveJellyfish.Chance * 0.6f;
            return 0f;
        }
    }
}
