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

        public readonly int StartInFrames => ConvertTimeSpanToFrames(Start);

        public readonly int EndInFrames => ConvertTimeSpanToFrames(End);

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

        public readonly bool WithinRange(TimeSpan time) => time >= Start && time <= End;

        public static int ConvertTimeSpanToFrames(TimeSpan span)
        {
            // 60 frames in 1000 milliseconds (60/1000).
            return (int)(span.TotalMilliseconds * 0.06f);
        }
    }
}
