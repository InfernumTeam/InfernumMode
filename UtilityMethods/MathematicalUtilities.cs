using InfernumMode.MachineLearning;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Utilities;

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
            float fireAngleSine = gravity * horizontalRange / (float)Math.Pow(shootSpeed, 2);

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
        public static Vector2 ClampMagnitude(this Vector2 v, float min, float max) => v.SafeNormalize(Vector2.UnitY) * MathHelper.Clamp(v.Length(), min, max);

        public static Vector2 MoveTowards(this Vector2 currentPosition, Vector2 targetPosition, float maxAmountAllowedToMove)
        {
            Vector2 v = targetPosition - currentPosition;
            if (v.Length() < maxAmountAllowedToMove)
                return targetPosition;

            return currentPosition + v.SafeNormalize(Vector2.Zero) * maxAmountAllowedToMove;
        }

        /// <summary>
        /// Gives an approximation of a derivative of a function at a given point based on the limit (f(x+h) - f(x)) / h, with an extremely small value for h.
        /// </summary>
        /// <param name="fx">The function to derive.</param>
        /// <param name="x">The input.</param>
        public static double ApproximateDerivative(this Func<double, double> fx, double x)
        {
            double h = 1e-7;
            return (float)((fx(x + h) - fx(x)) / h);
        }

        /// <summary>
        /// Gives an approximation of a derivative of a function at a given point based on the limit (f(x+h) - f(x)) / h, with an extremely small value for h across an entire matrix.
        /// </summary>
        /// <param name="fx">The function.</param>
        /// <param name="matrix">The function input.</param>
        public static GeneralMatrix ApproximateDerivative(Func<double, double> fx, GeneralMatrix matrix)
        {
            GeneralMatrix result = new GeneralMatrix(matrix.TotalRows, matrix.TotalColumns);
            for (int i = 0; i < result.TotalRows; i++)
            {
                for (int j = 0; j < result.TotalColumns; j++)
                    result[i, j] = ApproximateDerivative(fx, matrix[i, j]);
            }
            return result;
        }

        /// <summary>
        /// Approximates the partial derivative of a function at a given input for a specific variable.
        /// </summary>
        /// <param name="fxy">The function.</param>
        /// <param name="x">The function input.</param>
        public static double ApproximatePartialDerivative(this Func<double, double, double> fxy, double x, double y, int term)
        {
            switch (term)
            {
                case 0:
                    return (fxy(x + 1e-7, y) - fxy(x, y)) * 1e7;
                case 1:
                    return (fxy(x, y + 1e-7) - fxy(x, y)) * 1e7;
            }
            return 0D;
        }

        /// <summary>
        /// Approximates the partial derivative of a function at a given input for a specific variable.
        /// </summary>
        /// <param name="fx">The function.</param>
        /// <param name="m1">The first function input.</param>
        /// <param name="m2">The second function input.</param>
        public static GeneralMatrix ApproximatePartialDerivative(Func<double, double, double> fx, GeneralMatrix m1, GeneralMatrix m2, int term)
        {
            GeneralMatrix result = new GeneralMatrix(m1.TotalRows, m1.TotalColumns);
            for (int i = 0; i < result.TotalRows; i++)
            {
                for (int j = 0; j < result.TotalColumns; j++)
                    result[i, j] = ApproximatePartialDerivative(fx, m1[i, j], m2[i, j], term);
            }
            return result;
        }

        /// <summary>
        /// Returns a number between a minimum and maximum range.
        /// </summary>
        /// <param name="rng">The random number generator.</param>
        /// <param name="min">The lower bound for randomness.</param>
        /// <param name="max">The upper bound for randomness.</param>
        public static double NextRange(this UnifiedRandom rng, double min, double max) => rng.NextDouble() * (max - min) + min;

        /// <summary>
        /// Generates a 2D array of an arbitrary width and height and randomizes it with upper and lower bounds.
        /// </summary>
        /// <param name="rng">The random number generator.</param>
        /// <param name="width">The width of the array.</param>
        /// <param name="height">The height of the array.</param>
        /// <param name="min">The lower bound for randomness.</param>
        /// <param name="max">The upper bound for randomness.</param>
        public static GeneralMatrix GenerateRandomMatrix(this UnifiedRandom rng, int width, int height, double min, double max)
        {
            GeneralMatrix result = new GeneralMatrix(width, height);

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                    result[i, j] = rng.NextRange(min, max);
            }

            return result;
        }

        /// <summary>
        /// Applies a specific function to the indices of a generalized matrix.
        /// </summary>
        /// <param name="matrix">The matrix to apply the function to.</param>
        /// <param name="function">The function.</param>
        public static GeneralMatrix ApplyFunctionToMatrix(GeneralMatrix matrix, Func<double, double> function)
        {
            GeneralMatrix result = new GeneralMatrix(matrix.TotalRows, matrix.TotalColumns);
            for (int i = 0; i < result.TotalRows; i++)
            {
                for (int j = 0; j < result.TotalColumns; j++)
                    result[i, j] = function(matrix[i, j]);
            }
            return result;
        }

        /// <summary>
        /// Performs a logistic sigmoid function on an input.
        /// </summary>
        /// <param name="x">The input.</param>
        public static double Sigmoid(double x) => 1D / (1D + Math.Exp(-x));

        /// <summary>
        /// Gets the index of the number with the highest value in a collection of numbers.
        /// </summary>
        /// <param name="values">The collection of numbers.</param>
        public static int Argmax(double[] values)
        {
            int index = -1;
            double min = double.NegativeInfinity;

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] > min)
                {
                    index = i;
                    min = values[i];
                }
            }
            return index;
        }

        public static float Remap(float fromValue, float fromMin, float fromMax, float toMin, float toMax, bool clamped = true)
        {
            return MathHelper.Lerp(toMin, toMax, Utils.InverseLerp(fromMin, fromMax, fromValue, clamped));
        }
    }
}
