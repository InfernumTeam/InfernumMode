using Microsoft.Xna.Framework.Audio;
using System.Reflection;

namespace InfernumMode
{
    public static partial class Utilities
    {
        public static readonly MethodInfo ApplyReverbFunction = typeof(SoundEffectInstance).GetMethod("INTERNAL_applyReverb", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly MethodInfo ApplyLowPassFunction = typeof(SoundEffectInstance).GetMethod("INTERNAL_applyLowPassFilter", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void SetReverb(this SoundEffectInstance sound, float reverb)
        {
            ApplyReverbFunction.Invoke(sound, new object[] { Clamp(reverb, 0f, 1f) });
        }

        public static void SetLowPassFilter(this SoundEffectInstance sound, float filter)
        {
            ApplyLowPassFunction.Invoke(sound, new object[] { Clamp(filter, 0f, 1f) });
        }
    }
}
