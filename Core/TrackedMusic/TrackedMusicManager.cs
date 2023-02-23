using InfernumMode.Core.OverridingSystem;
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
    public class TrackedMusicManager : ModSystem
    {
        internal static List<int> TracksThatDontUseTerrariasSystem = new();

        internal static Dictionary<int, Song> CustomTracks = new();

        internal static Dictionary<int, string> CustomTrackDiskPositions = new();

        internal static ConstructorInfo SongConstructor;

        public static readonly List<string> CustomTrackPaths = new()
        {
            // Grief.
            "CalamityModMusic/Sounds/Music/SupremeCalamitas1",
            "CalamityModMusic/Sounds/Music/SupremeCalamitas1_FullIntro",

            // Lament.
            "CalamityModMusic/Sounds/Music/SupremeCalamitas2",

            // Epiphany.
            "CalamityModMusic/Sounds/Music/SupremeCalamitas3",

            // Acceptance.
            "CalamityModMusic/Sounds/Music/SupremeCalamitas4",
        };

        public static readonly Dictionary<string, BaseTrackedMusic> TrackInformation = new();

        // Song instances are expected to be read directly from the disk, not memory.
        // As such, we need to save them separately from the embedded tmod file data.
        public static readonly string MusicDirectory = $"{Main.SavePath}/TrackedMusic";

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

                string modName = path.Split('/').First();
                if (!ModLoader.TryGetMod(modName, out Mod mod))
                    continue;

                int musicSlotIndex = MusicLoader.GetMusicSlot(path);
                string musicPath = AssetPathHelper.CleanPath($"{MusicDirectory}/{path}.ogg");
                CustomTrackDiskPositions[musicSlotIndex] = musicPath;

                // Write the music to a permanent file if it hasn't been placed there yet.
                if (!File.Exists(musicPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(musicPath));
                    using FileStream saveStream = File.Create(musicPath);
                    using Stream musicStream = mod.GetFileStream(CustomTrackDiskPositions[musicSlotIndex], true);
                    musicStream.CopyTo(saveStream);
                }

                TracksThatDontUseTerrariasSystem.Add(musicSlotIndex);
                CustomTracks[musicSlotIndex] = (Song)SongConstructor.Invoke(new object[] { musicPath, path });
            }

            On.Terraria.Audio.LegacyAudioSystem.UpdateCommonTrack += DisableSoundsForCustomTracks;
            On.Terraria.Audio.LegacyAudioSystem.PauseAll += PauseMainTrack;
            On.Terraria.Audio.LegacyAudioSystem.ResumeAll += ResumeMainTrack;
        }

        private static void PauseMainTrack(On.Terraria.Audio.LegacyAudioSystem.orig_PauseAll orig, Terraria.Audio.LegacyAudioSystem self)
        {
            orig(self);

            if (MediaPlayer.State == MediaState.Playing)
                MediaPlayer.Pause();
        }

        private static void ResumeMainTrack(On.Terraria.Audio.LegacyAudioSystem.orig_ResumeAll orig, Terraria.Audio.LegacyAudioSystem self)
        {
            orig(self);

            if (MediaPlayer.State == MediaState.Paused)
                MediaPlayer.Resume();
        }

        private static void DisableSoundsForCustomTracks(On.Terraria.Audio.LegacyAudioSystem.orig_UpdateCommonTrack orig, Terraria.Audio.LegacyAudioSystem self, bool active, int i, float totalVolume, ref float tempFade)
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

        public static bool TryGetSongInformation(out BaseTrackedMusic information)
        {
            information = null;

            // If there is no tracked song currently being played, return immediately. There is no time-based information to acquire.
            if (TrackedSong is null)
                return false;

            // If the tracked song does not have information defined by the current registry, return immediately.
            if (!TrackInformation.TryGetValue(TrackedSong.Name, out information))
                return false;

            return true;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            // Don't run any audio effects server-side.
            if (Main.netMode == NetmodeID.Server)
                return;

            // Check to see if the track should still play.
            float volume = 0f;
            if (TrackedSong is not null)
            {
                int musicIndex = CustomTracks.Where(kv => kv.Value.Name == TrackedSong.Name).Select(kv => kv.Key).First();
                volume = Main.musicFade[musicIndex];

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
        }
    }
}
