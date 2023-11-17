using System;
using InfernumMode.Content.BossIntroScreens.InfernumScreens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossIntroScreens
{
    // Use the modcall to create an instance of this class, instead of directly accessing it.
    // God this is clunky, but at least it's highly customizable!!
    // Seriously though, this really is a mess. I recommend looking through the setup methods and modcalls to try to understand how it all links up.
    [Autoload(false)]
    public sealed class ModCallIntroScreen : BaseIntroScreen
    {
        #region Instance Variables
        private readonly LocalizedText textToDisplay;

        private readonly int animationTime;

        private readonly bool textShouldBeCentered;

        private readonly Func<bool> shouldBeActive;

        private readonly Func<float, float, Color> textColor;

        private float textScale;

        private Action doCompletionEffects;

        private bool shouldCoverScreen;

        private Color screenCoverColor;

        private Effect shaderToApplyToLetters;

        private Action<Effect> prepareShader;

        /// <summary>
        /// Params: AnimationTimer
        /// </summary>
        private Func<int, float> letterDisplayCompletionRatio;

        private Func<SoundStyle> soundToPlayWithLetterAddition;

        /// <summary>
        /// Params: AnimationTimer, AnimationTime, TextDelayInterpolant, LetterDisplayCompletionRatio.
        /// I beg for forgiveness for writing this abomination.
        /// </summary>
        private Func<int, int, float, float, bool> canPlaySound;

        private Func<SoundStyle> soundToPlayWithTextCreation;
        #endregion

        #region Overrides
        public override LocalizedText TextToDisplay => textToDisplay;

        public override int AnimationTime => animationTime;

        public override bool TextShouldBeCentered => textShouldBeCentered;

        public override bool ShouldBeActive() => shouldBeActive();

        /// <summary>
        /// This is also really evil.
        /// </summary>
        public override TextColorData TextColor => new((ratio) => { return textColor(ratio, AnimationCompletion); });

        public override float TextScale => textScale;

        public override void DoCompletionEffects() => doCompletionEffects();

        public override bool ShouldCoverScreen => shouldCoverScreen;

        public override Color ScreenCoverColor => screenCoverColor;

        public override Effect ShaderToApplyToLetters => shaderToApplyToLetters;

        public override void PrepareShader(Effect shader) => prepareShader(shader);

        public override float LetterDisplayCompletionRatio(int animationTimer) => letterDisplayCompletionRatio(AnimationTimer);

        public override SoundStyle? SoundToPlayWithLetterAddition => soundToPlayWithLetterAddition();

        public override bool CanPlaySound => canPlaySound(AnimationTimer, AnimationTime, TextDelayInterpolant, letterDisplayCompletionRatio(AnimationTimer));

        public override SoundStyle? SoundToPlayWithTextCreation => soundToPlayWithTextCreation();
        #endregion

        #region Constructor and setup methods
        private ModCallIntroScreen(LocalizedText textToDisplay, int animationTime, bool textShouldBeCentered, Func<bool> shouldBeActive, Func<float, float, Color> textColor)
        {
            this.textToDisplay = textToDisplay;
            this.animationTime = animationTime;
            this.textShouldBeCentered = textShouldBeCentered;
            this.shouldBeActive = shouldBeActive;
            this.textColor = new(textColor);
        }

        internal static ModCallIntroScreen InitializeNewModCallIntroScreen(LocalizedText textToDisplay, int animationTime, bool textShouldBeCentered, Func<bool> shouldBeActive, Func<float, float, Color> textColor)
        {
            ModCallIntroScreen screen = new(textToDisplay, animationTime, textShouldBeCentered, shouldBeActive, textColor);
            return screen;
        }

        internal ModCallIntroScreen SetupTextScale(float textScale)
        {
            this.textScale = textScale;
            return this;
        }

        internal ModCallIntroScreen SetupCompletionEffects(Action completionEffects)
        {
            doCompletionEffects = completionEffects;
            return this;
        }

        internal ModCallIntroScreen SetupScreenCovering(Color screenCoverColor)
        {
            shouldCoverScreen = true;
            this.screenCoverColor = screenCoverColor;
            return this;
        }

        internal ModCallIntroScreen SetupLetterShader(Effect shaderToApplyToLetters, Action<Effect> prepareShader)
        {
            this.shaderToApplyToLetters = shaderToApplyToLetters;
            this.prepareShader = prepareShader;
            return this;
        }

        internal ModCallIntroScreen SetupLetterDisplayCompletionRatio(Func<int, float> letterDisplayCompletionRatio)
        {
            this.letterDisplayCompletionRatio = letterDisplayCompletionRatio;
            return this;
        }

        internal ModCallIntroScreen SetupLetterAdditionSound(Func<SoundStyle> soundToPlayWithLetterAddition)
        {
            this.soundToPlayWithLetterAddition = soundToPlayWithLetterAddition;
            return this;
        }

        // https://tenor.com/bxqzw.gif
        internal ModCallIntroScreen SetupMainSound(Func<int, int, float, float, bool> canPlaySound, Func<SoundStyle> mainSound)
        {
            this.canPlaySound = canPlaySound;
            soundToPlayWithTextCreation = mainSound;
            return this;
        }

        internal void RegisterIntroScreen()
        {
            IntroScreenManager.IntroScreens.Add(this);
        }
        #endregion
    }
}
