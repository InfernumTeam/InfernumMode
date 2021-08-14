using Microsoft.Xna.Framework;
using Terraria;

namespace InfernumMode
{
    public static partial class Utilities
    {
        public static bool CircularCollision(Vector2 checkPosition, Rectangle hitbox, float radius)
        {
            float dist1 = Vector2.Distance(checkPosition, hitbox.TopLeft());
            float dist2 = Vector2.Distance(checkPosition, hitbox.TopRight());
            float dist3 = Vector2.Distance(checkPosition, hitbox.BottomLeft());
            float dist4 = Vector2.Distance(checkPosition, hitbox.BottomRight());

            float minDist = dist1;
            if (dist2 < minDist)
                minDist = dist2;
            if (dist3 < minDist)
                minDist = dist3;
            if (dist4 < minDist)
                minDist = dist4;

            return minDist <= radius;
        }

        public static bool EllipseCollision(Vector2 checkPosition, Vector2 focus1, Vector2 focus2, float distanceConstant, out float distance)
        {
            float distance1 = Vector2.Distance(checkPosition, focus1);
            float distance2 = Vector2.Distance(checkPosition, focus2);
            distance = distance1 + distance2;
            return distance <= distanceConstant;
        }
    }
}
