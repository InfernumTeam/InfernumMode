using System.Collections.Generic;

namespace InfernumMode.Core.TrackedMusic
{
    public class ProvidenceTrackedMusic : BaseTrackedMusic
    {
        public override string MusicPath => "CalamityModMusic/Sounds/Music/Providence";

        public override BPMHeadBobState HeadBobState => BPMHeadBobState.Half;

        public override float BeatsPerMinute => BeatsPerMinuteStatic;

        public override List<SongSection> HeadphonesHighPoints => new();

        public override List<SongSection> HighPoints => new();

        public static List<SongSection> Bells => new()
        {
            // Section 1.
            WithMSDelay(0, 1, 134, 150),
            WithMSDelay(0, 2, 567, 150),
            WithMSDelay(0, 3, 880, 150),
            WithMSDelay(0, 5, 253, 150),
            WithMSDelay(0, 6, 566, 150),
            WithMSDelay(0, 7, 879, 150),
            WithMSDelay(0, 9, 252, 150),
            WithMSDelay(0, 10, 566, 150),
            
            // Section 2.
            WithMSDelay(1, 41, 178, 150),
            WithMSDelay(1, 41, 835, 150),
            WithMSDelay(1, 42, 551, 150),
            WithMSDelay(1, 43, 864, 150),
            WithMSDelay(1, 46, 491, 150),
            WithMSDelay(1, 47, 207, 150),
            WithMSDelay(1, 47, 804, 150),
            WithMSDelay(1, 49, 237, 150),

            // Section 3.
            WithMSDelay(1, 51, 863, 150),
            WithMSDelay(1, 52, 579, 150),
            WithMSDelay(1, 53, 176, 150),
            WithMSDelay(1, 54, 549, 150),
            WithMSDelay(1, 57, 235, 150),
            WithMSDelay(1, 57, 895, 150),
            WithMSDelay(1, 58, 549, 150),
        };

        public static float BeatsPerMinuteStatic => 180f;
    }
}
