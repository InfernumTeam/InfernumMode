using System.Collections.Generic;

namespace InfernumMode.Core.TrackedMusic
{
    public class ProfanedGuardiansTrackedMusic : BaseTrackedMusic
    {
        public override string MusicPath => "CalamityModMusic/Sounds/Music/ProfanedGuardians";

        public override BPMHeadBobState HeadBobState => BPMHeadBobState.Half;

        public override float BeatsPerMinute => BeatsPerMinuteStatic;

        public override List<SongSection> HeadphonesHighPoints => new();

        public override List<SongSection> HighPoints => new();

        // These two are equivalent.
        public static float BeatsPerMinuteStatic => ProvidenceTrackedMusic.BeatsPerMinuteStatic;
    }
}
