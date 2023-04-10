using Microsoft.Xna.Framework.Graphics;

namespace InfernumMode.Common.Graphics.Interfaces
{
    public interface IPixelPrimitiveDrawer
    {
        public bool DrawBeforeNPCs => false;

        public void DrawPixelPrimitives(SpriteBatch spriteBatch);
    }
}
