using Terraria.Localization;
using Terraria.ModLoader;

namespace InfernumMode.Core.CrossCompatibility
{
    public class MusicDisplayHandler : ModSystem
    {
        internal static Mod MusicDisplay;

        public override void PostAddRecipes()
        {
            MusicDisplay = null;
            if (!ModLoader.TryGetMod("MusicDisplay", out MusicDisplay))
                return;

            void AddMusic(string path, string name)
            {
                LocalizedText author = Utilities.GetLocalization("MusicDisplay." + name + ".Author");
                LocalizedText displayName = Utilities.GetLocalization("MusicDisplay." + name + ".DisplayName");
                MusicDisplay.Call("AddMusic", (short)MusicLoader.GetMusicSlot(Mod, path), displayName, author, Mod.DisplayName);
            }

            AddMusic("Assets/Sounds/Music/LostColosseum", "ForgottenWinds");
            AddMusic("Assets/Sounds/Music/ProfanedTemple", "UnholySanctuary");
            AddMusic("Assets/Sounds/Music/SignusAmbience", "SignusAmbience");
        }
    }
}
