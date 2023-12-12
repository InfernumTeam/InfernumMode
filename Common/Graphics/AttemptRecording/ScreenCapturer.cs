using Gif.Components;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
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
            Vassal,
            Moonlord,
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

        private static CancellationTokenSource cancelThread;

        // Bit flags for specifying how bitmap data should be copied in unmanaged code.
        private const int SourceCopy = 0x00CC0020;

        private const int CaptureBitBlit = 0x40000000;

        public const int BaseRecordCountdownLength = 360 * RecordingFrameSkip;

        public const int RecordingFrameSkip = 3;

        // This is so people do not see .comgif files and get concerned that they have a virus or something.
        public const string InfoFileContents = "The weird files stored here are compressed .GIF files. These are created and used by InfernumMode, and are decompressed when needed by the mod.\n" +
            "If you wish to verify this, you can extract the mod in-game and check out 'InfernumMode/Common/Graphics/AttemptRecording/ScreenCapturer'.\n" +
            "Be aware it may spoil you if you have not completed the mod yet.\n" +
            "You can also safely delete any of these files without breaking anything, if you wish to not have the space taken up. - Toasty";

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

        private static bool ShouldCreateGif
        {
            get;
            set;
        }

        // If on a non-Windows operating system, don't use this system. Fundamental parts are not guaranteed to work outside of it.
        // Also it doesn't run on servers, obviously.
        public static bool IsSupported => Main.netMode == NetmodeID.SinglePlayer && Environment.OSVersion.Platform == PlatformID.Win32NT && InfernumConfig.Instance is not null && InfernumConfig.Instance.CreditsRecordings;

        // Certain characters are prohibited when naming folders. Consequently, it is important that a cleaned version of the name string be used if any such characters appear.
        public static string CleanedCharacterName => Main.LocalPlayer.name.Replace("\\", string.Empty).Replace("/", string.Empty).Replace(":", string.Empty).Replace("*", string.Empty)
            .Replace("?", string.Empty).Replace("\"", string.Empty).Replace("<", string.Empty).Replace(">", string.Empty).Replace("|", string.Empty);

        // The GIFs are saved in player specific folders, to ensure duplicate gifs are not saved per player.
        public static string FolderPath => $"{Main.SavePath}/BossFootage/{CleanedCharacterName}";

        public const float DownscaleFactor = 3f;

        public static string InfoFilePath => $"{Main.SavePath}/BossFootage/README.txt";

        // This is named this due to being compressed, it will not be playable via normal GIF players so having it appear like a normal
        // .gif file would be misleading and cause potential confusion.
        public const string FileExtension = ".compgif";

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
            cancelThread?.Dispose();
            cancelThread = new();
        }

        public override void PostUpdateEverything()
        {
            if (RecordCountdown >= 1)
            {
                RecordCountdown--;

                // If the countdown has decreased to zero, mark the GIF as ready to create.
                if (RecordCountdown == 0)
                    ShouldCreateGif = true;
            }
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

            if (RecordCountdown > 0)
            {
                // Append to the frames. For the sake of performance this happens on frame intervals instead of every frame.
                if (RecordCountdown % RecordingFrameSkip == 0)
                    frames.Add(GetScreenBitmap());
            }

            // Prepare the gif file if it should be done.
            if (ShouldCreateGif)
            {
                ShouldCreateGif = false;
                cancelThread?.Dispose();
                cancelThread = new();
                new Thread(() => CreateGif(cancelThread.Token)).Start();
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
                var downscaledSize = new Size((int)(CaptureWidth / DownscaleFactor), (int)(CaptureHeight / DownscaleFactor));
                Bitmap bitmap = new(Image.FromHbitmap(bitmapScreenHandle), downscaledSize);

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
            // Ensure that any background threads are gracefully stopped if the frames need to be manipulated.
            cancelThread?.Cancel();

            while (frames?.Any() ?? false)
            {
                frames.First().Dispose();
                frames.RemoveAt(0);
            }
        }

        private static void CreateGif(CancellationToken token)
        {
            if (!IsSupported)
                return;

            // Ensure none of the frames are null. If they are, creating the gif will crash the game so clear them all and do not continue.
            if (frames.Any(f => f == null))
            {
                ClearFrames();
                return;
            }

            // Check to see if the readme exists, and create it if not.
            if (!File.Exists(InfoFilePath))
            {
                string fileInfoDirectory = Path.GetDirectoryName(InfoFilePath);

                // Create the directory if necessary.
                if (!Directory.Exists(fileInfoDirectory))
                    Directory.CreateDirectory(fileInfoDirectory);

                using FileStream fileStream2 = File.Create(InfoFilePath);
                using BinaryWriter binaryWriter = new(fileStream2);
                binaryWriter.Write(InfoFileContents);
            }

            string filePath = $"{FolderPath}/{GetStringFromBoss(CurrentBoss)}{FileExtension}";
            string directory = Path.GetDirectoryName(filePath);
            AnimatedGifEncoder e = new();

            // Create the directory if necessary.
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            using MemoryStream stream = new();
            using FileStream fileStream = File.Create(filePath);
            e.Start(stream);
            e.SetDelay(0);
            e.SetRepeat(0);

            // This takes quite a while to complete, 
            for (int i = 0; i < frames.Count; i++)
            {
                if (token.IsCancellationRequested || i >= frames.Count)
                    return;

                e.AddFrame(frames[i]);
            }
            e.Finish();

            byte[] data = stream.ToArray();
            using var gZipMem = new MemoryStream(data.Length);
            using (var gZip = new GZipStream(gZipMem, CompressionLevel.Optimal))
                gZip.Write(data, 0, data.Length);

            data = gZipMem.ToArray();
            fileStream.Write(data);

            // Clear the frames.
            ClearFrames();
        }

        internal static Texture2D[] LoadGifAsTexture2Ds(RecordingBoss bossFootageToLoad, out bool baseCreditsUsed)
        {
            baseCreditsUsed = false;
            if (!IsSupported)
                return null;

            string filePath = $"{FolderPath}/{GetStringFromBoss(bossFootageToLoad)}{FileExtension}";

            if (File.Exists(filePath))
            {
                // Read the compressed data.
                try
                {
                    var compressedData = File.ReadAllBytes(filePath);

                    // Create a memory stream from it.
                    using MemoryStream memoryStream = new(compressedData);
                    using MemoryStream decompressionStream = new();

                    // Decompress the stream.
                    using var gZip = new GZipStream(memoryStream, CompressionMode.Decompress);

                    // Copy it to a new stream.
                    gZip.CopyTo(decompressionStream);

                    // Ensure the position is at the beginning.
                    decompressionStream.Position = 0;

                    // Load the gif from the new, decompressed stream.
                    using Bitmap gif = (Bitmap)Image.FromStream(decompressionStream);

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

                        int localIndex = i;
                        var imageColors = GetColorsFromImage(bitmap);
                        Main.QueueMainThreadAction(() =>
                        {
                            textures[localIndex] = new(Main.instance.GraphicsDevice, bitmap.Width, bitmap.Height);
                            textures[localIndex].SetData(imageColors);
                        });
                    }
                    return textures;
                }
                catch (IOException)
                {
                    // Return this if the file is in use.
                    return new Texture2D[] { ModContent.Request<Texture2D>(InfernumTextureRegistry.InvisPath, AssetRequestMode.ImmediateLoad).Value };
                }
            }
            else
            {
                baseCreditsUsed = true;
                return new Texture2D[] { ModContent.Request<Texture2D>(InfernumTextureRegistry.InvisPath, AssetRequestMode.ImmediateLoad).Value };
            }
        }

        // Adapted from https://gamedev.stackexchange.com/questions/6440/bitmap-to-texture2d-problem-with-colors
        private static uint[] GetColorsFromImage(Bitmap bitmap)
        {
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
                    imgData[i] = (byteData[i] & 0x000000ff) << 16 | (byteData[i] & 0x0000FF00) | (byteData[i] & 0x00FF0000) >> 16 | (byteData[i] & 0xFF000000);

                // Unlock the bits.
                bitmap.UnlockBits(origdata);
            }
            return imgData;
        }

        internal static string GetStringFromBoss(RecordingBoss boss)
        {
            return boss switch
            {
                RecordingBoss.SCal => "SCal",
                RecordingBoss.Draedon => "Draedon",
                RecordingBoss.Provi => "Providence",
                RecordingBoss.Vassal => "Vassal",
                RecordingBoss.Moonlord => "Moonlord",
                RecordingBoss.Calamitas => "Calamitas",
                RecordingBoss.WoF => "WallOfFlesh",
                RecordingBoss.KingSlime => "KingSlime",
                _ => "HowTheHellAreYouSeeingThis"
            };
        }
    }
#pragma warning restore CA1416 // Validate platform compatibility
}
