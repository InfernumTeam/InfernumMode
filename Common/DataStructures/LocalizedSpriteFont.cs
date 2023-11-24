using Microsoft.Xna.Framework;
using ReLogic.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria.GameContent;
using Terraria.Localization;

namespace InfernumMode.Common.DataStructures
{
    /// <summary>
    /// A wrapper for <see cref="DynamicSpriteFont"/> that dynamically accounts for unsupported languages, to avoid showing unsupported and unreadable text to the player.
    /// </summary>
    public sealed class LocalizedSpriteFont
    {
        private readonly DynamicSpriteFont EnglishFont;

        private readonly GameCulture[] DefaultSupportedLanguages;

        private readonly Dictionary<GameCulture, DynamicSpriteFont> OtherFonts;

        public DynamicSpriteFont Font
        {
            get
            {
                // Return the default english font if english is active, or it supports the current language by default.
                if (Utilities.LanguageIsActive(GameCulture.CultureName.English) || DefaultSupportedLanguages.Contains(Language.ActiveCulture))
                    return EnglishFont;

                // If there is a custom font registered for this language, return that.
                if (OtherFonts.TryGetValue(Language.ActiveCulture, out var font))
                    return font;

                // Else just return AndyBold as a fallback, as that as localization for every language.
                return FontAssets.MouseText.Value;
            }
        }

        /// <summary>
        /// Creates a font that dynamically accounts for unsupported languages. Defaults to AndyBold if a font cannot be found for the current language.
        /// </summary>
        /// <param name="defaultFont">The English font to use by default</param>
        /// <param name="defaultSupportedLanguages">The languages (other than English) that the font supports natively.</param>
        public LocalizedSpriteFont(DynamicSpriteFont defaultFont, params GameCulture.CultureName[] defaultSupportedLanguages)
        {
            EnglishFont = defaultFont;
            OtherFonts = new();

            GameCulture[] langages = new GameCulture[defaultSupportedLanguages.Length];
            for (int i = 0; i < defaultSupportedLanguages.Length; i++)
                langages[i] = GameCulture.FromCultureName(defaultSupportedLanguages[i]);

            DefaultSupportedLanguages = langages;
        }

        /// <summary>
        /// Registers a replacement font to use for the specified languge if it is not natively supported by this font.
        /// </summary>
        /// <param name="language"></param>
        /// <param name="font"></param>
        /// <returns></returns>
        public LocalizedSpriteFont WithLanguage(GameCulture.CultureName language, DynamicSpriteFont font)
        {
            if (!OtherFonts.ContainsKey(GameCulture.FromCultureName(language)))
                OtherFonts.Add(GameCulture.FromCultureName(language), font);
            return this;
        }

        public Vector2 MeasureString(string text) => Font.MeasureString(text);

        public static implicit operator DynamicSpriteFont(LocalizedSpriteFont font) => font.Font;
    }
}
