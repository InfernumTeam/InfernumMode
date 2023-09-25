using Microsoft.Xna.Framework;
using static CalamityMod.CalamityUtils;

namespace InfernumMode
{
    public static partial class Utilities
    {
        public static bool IsTileExposedToAir(int x, int y) => IsTileExposedToAir(x, y, out _);

        public static bool IsTileExposedToAir(int x, int y, out float? angleToOpenAir)
        {
            angleToOpenAir = null;
            if (!ParanoidTileRetrieval(x - 1, y).HasTile)
            {
                angleToOpenAir = Pi;
                return true;
            }
            if (!ParanoidTileRetrieval(x + 1, y).HasTile)
            {
                angleToOpenAir = 0f;
                return true;
            }
            if (!ParanoidTileRetrieval(x, y - 1).HasTile)
            {
                angleToOpenAir = PiOver2;
                return true;
            }
            if (!ParanoidTileRetrieval(x, y + 1).HasTile)
            {
                angleToOpenAir = -PiOver2;
                return true;
            }

            return false;
        }

        public static Rectangle ToWorldCoords(this Rectangle rectangle) => new(rectangle.X * 16, rectangle.Y * 16, rectangle.Width * 16, rectangle.Height * 16);

        public static Rectangle ToTileCoords(this Rectangle rectangle) => new(rectangle.X / 16, rectangle.Y / 16, rectangle.Width / 16, rectangle.Height / 16);
    }
}
