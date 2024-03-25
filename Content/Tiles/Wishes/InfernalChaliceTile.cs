using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace InfernumMode.Content.Tiles.Wishes
{
    public class InfernalChaliceTile : ModTile
    {
        public const int Width = 3;
        public const int Height = 5;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileSpelunker[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
            TileObjectData.newTile.Width = Width;
            TileObjectData.newTile.Height = Height;
            TileObjectData.newTile.Origin = new Point16(1, 4);
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.CoordinateHeights = [16, 16, 16, 16, 16];
            TileObjectData.newTile.DrawYOffset = 4;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.addTile(Type);
            AddMapEntry(new Color(106, 46, 96));
        }

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile t = Framing.GetTileSafely(i, j);
            if (t.TileFrameX != 0 || t.TileFrameY != 0)
                return false;

            Texture2D texture = ModContent.Request<Texture2D>("InfernumMode/Content/Tiles/Wishes/InfernalChaliceTileAnimation").Value;
            Texture2D glowmask = ModContent.Request<Texture2D>("InfernumMode/Content/Tiles/Wishes/InfernalChaliceTileAnimation").Value;
            Color color = Lighting.GetColor(i, j);
            int frame = (int)(i + j + 31 + Main.GlobalTimeWrappedHourly * 11f) % 8;
            Vector2 drawPosition = new Vector2(i * 16f, j * 16f) - Main.screenPosition - Vector2.UnitY * 8f;
            if (!Main.drawToScreen)
                drawPosition += Vector2.One * Main.offScreenRange;

            spriteBatch.Draw(texture, drawPosition, texture.Frame(1, 8, 0, frame), color, 0f, Vector2.Zero, 1f, 0, 0f);
            spriteBatch.Draw(glowmask, drawPosition, texture.Frame(1, 8, 0, frame), Color.White, 0f, Vector2.Zero, 1f, 0, 0f);

            return false;
        }
    }
}
