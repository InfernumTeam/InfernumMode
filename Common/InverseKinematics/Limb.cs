using Microsoft.Xna.Framework;
using Terraria;

namespace InfernumMode.Common.InverseKinematics
{
    public class Limb(float rotation, float length)
    {
        // Doubles are used instead of floats as a means of providing sufficient precision to not cause erroneous results when
        // doing approximations of derivative limits with small divisors.
        public double Rotation = rotation;

        public double Length = length;

        public Vector2 ConnectPoint;

        public Vector2 EndPoint => ConnectPoint + ((float)Rotation).ToRotationVector2() * (float)Length;
    }
}
