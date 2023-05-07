using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace InfernumMode.Content.UI
{
    // Custom UIScrollbar that lets you change the texture easily.
    public class AchievementUIScrollbar : UIScrollbar
    {
        public readonly Asset<Texture2D> _texture;

        public readonly Asset<Texture2D> _innerTexture;

        public AchievementUIScrollbar()
        {
            Width.Set(20f, 0f);
            MaxWidth.Set(20f, 0f);
            _texture = ModContent.Request<Texture2D>("InfernumMode/Content/Achievements/Textures/CustomScrollbarBackground", AssetRequestMode.ImmediateLoad);
            _innerTexture = Main.Assets.Request<Texture2D>("Images/UI/ScrollbarInner");
            PaddingTop = 5f;
            PaddingBottom = 5f;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetDimensions();
            CalculatedStyle innerDimensions = GetInnerDimensions();
            if ((bool)typeof(UIScrollbar).GetField("_isDragging", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(UIRenderingSystem.achievementUIManager.uIScrollbar1))
            {
                float offset = UserInterface.ActiveInstance.MousePosition.Y - innerDimensions.Y - (float)typeof(UIScrollbar).GetField("_dragYOffset", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(UIRenderingSystem.achievementUIManager.uIScrollbar1);
                ViewPosition = MathHelper.Clamp(offset / innerDimensions.Height * (float)typeof(UIScrollbar).GetField("_maxViewSize", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(UIRenderingSystem.achievementUIManager.uIScrollbar1), 0f, (float)typeof(UIScrollbar).GetField("_maxViewSize", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(UIRenderingSystem.achievementUIManager.uIScrollbar1) - (float)typeof(UIScrollbar).GetField("_viewSize", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(UIRenderingSystem.achievementUIManager.uIScrollbar1));
            }

            Rectangle handleRectangle = (Rectangle)typeof(UIScrollbar).GetMethod("GetHandleRectangle", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(UIRenderingSystem.achievementUIManager.uIScrollbar1, null);
            Vector2 mousePosition = UserInterface.ActiveInstance.MousePosition;
            bool isHoveringOverHandle = (bool)typeof(UIScrollbar).GetField("_isHoveringOverHandle", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(UIRenderingSystem.achievementUIManager.uIScrollbar1);
            bool tempBool = handleRectangle.Contains(new Point((int)mousePosition.X, (int)mousePosition.Y));
            typeof(UIScrollbar).GetField("_isHoveringOverHandle", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(UIRenderingSystem.achievementUIManager.uIScrollbar1, tempBool);
            if (!isHoveringOverHandle && (bool)typeof(UIScrollbar).GetField("_isHoveringOverHandle", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(UIRenderingSystem.achievementUIManager.uIScrollbar1) && Main.hasFocus)
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
            }

            DrawBar(spriteBatch, _texture.Value, dimensions.ToRectangle(), Color.White);
            DrawBar(spriteBatch, _innerTexture.Value, handleRectangle, Color.White * ((bool)typeof(UIScrollbar).GetField("_isDragging", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(UIRenderingSystem.achievementUIManager.uIScrollbar1) || (bool)typeof(UIScrollbar).GetField("_isHoveringOverHandle", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(UIRenderingSystem.achievementUIManager.uIScrollbar1) ? 1f : 0.85f));
        }

        internal static void DrawBar(SpriteBatch spriteBatch, Texture2D texture, Rectangle dimensions, Color color)
        {
            spriteBatch.Draw(texture, new Rectangle(dimensions.X, dimensions.Y - 6, dimensions.Width, 6), new Rectangle(0, 0, texture.Width, 6), color);
            spriteBatch.Draw(texture, new Rectangle(dimensions.X, dimensions.Y, dimensions.Width, dimensions.Height), new Rectangle(0, 6, texture.Width, 4), color);
            spriteBatch.Draw(texture, new Rectangle(dimensions.X, dimensions.Y + dimensions.Height, dimensions.Width, 6), new Rectangle(0, texture.Height - 6, texture.Width, 6), color);
        }
    }
}
