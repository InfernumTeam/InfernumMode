using Microsoft.Xna.Framework.Graphics;

namespace InfernumMode.Common.Graphics
{
    public interface ISpecializedDrawRegion
    {
        void SpecialDraw(SpriteBatch spriteBatch);

        void PrepareSpriteBatch(SpriteBatch spriteBatch);
    }
}
