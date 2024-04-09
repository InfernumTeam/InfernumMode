using System.Collections.Generic;

namespace InfernumMode.Core.TrackedMusic
{
    public class EmpressOfLightTrackedMusic : BaseTrackedMusic
    {
        public override string MusicPath => "InfernumModeMusic/Sounds/Music/EmpressOfLight";

        public override BPMHeadBobState HeadBobState => BPMHeadBobState.Quarter;

        public override float BeatsPerMinute => 165f;

        public override List<SongSection> HeadphonesHighPoints =>
        [
            new(TimeFormat(0, 13, 972), TimeFormat(0, 25, 959)),

            new(TimeFormat(1, 15, 415), TimeFormat(1, 40, 738)),

            new(TimeFormat(2, 20, 748), TimeFormat(2, 41, 310))
        ];

        public override List<SongSection> HighPoints => [];
    }
}
