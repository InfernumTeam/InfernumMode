using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Core.TrackedMusic
{
    // I don't know how to fix this. I'm sorry.
    public class TrackedMusicManager : ModSystem
    {
        internal static bool PausedBecauseOfUI;

        internal static List<int> TracksThatDontUseTerrariasSystem = new();

        internal static Dictionary<int, Song> CustomTracks = new();

        internal static Dictionary<int, string> CustomTrackDiskPositions = new();

        internal static ConstructorInfo SongConstructor;

        public static readonly List<string> CustomTrackPaths = new()
        {
            // Grief.
            "CalamityModMusic/Sounds/Music/CalamitasPhase1",
            "CalamityModMusic/Sounds/Music/CalamitasPhase1_FullIntro",

            // Lament.
            "CalamityModMusic/Sounds/Music/CalamitasPhase2",

            // Epiphany.
            "CalamityModMusic/Sounds/Music/CalamitasPhase3",

            // Acceptance.
            "CalamityModMusic/Sounds/Music/CalamitasDefeat",
        };

        public static readonly Dictionary<string, BaseTrackedMusic> TrackInformation = new();

        // Song instances are expected to be read directly from the disk, not memory.
        // As such, we need to save them separately from the embedded tmod file data.
        public static readonly string MusicDirectory = $"{Main.SavePath}/TrackedMusic";

        public delegate bool PauseInUIConditionDelegate();

        public static event PauseInUIConditionDelegate PauseInUIConditionEvent;

        public static Song TrackedSong
        {
            get;
            internal set;
        }

        public static TimeSpan SongElapsedTime => TrackedSong is null || Main.netMode == NetmodeID.Server ? TimeSpan.Zero : MediaPlayer.PlayPosition;

        // Due to the Infernum Music mod directly relying on this mod, it must load after it.
        // As such we need to wait until ALL mods have been loaded before we can perform this initialization, since Infernum Music's tracks
        // aren't loaded by the time Infernum is fully loaded.
        public override void PostSetupContent()
        {
            TracksThatDontUseTerrariasSystem = new();

            // Initialize the constructor for the Song instance, since that's internal for some reason.
            SongConstructor = typeof(Song).GetConstructor(Utilities.UniversalBindingFlags, new Type[]
            {
                typeof(string),
                typeof(string),
            });

            // Load all tracked music.
            foreach (Type musicType in Utilities.GetEveryTypeDerivedFrom(typeof(BaseTrackedMusic), InfernumMode.Instance.Code))
                ((BaseTrackedMusic)Activator.CreateInstance(musicType)).Load();

            foreach (string path in CustomTrackPaths)
            {
                if (Main.netMode == NetmodeID.Server)
                    break;

                // Perform some string manipulations to separate the mod and file name out from the path.
                string modName = path.Split('/').First();
                string fileName = string.Empty;
                foreach (string part in path.Split('/').Skip(1))
                    fileName += part + "/";
                fileName = string.Concat(fileName.Take(fileName.Length - 1)) + ".ogg";

                // Move onto the next path if the track's associated mod is not enabled.
                if (!ModLoader.TryGetMod(modName, out Mod mod))
                    continue;

                int musicSlotIndex = MusicLoader.GetMusicSlot(path);
                string musicPath = AssetPathHelper.CleanPath($"{MusicDirectory}/{path}.ogg");
                CustomTrackDiskPositions[musicSlotIndex] = musicPath;

                // Write the music to a permanent file if it hasn't been placed there yet.
                if (!File.Exists(musicPath) || !File.ReadAllText(musicPath).Trim().Any())
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(musicPath));
                    using FileStream saveStream = File.Create(musicPath);

                    // Move onto the next path if the track doesn't exist in the mod.
                    if (!mod.FileExists(fileName))
                        continue;

                    using Stream musicStream = mod.GetFileStream(fileName, true);
                    musicStream.CopyTo(saveStream);
                }

                TracksThatDontUseTerrariasSystem.Add(musicSlotIndex);
                CustomTracks[musicSlotIndex] = (Song)SongConstructor.Invoke(new object[] { musicPath, path });
            }

            //Terraria.Audio.On_LegacyAudioSystem.UpdateCommonTrack += DisableSoundsForCustomTracks;
            //Terraria.Audio.On_LegacyAudioSystem.UpdateCommonTrackTowardStopping += PermitVolumeFadeoutForCustomTracks;
            //Terraria.Audio.On_LegacyAudioSystem.PauseAll += PauseMainTrack;
            //Terraria.Audio.On_LegacyAudioSystem.ResumeAll += ResumeMainTrack;
        } // IDE0051

        #pragma warning disable IDE0051
        private static void PauseMainTrack(Terraria.Audio.On_LegacyAudioSystem.orig_PauseAll orig, Terraria.Audio.LegacyAudioSystem self)
        {
            orig(self);

            if (MediaPlayer.State == MediaState.Playing)
                MediaPlayer.Pause();
        }

        private static void ResumeMainTrack(Terraria.Audio.On_LegacyAudioSystem.orig_ResumeAll orig, Terraria.Audio.LegacyAudioSystem self)
        {
            orig(self);

            if (MediaPlayer.State == MediaState.Paused && !PausedBecauseOfUI)
                MediaPlayer.Resume();
        }

        private static void DisableSoundsForCustomTracks(Terraria.Audio.On_LegacyAudioSystem.orig_UpdateCommonTrack orig, Terraria.Audio.LegacyAudioSystem self, bool active, int i, float totalVolume, ref float tempFade)
        {
            if (TracksThatDontUseTerrariasSystem.Contains(i))
            {
                // If for some reason the file isn't present, just use the default system.
                if (!File.Exists(CustomTrackDiskPositions[i]))
                {
                    orig(self, active, i, totalVolume, ref tempFade);
                    return;
                }

                if (self.AudioTracks[i].IsPlaying)
                {
                    self.AudioTracks[i].SetVariable("Volume", 0f);
                    self.AudioTracks[i].Stop(AudioStopOptions.Immediate);
                }
                totalVolume = 0f;
                active = false;

                if (TrackedSong is null || TrackedSong.Name != CustomTracks[i].Name)
                {
                    TrackedSong = CustomTracks[i];
                    MediaPlayer.Stop();
                }
            }

            orig(self, active, i, totalVolume, ref tempFade);
        }

        private static void PermitVolumeFadeoutForCustomTracks(Terraria.Audio.On_LegacyAudioSystem.orig_UpdateCommonTrackTowardStopping orig, Terraria.Audio.LegacyAudioSystem self, int i, float totalVolume, ref float tempFade, bool isMainTrackAudible)
        {
            if (Main.gameMenu && MediaPlayer.State == MediaState.Playing)
            {
                TrackedSong = null;
                MediaPlayer.Stop();
            }

            if (TrackedSong is not null)
            {
                if (isMainTrackAudible)
                    tempFade -= 0.0075f;

                if (tempFade <= 0f)
                {
                    tempFade = 0f;
                    self?.AudioTracks[i]?.SetVariable("Volume", 0f);
                    self?.AudioTracks[i]?.Stop(AudioStopOptions.Immediate);
                }
                return;
            }
            orig(self, i, totalVolume, ref tempFade, isMainTrackAudible);
        }
        #pragma warning restore IDE0051

        public static bool TryGetSongInformation(out BaseTrackedMusic information)
        {
            information = null;
            return false;

            /*
            // If there is no tracked song currently being played, return immediately. There is no time-based information to acquire.
            if (TrackedSong is null)
                return false;

            // If the tracked song does not have information defined by the current registry, return immediately.
            if (!TrackInformation.TryGetValue(TrackedSong.Name, out information))
                return false;

            return true;
            */
        }

        public override void UpdateUI(GameTime gameTime)
        {
            return;

            /*
            // Don't run any audio effects server-side.
            if (Main.netMode == NetmodeID.Server)
                return;

            // Check to see if the track should still play.
            float volume = 0f;
            if (TrackedSong is not null)
            {
                int musicIndex = CustomTracks.Where(kv => kv.Value.Name == TrackedSong.Name).Select(kv => kv.Key).First();
                volume = Main.musicFade[musicIndex];

                // Check if the music should be paused in the UI.
                bool pauseInUI = false;
                foreach (Delegate d in PauseInUIConditionEvent.GetInvocationList())
                {
                    // This doesn't break if true in case an event contains secondary code that should be run regardless of the returned value.
                    if (((PauseInUIConditionDelegate)d).Invoke())
                        pauseInUI = true;
                }

                // Pause in the UI if necessary.
                if (pauseInUI && InfernumMode.CanUseCustomAIs)
                {
                    if (!PausedBecauseOfUI && Main.gamePaused)
                    {
                        if (MediaPlayer.State == MediaState.Playing)
                            MediaPlayer.Pause();
                        PausedBecauseOfUI = true;
                    }
                    else if (PausedBecauseOfUI && MediaPlayer.State == MediaState.Paused && !Main.gamePaused)
                    {
                        MediaPlayer.Resume();
                        PausedBecauseOfUI = false;
                    }
                }

                if (volume <= 0.0001f)
                {
                    MediaPlayer.Stop();
                    TrackedSong = null;
                    return;
                }
            }

            if (TrackedSong is null || !File.Exists(CustomTrackDiskPositions[CustomTracks.Where(kv => kv.Value.Name == TrackedSong.Name).Select(kv => kv.Key).First()]))
                return;

            if (MediaPlayer.State == MediaState.Stopped)
                MediaPlayer.Play(TrackedSong);

            float currentVolume = volume * Main.musicVolume * 0.4f;
            if (MediaPlayer.Volume != currentVolume)
                MediaPlayer.Volume = currentVolume;
            MediaPlayer.IsRepeating = true;
            MediaPlayer.IsMuted = false;

            */
        }
    }
}
