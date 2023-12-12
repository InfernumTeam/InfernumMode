using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Credits
{
    public class CreditMusicScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => (SceneEffectPriority)25;

        public override int Music => SetMusic();

        internal static int SetMusic()
        {
            if (InfernumMode.MusicModIsActive)
                return MusicLoader.GetMusicSlot(InfernumMode.InfernumMusicMod, "Sounds/Music/TitleScreen");
            return MusicID.Credits;
        }

        public override bool IsSceneEffectActive(Player player) => CreditManager.CreditsPlaying;
    }
}
