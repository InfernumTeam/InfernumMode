using Gif.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using SystemGraphics = System.Drawing.Graphics;

namespace InfernumMode.Common.Graphics.AttemptRecording
{
#pragma warning disable CA1416 // Validate platform compatibility
    public class ScreenCapturer : ModSystem
    {
        public enum RecordingBoss
        {
            KingSlime,
            WoF,
            Calamitas,
            Provi,
            Draedon,
            SCal
        }

        private static IntPtr desktopWindowHandle;

        private static IntPtr deviceContextHandle;

        private static IntPtr compatibleDevideContextHandle;

        private static int captureWidth;

        private static int captureHeight;

        private static List<Bitmap> frames;

        // Bit flags for specifying how bitmap data should be copied in unmanaged code.
        private const int SourceCopy = 0x00CC0020;

        private const int CaptureBitBlit = 0x40000000;

        // 6 seconds, due to it only recording every 3rd frame.
        public const int BaseRecordCountdownLength = 1080;

        public static int RecordCountdown
        {
            get;
            set;
        }

        internal static RecordingBoss CurrentBoss
        {
            get;
            set;
        }
        // If on a non-Windows operating system, don't use this system. Fundamental parts are not guaranteed to work outside of it.
        // Also it doesn't run on servers, obviously.
        public static bool IsSupported => Main.netMode != NetmodeID.Server && Environment.OSVersion.Platform == PlatformID.Win32NT;

        // The GIFs are saved in a folder for the player, to ensure duplicate gifs are not saved per player.
        public static string FolderPath => $"{Main.SavePath}/BossFootage/{Main.LocalPlayer.name}";

        public const string FileExtension = ".gif";

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
            // Do not attempt to record anything if the game is not the active window.
            // Not doing this means the users PC screen gets recorded into the gif as well,
            // which would not go down well.
            if (!Main.instance.IsActive)
                return;

            // Resize the capture window as necessary.
            CaptureWidth = (int)(Main.screenWidth * 0.45f);
            CaptureHeight = (int)(Main.screenHeight * 0.45f);

            if (RecordCountdown >= 1)
            {
                RecordCountdown--;

                // Append to the frames. For the sake of performance this happens on frame intervals instead of every frame.
                if (RecordCountdown % 3 == 0)
                    frames.Add(GetScreenBitmap());

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

            // Ensure none of the frames are null. If they are, creating the gif will crash the game so clear them all and do not continue.
            if (frames.Any(f => f == null))
            {
                ClearFrames();
                return;
            }

            string filePath = $"{FolderPath}/{GetStringFromBoss(CurrentBoss)}{FileExtension}";
            AnimatedGifEncoder e = new();

            using MemoryStream stream = new();
            using FileStream fileStream = new(filePath, FileMode.Create);
            e.Start(stream);
            e.SetDelay(0);
            e.SetRepeat(0);
            for (int i = 0; i < frames.Count; i++)
                e.AddFrame(frames[i]);
            e.Finish();

            byte[] data = stream.ToArray();
            fileStream.Write(data);
        }

        internal static Texture2D[] LoadGifAsTexture2Ds(RecordingBoss bossFootageToLoad, out bool baseCreditsUsed)
        {
            baseCreditsUsed = false;
            if (!IsSupported)
                return null;

            string filePath = $"{FolderPath}/{GetStringFromBoss(bossFootageToLoad)}{FileExtension}";

            Image gif;
            // Load the gif and set the frame count.

            if (File.Exists(filePath))
                gif = Image.FromFile(filePath);
            else
            {
                baseCreditsUsed = true;
                return new Texture2D[] { ModContent.Request<Texture2D>("InfernumMode/Assets/ExtraTextures/testcredits", AssetRequestMode.ImmediateLoad).Value };
            }

            // Get the GUID for the frame dimension of the GIF (which is used to specify the frame dimension when accessing individual frames).
            FrameDimension dimension = new(gif.FrameDimensionsList[0]);

            // Get the total number of frames in the GIF.
            int frameCount = gif.GetFrameCount(dimension);
            Texture2D[] textures = new Texture2D[frameCount];

            // Loop through each frame and add it to the array.
            for (int i = 0; i < frameCount; i++)
            {
                // Set the active frame of the GIF to the current frame.
                gif.SelectActiveFrame(dimension, i);

                // Create a new Bitmap instance and copy the current frame into it.
                Bitmap bitmap = new(gif.Width, gif.Height);
                SystemGraphics graphics = SystemGraphics.FromImage(bitmap);
                graphics.DrawImage(gif, System.Drawing.Point.Empty);
                textures[i] = GetTextureFromImage(bitmap);
            }

            gif.Dispose();
            return textures;
        }

        // Adapted from https://gamedev.stackexchange.com/questions/6440/bitmap-to-texture2d-problem-with-colors
        private static Texture2D GetTextureFromImage(Bitmap bitmap)
        {
            // Create a new, empty texture.
            Texture2D texture = new(Main.instance.GraphicsDevice, bitmap.Width, bitmap.Height);
            // Create an array of the bitmap size.
            uint[] imgData = new uint[bitmap.Width * bitmap.Height];
            unsafe
            {
                // Lock the bits.
                BitmapData origdata = bitmap.LockBits(new(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
                uint* byteData = (uint*)origdata.Scan0;

                // Loop through each pixel, and set it to the bitmap's info. This also swaps the BGRA of the bitmap to RGBA which the texture2d uses. Not doing this
                // results in the textures r and b channels being swapped which is not ideal.
                for (int i = 0; i < imgData.Length; i++)
                    imgData[i] = ((byteData[i] & 0x000000ff) << 16 | (byteData[i] & 0x0000FF00) | (byteData[i] & 0x00FF0000) >> 16 | (byteData[i] & 0xFF000000));
                // Unlock the bits.
                bitmap.UnlockBits(origdata);
            }
            // Set the textures data, and return it.
            texture.SetData(imgData);
            return texture;
        }

        internal static string GetStringFromBoss(RecordingBoss boss)
        {
            return boss switch
            {
                RecordingBoss.SCal => "SCal",
                RecordingBoss.Draedon => "Draedon",
                RecordingBoss.Provi => "Providence",
                RecordingBoss.Calamitas => "Calamitas",
                RecordingBoss.WoF => "WallOfFlesh",
                RecordingBoss.KingSlime => "KingSlime",
                _ => "HowTheHellAreYouSeeingThis"
            };
        }
    }
#pragma warning restore CA1416 // Validate platform compatibility
}