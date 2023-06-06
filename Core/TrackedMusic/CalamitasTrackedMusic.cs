using System.Collections.Generic;

namespace InfernumMode.Core.TrackedMusic
{
    public class CalamitasTrackedMusic : BaseTrackedMusic
    {
        public override string MusicPath => "InfernumModeMusic/Sounds/Music/Calamitas";

        public override BPMHeadBobState HeadBobState => BPMHeadBobState.Quarter;

        public override float BeatsPerMinute => 204f;

        public static List<SongSection> SolariaSections => new()
        {
            // For in their heart remains their guiding light...
            new(TimeFormat(0, 8, 252), TimeFormat(0, 19, 124)),

            // Have you all known what it's like to feel hatred?
            // To realize that there's room for it in your heart?
            new(TimeFormat(0, 19, 517), TimeFormat(0, 32, 747)),
            
            // [S] See these scars that hide memories before I tried to live with myself?
            // [M] Those scars that hide memories of the times I could not live with myself...
            // [S] They were born of pain from my heart, which had become blackened and broken
            // [M] If they're born of pain from my heart, is it blackened and broken?
            new(TimeFormat(1, 20, 426), TimeFormat(1, 49, 636)),

            // Hear my last hopes...
            // Engrave them for all to see...
            // The shattered wings that still dream of taking flight...
            // My soul will burn...
            // (My soul will burn)
            // By the flames of calamity...
            // (By the flames of the world that scarred my heart)
            // Despite these trials of fire, in the end I still see the light...
            new(TimeFormat(2, 13, 344), TimeFormat(2, 57, 486)),

            // WHAT IF THE LIGHT THAT YOU SEE NOW IS JUST A FAKE- AN ILLUSION?
            new(TimeFormat(3, 19, 492), TimeFormat(3, 28, 6)),

            // YOU REALLY THINK THAT WHAT YOU'VE DONE WILL BE ENOUGH FOR THOSE VOICES!?
            new(TimeFormat(3, 35, 341), TimeFormat(3, 43, 463)),
            
            // THEN LET THE ONE INSIDE YOUR HEAD CONTINUE TO MAKE YOUR LIFE HELL!
            new(TimeFormat(3, 43, 463), TimeFormat(3, 51, 453)),

            // Here we are again back in the void where my heart lays dead and my mind's driven insane
            // The pale imitation of myself trying to reach for the stars
            // But if in the end there's no one that can hear the sounds of my soul writhing in pain
            // Then let the storm before dawn become as dark as my own scars
            // (Here we are again back in the void where my heart lays dead and my mind's driven insane
            // The pale imitation of myself trying to reach for the stars)
            // But if in the end there's no one that can hear the screams of my soul howling in vain
            // Then let the sky after rain become as dark as my own scars...
            new(TimeFormat(3, 52, 239), TimeFormat(5, 8, 604)),
        };

        public static List<SongSection> MaiSections => new()
        {
            // For in their heart remains their guiding light...
            new(TimeFormat(0, 8, 252), TimeFormat(0, 19, 124)),

            // Long ago I never thought I would feel it.
            // But it seems that I was just too naive to see the truth.
            new(TimeFormat(0, 32, 747), TimeFormat(0, 51, 347)),
            
            // [S] See these scars that hide memories before I tried to live with myself?
            // [M] Those scars that hide memories of the times I could not live with myself...
            // [S] They were born of pain from my heart, which had become blackened and broken
            // [M] If they're born of pain from my heart, is it blackened and broken?
            new(TimeFormat(1, 20, 426), TimeFormat(1, 49, 636)),

            // Hear my last hopes...
            // Engrave them for all to see...
            // The shattered wings that still dream of taking flight...
            // My soul will burn...
            // (My soul will burn)
            // By the flames of calamity...
            // (By the flames of the world that scarred my heart)
            // Despite these trials of fire, in the end I still see the light...
            new(TimeFormat(2, 13, 344), TimeFormat(2, 57, 486)),
            
            // WHAT YOU'RE DOING IS DELUDING YOURSELF INTO SEEING FALSE HOPES!
            new(TimeFormat(3, 28, 6), TimeFormat(3, 35, 341)),
            
            // THEN LET THE ONE INSIDE YOUR HEAD CONTINUE TO MAKE YOUR LIFE HELL!
            new(TimeFormat(3, 43, 463), TimeFormat(3, 51, 453)),

            // Here we are again back in the void where my heart lays dead and my mind's driven insane
            // The pale imitation of myself trying to reach for the stars
            // But if in the end there's no one that can hear the sounds of my soul writhing in pain
            // Then let the storm before dawn become as dark as my own scars
            // (Here we are again back in the void where my heart lays dead and my mind's driven insane
            // The pale imitation of myself trying to reach for the stars)
            // But if in the end there's no one that can hear the screams of my soul howling in vain
            // Then let the sky after rain become as dark as my own scars...
            new(TimeFormat(3, 52, 239), TimeFormat(5, 8, 604)),
        };

        public override List<SongSection> HeadphonesHighPoints => new();

        public override Dictionary<SongSection, int> SongSections => new();

        public override List<SongSection> HighPoints => new();
    }
}
