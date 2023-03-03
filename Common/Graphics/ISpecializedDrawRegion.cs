using Microsoft.Xna.Framework.Graphics;

namespace InfernumMode.Common.Graphics
{
    // Used the sake of clumping up projectile drawcode together so that multiple projectiles don't drag the game's performance to a halt due to excessive sprite batch
    // restarts.
    public interface ISpecializedDrawRegion
    {
        void SpecialDraw(SpriteBatch spriteBatch);

        void PrepareSpriteBatch(SpriteBatch spriteBatch);
    }
}
