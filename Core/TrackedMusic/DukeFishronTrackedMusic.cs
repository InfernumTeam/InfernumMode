using System.Collections.Generic;

namespace InfernumMode.Core.TrackedMusic
{
    public class DukeFishronTrackedMusic : BaseTrackedMusic
    {
        public override string MusicPath => "InfernumModeMusic/Sounds/Music/DukeFishron";

        public override BPMHeadBobState HeadBobState => BPMHeadBobState.Half;

        public override float BeatsPerMinute => 144f;

        public override List<SongSection> HeadphonesHighPoints =>
        [
            new(TimeFormat(0, 8, 303), TimeFormat(0, 31, 367)),

            new(TimeFormat(1, 16, 441), TimeFormat(1, 41, 482)),

            new(TimeFormat(2, 2, 174), TimeFormat(2, 9, 950))
        ];

        public override List<SongSection> HighPoints => [];
    }
}
