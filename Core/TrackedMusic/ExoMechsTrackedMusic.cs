using System.Collections.Generic;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.ExoMechAIUtilities;

namespace InfernumMode.Core.TrackedMusic
{
    public class ExoMechsTrackedMusic : BaseTrackedMusic
    {
        public override string MusicPath => "InfernumModeMusic/Sounds/Music/ExoMechBosses";

        public override BPMHeadBobState HeadBobState => BPMHeadBobState.Quarter;

        public override float BeatsPerMinute => 204f;

        public override List<SongSection> HeadphonesHighPoints =>
        [
            new(TimeFormat(0, 19, 185), TimeFormat(0, 28, 228)),

            new(TimeFormat(0, 43, 747), TimeFormat(0, 52, 545)),

            new(TimeFormat(1, 9, 409), TimeFormat(1, 12, 98)),

            new(TimeFormat(2, 24, 929), TimeFormat(2, 45, 213)),

            new(TimeFormat(4, 14, 418), TimeFormat(4, 33, 236)),
        ];

        public override Dictionary<SongSection, int> SongSections => new()
        {
            // Thanatos section 1
            [new(TimeFormat(0, 19, 94), TimeFormat(0, 43, 802))] = (int)ExoMechMusicPhases.Thanatos,
            // ArtApo section 1
            [new(TimeFormat(0, 43, 803), TimeFormat(1, 2, 747))] = (int)ExoMechMusicPhases.Twins,
            // Ares section 1
            [new(TimeFormat(1, 12, 028), TimeFormat(1, 27, 391))] = (int)ExoMechMusicPhases.Ares,
            // All 3 Section 1
            [new(TimeFormat(1, 27, 392), TimeFormat(2, 05, 70))] = (int)ExoMechMusicPhases.AllThree,

            // Thanatos section 2
            [new(TimeFormat(2, 25, 0), TimeFormat(2, 29, 665))] = (int)ExoMechMusicPhases.Thanatos,
            // ArtApo section 2
            [new(TimeFormat(2, 29, 666), TimeFormat(2, 34, 385))] = (int)ExoMechMusicPhases.Twins,
            // Ares section 2
            [new(TimeFormat(2, 34, 386), TimeFormat(2, 37, 070))] = (int)ExoMechMusicPhases.Ares,
            // Thanatos section 3
            [new(TimeFormat(2, 37, 071), TimeFormat(2, 40, 254))] = (int)ExoMechMusicPhases.Thanatos,
            // Twins section 3
            [new(TimeFormat(2, 40, 255), TimeFormat(2, 41, 321))] = (int)ExoMechMusicPhases.Twins,
            // Ares section 3
            [new(TimeFormat(2, 41, 322), TimeFormat(2, 45, 012))] = (int)ExoMechMusicPhases.Ares,
            // All 3 section 2
            [new(TimeFormat(2, 45, 013), TimeFormat(3, 22, 659))] = (int)ExoMechMusicPhases.AllThree,

            // Draedon section
            [new(TimeFormat(3, 22, 659), TimeFormat(4, 09, 793))] = (int)ExoMechMusicPhases.Draedon,

            // All 3 section 3
            [new(TimeFormat(4, 14, 421), TimeFormat(4, 52, 055))] = (int)ExoMechMusicPhases.Draedon
        };

        public override List<SongSection> HighPoints => [];
    }
}
