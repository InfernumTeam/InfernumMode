using System.Collections.Generic;

namespace InfernumMode.Core.TrackedMusic
{
    public class ProvidenceTrackedMusic : BaseTrackedMusic
    {
        public override string MusicPath => "CalamityModMusic/Sounds/Music/Providence";

        public override BPMHeadBobState HeadBobState => BPMHeadBobState.Half;

        public override float BeatsPerMinute => 180f;

        public override List<SongSection> HeadphonesHighPoints => new();

        public override List<SongSection> HighPoints => new();

        public static List<SongSection> Bells => new()
        {
            // Section 1.
            WithMSDelay(0, 1, 134, 50),
            WithMSDelay(0, 2, 567, 50),
            WithMSDelay(0, 3, 880, 50),
            WithMSDelay(0, 5, 253, 50),
            WithMSDelay(0, 6, 566, 50),
            WithMSDelay(0, 7, 879, 50),
            WithMSDelay(0, 9, 252, 50),
            WithMSDelay(0, 10, 566, 50),
            
            // Section 2.
            WithMSDelay(1, 41, 178, 50),
            WithMSDelay(1, 41, 835, 50),
            WithMSDelay(1, 42, 551, 50),
            WithMSDelay(1, 43, 864, 50),
            WithMSDelay(1, 46, 491, 50),
            WithMSDelay(1, 47, 207, 50),
            WithMSDelay(1, 47, 804, 50),
            WithMSDelay(1, 49, 237, 50),

            // Section 3.
            WithMSDelay(1, 51, 863, 50),
            WithMSDelay(1, 52, 579, 50),
            WithMSDelay(1, 53, 176, 50),
            WithMSDelay(1, 54, 549, 50),
            WithMSDelay(1, 57, 235, 50),
            WithMSDelay(1, 57, 895, 50),
            WithMSDelay(1, 58, 549, 50),
        };
    }
}
