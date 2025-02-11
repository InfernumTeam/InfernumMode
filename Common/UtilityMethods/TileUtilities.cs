using CalamityMod.Items.Tools.ClimateChange;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;

namespace InfernumMode
{
    public static partial class Utilities
    {
        public static bool IsTileExposedToAir(int x, int y) => IsTileExposedToAir(x, y, out _);

        public static bool IsTileExposedToAir(int x, int y, out float? angleToOpenAir)
        {
            angleToOpenAir = null;
            if (!Framing.GetTileSafely(x - 1, y).HasTile)
            {
                angleToOpenAir = Pi;
                return true;
            }
            if (!Framing.GetTileSafely(x + 1, y).HasTile)
            {
                angleToOpenAir = 0f;
                return true;
            }
            if (!Framing.GetTileSafely(x, y - 1).HasTile)
            {
                angleToOpenAir = PiOver2;
                return true;
            }
            if (!Framing.GetTileSafely(x, y + 1).HasTile)
            {
                angleToOpenAir = -PiOver2;
                return true;
            }

            return false;
        }

        public static Rectangle ToWorldCoords(this Rectangle rectangle) => new(rectangle.X * 16, rectangle.Y * 16, rectangle.Width * 16, rectangle.Height * 16);

        public static Rectangle ToTileCoords(this Rectangle rectangle) => new(rectangle.X / 16, rectangle.Y / 16, rectangle.Width / 16, rectangle.Height / 16);

        public static void StartRain(bool torrentialTear = false, bool maxSeverity = false, bool worldSync = true)
        {
            int framesInDay = 86400;
            int framesInHour = framesInDay / 24;
            Main.rainTime = Main.rand.Next(framesInHour * 8, framesInDay);
            if (Main.rand.NextBool(3))
            {
                Main.rainTime += Main.rand.Next(0, framesInHour);
            }
            if (Main.rand.NextBool(4))
            {
                Main.rainTime += Main.rand.Next(0, framesInHour * 2);
            }
            if (Main.rand.NextBool(5))
            {
                Main.rainTime += Main.rand.Next(0, framesInHour * 2);
            }
            if (Main.rand.NextBool(6))
            {
                Main.rainTime += Main.rand.Next(0, framesInHour * 3);
            }
            if (Main.rand.NextBool(7))
            {
                Main.rainTime += Main.rand.Next(0, framesInHour * 4);
            }
            if (Main.rand.NextBool(8))
            {
                Main.rainTime += Main.rand.Next(0, framesInHour * 5);
            }
            float randRainExtender = 1f;
            if (Main.rand.NextBool())
            {
                randRainExtender += 0.05f;
            }
            if (Main.rand.NextBool(3))
            {
                randRainExtender += 0.1f;
            }
            if (Main.rand.NextBool(4))
            {
                randRainExtender += 0.15f;
            }
            if (Main.rand.NextBool(5))
            {
                randRainExtender += 0.2f;
            }
            Main.rainTime = (int)(Main.rainTime * randRainExtender);
            Main.raining = true;
            if (torrentialTear)
                TorrentialTear.AdjustRainSeverity(maxSeverity);

            if (worldSync)
                CalamityNetcode.SyncWorld();
        }
    }
}
