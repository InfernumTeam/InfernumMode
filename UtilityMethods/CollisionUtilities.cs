using Microsoft.Xna.Framework;
using Terraria;
using Terraria.WorldBuilding;

namespace InfernumMode
{
    public static partial class Utilities
    {
        public static Vector2 GetGroundPositionFrom(Vector2 v, GenSearch search = null)
        {
            if (search is null)
                search = new Searches.Down(9001);
            if (!WorldUtils.Find(v.ToTileCoordinates(), Searches.Chain(search, new Conditions.IsSolid()), out Point result))
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

        public static bool SolidCollision(Vector2 Position, int Width, int Height, bool acceptTopSurfaces)
        {
            int right = (int)((Position.X + Width) / 16f) + 2;
            int top = (int)(Position.Y / 16f) - 1;
            int bottom = (int)((Position.Y + Height) / 16f) + 2;
            int left = Utils.Clamp((int)(Position.X / 16f) - 1, 0, Main.maxTilesX - 1);
            right = Utils.Clamp(right, 0, Main.maxTilesX - 1);
            top = Utils.Clamp(top, 0, Main.maxTilesY - 1);
            bottom = Utils.Clamp(bottom, 0, Main.maxTilesY - 1);
            for (int i = left; i < right; i++)
            {
                for (int j = top; j < bottom; j++)
                {
                    Tile tile = Main.tile[i, j];
                    if (tile != null && tile.active() && !tile.inActive())
                    {
                        bool solidTile = Main.tileSolid[tile.type] && !Main.tileSolidTop[tile.type];
                        if (acceptTopSurfaces)
                        {
                            solidTile |= Main.tileSolidTop[tile.type] && tile.frameY == 0;
                        }
                        if (solidTile)
                        {
                            Vector2 v = new Vector2(i, j) * 16f;
                            int verticalLeniance = 16;
                            if (tile.halfBrick())
                            {
                                v.Y += 8f;
                                verticalLeniance -= 8;
                            }
                            if (Position.X + Width > v.X && Position.X < v.X + 16f && Position.Y + Height > v.Y && Position.Y < v.Y + verticalLeniance)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
