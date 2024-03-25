using System.Collections.Generic;

namespace InfernumMode.Core.TrackedMusic
{
    public class CultistTrackedMusic : BaseTrackedMusic
    {
        public override string MusicPath => "InfernumModeMusic/Sounds/Music/LunaticCultist";

        public override BPMHeadBobState HeadBobState => BPMHeadBobState.Quarter;

        public override float BeatsPerMinute
        {
            // DAMN YOU JARETO YOU MF
            get
            {
                // Ending section.
                if (TrackedMusicManager.SongElapsedTime >= TimeFormat(2, 29, 310))
                    return 220f;

                // Solar section.
                if (TrackedMusicManager.SongElapsedTime >= TimeFormat(1, 52, 674))
                    return 188f;

                // Stardust section.
                if (TrackedMusicManager.SongElapsedTime >= TimeFormat(1, 20, 62))
                    return 144f;

                // Vortex section.
                if (TrackedMusicManager.SongElapsedTime >= TimeFormat(0, 45, 19))
                    return 176f;

                // Starting section.
                return 220f;
            }
        }

        public override List<SongSection> HeadphonesHighPoints =>
        [
            new(TimeFormat(0, 8, 886), TimeFormat(0, 18, 778)),

            new(TimeFormat(1, 2, 373), TimeFormat(1, 15, 787)),

            new(TimeFormat(1, 30, 709), TimeFormat(1, 40, 769)),

            new(TimeFormat(2, 5, 836), TimeFormat(2, 15, 645)),

            new(TimeFormat(2, 30, 316), TimeFormat(3, 10, 808)),
        ];

        public override List<SongSection> HighPoints => [];
    }
}
