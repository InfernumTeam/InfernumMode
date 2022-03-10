using Terraria.Audio;

namespace InfernumMode
{
    public static partial class Utilities
    {
        public static CustomSoundStyle GetTrackableSound(string path)
        {
            return new CustomSoundStyle(InfernumMode.Instance.GetSound(path), SoundType.Sound);
        }
    }
}
