using CalamityMod.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace InfernumMode.BossIntroScreens
{
    public abstract class BaseIntroScreen
    {

        public int AnimationTimer;

        public float AnimationCompletion => MathHelper.Clamp(AnimationTimer / (float)AnimationTime, 0f, 1f);

        public bool HasPlayedMainSound = false;

        protected virtual Vector2 BaseDrawPosition
        {
            get
            {
                if (TextShouldBeCentered)
                    return new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.35f);
                return new Vector2(Main.screenWidth - 400f, Main.screenHeight - 300f);
            }
        }

        public DynamicSpriteFont FontToUse => BossHealthBarManager.HPBarFont;

        public Vector2 DrawPosition => BaseDrawPosition;

        public virtual float TextScale => MinorBossTextScale;

        public virtual TextColorData TextColor => Color.White;

        public virtual Color ScreenCoverColor => Color.Black;

        public virtual int AnimationTime => ShouldCoverScreen ? 115 : 150;

        public virtual float TextDelayInterpolant => ShouldCoverScreen ? 0.4f : 0.05f;

        public virtual bool TextShouldBeCentered => false;

        public virtual bool ShouldCoverScreen => false;

        public virtual Effect ShaderToApplyToLetters => null;

        public virtual LegacySoundStyle SoundToPlayWithLetterAddition { get; } = null;

        public virtual bool CanPlaySound => AnimationTimer >= (int)(AnimationTime * (TextDelayInterpolant + 0.05f));

        public abstract string TextToDisplay { get; }

        public abstract bool ShouldBeActive();

        public abstract LegacySoundStyle SoundToPlayWithTextCreation { get; }

        public static float MinorBossTextScale = 1.1f;

        public static float MajorBossTextScale = 1.45f;

        // Haha bottom text lmao
        public static float BottomTextScale = 2.4f;

        public static float AspectRatioFactor => Main.screenHeight / 1440f;

        public virtual void PrepareShader(Effect shader) { }

        public virtual float LetterDisplayCompletionRatio(int animationTimer) => 1f;

        public virtual void Draw(SpriteBatch sb)
        {
            if (Main.netMode == NetmodeID.Server || AnimationTimer <= 0 || AnimationTimer >= AnimationTime)
                return;

            // Draw the screen cover if it's enabled.
            if (ShouldCoverScreen)
            {
                bool isBright = ScreenCoverColor.ToVector3().Length() / 1.414f > 0.8f;
                if (isBright)
                    sb.SetBlendState(BlendState.Additive);

                Texture2D greyscaleTexture = ModContent.GetTexture("CalamityMod/ExtraTextures/THanosAura");
                float coverScaleFactor = Utils.InverseLerp(0f, 0.5f, AnimationCompletion, true) * 12.5f;
                coverScaleFactor *= Utils.InverseLerp(1f, 0.84f, AnimationCompletion, true);

                Vector2 coverCenter = new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.32f);

                for (int i = 0; i < 2; i++)
                    sb.Draw(greyscaleTexture, coverCenter, null, ScreenCoverColor, 0f, greyscaleTexture.Size() * 0.5f, coverScaleFactor, 0, 0);

                if (isBright)
                    sb.ResetBlendState();
            }

            // Prepare the sprite batch for the shader, if one is applied.
            if (ShaderToApplyToLetters != null)
                sb.EnterShaderRegion();

            DrawText(sb);

            if (ShaderToApplyToLetters != null)
                sb.ExitShaderRegion();
        }

        internal Vector2 CalculateOffsetOfCharacter(string character)
        {
            float extraOffset = character.ToLower() == "i" ? TextScale * 9f : 0f;
            return Vector2.UnitX * (FontToUse.MeasureString(character).X + extraOffset + TextScale * 10f) * AspectRatioFactor;
        }

        public virtual void DrawText(SpriteBatch sb)
        {
            float opacity = Utils.InverseLerp(TextDelayInterpolant, TextDelayInterpolant + 0.05f, AnimationCompletion, true) * Utils.InverseLerp(1f, 0.77f, AnimationCompletion, true);

            if (CanPlaySound && SoundToPlayWithTextCreation != null)
            {
                if (!HasPlayedMainSound)
                {
                    Main.PlaySound(SoundToPlayWithTextCreation, Main.LocalPlayer.Center);
                    HasPlayedMainSound = true;
                }
            }

            int absoluteLetterCounter = 0;
            bool playedNewLetterSound = false;
            string[] splitTextInstances = TextToDisplay.Split('\n');
            for (int i = 0; i < splitTextInstances.Length; i++)
            {
                string splitText = splitTextInstances[i];
                Vector2 offset = -Vector2.UnitX * splitText.Sum(c => CalculateOffsetOfCharacter(c.ToString()).X * (i > 0f ? BottomTextScale : 1f)) * 0.5f;
                Vector2 textScale = Vector2.One * TextScale * AspectRatioFactor;
                if (i > 0)
                {
                    offset.Y += BottomTextScale * TextScale * AspectRatioFactor * i * 24f;
                    textScale *= BottomTextScale;
                }

                for (int j = 0; j < splitText.Length; j++)
                {
                    float individualLineLetterCompletionRatio = j / (float)(splitText.Length - 1f);
                    float absoluteLineLetterCompletionRatio = absoluteLetterCounter / (float)(TextToDisplay.Length - 1f);
                    int previousTotalLettersToDisplay = (int)(TextToDisplay.Length * LetterDisplayCompletionRatio(AnimationTimer - 1));
                    int totalLettersToDisplay = (int)(TextToDisplay.Length * LetterDisplayCompletionRatio(AnimationTimer));

                    // Play a sound if a new letter was added and a sound of this effect is initialized.
                    if (totalLettersToDisplay > previousTotalLettersToDisplay && SoundToPlayWithLetterAddition != null && !playedNewLetterSound)
                    {
                        Main.PlaySound(SoundToPlayWithLetterAddition, Main.LocalPlayer.Center);
                        playedNewLetterSound = true;
                    }

                    // If the completion ratio of the absolute letter count has passed the termination point, stop. 
                    if (absoluteLineLetterCompletionRatio >= LetterDisplayCompletionRatio(AnimationTimer))
                        break;

                    if (ShaderToApplyToLetters != null)
                    {
                        ShaderToApplyToLetters.Parameters["uTime"].SetValue(Main.GlobalTime);
                        ShaderToApplyToLetters.Parameters["uLetterCompletionRatio"].SetValue(individualLineLetterCompletionRatio);
                        PrepareShader(ShaderToApplyToLetters);
                        ShaderToApplyToLetters.CurrentTechnique.Passes[0].Apply();
                    }

                    // Push the offset.
                    string character = splitText[j].ToString();
                    offset += CalculateOffsetOfCharacter(character) * (i > 0f ? BottomTextScale : 1f);

                    Color textColor = TextColor.Calculate(individualLineLetterCompletionRatio) * opacity;
                    Vector2 origin = Vector2.UnitX * FontToUse.MeasureString(character) * 0.5f;

                    // Draw afterimage instances of the the text.
                    for (int k = 0; k < 4; k++)
                    {
                        float afterimageOpacityInterpolant = Utils.InverseLerp(1f, TextDelayInterpolant + 0.05f, AnimationCompletion, true);
                        float afterimageOpacity = (float)Math.Pow(afterimageOpacityInterpolant, 2D) * 0.3f;
                        Color afterimageColor = textColor * afterimageOpacity;
                        Vector2 drawOffset = (MathHelper.TwoPi * k / 4f).ToRotationVector2() * (1f - afterimageOpacityInterpolant) * 30f;
                        ChatManager.DrawColorCodedStringShadow(sb, FontToUse, character, DrawPosition + drawOffset + offset, Color.Black * afterimageOpacity * opacity, 0f, origin, textScale);
                        ChatManager.DrawColorCodedString(sb, FontToUse, character, DrawPosition + drawOffset + offset, afterimageColor, 0f, origin, textScale);
                    }

                    // Draw the base text.
                    ChatManager.DrawColorCodedStringShadow(sb, FontToUse, character, DrawPosition + offset, Color.Black * opacity, 0f, origin, textScale);
                    ChatManager.DrawColorCodedString(sb, FontToUse, character, DrawPosition + offset, textColor, 0f, origin, textScale);

                    // Increment the absolute letter counter.
                    absoluteLetterCounter++;
                }
            }
        }

        public void Update()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            if (!ShouldBeActive())
            {
                AnimationTimer = 0;
                HasPlayedMainSound = false;
                return;
            }

            if (AnimationTimer < AnimationTime)
                AnimationTimer++;
        }
    }
}