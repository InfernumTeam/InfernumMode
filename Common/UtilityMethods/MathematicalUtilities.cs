using CalamityMod;
using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace InfernumMode
{
    public static partial class Utilities
    {
        /// <summary>
        /// A simplified variation of AngleTowards.
        /// </summary>
        /// <param name="originalRotation">The original angle to adjust.</param>
        /// <param name="idealRotation">The ideal rotation.</param>
        /// <param name="maxChange">The maximum angular increment to make to approach the destination.</param>
        public static float SimpleAngleTowards(this float originalRotation, float idealRotation, float maxChange)
        {
            float difference = MathHelper.WrapAngle(idealRotation - originalRotation);
            difference = MathHelper.Clamp(difference, -maxChange, maxChange);

            return originalRotation + difference;
        }

        /// <summary>
        /// Smoothly interpolates an angle between two bounds.
        /// </summary>
        /// <param name="angle">The angular interpolant.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        public static float AngularSmoothstep(float angle, float min, float max) => ((max - min) * ((float)Math.Cos(angle) * 0.5f)) + min + ((max - min) * 0.5f);
        
        /// <summary>
        /// Determines the angular distance between two vectors based on dot product comparisons. This method ensures underlying normalization is performed safely.
        /// </summary>
        /// <param name="v1">The first vector.</param>
        /// <param name="v2">The second vector.</param>
        public static float AngleBetween(this Vector2 v1, Vector2 v2) => (float)Math.Acos(Vector2.Dot(v1.SafeNormalize(Vector2.Zero), v2.SafeNormalize(Vector2.Zero)));

        /// <summary>
        /// Uses a rewritten horizontal range formula to determine the direction to fire a projectile in order for it to hit a destination. Falls back on a certain value if no such direction can exist. If no fallback is provided, a clamp is used.
        /// </summary>
        /// <param name="shootingPosition">The starting position of the projectile.</param>
        /// <param name="destination">The destination for the projectile to land at.</param>
        /// <param name="gravity">The gravity of the projectile.</param>
        /// <param name="shootSpeed">The magnitude </param>
        /// <param name="nanFallback">The direction to fall back to if the calculations result in any NaNs. If nothing is specified, a clamp is performed to prevent any chance of NaNs at all.</param>
        public static Vector2 GetProjectilePhysicsFiringVelocity(Vector2 shootingPosition, Vector2 destination, float gravity, float shootSpeed, out float fireAngle, Vector2? nanFallback = null)
        {
            // Ensure that the gravity has the right sign for Terraria's coordinate system.
            gravity = -Math.Abs(gravity);

            float horizontalRange = MathHelper.Distance(shootingPosition.X, destination.X);
            float fireAngleSine = gravity * horizontalRange / (float)Math.Pow(shootSpeed, 2D);

            // Clamp the sine if no fallback is provided.
            if (nanFallback is null)
                fireAngleSine = MathHelper.Clamp(fireAngleSine, -1f, 1f);

            fireAngle = (float)Math.Asin(fireAngleSine) * 0.5f;

            // Get out of here if no valid firing angle exists. This can only happen if a fallback does indeed exist.
            if (float.IsNaN(fireAngle))
                return nanFallback.Value * shootSpeed;

            Vector2 fireVelocity = new Vector2(0f, -shootSpeed).RotatedBy(fireAngle);
            fireVelocity.X *= (destination.X - shootingPosition.X < 0).ToDirectionInt();
            return fireVelocity;
        }

        /// <summary>
        /// Gets a unit direction towards an arbitrary destination for an entity based on its center. Has <see cref="float.NaN"/> safety in the form of a fallback vector.
        /// </summary>
        /// <param name="entity">The entity to check from.</param>
        /// <param name="destination">The destination to get the direction to.</param>
        /// <param name="fallback">A fallback value to use in the event of an unsafe normalization.</param>
        public static Vector2 SafeDirectionTo(this Entity entity, Vector2 destination, Vector2 fallback = default) => (destination - entity.Center).SafeNormalize(fallback);

        /// <summary>
        /// Calculates a smoothstep of a variant with degree 11. Considerably more expensive than a traditional smoothstep, but also far smoother.
        /// </summary>
        /// <param name="x">The input to the smoothstep. Clamped between 0 and 1.</param>
        public static float UltrasmoothStep(float x)
        {
            x = MathHelper.Clamp(x, 0f, 1f);
            return MathHelper.SmoothStep(0f, 1f, MathHelper.SmoothStep(0f, 1f, x));
        }

        /// <summary>
        /// Rotates a vector's direction towards an ideal angle at a specific incremental rate. Can be returned as a unit vector.
        /// </summary>
        /// <param name="originalVector">The origina vector to turn.</param>
        /// <param name="idealAngle">The ideal direction to approach.</param>
        /// <param name="angleIncrement">The maximum angular increment to make to approach the destination.</param>
        /// <param name="returnUnitVector">Whether the vector should be returned as unit vector or not.</param>
        public static Vector2 RotateTowards(this Vector2 originalVector, float idealAngle, float angleIncrement, bool returnUnitVector = false)
        {
            Vector2 newDirection = originalVector.ToRotation().AngleTowards(idealAngle, angleIncrement).ToRotationVector2();
            if (!returnUnitVector)
                return newDirection * originalVector.Length();
            return newDirection;
        }

        /// <summary>
        /// Rotates towards a given vector direction-wise. Magnitude is maintained.
        /// </summary>
        /// <param name="originalVector">The original vector.</param>
        /// <param name="idealVector">The ideal vector to approach.</param>
        /// <param name="interpolant">The interpolant.</param>
        public static Vector2 AngleDirectionLerp(this Vector2 originalVector, Vector2 idealVector, float interpolant)
        {
            float offsetAngle = originalVector.AngleBetween(idealVector) * MathHelper.Clamp(interpolant, 0f, 1f);
            return originalVector.RotateTowards(idealVector.ToRotation(), offsetAngle);
        }

        /// <summary>
        /// Clamps the magnitude of a vector via safe normalization.
        /// </summary>
        /// <param name="v">The vector.</param>
        /// <param name="min">The minimum magnitude.</param>
        /// <param name="max">The maximum magnitude.</param>
        public static Vector2 ClampMagnitude(this Vector2 v, float min, float max)
        {
            Vector2 result = v.SafeNormalize(Vector2.UnitY) * MathHelper.Clamp(v.Length(), min, max);
            if (result.HasNaNs())
                return Vector2.UnitY * -min;
            
            return result;
        }

        public static Vector2 MoveTowards(this Vector2 currentPosition, Vector2 targetPosition, float maxAmountAllowedToMove)
        {
            Vector2 v = targetPosition - currentPosition;
            if (v.Length() < maxAmountAllowedToMove)
                return targetPosition;

            return currentPosition + v.SafeNormalize(Vector2.Zero) * maxAmountAllowedToMove;
        }

        public static int Factorial(int n)
        {
            if (n <= 1)
                return 1;

            int sum = n;
            int result = n;

            for (int i = n - 2; i > 1; i -= 2)
            {
                sum += i;
                result *= sum;
            }

            if (n % 2 != 0)
                result *= n / 2 + 1;

            return result;
        }

        public static int NumberOfCombinations(int sizeOfSet, int totalToSelect) =>
            Factorial(sizeOfSet) / (Factorial(totalToSelect) * Factorial(sizeOfSet - totalToSelect));

        /// <summary>
        /// Returns a 0-1 value based on an easing curve.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static float EaseInBounce(float value) => 1f - EaseOutBounce(1f - value);

        /// <summary>
        /// Returns a 0-1 value based on an easing curve.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static float EaseOutBounce(float value)
        {
            float n1 = 7.5625f;
            float d1 = 2.75f;

            if (value < 1f / d1) {
                return n1 * value * value;
            } else if (value < 2f / d1) {
                return n1 * (value -= 1.5f / d1) * value + 0.75f;
            } else if (value < 2.5f / d1)
            {
                return n1 * (value -= 2.25f / d1) * value + 0.9375f;
            }
            else
            {
                return n1 * (value -= 2.625f / d1) * value + 0.984375f;
            }
        }

        /// <summary>
        /// Returns a 0-1 value based on an easing curve.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static float EaseInOutCubic(float value)
        {
            return value < 0.5f ?
                4f * value * value * value * value :
                1f - MathF.Pow(-2f * value + 2f, 3f) / 2f;
        }

        public static float EndingHeight(this CalamityUtils.CurveSegment segment) => segment.startingHeight + segment.elevationShift;
    }
}
