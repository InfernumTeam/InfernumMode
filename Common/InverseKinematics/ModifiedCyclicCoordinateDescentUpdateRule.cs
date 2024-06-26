﻿using Microsoft.Xna.Framework;
using Terraria;

namespace InfernumMode.Common.InverseKinematics
{
    public class ModifiedCyclicCoordinateDescentUpdateRule(float angularOffsetAcceleration, float angularDeviationLenience) : IInverseKinematicsUpdateRule
    {
        public float AngularOffsetAcceleration = angularOffsetAcceleration;

        public float AngularDeviationLenience = angularDeviationLenience;

        public void Update(LimbCollection limbs, Vector2 destination)
        {
            float distanceFromEnd = Vector2.Distance(destination, limbs.EndPoint);
            float slowdownInterpolant = Utils.GetLerpValue(4f, 35f, distanceFromEnd, true);

            Vector2 originalEndPoint = limbs.EndPoint;

            for (int i = limbs.Limbs.Length - 1; i >= 0; i--)
            {
                // Move based on angular offsets. Movement is dampened the closer a limb is to being the first limb.
                Vector2 currentToEndOffset = originalEndPoint - limbs.Limbs[i].ConnectPoint;
                Vector2 currentToDestinationOffset = destination - limbs.Limbs[i].ConnectPoint;
                Vector2 perpendicularDirection = currentToDestinationOffset.RotatedBy(PiOver2);
                float angularOffset = currentToEndOffset.AngleBetween(currentToDestinationOffset) * Sqrt((i + 1f) / limbs.Limbs.Length);

                // Determine direction by choosing the angle which approaches the destination faster.
                float leftAngularOffset = currentToEndOffset.AngleBetween(currentToDestinationOffset - perpendicularDirection);
                float rightAngularOffset = currentToEndOffset.AngleBetween(currentToDestinationOffset + perpendicularDirection);

                if (leftAngularOffset > rightAngularOffset)
                    angularOffset *= -1f;

                // Perform safety checks on the result of the underlying arccosines.
                if (float.IsNaN(angularOffset))
                    break;

                // Update rotation.
                limbs.Limbs[i].Rotation += angularOffset * slowdownInterpolant * AngularOffsetAcceleration;

                // And limit it so that it doesn't look weird.
                if (i > 0)
                {
                    float behindRotation = (float)limbs.Limbs[i - 1].Rotation;
                    limbs.Limbs[i].Rotation = Clamp((float)limbs.Limbs[i].Rotation, behindRotation - AngularDeviationLenience, behindRotation + AngularDeviationLenience);
                }

                if (limbs.Limbs[i].Rotation < 0f)
                    limbs.Limbs[i].Rotation += TwoPi;
            }
        }
    }
}
