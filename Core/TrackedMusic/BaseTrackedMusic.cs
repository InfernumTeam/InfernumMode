using System;
using System.Collections.Generic;

namespace InfernumMode.Core.TrackedMusic
{
    public abstract class BaseTrackedMusic
    {
        public abstract string MusicPath
        {
            get;
        }

        public abstract float BeatsPerMinute
        {
            get;
        }

        public abstract BPMHeadBobState HeadBobState
        {
            get;
        }

        public abstract List<SongSection> HeadphonesHighPoints
        {
            get;
        }

        public abstract List<SongSection> HighPoints
        {
            get;
        }

        public virtual Dictionary<SongSection, int> SongSections
        {
            get;
        }

        internal void Load()
        {
            TrackedMusicManager.CustomTrackPaths.Add(MusicPath);
            TrackedMusicManager.TrackInformation[MusicPath] = this;
        }

        public static TimeSpan TimeFormat(int minutes, int seconds, int milliseconds) => new(0, 0, minutes, seconds, milliseconds);

        public static SongSection WithMSDelay(int minutes, int seconds, int milliseconds, int delayInMilliseconds)
        {
            TimeSpan start = new(0, 0, minutes, seconds, milliseconds);
            TimeSpan end = start.Add(new(0, 0, 0, 0, delayInMilliseconds));
            return new(start, end);
        }
    }
}
