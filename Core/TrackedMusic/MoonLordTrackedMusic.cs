using System.Collections.Generic;

namespace InfernumMode.Core.TrackedMusic
{
    public class MoonLordTrackedMusic : BaseTrackedMusic
    {
        public override string MusicPath => "InfernumModeMusic/Sounds/Music/MoonLord";

        public override BPMHeadBobState HeadBobState => BPMHeadBobState.Half;

        public override float BeatsPerMinute => 144f;

        public override List<SongSection> HeadphonesHighPoints => new()
        {
            new(TimeFormat(0, 36, 945), TimeFormat(1, 4, 648)),

            new(TimeFormat(1, 38, 660), TimeFormat(2, 3, 430)),

            new(TimeFormat(2, 29, 460), TimeFormat(2, 46, 253)),

            new(TimeFormat(3, 18, 160), TimeFormat(3, 39, 781))
        };

        public override List<SongSection> HighPoints => new();
    }
}
