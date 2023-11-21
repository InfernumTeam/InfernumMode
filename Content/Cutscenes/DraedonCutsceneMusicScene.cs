using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Cutscenes
{
    public class DraedonCutsceneMusicScene : ModSceneEffect
    {
        public override int Music => MusicLoader.GetMusicSlot("CalamityMod/Sounds/Music/DraedonsAmbience");

        public override SceneEffectPriority Priority => (SceneEffectPriority)10;

        public override bool IsSceneEffectActive(Player player) => CutsceneManager.IsActive(ModContent.GetInstance<DraedonPostMechsCutscene>());
    }
}
