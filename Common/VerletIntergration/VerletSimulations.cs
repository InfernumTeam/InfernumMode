using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;

namespace InfernumMode.Common.VerletIntergration
{
    /// <summary>
    /// Contains various simulations for verlet chains.
    /// </summary>
    public static class VerletSimulations
    {
        public static List<VerletSimulatedSegmentInfernum> TileCollisionVerletSimulation(List<VerletSimulatedSegmentInfernum> segments, float segmentDistance, int loops = 10, float gravity = 0.3f)
        {
            // https://youtu.be/PGk0rnyTa1U?t=400 is a good verlet integration chains reference.
            List<int> groundHitSegments = new();
            for (int i = segments.Count - 1; i >= 0; i--)
            {
                var segment = segments[i];
                if (!segment.Locked)
                {
                    Vector2 positionBeforeUpdate = segment.Position;

                    // Disallow tile collision.
                    gravity *= Lerp(1f, 1.02f, i / (float)segments.Count);
                    float maxFallSpeed = 19f;
                    Vector2 gravityForce = Vector2.UnitY * gravity;

                    if (Collision.WetCollision(segment.Position, 1, 1))
                    {
                        gravityForce *= 0.4f;
                        maxFallSpeed *= 0.3f;
                    }

                    // Add gravity to the segment.
                    Vector2 newVelocity = segment.Velocity + gravityForce;
                    if (newVelocity.Y >= maxFallSpeed)
                        newVelocity.Y = maxFallSpeed;

                    Vector2 velocity = Collision.TileCollision(segment.Position, newVelocity, (int)segmentDistance, (int)segmentDistance);

                    if (velocity.Distance(newVelocity) >= 0.15f)
                    {
                        groundHitSegments.Add(i);
                        segment.Locked = true;
                    }

                    segment.Position += velocity;
                    segment.Velocity = velocity;

                    segment.OldPosition = positionBeforeUpdate;
                }
            }

            int segmentCount = segments.Count;

            for (int k = 0; k < loops; k++)
            {
                for (int j = 0; j < segmentCount - 1; j++)
                {
                    VerletSimulatedSegmentInfernum pointA = segments[j];
                    VerletSimulatedSegmentInfernum pointB = segments[j + 1];
                    Vector2 segmentCenter = (pointA.Position + pointB.Position) / 2f;
                    Vector2 segmentDirection = (pointA.Position - pointB.Position).SafeNormalize(Vector2.UnitY);

                    if (!pointA.Locked && !groundHitSegments.Contains(j))
                        pointA.Position = segmentCenter + segmentDirection * segmentDistance / 2f;

                    if (!pointB.Locked && !groundHitSegments.Contains(j + 1))
                        pointB.Position = segmentCenter - segmentDirection * segmentDistance / 2f;

                    segments[j] = pointA;
                    segments[j + 1] = pointB;
                }
            }

            return segments;
        }
    }
}
