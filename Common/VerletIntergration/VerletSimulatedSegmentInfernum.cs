using Microsoft.Xna.Framework;

namespace InfernumMode.Common.VerletIntergration
{
    /// <summary>
    /// Represents a simple verlet point.
    /// </summary>
    public class VerletSimulatedSegmentInfernum(Vector2 position, Vector2 velocity, bool locked = false)
    {
        public bool Locked = locked;

        public Vector2 Position = position;

        public Vector2 OldPosition = position;

        public Vector2 Velocity = velocity;
    }
}
