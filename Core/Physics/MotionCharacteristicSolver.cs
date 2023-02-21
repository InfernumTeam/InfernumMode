using Microsoft.Xna.Framework;
using System;

namespace InfernumMode.Core.Physics
{
    // This attempts to numerically solve (via the semi-implicit Euler Method) second-order systems of the form:
    // y + k1 * y' + k2 * y'' = x + k3 * x'
    // This allows us to perform cool spline-like interpolation effects without the need for starting and ending points.
    // The following video gives some good insight into the mechanics of it:
    // https://www.youtube.com/watch?v=KPoeNZZ6H4s
    public class MotionCharacteristicSolver
    {
        // State variables of the system.
        public Vector2 PreviousInputPosition
        {
            get;
            private set;
        }

        public Vector2 PreviousOutputPosition
        {
            get;
            private set;
        }

        public Vector2 PreviousOutputVelocity
        {
            get;
            private set;
        }

        // Coefficients as defined in the system definition, as well as necessary for pole-matching equations.
        public float K1
        {
            get;
            private set;
        }

        public float K2
        {
            get;
            private set;
        }

        public float K3
        {
            get;
            private set;
        }

        public float W
        {
            get;
            private set;
        }

        public float Zeta
        {
            get;
            private set;
        }

        public float D
        {
            get;
            private set;
        }

        public bool UsesVeryFastSpeeds
        {
            get;
            private set;
        }

        public static float TimeStep => 1f / 20f;

        // Frequency is a measure of the speed at which the system responds to input changes.

        // The damping coefficient determines how the system settles to the ideal state. When it is zero, there is no dampening, and
        // the result basically degenerates into an infinite oscillation, which is not ideal. Values between zero and one will naturally damp
        // towards the ideal state at a degree dependent on the size of the number. Values above one do not have any vibrating dampening motion at
        // all, and simply approaches the ideal state at a degree that is slower the higher the value is.

        // When the response sharpness is greater than zero it immediately approaches the ideal state, overshooting if it's greater than one.
        // When it's less than zero it anticipates a bit before moving towards the ideal state.
        public MotionCharacteristicSolver(Vector2 startingPosition, bool usesVeryFastSpeeds, float frequency, float dampingCoefficient, float responseSharpness)
        {
            // Form the variables into a way that's workable with the k1/2/3 coefficients.
            K1 = dampingCoefficient / MathHelper.Pi / frequency;
            K2 = 1f / (float)Math.Pow(MathHelper.TwoPi * dampingCoefficient, 2D);
            K3 = responseSharpness * dampingCoefficient / MathHelper.TwoPi / frequency;
            W = MathHelper.TwoPi * frequency;
            Zeta = dampingCoefficient;
            D = W * (float)Math.Sqrt(Math.Abs(dampingCoefficient * dampingCoefficient - 1f));

            // Determines whether pole-matching should be used for stability on high speeds.
            UsesVeryFastSpeeds = usesVeryFastSpeeds;

            // Initialize states.
            PreviousInputPosition = startingPosition;
            PreviousOutputPosition = startingPosition;
            PreviousOutputVelocity = Vector2.Zero;
        }

        public void Update(Vector2 position, Vector2? velocity = null)
        {
            // Estimate velocity by taking a linear, single-frame step if nothing is inputted.
            if (velocity is null)
            {
                Vector2 positionOffset = position - PreviousInputPosition;
                velocity = positionOffset / TimeStep;
                PreviousInputPosition = position;
            }

            // Estimate the next moment of acceleration with a linear single-frame step.
            // Since we don't have a defined value for the acceleration with can achieve an estimate like such:

            // y'[n + 1] = y'[n] + T * y'' (The step definition)
            // y + k1 * y' + k2 * y'' = x + k3 * x' (The system definitions)

            // We need to rewrite the system definition to solve for y''. This is done as follows:
            // y'' = (x + k3 * x' - y - k1 * y') / k2
            // We will use this as a substitute in the step definition.
            Vector2 accelerationSubstitute = (position + K3 * velocity.Value - PreviousOutputPosition - K1 * PreviousOutputVelocity) / K2;

            // All of this is well and good, but you may notice that this a feedback loop, which means that the state can easily "explode" due to
            // accumulated errors and quickly shot off to NaNs, supposedly due to some "magnitude" being too high (similar to how repeatedly multiply numbers greater than one results in explosions).
            // Fortunately, we can represent all of this in the form of a matrix, and use the eigenvalues from it as a way of intuitively extracting this "magnitude" and determining if
            // stability checks are necessary.
            // The precise math of why this works can be found in the video at the top of this cs file; writing it out mathematically in this comment would be a serious headache.
            float k2Stable;
            if (!UsesVeryFastSpeeds || W * TimeStep < Zeta)
                k2Stable = MathHelper.Max(MathHelper.Max(K2, TimeStep * K1), TimeStep * TimeStep * 0.5f + TimeStep * K1 * 0.5f);

            else
            {
                float t1 = (float)Math.Exp(-Zeta * W * TimeStep);
                float alpha = 2f * t1;
                if (Zeta <= 1f)
                    alpha *= (float)Math.Cos(TimeStep * D);
                else
                    alpha *= (float)Math.Cosh(TimeStep * D);
                float beta = t1 * t1;
                float t2 = TimeStep / (1f + beta - alpha);
                k2Stable = TimeStep * t2;
            }

            float maxTimeStepMagnitude = (float)Math.Sqrt(k2Stable * 4f + K1 * K1) - K1;
            int iterations = (int)Math.Ceiling(TimeStep / maxTimeStepMagnitude);
            float timeStep = TimeStep / iterations;

            // Estimate the next moment of position and velocity with linear single-frame steps.
            for (int i = 0; i < iterations; i++)
            {
                PreviousOutputPosition += timeStep * PreviousOutputVelocity;
                PreviousOutputVelocity += timeStep * accelerationSubstitute;
            }
        }
    }
}
