using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;

namespace InfernumMode.Common
{
    public class VerletSimulatedSegmentInfernum
    {
        public bool locked;

        public Vector2 position;

        public Vector2 oldPosition;

        public Vector2 velocity;

        public VerletSimulatedSegmentInfernum(Vector2 _position, Vector2 velocity, bool locked = false)
        {
            this.locked = locked;
            position = _position;
            oldPosition = _position;
            this.velocity = velocity;
        }

        public static List<VerletSimulatedSegmentInfernum> TileCollisionVerletSimulation(List<VerletSimulatedSegmentInfernum> segments, float segmentDistance, int loops = 10, float gravity = 0.3f)
        {
            // https://youtu.be/PGk0rnyTa1U?t=400 is a good verlet integration chains reference.
            List<int> groundHitSegments = new();
            for (int i = segments.Count - 1; i >= 0; i--)
            {
                var segment = segments[i];
                if (!segment.locked)
                {
                    Vector2 positionBeforeUpdate = segment.position;

                    // Disallow tile collision.
                    gravity *= MathHelper.Lerp(1f, 1.02f, i / (float)segments.Count);
                    float maxFallSpeed = 19f;
                    Vector2 gravityForce = Vector2.UnitY * gravity;
                    if (Collision.WetCollision(segment.position, 1, 1))
                    {
                        gravityForce *= 0.4f;
                        maxFallSpeed *= 0.3f;
                    }

                    // Add gravity to the segment.
                    Vector2 newVelocity = segment.velocity + gravityForce;
                    if (newVelocity.Y >= maxFallSpeed)
                        newVelocity.Y = maxFallSpeed;

                    Vector2 velocity = Collision.TileCollision(segment.position, newVelocity, (int)segmentDistance, (int)segmentDistance);

                    if (velocity.Distance(newVelocity) >= 0.15f)
                    {
                        groundHitSegments.Add(i);
                        segment.locked = true;
                    }

                    segment.position += velocity;
                    segment.velocity = velocity;

                    segment.oldPosition = positionBeforeUpdate;
                }
            }

            int segmentCount = segments.Count;

            for (int k = 0; k < loops; k++)
            {
                for (int j = 0; j < segmentCount - 1; j++)
                {
                    VerletSimulatedSegmentInfernum pointA = segments[j];
                    VerletSimulatedSegmentInfernum pointB = segments[j + 1];
                    Vector2 segmentCenter = (pointA.position + pointB.position) / 2f;
                    Vector2 segmentDirection = (pointA.position - pointB.position).SafeNormalize(Vector2.UnitY);

                    if (!pointA.locked && !groundHitSegments.Contains(j))
                        pointA.position = segmentCenter + segmentDirection * segmentDistance / 2f;

                    if (!pointB.locked && !groundHitSegments.Contains(j + 1))
                        pointB.position = segmentCenter - segmentDirection * segmentDistance / 2f;

                    segments[j] = pointA;
                    segments[j + 1] = pointB;
                }
            }

            return segments;
        }
    }
}
