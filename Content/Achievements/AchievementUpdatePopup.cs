using InfernumMode.Core.ILEditingStuff;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace InfernumMode.Content.Achievements
{
    public class AchievementUpdatePopup : IInGameNotification
    {
        #region Fields
        private int IngameDisplayTimeLeft;

        private float Scale
        {
            get
            {
                if (IngameDisplayTimeLeft < 30)
                    return MathHelper.Lerp(0f, 1f, IngameDisplayTimeLeft / 30f);
                if (IngameDisplayTimeLeft > 285)
                    return MathHelper.Lerp(1f, 0f, (IngameDisplayTimeLeft - 285f) / 15f);

                return 1f;
            }
        }

        private float Opacity
        {
            get
            {
                float scale = Scale;
                if (scale <= 0.5f)
                    return 0f;

                return (scale - 0.5f) / 0.5f;
            }
        }

        public bool ShouldBeRemoved { get; private set; }

        public Achievement Achievement { get; private set; }

        private Rectangle AchievementIconFrame;

        public object CreationObject { get; private set; }

        private readonly Asset<Texture2D> AchievementTexture;

        private readonly Asset<Texture2D> AchievementBorderTexture;

        #endregion

        #region Methods
        public AchievementUpdatePopup(Achievement achievement)
        {
            Achievement = achievement;
            IngameDisplayTimeLeft = 300;
            AchievementIconFrame = new Rectangle(66, achievement.PositionInMainList * 66, 64, 64);
            AchievementTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Achievements/Textures/Achievement", AssetRequestMode.ImmediateLoad);
            AchievementBorderTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Achievements/Textures/InfernumAchievement_Border", AssetRequestMode.ImmediateLoad);
        }

        public void Update()
        {
            if (IngameDisplayTimeLeft > 0)
                IngameDisplayTimeLeft--;
            else
                ShouldBeRemoved = true;
        }

        public void PushAnchor(ref Vector2 anchorPosition)
        {
            float offset = 50f * Opacity;
            anchorPosition.Y -= offset;
        }

        public void DrawInGame(SpriteBatch sb, Vector2 bottomAnchorPosition)
        {
            float opacity = Opacity;
            if (opacity > 0f)
            {
                string oldPercentageText = Achievement.CurrentCompletion - 1 + "/" + Achievement.TotalCompletion;
                string percentageText = Achievement.CurrentCompletion + "/" + Achievement.TotalCompletion;
                string activeText = oldPercentageText;
                if (IngameDisplayTimeLeft <= 150)
                    activeText = percentageText;
                float textScale = Scale * 1.1f;
                Vector2 size = (FontAssets.ItemStack.Value.MeasureString(activeText) + new Vector2(65f, 10f)) * textScale;
                Rectangle drawRectangle = Utils.CenteredRectangle(bottomAnchorPosition + new Vector2(0f, (0f - size.Y) * 0.5f), size);
                Vector2 mouseScreen = Main.MouseScreen;
                bool hovering = drawRectangle.Contains(mouseScreen.ToPoint());
                Utils.DrawInvBG(c: hovering ? new Color(164, 64, 64) * 0.75f : new Color(164, 64, 64) * 0.5f, sb: sb, R: drawRectangle);
                float drawScale = textScale * 0.3f;
                Vector2 drawPosition = drawRectangle.Right() - Vector2.UnitX * textScale * (1.75f * drawScale * AchievementIconFrame.Width);
                sb.Draw(AchievementTexture.Value, drawPosition, AchievementIconFrame, Color.White * opacity, 0f, new Vector2(0f, AchievementIconFrame.Height / 2), drawScale, SpriteEffects.None, 0f);
                sb.Draw(AchievementBorderTexture.Value, drawPosition, null, Color.White * opacity, 0f, new Vector2(0f, AchievementIconFrame.Height / 2), drawScale, SpriteEffects.None, 0f);

                // Draw completion amount.
                float textScale2 = textScale;
                if (IngameDisplayTimeLeft is <= 150 and >= 130)
                    textScale2 = MathHelper.Lerp(textScale, textScale * 1.1f, Utils.GetLerpValue(130, 150, IngameDisplayTimeLeft));
                else if (IngameDisplayTimeLeft is <= 180 and > 150)
                    textScale2 = MathHelper.Lerp(textScale, textScale * 1.1f, Utils.GetLerpValue(180, 151, IngameDisplayTimeLeft));

                Utils.DrawBorderString(color: new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor / 5, Main.mouseTextColor) * opacity, sb: sb, text: activeText, pos: drawPosition - Vector2.UnitX * 15f, scale: textScale2 * 0.8f, anchorx: 1f, anchory: 0.3f);

                if (hovering)
                    OnMouseOver();
            }
        }

        private void OnMouseOver()
        {
            if (!PlayerInput.IgnoreMouseInterface)
            {
                Main.player[Main.myPlayer].mouseInterface = true;
                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    Main.mouseLeftRelease = false;
                    IngameDisplayTimeLeft = 0;
                    ShouldBeRemoved = true;
                    SoundEngine.PlaySound(SoundID.MenuOpen);
                    AchievementMenuUIHookEdit.OpenAchievementMenu();
                }
            }
        }

        public void DrawInNotificationsArea(SpriteBatch spriteBatch, Rectangle area, ref int gamepadPointLocalIndexTouse)
        {

        }
        #endregion
    }
}
