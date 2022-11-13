using CalamityMod;
using CalamityMod.Tiles.Abyss;
using InfernumMode.Items.Placeables;
using InfernumMode.WorldGeneration;
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

        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            Point p = new(i, j);
            if (Main.tile[p.X, p.Y - 1].HasTile)
            {
                if (Main.tile[p.X, p.Y - 1].TileType == ModContent.TileType<SulphurousGroundVines>())
                {
                    WorldGen.KillTile(p.X, p.Y - 1, false, false, false);
                    if (!Main.tile[p.X, p.Y - 1].HasTile && Main.netMode != NetmodeID.SinglePlayer)
                        NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, p.X, p.Y - 1);
                }
            }
        }

        public static bool TryToGrowSmallPlantAbove(Point p)
        {
            Tile t = CalamityUtils.ParanoidTileRetrieval(p.X, p.Y);

            // If the tile is empty or not gravel, plants cannot grow.
            if (!t.HasTile || t.TileType != TileType)
                return false;

            // If the above or current tile is occupied, plants cannot grow.
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

        public static bool TryToGrowVine(Point p)
        {
            if (!WorldGen.SolidTile(p) || Main.tile[p.X, p.Y - 1].TileType == ModContent.TileType<SulphurousGroundVines>())
                return false;

            if (!CustomAbyss.InsideOfLayer1Forest(p))
                return false;

            // Check if there are any tiles above. If there are, don't grow vines.
            for (int dy = 1; dy < 7; dy++)
            {
                if (Main.tile[p.X, p.Y - dy].HasTile)
                    return false;
            }

            Main.tile[p.X, p.Y - 1].TileType = (ushort)ModContent.TileType<SulphurousGroundVines>();
            Main.tile[p.X, p.Y - 1].Get<TileWallWireStateData>().TileFrameX = (short)(WorldGen.genRand.Next(6) * 18);
            Main.tile[p.X, p.Y - 1].Get<TileWallWireStateData>().TileFrameY = 0;
            Main.tile[p.X, p.Y - 1].Get<TileWallWireStateData>().HasTile = true;
            return true;
        }

        public override void RandomUpdate(int i, int j) => TryToGrowVine(new(i, j));

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
        {
            TileFraming.CustomMergeFrame(i, j, Type, ModContent.TileType<SulphurousGravel>(), false, false, false, false, resetFrame);
            return false;
        }
    }
}
