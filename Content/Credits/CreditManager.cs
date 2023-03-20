using InfernumMode.Common.Graphics.AttemptRecording;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Threading;
using Terraria;
using Terraria.ID;

namespace InfernumMode.Content.Credits
{
    public static class CreditManager
    {
        private enum CreditState
        {
            LoadingTextures,
            Playing
        }

        public static bool CreditsPlaying { get; private set; } = false;

        private static int CreditsTimer = 0;

        private static int ActiveGifIndex = 0;

        private static CreditAnimationObject[] CreditGIFs;

        private static CreditState CurrentState = CreditState.LoadingTextures;

        public const int TotalGIFs = 6;

        internal static void StartRecordingFootageForCredits(ScreenCapturer.RecordingBoss boss)
        {
            if (Main.netMode == NetmodeID.Server || ScreenCapturer.RecordCountdown > 0)
                return;

            // Only start a recording if one does not exist for this player and boss, to avoid overriding them.
            if (File.Exists($"{ScreenCapturer.FolderPath}/{ScreenCapturer.GetStringFromBoss(boss)}{ScreenCapturer.FileExtension}"))
                return;

            ScreenCapturer.CurrentBoss = boss;
            ScreenCapturer.RecordCountdown = ScreenCapturer.BaseRecordCountdownLength;
        }

        internal static void BeginCredits()
        {
            // Return if the credits are already playing.
            if (CreditsPlaying)
                return;

            // Else, mark them as playing.
            CreditsPlaying = true;
        }

        internal static void UpdateCredits()
        {
            if (!CreditsPlaying)
                return;

            float gifTime = 360f;
            float fadeInTime = 60f;
            float fadeOutTime = gifTime = fadeInTime;

            switch (CurrentState)
            {
                case CreditState.LoadingTextures:
                    // The textures must be loaded for each gif, do this all at once here and wait the allocated time to ensure they've all loaded, and to give
                    // the player time to enjoy the victory.
                    if (CreditsTimer == 0)
                        new Thread(SetupObjects).Start();

                    if (CreditsTimer >= gifTime)
                        CurrentState = CreditState.Playing;
                    break;

                case CreditState.Playing:
                    if (CreditsTimer <= gifTime)
                    {
                        if (CreditGIFs.IndexInRange(ActiveGifIndex))
                            CreditGIFs[ActiveGifIndex]?.Update();

                        if (CreditsTimer == gifTime)
                        {
                            if (ActiveGifIndex < TotalGIFs - 1)
                            {
                                // Dispose of all the textures.
                                CreditGIFs[ActiveGifIndex]?.DisposeTextures();
                                CreditGIFs[ActiveGifIndex] = null;
                                ActiveGifIndex++;
                                CreditsTimer = 0;
                                return;
                            }
                            else
                            {
                                ActiveGifIndex = 0;
                                CreditsTimer = 0;
                                CreditsPlaying = false;
                                return;
                            }    
                        }
                    }
                    break;
            }

            CreditsTimer++;
        }

        internal static void DrawCredits()
        {
            // Only draw if the credits are playing.
            if (!CreditsPlaying || CurrentState != CreditState.Playing)
                return;

            float gifTime = 360f;
            float fadeInTime = 60f;
            float fadeOutTime = gifTime - fadeInTime;

            if (CreditsTimer <= gifTime)
            {
                float opacity = 1f;

                if (CreditsTimer <= fadeInTime)
                    opacity = Utils.GetLerpValue(0f, fadeInTime, CreditsTimer, true);
                else if (CreditsTimer >= fadeOutTime)
                    opacity = Utils.GetLerpValue(fadeOutTime, gifTime, CreditsTimer, true);

                if (CreditGIFs.IndexInRange(ActiveGifIndex))
                    CreditGIFs[ActiveGifIndex]?.Draw(CreditsTimer, opacity);
            }
        }

        private static void SetupObjects()
        {
            CreditGIFs = new CreditAnimationObject[TotalGIFs];

            for (int i = 0; i < TotalGIFs; i++)
            {
                ScreenCapturer.RecordingBoss boss = i switch
                {
                    0 => ScreenCapturer.RecordingBoss.KingSlime,
                    1 => ScreenCapturer.RecordingBoss.WoF,
                    2 => ScreenCapturer.RecordingBoss.Calamitas,
                    3 => ScreenCapturer.RecordingBoss.Provi,
                    4 => ScreenCapturer.RecordingBoss.Draedon,
                    _ => ScreenCapturer.RecordingBoss.SCal
                };

                Texture2D[] textures = ScreenCapturer.LoadGifAsTexture2Ds(boss);
                CreditGIFs[i] = new CreditAnimationObject(new(Main.screenWidth * 0.3f, Main.screenHeight * 0.4f), -Vector2.UnitY, textures);
            }
        }
    }
}
