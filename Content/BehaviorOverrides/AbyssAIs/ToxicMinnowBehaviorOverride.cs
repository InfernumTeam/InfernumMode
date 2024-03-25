using CalamityMod;
using CalamityMod.NPCs.Abyss;
using InfernumMode.Content.Tiles.Abyss;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.AbyssAIs
{
    public class ToxicMinnowBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<ToxicMinnow>();

        #region AI and Behaviors

        public const int MinSchoolSize = 4;

        public const int MaxSchoolSize = 9;

        public override bool PreAI(NPC npc)
        {
            npc.noGravity = true;
            npc.TargetClosest();
            npc.Infernum().IsAbyssPredator = true;
            NPCID.Sets.UsesNewTargetting[npc.type] = true;

            // Choose a direction.
            npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();

            // Create an initial school of fish if in water.
            // Fish spawned by this cannot create more fish.
            if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[0] == 0f && npc.wet)
            {
                Utilities.SpawnSchoolOfFish(npc, MinSchoolSize, MaxSchoolSize);
                return false;
            }

            // Sit helplessly if not in water.
            if (!npc.wet)
            {
                if (Math.Abs(npc.velocity.Y) < 0.45f)
                {
                    npc.velocity.X *= 0.95f;
                    npc.rotation = npc.rotation.AngleLerp(0f, 0.15f).AngleTowards(0f, 0.15f);
                }
                npc.noGravity = false;
                return false;
            }

            Vector2 ahead = npc.Center + npc.velocity * 40f;
            bool aboutToLeaveWorld = ahead.X >= Main.maxTilesX * 16f - 700f || ahead.X < 700f;
            bool shouldTurnAround = aboutToLeaveWorld;
            ref float hasFoundPlayer = ref npc.Infernum().ExtraAI[0];

            for (float x = -0.47f; x < 0.47f; x += 0.06f)
            {
                Vector2 checkDirection = npc.velocity.SafeNormalize(Vector2.Zero).RotatedBy(x);
                if (!Collision.CanHit(npc.Center, 1, 1, npc.Center + checkDirection * 125f, 1, 1) ||
                    !Collision.WetCollision(npc.Center + checkDirection * 50f, npc.width, npc.height))
                {
                    shouldTurnAround = true;
                    break;
                }
            }

            // Avoid walls and exiting water.
            if (shouldTurnAround)
                Utilities.TurnAroundBehavior(npc, ahead, shouldTurnAround);
            else
                DoSchoolingMovement(npc, ref hasFoundPlayer);

            // Move in some random direction if stuck.
            if (npc.velocity == Vector2.Zero)
            {
                npc.velocity = Main.rand.NextVector2CircularEdge(4f, 4f);
                npc.netUpdate = true;
            }

            // Clamp velocities.
            if (npc.velocity.Length() < 2f)
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * 2f;
            if (npc.velocity.Length() < 5.4f)
                npc.velocity *= 1.024f;
            if (npc.velocity.Length() > 10f)
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * 10f;

            // Define rotation.
            npc.rotation = npc.velocity.ToRotation();
            if (npc.spriteDirection == -1)
                npc.rotation += Pi;
            return false;
        }

        // Does schooling movement in conjunction with other sea minnows.
        // This is largely based on the boids algorithm.
        public static void DoSchoolingMovement(NPC npc, ref float hasFoundPlayer)
        {
            List<NPC> otherFish = Main.npc.Take(Main.maxNPCs).Where(n =>
            {
                bool nearbyAndInRange = n.WithinRange(npc.Center, 1350f) && Collision.CanHitLine(npc.Center, 1, 1, n.Center, 1, 1);
                return n.type == npc.type && n.whoAmI != npc.whoAmI && nearbyAndInRange;
            }).ToList();

            // Get the center of the flock position and move towards it.
            List<NPC> flockNeighbors = otherFish.Where(n => n.WithinRange(npc.Center, 300f)).ToList();
            Vector2 centerOfFlock;
            if (flockNeighbors.Count > 0)
            {
                centerOfFlock = Vector2.Zero;
                foreach (NPC neighbor in flockNeighbors)
                    centerOfFlock += neighbor.Center;
                centerOfFlock /= flockNeighbors.Count;
            }
            else
                centerOfFlock = npc.Center;

            float clockCenterMoveInterpolant = Utils.GetLerpValue(0f, 40f, npc.Distance(centerOfFlock), true);
            npc.velocity += npc.SafeDirectionTo(centerOfFlock, -Vector2.UnitY) * clockCenterMoveInterpolant * 0.1f;

            // Align with other fish.
            List<NPC> alignmentNeighbors = otherFish.Where(n => n.WithinRange(npc.Center, 360f)).ToList();
            Vector2 flockDirection;
            if (flockNeighbors.Count > 0)
            {
                flockDirection = Vector2.Zero;
                foreach (NPC neighbor in flockNeighbors)
                    flockDirection += neighbor.velocity;
                flockDirection /= flockNeighbors.Count;
            }
            else
                flockDirection = npc.velocity.RotatedBy(Pi * 0.013f);

            // Angle towards the flock's current direction.
            npc.velocity = npc.velocity.ToRotation().AngleLerp(flockDirection.ToRotation(), 0.04f).ToRotationVector2() * npc.velocity.Length();

            // Avoid close fish.
            List<NPC> closeNeighbors = otherFish.Where(n => n.WithinRange(npc.Center, 100f)).ToList();
            if (flockNeighbors.Count > 0)
            {
                Vector2 avoidVelocity = Vector2.Zero;
                foreach (NPC neighbor in flockNeighbors)
                {
                    float avoidFactor = Utils.GetLerpValue(150f, 0f, npc.Distance(neighbor.Center), true);
                    avoidVelocity -= npc.SafeDirectionTo(neighbor.Center, Vector2.UnitX) * avoidFactor * 0.74f;
                }
                avoidVelocity /= flockNeighbors.Count;
                npc.velocity += avoidVelocity;
            }

            float playerSearchDistance = hasFoundPlayer == 1f ? 900f : 200f;
            Utilities.TargetClosestAbyssPredator(npc, false, 675f, playerSearchDistance);
            NPCAimedTarget target = npc.GetTargetData();
            if (target.Type == NPCTargetType.Player && !npc.WithinRange(target.Center, playerSearchDistance))
                target.Type = NPCTargetType.None;

            if (target.Type == NPCTargetType.Player && hasFoundPlayer != 1f)
            {
                hasFoundPlayer = 1f;
                npc.netUpdate = true;
            }

            // Chase potential prey.
            if (!target.Invalid)
            {
                float targetAttractionInterpolant = Utils.GetLerpValue(250f, 100f, npc.Distance(target.Center), true);
                npc.velocity += npc.SafeDirectionTo(target.Center) * targetAttractionInterpolant * 1.5f;
            }

            // If there are no prey, search for kelp to stay near.
            else
            {
                int kelpID = ModContent.TileType<AbyssalKelp>();
                float minDistance = 99999999f;
                Point centerTileCoords = npc.Center.ToTileCoordinates();
                Vector2? kelpCenter = null;

                // Find the closest kelp position, if there is some nearby.
                for (int i = -25; i < 25; i++)
                {
                    for (int j = -25; j < 25; j++)
                    {
                        Tile t = Framing.GetTileSafely(centerTileCoords.X + i, centerTileCoords.Y + j);
                        if (!t.HasTile || t.TileType != kelpID)
                            continue;

                        Vector2 potentialKelpCenter = new Point(centerTileCoords.X + i, centerTileCoords.Y + j).ToWorldCoordinates();
                        float distanceToPotential = npc.DistanceSQ(potentialKelpCenter);
                        if (distanceToPotential < minDistance)
                        {
                            kelpCenter = potentialKelpCenter;
                            minDistance = distanceToPotential;
                        }
                    }
                }

                // Mmmmmmmm Kelp.........
                if (kelpCenter is not null)
                {
                    npc.velocity += npc.SafeDirectionTo(kelpCenter.Value) * 0.5f;
                    if (npc.WithinRange(kelpCenter.Value, 100f))
                        npc.velocity *= 0.95f;
                }
            }

            // Swim around idly.
            npc.velocity = npc.velocity.RotatedBy(Pi * (npc.whoAmI % 2f == 0f).ToDirectionInt() * 0.004f);
        }
        #endregion AI and Behaviors
    }
}
