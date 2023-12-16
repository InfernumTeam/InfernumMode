using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Walls
{
    public class ProfanedTempleBGWall : ModWall
    {
        public Asset<Texture2D> WallTexture
        {
            get;
            private set;
        }

        public override void SetStaticDefaults()
        {
            Main.wallHouse[Type] = false;
            AddMapEntry(Color.SaddleBrown);

            WallTexture = ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad);
        }

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            // The texture dimensions BEFORE THE EXTENSION divided by 16.
            int width = (WallTexture.Width() - 16) / 16;
            int height = (WallTexture.Height() - 16) / 16;

            // Smaller -> Larger
            // Closer -> Further
            int parallaxDepth = 14;

            // Draw offset is required here due to Terraria's tile rendering optimizations.
            Vector2 drawOffset = (Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange)) - new Vector2(8f);
            Vector2 drawPosition = new Vector2(i, j).ToWorldCoordinates() - Main.screenPosition + drawOffset;

            // Offset the frame position by a static amount + the screen position, to line up one of the vertical corridors visually.
            Vector2 frameOffset = WorldSaveSystem.ProvidenceArena.TopLeft() - new Vector2(i - 300, j - 1500) + Main.screenPosition;
            // Get a 16x16 frame from the texture using the offset and parallax depth to make it move with the player.
            Rectangle frame = new((i * 16 - (int)(frameOffset.X / parallaxDepth)) % (width * 16), (j * 16 - (int)(frameOffset.Y / parallaxDepth)) % (height * 16), 16, 16);

            // Do some gamma correction stuff to give the lighting WAY more depth.
            Vector3 lightColorVec = Lighting.GetColor(i, j).ToVector3();
            lightColorVec = lightColorVec.Pow3(2.5f);
            Color lightColor = new(lightColorVec);

            // Draw the wall.
            spriteBatch.Draw(WallTexture.Value, drawPosition, frame, lightColor with { A = 255 }, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
