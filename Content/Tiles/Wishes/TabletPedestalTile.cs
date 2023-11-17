using InfernumMode.Content.Items.Misc;
using InfernumMode.Content.Projectiles.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace InfernumMode.Content.Tiles.Wishes
{
    public class TabletPedestalTile : ModTile
    {
        public const int Width = 3;
        public const int Height = 4;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileSpelunker[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
            TileObjectData.newTile.Width = Width;
            TileObjectData.newTile.Height = Height;
            TileObjectData.newTile.Origin = new Point16(1, 3);
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16, 16, 16 };
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.addTile(Type);
            AddMapEntry(new Color(107, 142, 160));
        }

        public override bool RightClick(int i, int j)
        {
            Tile tile = Main.tile[i, j];

            int left = i - tile.TileFrameX / 18;
            int top = j - tile.TileFrameY / 18;

            if (!Main.LocalPlayer.HasItem(ModContent.ItemType<DisenchantedTablet>()))
                return true;

            Vector2 animationSpawnPosition = new Vector2(left + Width / 2, top).ToWorldCoordinates();
            animationSpawnPosition += new Vector2(0f, -24f);
            Projectile.NewProjectile(new EntitySource_WorldEvent(), animationSpawnPosition, Vector2.Zero, ModContent.ProjectileType<DisenchantedTabletProj>(), 0, 0f);

            // Eat the disenchanted tablet.
            Main.LocalPlayer.ConsumeItem(ModContent.ItemType<DisenchantedTablet>(), true);
            return true;
        }

        public override void MouseOver(int i, int j)
        {
            Main.LocalPlayer.cursorItemIconID = ModContent.ItemType<DisenchantedTablet>();
            Main.LocalPlayer.noThrow = 2;
            Main.LocalPlayer.cursorItemIconEnabled = true;
        }

        public override void MouseOverFar(int i, int j)
        {
            Main.LocalPlayer.cursorItemIconID = ModContent.ItemType<DisenchantedTablet>();
            Main.LocalPlayer.noThrow = 2;
            Main.LocalPlayer.cursorItemIconEnabled = true;
        }
    }
}
