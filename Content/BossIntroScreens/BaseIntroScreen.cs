using CalamityMod;
using CalamityMod.Buffs.StatBuffs;
using InfernumMode.Assets.Fonts;
using InfernumMode.Content.BossIntroScreens.InfernumScreens;
using InfernumMode.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Globalization;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace InfernumMode.Content.BossIntroScreens
{
    public abstract class BaseIntroScreen : ModType
    {
        public int AnimationTimer;

        public float AnimationCompletion => Clamp(AnimationTimer / (float)AnimationTime, 0f, 1f);

        public bool HasPlayedMainSound;

        public string CachedText = string.Empty;

        protected virtual Vector2 BaseDrawPosition
        {
            get
            {
                if (TextShouldBeCentered)
                    return new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.35f);
                return new Vector2(Main.screenWidth - 400f, Main.screenHeight - 300f);
            }
        }

        public const float MinorBossTextScale = 1.1f;

        public const float MajorBossTextScale = 1.45f;

        // Haha bottom text lmao
        public const float BottomTextScale = 2.1f;

        public static float AspectRatioFactor => Main.screenHeight / 1440f;

        public Vector2 DrawPosition => BaseDrawPosition;

        public virtual float TextScale => MinorBossTextScale;

        public virtual TextColorData TextColor => Color.White;

        public virtual Color ScreenCoverColor => Color.Black;

        public virtual int AnimationTime => ShouldCoverScreen ? 115 : 150;

        public virtual float TextDelayInterpolant => ShouldCoverScreen ? 0.4f : 0.05f;

        public virtual bool TextShouldBeCentered => false;

        public virtual bool ShouldCoverScreen => false;

        public virtual bool CaresAboutBossEffectCondition => true;

        public virtual Effect ShaderToApplyToLetters => null;

        public virtual SoundStyle? SoundToPlayWithLetterAddition { get; }

        public virtual bool CanPlaySound => AnimationTimer >= (int)(AnimationTime * (TextDelayInterpolant + 0.05f));

        public virtual LocalizedText TextToDisplay => GetLocalizedText("TextToDisplay");

        public abstract SoundStyle? SoundToPlayWithTextCreation { get; }

        protected sealed override void Register()
        {
            ModTypeLookup<BaseIntroScreen>.Register(this);

            if (!IntroScreenManager.IntroScreens.Contains(this))
                IntroScreenManager.IntroScreens.Add(this);
        }

        public sealed override void SetupContent() => SetStaticDefaults();

        public abstract bool ShouldBeActive();

        public virtual void DoCompletionEffects() { }

        public virtual void PrepareShader(Effect shader) { }

        public virtual float LetterDisplayCompletionRatio(int animationTimer) => 1f;

        public virtual void Draw(SpriteBatch sb)
        {
            bool notInvolvedWithBoss = !Main.LocalPlayer.HasBuff(ModContent.BuffType<BossEffects>());
            if (!CalamityConfig.Instance.BossZen || !CaresAboutBossEffectCondition)
                notInvolvedWithBoss = false;

            if (AnimationTimer >= AnimationTime - 1f)
            {
                CachedText = string.Empty;
                DoCompletionEffects();
            }

            if (Main.netMode == NetmodeID.Server || AnimationTimer <= 0 || AnimationTimer >= AnimationTime || notInvolvedWithBoss && !ShouldBeActive())
            {
                if (AnimationTimer < AnimationTime)
                    AnimationTimer = 0;
                return;
            }

            // Draw the screen cover if it's enabled.
            if (ShouldCoverScreen)
            {
                bool isBright = ScreenCoverColor.ToVector3().Length() / 1.414f > 0.8f;
                if (isBright)
                {
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
                }

                Texture2D greyscaleTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Thanatos/THanosAura").Value;
                float coverScaleFactor = Utils.GetLerpValue(0f, 0.5f, AnimationCompletion, true) * 12.5f;
                coverScaleFactor *= Utils.GetLerpValue(1f, 0.84f, AnimationCompletion, true);

                Vector2 coverCenter = new(Main.screenWidth * 0.5f, Main.screenHeight * 0.32f);

                for (int i = 0; i < 2; i++)
                    sb.Draw(greyscaleTexture, coverCenter, null, ScreenCoverColor, 0f, greyscaleTexture.Size() * 0.5f, coverScaleFactor, 0, 0);

                if (isBright)
                {
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, null);
                }
            }

            // Prepare the sprite batch for the shader, if one is applied.
            if (ShaderToApplyToLetters != null)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer);
            }

            DrawText(sb);

            if (ShaderToApplyToLetters != null)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, null);
            }
        }

        protected LocalizedText GetLocalizedText(string key)
        {
            string suffix = $"IntroScreen.{GetType().Name}";
            string localizationKey = $"{suffix}.{key}";

            return Utilities.GetLocalization(localizationKey);
        }


        internal Vector2 CalculateOffsetOfCharacter(string character)
        {
            float extraOffset = character.ToLower(CultureInfo.InvariantCulture) == "i" ? 9f : 0f;
            return Vector2.UnitX * (InfernumFontRegistry.BossIntroScreensFont.MeasureString(character).X + extraOffset + 10f) * AspectRatioFactor * TextScale;
        }

        public virtual void DrawText(SpriteBatch sb)
        {
            float opacity = Utils.GetLerpValue(TextDelayInterpolant, TextDelayInterpolant + 0.05f, AnimationCompletion, true) * Utils.GetLerpValue(1f, 0.77f, AnimationCompletion, true);

            if (CanPlaySound && SoundToPlayWithTextCreation != null)
            {
                if (!HasPlayedMainSound)
                {
                    SoundEngine.PlaySound(SoundToPlayWithTextCreation.Value);
                    HasPlayedMainSound = true;
                }
            }

            int absoluteLetterCounter = 0;
            bool playedNewLetterSound = false;
            string[] splitTextInstances = CachedText.Split('\n');
            for (int i = 0; i < splitTextInstances.Length; i++)
            {
                string splitText = splitTextInstances[i];
                bool useBigText = i > 0 || splitTextInstances.Length == 1;
                Vector2 offset = -Vector2.UnitX * splitText.Sum(c => CalculateOffsetOfCharacter(c.ToString()).X * (useBigText ? BottomTextScale : 1f)) * 0.5f;
                Vector2 textScale = Vector2.One * TextScale * AspectRatioFactor;
                if (i > 0)
                    offset.Y += BottomTextScale * TextScale * AspectRatioFactor * i * 24f;
                if (useBigText)
                    textScale *= BottomTextScale;

                for (int j = 0; j < splitText.Length; j++)
                {
                    float individualLineLetterCompletionRatio = j / (float)(splitText.Length - 1f);
                    float absoluteLineLetterCompletionRatio = absoluteLetterCounter / (float)(CachedText.Length - 1f);
                    int previousTotalLettersToDisplay = (int)(CachedText.Length * LetterDisplayCompletionRatio(AnimationTimer - 1));
                    int totalLettersToDisplay = (int)(CachedText.Length * LetterDisplayCompletionRatio(AnimationTimer));

                    // Play a sound if a new letter was added and a sound of this effect is initialized.
                    if (totalLettersToDisplay > previousTotalLettersToDisplay && SoundToPlayWithLetterAddition != null && !playedNewLetterSound)
                    {
                        SoundEngine.PlaySound(SoundToPlayWithLetterAddition.Value);
                        playedNewLetterSound = true;
                    }

                    // If the completion ratio of the absolute letter count has passed the termination point, stop. 
                    if (absoluteLineLetterCompletionRatio >= LetterDisplayCompletionRatio(AnimationTimer))
                        break;

                    if (ShaderToApplyToLetters != null)
                    {
                        ShaderToApplyToLetters.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
                        ShaderToApplyToLetters.Parameters["uLetterCompletionRatio"]?.SetValue(individualLineLetterCompletionRatio);
                        PrepareShader(ShaderToApplyToLetters);
                        ShaderToApplyToLetters.CurrentTechnique.Passes[0].Apply();
                    }

                    // Push the offset.
                    string character = splitText[j].ToString();
                    offset += CalculateOffsetOfCharacter(character) * (useBigText ? BottomTextScale : 1f);

                    Color textColor = TextColor.Calculate(individualLineLetterCompletionRatio) * opacity;
                    Vector2 origin = Vector2.UnitX * InfernumFontRegistry.BossIntroScreensFont.MeasureString(character);

                    // Draw afterimage instances of the the text.
                    for (int k = 0; k < 4; k++)
                    {
                        float afterimageOpacityInterpolant = Utils.GetLerpValue(1f, TextDelayInterpolant + 0.05f, AnimationCompletion, true);
                        float afterimageOpacity = Pow(afterimageOpacityInterpolant, 2f) * 0.3f;
                        Color afterimageColor = textColor * afterimageOpacity;
                        Vector2 drawOffset = (TwoPi * k / 4f).ToRotationVector2() * (1f - afterimageOpacityInterpolant) * 30f;
                        ChatManager.DrawColorCodedStringShadow(sb, InfernumFontRegistry.BossIntroScreensFont, character, DrawPosition + drawOffset + offset, Color.Black * afterimageOpacity * opacity, 0f, origin, textScale, -1, 1.5f);
                        ChatManager.DrawColorCodedString(sb, InfernumFontRegistry.BossIntroScreensFont, character, DrawPosition + drawOffset + offset, afterimageColor, 0f, origin, textScale);
                    }

                    // Draw the base text.
                    ChatManager.DrawColorCodedStringShadow(sb, InfernumFontRegistry.BossIntroScreensFont, character, DrawPosition + offset, Color.Black * opacity, 0f, origin, textScale, -1, 1.5f);
                    ChatManager.DrawColorCodedString(sb, InfernumFontRegistry.BossIntroScreensFont, character, DrawPosition + offset, textColor, 0f, origin, textScale);

                    // Increment the absolute letter counter.
                    absoluteLetterCounter++;
                }
            }
        }

        public void Update()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            if (!ShouldBeActive() || !InfernumConfig.Instance.BossIntroductionAnimationsAreAllowed)
            {
                AnimationTimer = 0;
                HasPlayedMainSound = false;
                CachedText = string.Empty;
                return;
            }

            if (string.IsNullOrEmpty(CachedText))
                CachedText = TextToDisplay.ToString();

            if (AnimationTimer < AnimationTime)
                AnimationTimer++;
        }
    }
}
