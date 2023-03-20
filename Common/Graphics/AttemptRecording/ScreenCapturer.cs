using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using SystemGraphics = System.Drawing.Graphics;
using SystemRectangle = System.Drawing.Rectangle;

namespace InfernumMode.Common.Graphics
{
#pragma warning disable CA1416 // Validate platform compatibility
    public class ScreenCapturer : ModSystem
    {
        private static IntPtr desktopWindowHandle;

        private static IntPtr deviceContextHandle;

        private static IntPtr compatibleDevideContextHandle;

        private static int captureWidth;

        private static int captureHeight;

        private static List<Bitmap> frames;

        // Bit flags for specifying how bitmap data should be copied in unmanaged code.
        private const int SourceCopy = 0x00CC0020;

        private const int CaptureBitBlit = 0x40000000;

        public static int RecordCountdown
        {
            get;
            set;
        }

        // If on a non-Windows operating system, don't use this system. Fundamental parts are not guaranteed to work outside of it.
        // Also it doesn't run on servers, obviously.
        public static bool IsSupported => Main.netMode != NetmodeID.Server && Environment.OSVersion.Platform == PlatformID.Win32NT;

        public static int CaptureWidth
        {
            get => captureWidth;
            set
            {
                if (captureWidth != value)
                {
                    RegenerateHandles();
                    captureWidth = value;
                }
            }
        }

        public static int CaptureHeight
        {
            get => captureHeight;
            set
            {
                if (captureHeight != value)
                {
                    RegenerateHandles();
                    captureHeight = value;
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr windowHandle);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr windowHandle, IntPtr deviceContextHandle);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr deviceContextHandle);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr deviceContextHandle, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr deviceContextHandle, IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr deviceContextHandleDestination, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr deviceContextHandleSrc, int nXSrc, int nYSrc, uint dwRop);

        [DllImport("gdi32.dll")]
        private static extern int DeleteDC(IntPtr deviceContextHandle);

        [DllImport("gdi32.dll")]
        private static extern int DeleteObject(IntPtr objectHandle);

        public override void OnModLoad()
        {
            if (!IsSupported)
                return;

            RegenerateHandles();

            frames = new();
            Main.OnPostDraw += HandleRecordingFrame;
        }

        public override void OnModUnload()
        {
            FreeHandles();
        }

        private static void RegenerateHandles()
        {
            if (!IsSupported)
                return;

            // Discard now-unusable recording data due to the change in image size.
            RecordCountdown = 0;
            ClearFrames();

            FreeHandles();

            // Get the handle to the desktop window.
            desktopWindowHandle = GetDesktopWindow();

            // Create a device context for this process.
            deviceContextHandle = GetDC(desktopWindowHandle);

            // Create a compatible device context for the screen.
            compatibleDevideContextHandle = CreateCompatibleDC(deviceContextHandle);
        }

        private static void FreeHandles()
        {
            if (!IsSupported)
                return;

            if (compatibleDevideContextHandle != IntPtr.Zero)
                _ = DeleteDC(compatibleDevideContextHandle);

            if (desktopWindowHandle != IntPtr.Zero)
                _ = ReleaseDC(desktopWindowHandle, deviceContextHandle);
        }

        private void HandleRecordingFrame(GameTime obj)
        {
            // Resize the capture window as necessary.
            CaptureWidth = (int)(Main.screenWidth * 0.45f);
            CaptureHeight = (int)(Main.screenHeight * 0.45f);

            if (RecordCountdown >= 1)
            {
                // Append to the frames. For the sake of performance this happens on frame intervals instead of every frame.
                if (RecordCountdown % 3 == 0)
                    frames.Add(GetScreenBitmap());
                RecordCountdown--;

                // Prepare the gif file once the recording countdown is done.
                if (RecordCountdown <= 0)
                    new Thread(CreateGif).Start();
            }
        }

        private static Bitmap GetScreenBitmap()
        {
            if (!IsSupported)
                return null;

            int x = (Main.instance.GraphicsDevice.Viewport.Width - CaptureWidth) / 2;
            int y = (Main.instance.GraphicsDevice.Viewport.Height - CaptureHeight) / 2;

            // Create a compatible bitmap section that can be captured by the device.
            IntPtr bitmapScreenHandle = CreateCompatibleBitmap(deviceContextHandle, CaptureWidth, CaptureHeight);

            // Select the bitmap into the compatible device context.
            IntPtr oldBitmap = SelectObject(compatibleDevideContextHandle, bitmapScreenHandle);

            // Copy the specified region of the desktop to the bitmap.
            bool success = BitBlt(compatibleDevideContextHandle, 0, 0, CaptureWidth, CaptureHeight, deviceContextHandle, x, y, SourceCopy | CaptureBitBlit);

            // If the copy was successful, create a new bitmap from the handle.
            if (success)
            {
                Bitmap bitmap = Image.FromHbitmap(bitmapScreenHandle);

                // Clean up unmanaged resources.
                SelectObject(compatibleDevideContextHandle, oldBitmap);
                _ = DeleteObject(bitmapScreenHandle);
                return bitmap;
            }

            // If the copy failed, return null.
            else
            {
                // Clean up the GDI objects
                SelectObject(compatibleDevideContextHandle, oldBitmap);
                _ = DeleteObject(bitmapScreenHandle);
                return null;
            }
        }

        private static void ClearFrames()
        {
            while (frames?.Any() ?? false)
            {
                frames.First().Dispose();
                frames.RemoveAt(0);
            }
        }

        private static void CreateGif()
        {
            if (!IsSupported)
                return;

            Bitmap gif = new(CaptureWidth, CaptureHeight);

            // Define encoders.
            EncoderParameters startingEncoder = new(1);
            startingEncoder.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.MultiFrame);

            EncoderParameters generalEncoder = new(2);
            generalEncoder.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.FrameDimensionTime);
            generalEncoder.Param[1] = new EncoderParameter(Encoder.Quality, 0L);

            // Create a new Graphics object from the bitmap.
            SystemGraphics graphics = SystemGraphics.FromImage(gif);

            // Set the graphics settings as needed.
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Create an array to hold the BitmapData for each frame.
            BitmapData[] bitmapDataArray = new BitmapData[frames.Count];

            // Create a gif encoder and write each frame to the memory stream.
            string filePath = $"{Main.SavePath}/BossFootage/lol2.gif";

            // Ensure that the directory exists.
            if (!File.Exists(filePath))
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            ImageCodecInfo[] imageEncoders = ImageCodecInfo.GetImageEncoders();
            ImageCodecInfo gifEncoder = imageEncoders.FirstOrDefault(codec => codec.FormatID == ImageFormat.Gif.Guid);
            graphics.DrawImage(frames[0], new SystemRectangle(0, 0, CaptureWidth, CaptureHeight));
            gif.Save(filePath, gifEncoder, startingEncoder);

            for (int i = 0; i < frames.Count; i++)
            {
                graphics.DrawImage(frames[i], new SystemRectangle(0, 0, CaptureWidth, CaptureHeight), new SystemRectangle(0, 0, CaptureWidth, CaptureHeight), GraphicsUnit.Pixel);
                gif.SaveAdd(frames[i], generalEncoder);
            }

            // Dispose of various objects.
            startingEncoder.Dispose();
            generalEncoder.Dispose();
            graphics.Dispose();

            // Clear the frames.
            ClearFrames();
        }
    }
#pragma warning restore CA1416 // Validate platform compatibility
}