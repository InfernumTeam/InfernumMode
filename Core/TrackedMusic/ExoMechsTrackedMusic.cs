using System.Collections.Generic;

namespace InfernumMode.Core.TrackedMusic
{
    public class ExoMechsTrackedMusic : BaseTrackedMusic
    {
        public override string MusicPath => "InfernumModeMusic/Sounds/Music/ExoMechBosses";

        public override BPMHeadBobState HeadBobState => BPMHeadBobState.Quarter;

        public override float BeatsPerMinute => 204f;

        public override List<SongSection> HighPoints => new()
        {
            new(TimeFormat(0, 19, 185), TimeFormat(0, 28, 228)),

            new(TimeFormat(0, 43, 747), TimeFormat(0, 52, 545)),

            new(TimeFormat(1, 9, 409), TimeFormat(1, 12, 98)),

            new(TimeFormat(2, 24, 929), TimeFormat(2, 45, 213)),

            new(TimeFormat(4, 14, 418), TimeFormat(4, 33, 236)),
        };
    }
}
