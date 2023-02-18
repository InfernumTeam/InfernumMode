using System.Collections.Generic;

namespace InfernumMode.Core.TrackedMusic
{
    public class QueenBeeTrackedMusic : BaseTrackedMusic
    {
        public override string MusicPath => "InfernumModeMusic/Sounds/Music/QueenBee";

        public override BPMHeadBobState HeadBobState => BPMHeadBobState.Half;

        public override float BeatsPerMinute => 167f;

        public override List<SongSection> HighPoints => new()
        {
            new(TimeFormat(0, 10, 916), TimeFormat(0, 21, 622)),

            new(TimeFormat(0, 55, 523), TimeFormat(1, 18, 298)),

            new(TimeFormat(1, 27, 850), TimeFormat(1, 48, 622)),
        };
    }
}
