using Microsoft.Xna.Framework;

namespace InfernumMode.InverseKinematics
{
    public class Limb
    {
        public Vector2 StartingPoint;
        public float Rotation;

        public Limb(Vector2 startingPoint, float rotation)
        {
            StartingPoint = startingPoint;
            Rotation = rotation;
        }
    }
}