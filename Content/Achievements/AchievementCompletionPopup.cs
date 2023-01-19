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
    public class AchivementCompletionPopup : IInGameNotification
    {
        #region Fields

        private readonly string Title;

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

        public object CreationObject { get; private set; }

        private Rectangle AchievementIconFrame;

        private readonly Asset<Texture2D> AchievementTexture;

        private readonly Asset<Texture2D> AchievementBorderTexture;

        #endregion

        #region Methods
        public AchivementCompletionPopup(Achievement achievement)
        {
            CreationObject = achievement;
            IngameDisplayTimeLeft = 300;
            Title = achievement.Name;
            AchievementIconFrame = new Rectangle(0, achievement.PositionInMainList * 66, 64, 64);
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
                float textScale = Scale * 1.1f;
                Vector2 size = (FontAssets.ItemStack.Value.MeasureString(Title) + new Vector2(58f, 10f)) * textScale;
                Rectangle drawRectangle = Utils.CenteredRectangle(bottomAnchorPosition + new Vector2(0f, (0f - size.Y) * 0.5f), size);
                Vector2 mouseScreen = Main.MouseScreen;
                bool hovering = drawRectangle.Contains(mouseScreen.ToPoint());
                Utils.DrawInvBG(c: hovering ? new Color(164, 64, 64) * 0.75f : new Color(164, 64, 64) * 0.5f, sb: sb, R: drawRectangle);
                float drawScale = textScale * 0.3f;
                Vector2 drawPosition = drawRectangle.Right() - Vector2.UnitX * textScale * (12f + drawScale * AchievementIconFrame.Width);
                sb.Draw(AchievementTexture.Value, drawPosition, AchievementIconFrame, Color.White * opacity, 0f, new Vector2(0f, AchievementIconFrame.Height / 2), drawScale, SpriteEffects.None, 0f);
                sb.Draw(AchievementBorderTexture.Value, drawPosition, null, Color.White * opacity, 0f, new Vector2(0f, AchievementIconFrame.Height / 2), drawScale, SpriteEffects.None, 0f);
                Utils.DrawBorderString(color: new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor / 5, Main.mouseTextColor) * opacity, sb: sb, text: Title, pos: drawPosition - Vector2.UnitX * 10f, scale: textScale * 0.9f, anchorx: 1f, anchory: 0.4f);
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
