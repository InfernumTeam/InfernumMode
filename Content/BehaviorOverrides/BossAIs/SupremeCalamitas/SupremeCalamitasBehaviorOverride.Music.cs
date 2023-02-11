using InfernumMode.Core.OverridingSystem;
using System;
using System.Collections.Generic;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public partial class SupremeCalamitasBehaviorOverride : NPCBehaviorOverride
    {
        public static List<TimeSpan> Grief_HighPoints => new()
        {
            // No more holding back
            TimeFormat(0, 15, 980),
            TimeFormat(0, 20, 130),

            // Again...
            TimeFormat(0, 58, 315),
            TimeFormat(1, 8, 484),

            // They were seen as a threat from the moment the dragon was killed by their hand
            TimeFormat(1, 30, 274),
            TimeFormat(1, 41, 342),
            
            // Are you sure you'd want to lose your mind from a legend who's power could salvage mandkind?
            TimeFormat(2, 24, 370),
            TimeFormat(2, 37, 928),
        };

        public static List<TimeSpan> Lament_HighPoints => new()
        {
            // You have known for certain
            TimeFormat(0, 10, 702),
            TimeFormat(0, 15, 632),

            // Yet...
            TimeFormat(0, 31, 985),
            TimeFormat(0, 34, 30),

            // Signs of dissapointment
            TimeFormat(0, 53, 90),
            TimeFormat(0, 57, 540),

            // Sadness...
            TimeFormat(1, 41, 245),
            TimeFormat(1, 44, 725),
            
            // Courage...
            TimeFormat(1, 49, 542),
            TimeFormat(1, 51, 226),
            
            // Madness...
            TimeFormat(1, 54, 892),
            TimeFormat(1, 57, 659),

            // Strife...
            TimeFormat(2, 18, 512),
            TimeFormat(2, 22, 490),
        };

        public static Dictionary<TimeSpan, float> Epiphany_HighPoints => new()
        {
            // The end...
            [TimeFormat(0, 32, 314)] = 1f,
            [TimeFormat(0, 34, 558)] = 1f,

            // Has come-
            // The witch, and the prophecy
            [TimeFormat(0, 34, 559)] = 1.75f,
            [TimeFormat(0, 42, 411)] = 1.75f,

            // The world on the line
            [TimeFormat(0, 47, 909)] = 1f,
            [TimeFormat(0, 53, 407)] = 1f,

            // Calamity...
            [TimeFormat(1, 0, 588)] = 1.6f,
            [TimeFormat(1, 4, 178)] = 1.6f,

            // Intertwined?
            [TimeFormat(1, 10, 908)] = 1f,
            [TimeFormat(1, 14, 276)] = 1f,

            // Heavy instruments
            [TimeFormat(1, 41, 316)] = 0.8f,
            [TimeFormat(1, 42, 551)] = 0.8f,

            // Reasoning...
            [TimeFormat(2, 4, 93)] = 0.8f,
            [TimeFormat(2, 5, 551)] = 0.8f,

            // Our world to change and will be remembered...
            [TimeFormat(2, 34, 275)] = 1.1f,
            [TimeFormat(2, 39, 100)] = 1.1f,

            // Throughout the...
            [TimeFormat(2, 39, 101)] = 1.3f,
            [TimeFormat(2, 42, 241)] = 1.3f,

            // Years...
            [TimeFormat(2, 42, 242)] = 2f,
            [TimeFormat(2, 53, 798)] = 2f,

            // Ambiguous lyrics
            [TimeFormat(3, 3, 559)] = 1.4f,
            [TimeFormat(3, 5, 354)] = 1.4f,

            // Calamity bells
            [TimeFormat(3, 12, 423)] = 1f,
            [TimeFormat(3, 13, 770)] = 1f,

            // The...
            [TimeFormat(3, 24, 652)] = 1.3f,
            [TimeFormat(3, 27, 121)] = 1.3f,

            // Years...
            [TimeFormat(3, 28, 18)] = 2f,
            [TimeFormat(4, 7, 961)] = 2f,
        };

        public static TimeSpan TimeFormat(int minutes, int seconds, int milliseconds)
        {
            return new TimeSpan(0, 0, minutes, seconds, milliseconds);
        }
    }
}