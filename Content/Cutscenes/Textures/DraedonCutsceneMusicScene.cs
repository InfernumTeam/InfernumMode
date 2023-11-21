using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Cutscenes.Textures
{
    public class DraedonCutsceneMusicScene : ModSceneEffect
    {
        public override int Music => MusicLoader.GetMusicSlot("CalamityMod/Sounds/Music/DraedonsAmbience");

        public override SceneEffectPriority Priority => (SceneEffectPriority)10;

        public override bool IsSceneEffectActive(Player player)
        {
            if (CutsceneManager.IsCutsceneActive(ModContent.GetInstance<DraedonPostMechsCutscene>()))
                return true;// CutsceneManager.ActiveCutscene.Timer >= DraedonPostMechsCutscene.InitialWait / 2;

            return false;
        }
    }
}
