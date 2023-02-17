using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class HeadphonesMusicSystem : ModSceneEffect
    {
        public override int Music => MusicLoader.GetMusicSlot(InfernumMode.InfernumMusicMod, $"Sounds/Music/{Main.LocalPlayer.Infernum_Music().CurrentTrackName}");

        public override bool IsSceneEffectActive(Player player) => player.Infernum_Music().UsingHeadphones && !string.IsNullOrEmpty(player.Infernum_Music().CurrentTrackName) && InfernumMode.MusicModIsActive;

        public override SceneEffectPriority Priority => (SceneEffectPriority)20;
    }
}
