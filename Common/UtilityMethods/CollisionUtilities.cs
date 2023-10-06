using CalamityMod;
using InfernumMode.Common.Worldgen;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.WorldBuilding;

namespace InfernumMode
{
    public static partial class Utilities
    {
        public static Point GetGroundPositionFrom(Point p, GenSearch search = null)
        {
            search ??= new Searches.Down(9001);

            if (!WorldUtils.Find(p, Searches.Chain(search, new Conditions.IsSolid(), new CustomTileConditions.ActiveAndNotActuated()), out Point result))
                return result;
            return result;
        }

        public static Vector2 GetGroundPositionFrom(Vector2 v, GenSearch search = null)
        {
            search ??= new Searches.Down(9001);
            if (!WorldUtils.Find(v.ToTileCoordinates(), Searches.Chain(search, new Conditions.IsSolid(), new CustomTileConditions.ActiveAndNotActuated(), new CustomTileConditions.NotPlatform()), out Point result))
                return v;
            return result.ToWorldCoordinates();
        }

        public static bool RotatingHitboxCollision(this Entity entity, Vector2 targetTopLeft, Vector2 targetHitboxDimensions, Vector2? directionOverride = null)
        {
            Vector2 lineDirection = directionOverride ?? entity.velocity;

            // Ensure that the line direction is a unit vector.
            lineDirection = lineDirection.SafeNormalize(Vector2.UnitY);
            Vector2 start = entity.Center - lineDirection * entity.height * 0.5f;
            Vector2 end = entity.Center + lineDirection * entity.height * 0.5f;

            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetTopLeft, targetHitboxDimensions, start, end, entity.width, ref _);
        }

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

        public static bool ActualSolidCollisionTop(Vector2 topLeft, int width, int height)
        {
            int x = (int)(topLeft.X / 16f);
            int y = (int)(topLeft.Y / 16f);
            for (int i = x; i < x + width / 16; i++)
            {
                for (int j = y; j < y + height / 16; j++)
                {
                    Tile t = CalamityUtils.ParanoidTileRetrieval(i, j);
                    if (!t.HasUnactuatedTile)
                        continue;

                    if (t.TileType == TileID.Platforms || TileID.Sets.Platforms[t.TileType])
                        return true;
                }
            }

            bool halfCorrectCheck = Collision.SolidCollision(topLeft, width, height);
            return halfCorrectCheck;
        }
    }
}
