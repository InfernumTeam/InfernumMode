using Microsoft.Xna.Framework;

namespace InfernumMode.Physics
{
    public class ProceduralAnimationSolver
    {
        // These are the constants laid out in the following equation, which this implementation seeks to numerically solve:
        // y + k1 * y' + k2 * y'' = x + k3 * x'
        public float OutputVelocityFactor;

        public float OutputAccelerationFactor;

        public float InputVelocityFactor;

        public Vector2 PreviousInput = Vector2.Zero;

        public Vector2 PreviousPosition = Vector2.Zero;

        public Vector2 PreviousVelocity = Vector2.Zero;

        public const float T = 0.016666f;

        public ProceduralAnimationSolver(Vector2 initialPosition)
        {
            PreviousInput= initialPosition;
            PreviousPosition = initialPosition;
        }
        
        public void UpdateState(Vector2 position)
        {
            Vector2 velocityStepEstimate = (position - PreviousInput) / T;
            PreviousInput = position;

            PreviousPosition += PreviousVelocity * T;
            PreviousVelocity += (position - InputVelocityFactor * velocityStepEstimate - PreviousPosition - OutputVelocityFactor * PreviousVelocity) / T;
        }
    }
}