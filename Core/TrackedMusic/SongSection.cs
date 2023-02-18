using System;

namespace InfernumMode.Core.TrackedMusic
{
    public struct SongSection
    {
        public TimeSpan Start
        {
            get;
            internal set;
        }

        public TimeSpan End
        {
            get;
            internal set;
        }

        public SongSection(int minutes, int seconds, int milliseconds, TimeSpan duration)
        {
            Start = BaseTrackedMusic.TimeFormat(minutes, seconds, milliseconds);
            End = Start + duration;
        }

        public SongSection(TimeSpan start, TimeSpan end)
        {
            Start = start;
            End = end;
        }

        public bool WithinRange(TimeSpan time) => time >= Start && time <= End;
    }
}
