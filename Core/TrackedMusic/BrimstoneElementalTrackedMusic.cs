using System.Collections.Generic;

namespace InfernumMode.Core.TrackedMusic
{
    public class BrimstoneElementalTrackedMusic : BaseTrackedMusic
    {
        public override string MusicPath => "CalamityModMusic/Sounds/Music/BrimstoneElemental";

        public override BPMHeadBobState HeadBobState => BPMHeadBobState.Half;

        // I don't know the actual value for this, but it doesn't matter since this isn't used by headphones.
        public override float BeatsPerMinute => 0f;

        public override List<SongSection> HeadphonesHighPoints => new();

        public override List<SongSection> HighPoints => new()
        {
            new(TimeFormat(0, 3, 591), TimeFormat(0, 3, 691)),

            new(TimeFormat(0, 5, 417), TimeFormat(0, 4, 517)),

            new(TimeFormat(0, 7, 242), TimeFormat(0, 7, 542)),

            new(TimeFormat(0, 10, 833), TimeFormat(0, 10, 933)),

            new(TimeFormat(0, 12, 720), TimeFormat(0, 12, 820)),

            new(TimeFormat(0, 14, 845), TimeFormat(0, 14, 945)),

            new(TimeFormat(0, 21, 727), TimeFormat(0, 21, 827)),

            new(TimeFormat(0, 28, 361), TimeFormat(0, 28, 461)),

            new(TimeFormat(0, 36, 333), TimeFormat(0, 36, 433)),

            new(TimeFormat(0, 42, 845), TimeFormat(0, 42, 945)),

            new(TimeFormat(0, 50, 150), TimeFormat(0, 50, 250)),

            new(TimeFormat(0, 57, 391), TimeFormat(0, 57, 491)),

            new(TimeFormat(1, 5, 424), TimeFormat(1, 5, 524)),

            new(TimeFormat(1, 12, 728), TimeFormat(1, 12, 828)),

            new(TimeFormat(1, 14, 493), TimeFormat(1, 14, 593)),

            new(TimeFormat(1, 16, 318), TimeFormat(1, 16, 418)),

            new(TimeFormat(1, 18, 144), TimeFormat(1, 18, 244)),

            new(TimeFormat(1, 19, 970), TimeFormat(1, 20, 70)),

            new(TimeFormat(1, 23, 620), TimeFormat(1, 23, 720)),

            new(TimeFormat(1, 27, 273), TimeFormat(1, 27, 373)),

            new(TimeFormat(1, 30, 864), TimeFormat(1, 30, 964)),

            new(TimeFormat(1, 34, 515), TimeFormat(1, 34, 615)),

            new(TimeFormat(1, 38, 106), TimeFormat(1, 38, 206)),

            new(TimeFormat(1, 41, 818), TimeFormat(1, 41, 918)),

            new(TimeFormat(1, 49, 61), TimeFormat(1, 49, 161)),

            new(TimeFormat(1, 56, 364), TimeFormat(1, 56, 464)),

            new(TimeFormat(2, 3, 606), TimeFormat(2, 3, 706)),

            new(TimeFormat(2, 10, 909), TimeFormat(2, 11, 9)),

            new(TimeFormat(2, 14, 500), TimeFormat(2, 14, 600)),

            new(TimeFormat(2, 16, 204), TimeFormat(2, 16, 304)),

            new(TimeFormat(2, 18, 152), TimeFormat(2, 18, 252)),

            new(TimeFormat(2, 21, 803), TimeFormat(2, 21, 903)),

            new(TimeFormat(2, 23, 568), TimeFormat(2, 23, 668))
        };
    }
}
