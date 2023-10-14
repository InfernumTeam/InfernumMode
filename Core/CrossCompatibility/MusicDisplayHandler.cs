using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                LocalizedText author = Language.GetText("Mods.InfernumModeMusic.MusicDisplay." + name + ".Author");
                LocalizedText displayName = Language.GetText("Mods.InfernumModeMusic.MusicDisplay." + name + ".DisplayName");
                MusicDisplay.Call("AddMusic", (short)MusicLoader.GetMusicSlot(Mod, path), displayName, author, Mod.DisplayName);
            }

            AddMusic("Assets/Sounds/Music/LostColosseum", "Forgotten Winds");
            AddMusic("Assets/Sounds/Music/ProfanedTemple", "Unholy Sanctuary");
            AddMusic("Assets/Sounds/Music/SignusAmbience", "???");
        }
    }
}
