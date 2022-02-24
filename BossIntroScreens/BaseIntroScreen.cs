using CalamityMod.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace InfernumMode.BossIntroScreens
{
    public abstract class BaseIntroScreen
    {
        protected virtual Vector2 BaseDrawPosition
        {
            get
            {
                if (TextShouldBeCentered)
                    return new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.35f);
                return new Vector2(Main.screenWidth - 400f, Main.screenHeight - 300f);
            }
        }

        public int AnimationTimer;

        public float AnimationCompletion => MathHelper.Clamp(AnimationTimer / (float)AnimationTime, 0f, 1f);

        public DynamicSpriteFont FontToUse => BossHealthBarManager.HPBarFont;

        public Vector2 DrawPosition => BaseDrawPosition - FontToUse.MeasureString(TextToDisplay) * new Vector2(0.5f, 0f) * TextScale;

        public virtual float TextScale => 1.5f;

        public virtual Color TextColor => Color.White;

        public virtual Color ScreenCoverColor => Color.Black;

        public virtual bool TextShouldBeCentered => false;

        public virtual bool ShouldCoverScreen => false;

        public virtual int AnimationTime => ShouldCoverScreen ? 115 : 150;

        public abstract string TextToDisplay { get; }

        public abstract bool ShouldBeActive();

        public abstract LegacySoundStyle SoundToPlayWithText { get; }

        public virtual void Draw(SpriteBatch sb)
        {
            if (Main.netMode == NetmodeID.Server || AnimationTimer <= 0 || AnimationTimer >= AnimationTime)
                return;

            // Draw the screen cover if it's enabled.
            if (ShouldCoverScreen)
            {
                sb.SetBlendState(BlendState.Additive);
                Texture2D greyscaleTexture = ModContent.GetTexture("CalamityMod/ExtraTextures/THanosAura");
                float coverScaleFactor = Utils.InverseLerp(0f, 0.65f, AnimationCompletion, true) * 12.5f;
                coverScaleFactor *= Utils.InverseLerp(1f, 0.84f, AnimationCompletion, true);

                Vector2 coverCenter = new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.32f);
                sb.Draw(greyscaleTexture, coverCenter, null, ScreenCoverColor, 0f, greyscaleTexture.Size() * 0.5f, coverScaleFactor, 0, 0);
                sb.ResetBlendState();
            }
            DrawText(sb);
        }

        public virtual void DrawText(SpriteBatch sb)
        {
            float textDelay = ShouldCoverScreen ? 0.4f : 0.05f;
            float opacity = Utils.InverseLerp(textDelay, textDelay + 0.05f, AnimationCompletion, true) * Utils.InverseLerp(1f, 0.77f, AnimationCompletion, true);
            Color textColor = TextColor * opacity;

            if (AnimationTimer == (int)(AnimationTime * (textDelay + 0.05f)) && SoundToPlayWithText != null)
                Main.PlaySound(SoundToPlayWithText, Main.LocalPlayer.Center);

            // Draw afterimage instances of the the text.
            Vector2 textScale = Vector2.One * TextScale;
            for (int i = 0; i < 3; i++)
            {
                float afterimageOpacityInterpolant = Utils.InverseLerp(1f, textDelay + 0.05f, AnimationCompletion, true);
                float afterimageOpacity = (float)Math.Pow(afterimageOpacityInterpolant, 2D) * 0.3f;
                Color afterimageColor = textColor * afterimageOpacity;
                Vector2 drawOffset = (MathHelper.TwoPi * i / 3f).ToRotationVector2() * (1f - afterimageOpacityInterpolant) * 30f;
                ChatManager.DrawColorCodedStringShadow(sb, FontToUse, TextToDisplay, DrawPosition + drawOffset, Color.Black * afterimageOpacity * opacity, 0f, Vector2.Zero, textScale);
                ChatManager.DrawColorCodedString(sb, FontToUse, TextToDisplay, DrawPosition + drawOffset, afterimageColor, 0f, Vector2.Zero, textScale);
            }

            // Draw the base text.
            ChatManager.DrawColorCodedStringShadow(sb, FontToUse, TextToDisplay, DrawPosition, Color.Black * opacity, 0f, Vector2.Zero, textScale);
            ChatManager.DrawColorCodedString(sb, FontToUse, TextToDisplay, DrawPosition, textColor, 0f, Vector2.Zero, textScale);
        }

        public void Update()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            if (!ShouldBeActive())
            {
                AnimationTimer = 0;
                return;
            }

            if (AnimationTimer < AnimationTime)
                AnimationTimer++;
        }
    }
}