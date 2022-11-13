using CalamityMod;
using CalamityMod.Tiles.Abyss;
using InfernumMode.Items.Placeables;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Tiles.Abyss
{
    public class SulphurousGravel : ModTile
    {
        public static int TileType
        {
            get;
            private set;
        }

        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;

            CalamityUtils.MergeWithGeneral(Type);
            CalamityUtils.MergeWithAbyss(Type);
            CalamityUtils.SetMerge(Type, ModContent.TileType<AbyssGravel>());

            DustType = 32;
            ItemDrop = ModContent.ItemType<SulphurousGravelItem>();
            AddMapEntry(new Color(84, 90, 127));
            MineResist = 1.3f;
            HitSound = SoundID.Dig;

            // Save the ID.
            TileType = Type;
        }

        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }

        public static bool TryToGrowSmallPlantAbove(Point p)
        {
            Tile t = CalamityUtils.ParanoidTileRetrieval(p.X, p.Y);

            // If the tile is empty or not gravel, plants cannot grow.
            if (!t.HasTile || t.TileType != TileType)
                return false;

            // If the above tile is occupied, plants cannot grow.
            Tile above = CalamityUtils.ParanoidTileRetrieval(p.X, p.Y - 1);
            if (above.HasTile)
                return false;

            Main.tile[p.X, p.Y - 1].TileType = (ushort)ModContent.TileType<SulphurousPlants>();
            Main.tile[p.X, p.Y - 1].TileFrameX = (short)(Main.rand.Next(23) * 18);
            Main.tile[p.X, p.Y - 1].Get<TileWallWireStateData>().HasTile = true;

            // Ensure that the ground tile is solid.
            Main.tile[p].Get<TileWallWireStateData>().IsHalfBlock = false;
            Main.tile[p].Get<TileWallWireStateData>().Slope = SlopeType.Solid;

            return true;
        }

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
        {
            TileFraming.CustomMergeFrame(i, j, Type, ModContent.TileType<SulphurousGravel>(), false, false, false, false, resetFrame);
            return false;
        }
    }
}
