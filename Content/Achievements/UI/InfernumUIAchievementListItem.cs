using InfernumMode.Core.GlobalInstances.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace InfernumMode.Content.Achievements.UI
{
    // Copy of Terraria.GameContent.UI.Elements made to work with our achievements instead.
    public class InfernumUIAchievementListItem : UIPanel
    {
        private Achievement _achievement;

        private UIImageFramed _achievementIcon;

        private UIImage _achievementIconBorders;

        private Rectangle _iconFrame;

        private Rectangle _iconFrameUnlocked;

        private Rectangle _iconFrameLocked;

        private Asset<Texture2D> _innerPanelTopTexture;

        private Asset<Texture2D> _innerPanelBottomTexture;

        private bool _locked;

        private readonly bool _large;

        public InfernumUIAchievementListItem(Achievement achievement, bool largeForOtherLanguages)
        {
            _achievement = achievement;
            BackgroundColor = new Color(89, 26, 26) * 0.8f;
            BorderColor = new Color(44, 13, 13) * 0.8f;
            _large = largeForOtherLanguages;
            float heightOffset = 16 + _large.ToInt() * 20;
            float iconBorderLeftOffset = _large.ToInt() * 6;
            float iconBorderTopOffset = _large.ToInt() * 12;
            Height.Set(66f + heightOffset, 0f);
            Width.Set(0f, 1f);
            PaddingTop = 8f;
            PaddingLeft = 9f;
            int iconIndex = AchievementPlayer.GetIconIndex(achievement);

            _iconFrameUnlocked = new Rectangle(0, iconIndex * 66, 64, 64);
            _iconFrameLocked = _iconFrameUnlocked;
            _iconFrameLocked.X += 66;
            _iconFrame = _iconFrameLocked;
            UpdateIconFrame();

            _achievementIcon = new UIImageFramed(ModContent.Request<Texture2D>("InfernumMode/Content/Achievements/Textures/Achievement", AssetRequestMode.ImmediateLoad), _iconFrame);
            _achievementIcon.Left.Set(iconBorderLeftOffset, 0f);
            _achievementIcon.Top.Set(iconBorderTopOffset, 0f);
            Append(_achievementIcon);

            _achievementIconBorders = new UIImage(ModContent.Request<Texture2D>("InfernumMode/Content/Achievements/Textures/InfernumAchievement_Border", AssetRequestMode.ImmediateLoad));
            _achievementIconBorders.Left.Set(-4f + iconBorderLeftOffset, 0f);
            _achievementIconBorders.Top.Set(-4f + iconBorderTopOffset, 0f);
            Append(_achievementIconBorders);

            _innerPanelTopTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Achievements/Textures/InfernumAchievement_InnerPanelTop", AssetRequestMode.ImmediateLoad);
            if (_large)
                _innerPanelBottomTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Achievements/Textures/InfernumAchievement_InnerPanelBottom_Large", AssetRequestMode.ImmediateLoad);
            else
                _innerPanelBottomTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Achievements/Textures/InfernumAchievement_InnerPanelBottom", AssetRequestMode.ImmediateLoad);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            int largeOffset = _large.ToInt() * 6;

            // Lock the UI once the achievement is finished.
            _locked = !_achievement.IsCompleted;

            UpdateIconFrame();
            CalculatedStyle innerDimensions = GetInnerDimensions();
            CalculatedStyle dimensions = _achievementIconBorders.GetDimensions();
            Vector2 positionOffset = new(dimensions.X + dimensions.Width + 7f, innerDimensions.Y);
            float completionRatio = _achievement.CompletionRatio;
            bool canDrawProgress = _achievement.TotalCompletion > 1 && _locked;

            float panelWidth = innerDimensions.Width - dimensions.Width + 1f - largeOffset * 2;
            Vector2 baseScale = new(0.85f);
            Vector2 baseScale2 = new(0.92f);
            string descriptionText = FontAssets.ItemStack.Value.CreateWrappedText(_achievement.Description, (panelWidth - 20f) * (1f / baseScale2.X), Language.ActiveCulture.CultureInfo);

            Color nameTextColor = _locked ? Color.Silver : new Color(250, 190, 73);
            nameTextColor = Color.Lerp(nameTextColor, Color.White, IsMouseHovering ? 0.5f : 0f);

            Color descriptionTextColor = _locked ? Color.DarkGray : Color.Silver;
            descriptionTextColor = Color.Lerp(descriptionTextColor, Color.White, IsMouseHovering ? 1f : 0f);

            Color panelColor = IsMouseHovering ? Color.White : Color.Gray;
            Vector2 panelDrawPosition = positionOffset - Vector2.UnitY * 2f + Vector2.UnitX * largeOffset;

            // Draw the top of the panel.
            DrawPanelTop(spriteBatch, panelDrawPosition, panelWidth, panelColor);
            panelDrawPosition.Y += 3f;
            panelDrawPosition.X += 9f;
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, _achievement.Name, panelDrawPosition, nameTextColor, 0f, Vector2.Zero, baseScale, panelWidth);

            // Draw the bottom of the panel.
            panelDrawPosition.X -= 17f;

            Vector2 position = positionOffset + Vector2.UnitY * 27f + Vector2.UnitX * largeOffset;
            DrawPanelBottom(spriteBatch, position, panelWidth, panelColor);
            position.X += 8f;
            position.Y += 4f;

            // Draw the description.
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, descriptionText, position, descriptionTextColor, 0f, Vector2.Zero, baseScale2);

            // Draw the progress if it's available.
            if (canDrawProgress)
            {
                Vector2 drawPosition = panelDrawPosition + Vector2.UnitX * panelWidth + Vector2.UnitY;
                string percentageText = _achievement.CurrentCompletion + "/" + _achievement.TotalCompletion;
                Vector2 textScale = new(0.75f);
                Vector2 stringSize2 = ChatManager.GetStringSize(FontAssets.ItemStack.Value, percentageText, textScale);

                float barWidth = 80f;
                Color fillColor = new(255, 255, 100);
                Color progressBarColor = new(255, 255, 255);
                if (!IsMouseHovering)
                {
                    progressBarColor = Color.Lerp(progressBarColor, Color.Black, 0.25f);
                    fillColor = Color.Lerp(fillColor, Color.Black, 0.25f);
                }

                DrawProgressBar(spriteBatch, completionRatio, drawPosition - Vector2.UnitX * barWidth * 0.7f, barWidth, progressBarColor, fillColor, fillColor.MultiplyRGBA(new Color(new Vector4(1f, 1f, 1f, 0.5f))));
                drawPosition.X -= barWidth * 1.4f + stringSize2.X;
                ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, percentageText, drawPosition, nameTextColor, 0f, new Vector2(0f, 0f), textScale, 90f);
            }
        }

        private void DrawPanelTop(SpriteBatch spriteBatch, Vector2 position, float width, Color color)
        {
            spriteBatch.Draw(_innerPanelTopTexture.Value, position, new Rectangle(0, 0, 2, _innerPanelTopTexture.Height()), color);
            spriteBatch.Draw(_innerPanelTopTexture.Value, new Vector2(position.X + 2f, position.Y), new Rectangle(2, 0, 2, _innerPanelTopTexture.Height()), color, 0f, Vector2.Zero, new Vector2((width - 4f) / 2f, 1f), SpriteEffects.None, 0f);
            spriteBatch.Draw(_innerPanelTopTexture.Value, new Vector2(position.X + width - 2f, position.Y), new Rectangle(4, 0, 2, _innerPanelTopTexture.Height()), color);
        }

        private void DrawPanelBottom(SpriteBatch spriteBatch, Vector2 position, float width, Color color)
        {
            spriteBatch.Draw(_innerPanelBottomTexture.Value, position, new Rectangle(0, 0, 6, _innerPanelBottomTexture.Height()), color);
            spriteBatch.Draw(_innerPanelBottomTexture.Value, new Vector2(position.X + 6f, position.Y), new Rectangle(6, 0, 7, _innerPanelBottomTexture.Height()), color, 0f, Vector2.Zero, new Vector2((width - 12f) / 7f, 1f), SpriteEffects.None, 0f);
            spriteBatch.Draw(_innerPanelBottomTexture.Value, new Vector2(position.X + width - 6f, position.Y), new Rectangle(13, 0, 6, _innerPanelBottomTexture.Height()), color);
        }

        private static void DrawProgressBar(SpriteBatch spriteBatch, float progress, Vector2 spot, float Width = 169f, Color BackColor = default, Color FillingColor = default, Color BlipColor = default)
        {
            // Initialize things if nothing valid is supplied.
            progress = MathHelper.Clamp(progress, 0f, 1f);
            if (BlipColor == Color.Transparent)
                BlipColor = new Color(255, 165, 0, 127);

            if (FillingColor == Color.Transparent)
                FillingColor = new Color(255, 241, 51);

            if (BackColor == Color.Transparent)
                FillingColor = new Color(255, 255, 255);

            Texture2D barTexture = TextureAssets.ColorBar.Value;
            Texture2D pixelTexture = TextureAssets.MagicPixel.Value;
            float height = 8f;
            float width = Width / 169f;
            Vector2 position = spot + Vector2.UnitY * height + Vector2.UnitX * 1f;
            spriteBatch.Draw(barTexture, spot, new Rectangle(5, 0, barTexture.Width - 9, barTexture.Height), BackColor, 0f, new Vector2(84.5f, 0f), new Vector2(width, 1f), SpriteEffects.None, 0f);
            spriteBatch.Draw(barTexture, spot + new Vector2(width * -84.5f - 5f, 0f), new Rectangle(0, 0, 5, barTexture.Height), BackColor, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
            spriteBatch.Draw(barTexture, spot + new Vector2(width * 84.5f, 0f), new Rectangle(barTexture.Width - 4, 0, 4, barTexture.Height), BackColor, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
            position += Vector2.UnitX * (progress - 0.5f) * Width;
            position.X--;
            spriteBatch.Draw(pixelTexture, position, new Rectangle(0, 0, 1, 1), FillingColor, 0f, new Vector2(1f, 0.5f), new Vector2(Width * progress, height), SpriteEffects.None, 0f);
            if (progress != 0f)
                spriteBatch.Draw(pixelTexture, position, new Rectangle(0, 0, 1, 1), BlipColor, 0f, new Vector2(1f, 0.5f), new Vector2(2f, height), SpriteEffects.None, 0f);

            spriteBatch.Draw(pixelTexture, position, new Rectangle(0, 0, 1, 1), Color.Black, 0f, new Vector2(0f, 0.5f), new Vector2(Width * (1f - progress), height), SpriteEffects.None, 0f);
        }

        private void UpdateIconFrame()
        {
            if (!_locked)
                _iconFrame = _iconFrameUnlocked;
            else
                _iconFrame = _iconFrameLocked;

            if (_achievementIcon != null)
                _achievementIcon.SetFrame(_iconFrame);
        }
        public override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);
            BackgroundColor = new Color(119, 46, 46);
            BorderColor = new Color(56, 20, 20);
        }

        public override void MouseOut(UIMouseEvent evt)
        {
            base.MouseOut(evt);
            BackgroundColor = new Color(89, 26, 26) * 0.8f;
            BorderColor = new Color(44, 13, 13) * 0.8f;
        }
    }
}
