using Terraria.Localization;

namespace InfernumMode
{
    public static partial class Utilities
    {
        /// <summary>
        /// Shortcut for <see cref="Language.GetText(string)"/> with "Mods.InfernumMode." already prefixed.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static LocalizedText GetLocalization(string key) => Language.GetOrRegister(InfernumMode.Instance.GetLocalizationKey(key));
    }
}
