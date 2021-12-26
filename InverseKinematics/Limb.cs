using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;

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

        // I'm going to shoot someone.
        public static void UpdateRotationOfDualSet(Limb[] limbs, int direction, Vector2 endPosition)
        {
            float horizontalTotalDistance = endPosition.X - limbs[0].StartingPoint.X;
            float verticalTotalDistance = endPosition.Y - limbs[0].StartingPoint.Y;
            float horizontalTotalLengthSquared = (float)Math.Pow(horizontalTotalDistance, 2D);
            float verticalTotalLengthSquared = (float)Math.Pow(verticalTotalDistance, 2D);
            float firstLineLength = Vector2.Distance(limbs[0].StartingPoint, limbs[1].StartingPoint);
            float secondLineLength = Vector2.Distance(limbs[1].StartingPoint, endPosition);
            float firstLineLengthSquared = (float)Math.Pow(firstLineLength, 2D);
            float secondLineLengthSquared = (float)Math.Pow(secondLineLength, 2D);

            limbs[1].Rotation = (float)Math.Acos((horizontalTotalLengthSquared + verticalTotalLengthSquared - firstLineLengthSquared - secondLineLengthSquared) / (firstLineLength * secondLineLength * 2f));

            limbs[0].Rotation = (float)Math.Atan(verticalTotalDistance / horizontalTotalDistance) * -direction;
            limbs[0].Rotation -= (float)Math.Atan(secondLineLength * (float)Math.Sin(limbs[1].Rotation) / (firstLineLength + secondLineLength * (float)Math.Cos(limbs[1].Rotation)));
        }

        public void SendData(BinaryWriter writer)
		{
            writer.WritePackedVector2(StartingPoint);
            writer.Write(Rotation);
		}

        public void ReceiveData(BinaryReader reader)
		{
            StartingPoint = reader.ReadPackedVector2();
            Rotation = reader.ReadSingle();
		}
    }
}