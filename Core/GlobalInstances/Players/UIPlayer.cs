using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class UIPlayer : ModPlayer
    {
        public bool DrawPlaqueUI;

        public override void OnEnterWorld()
        {
            if (!InfernumMode.CalMusicModIsActive && !InfernumMode.MusicModIsActive)
                return;

            string mods;
            if (InfernumMode.CalMusicModIsActive)
                mods = $"{InfernumMode.CalamityModMusic.DisplayName}{(InfernumMode.MusicModIsActive ? " & " + InfernumMode.InfernumMusicMod.DisplayName : string.Empty)}";
            else
                mods = $"{InfernumMode.InfernumMusicMod.DisplayName}";
            Main.NewText($"[c/b90000:Infernum Mod: You have the {mods} mod(s) enabled, these may cause some boss fights to crash.]" +
                $"\n[c/b90000:A fix is being worked on, but for the meantime disabling the mod(s) will fix the crashing.]");
        }
    }
}
