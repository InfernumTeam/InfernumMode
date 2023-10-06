using Microsoft.Xna.Framework;

namespace InfernumMode.Common.VerletIntergration
{
    /// <summary>
    /// Represents a simple verlet point.
    /// </summary>
    public class VerletSimulatedSegmentInfernum
    {
        public bool Locked;

        public Vector2 Position;

        public Vector2 OldPosition;

        public Vector2 Velocity;

        public VerletSimulatedSegmentInfernum(Vector2 position, Vector2 velocity, bool locked = false)
        {
            Locked = locked;
            Position = position;
            OldPosition = position;
            Velocity = velocity;
        }
    }
}
