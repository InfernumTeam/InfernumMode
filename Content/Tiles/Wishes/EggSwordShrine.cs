using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace InfernumMode.Content.Tiles.Wishes
{
    public class EggSwordShrine : ModTile
    {
        public const int Width = 3;

        public const int Height = 2;

        public static bool IsBreakable => NPC.downedGolemBoss;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileSpelunker[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
            TileObjectData.newTile.Width = Width;
            TileObjectData.newTile.Height = Height;
            TileObjectData.newTile.Origin = new Point16(1, 1);
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.CoordinateHeights = [16, 16];
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.addTile(Type);
            AddMapEntry(new Color(126, 52, 42));
        }

        public override bool CanExplode(int i, int j) => IsBreakable;

        public override bool CanKillTile(int i, int j, ref bool blockDamaged) => IsBreakable;

        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            if (Main.LocalPlayer.WithinRange(new(i * 16f, j * 16f), 450f) && !IsBreakable)
            {
                Utilities.CreateShockwave(new Vector2(i * 16f + 8f, j * 16f + 8f), 2, 8, 75f, false);
                LumUtils.BroadcastLocalizedText("Mods.InfernumMode.Status.EggSwordShrineBreakFailure", new(240, 174, 86));
            }
        }
    }
}
