using CalamityMod.Tiles.Abyss;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace InfernumMode.Content.Tiles.Misc
{
    public class AquaticScourgeSkullTile : ModTile
    {
        public const int Width = 7;
        public const int Height = 6;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileSpelunker[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
            TileObjectData.newTile.Width = Width;
            TileObjectData.newTile.Height = Height;
            TileObjectData.newTile.Origin = new Point16(3, 5);
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.CoordinateHeights = [16, 16, 16, 16, 16, 16];
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.DrawYOffset = 6;
            TileObjectData.addTile(Type);
            AddMapEntry(new Color(122, 97, 87));
        }

        public override void KillMultiTile(int i, int j, int frameX, int frameY) => HitSound = AbyssGravel.MineSound with { Pitch = 0.3f };
    }
}
