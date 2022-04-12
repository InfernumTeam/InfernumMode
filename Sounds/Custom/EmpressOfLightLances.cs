using Microsoft.Xna.Framework.Audio;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Sounds.Custom
{
    public class EmpressOfLightLances : ModSound
    {
        public override SoundEffectInstance PlaySound(ref SoundEffectInstance soundInstance, float volume, float pan, SoundType type)
        {
            soundInstance = sound.CreateInstance();
            soundInstance.Volume = volume;
            soundInstance.Pan = pan;
            SoundEngine.PlaySoundInstance(soundInstance);
            return soundInstance;
        }
    }
}
