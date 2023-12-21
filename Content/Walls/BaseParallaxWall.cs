using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Walls
{
    public abstract class BaseParallaxWall : ModWall
    {
        /// <summary>
        /// The depth of the parallax effect. Lower values are closer, higher values are further.
        /// </summary>
        public virtual int ParallaxDepth => 8;

        public abstract Color MapColor { get; }

        public virtual Vector2 AdditionalOffset(int i, int j) => Vector2.Zero;

        public Asset<Texture2D> WallTexture
        {
            get;
            private set;
        }

        public sealed override void SetStaticDefaults()
        {
            Main.wallHouse[Type] = false;
            AddMapEntry(MapColor);

            WallTexture = ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad);
        }

        public override bool CanExplode(int i, int j) => false;

        public override void KillWall(int i, int j, ref bool fail) => fail = true;

        public sealed override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            // The texture dimensions BEFORE THE EXTENSION divided by 16.
            int width = (WallTexture.Width() - 16) / 16;
            int height = (WallTexture.Height() - 16) / 16;

            // Draw offset is required here due to Terraria's tile rendering optimizations.
            Vector2 drawOffset = (Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange)) - new Vector2(8f);
            Vector2 drawPosition = new Vector2(i, j).ToWorldCoordinates() - Main.screenPosition + drawOffset;

            // Offset the frame position.
            Vector2 frameOffset = AdditionalOffset(i, j) + Main.screenPosition;
            // Get a 16x16 frame from the texture using the offset and parallax depth to make it move with the player.
            Rectangle frame = new((int)((i * 16 + (frameOffset.X / ParallaxDepth)) % (width * 16)), (int)((j * 16 + (frameOffset.Y / ParallaxDepth)) % (height * 16)), 16, 16);

            // Do some gamma correction stuff to give the lighting WAY more depth.
            Vector3 lightColorVec = Lighting.GetColor(i, j).ToVector3();
            lightColorVec = lightColorVec.Pow3(3f);
            Color lightColor = new(lightColorVec);

            // Draw the wall.
            spriteBatch.Draw(WallTexture.Value, drawPosition, frame, lightColor with { A = 255 }, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
