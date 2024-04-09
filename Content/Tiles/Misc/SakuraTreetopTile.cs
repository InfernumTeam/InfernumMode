using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Tiles.Misc
{
    public class SakuraTreetopTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileNoFail[Type] = true;
            Main.tileFrameImportant[Type] = true;
        }


        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

            Vector2 drawOffset = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawPosition = new Vector2(i + 0.5f, j + 0.25f) * 16f + drawOffset + Vector2.UnitY * 16f - Main.screenPosition;
            Vector2 origin = new(texture.Width * 0.5f, texture.Height);

            Color color = Lighting.GetColor(i, j);
            spriteBatch.Draw(texture, drawPosition, null, color, 0f, origin, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
