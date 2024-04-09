using System.Collections.Generic;
using InfernumMode.Content.BehaviorOverrides.BossAIs.Deerclops;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace InfernumMode.Content.Tiles.Abyss
{
    public class LargeLumenylCrystal : ModTile
    {
        internal static Dictionary<Point, IcicleDrawer> CrystalCache = [];

        public override void SetStaticDefaults()
        {
            // This is necessary to ensure that the primitives properly render.
            TileID.Sets.DrawTileInSolidLayer[Type] = true;

            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileObsidianKill[Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
            TileObjectData.addTile(Type);
            AddMapEntry(new Color(32, 214, 200));

            HitSound = SoundID.Item27;
            DustType = 67;

            base.SetStaticDefaults();
        }

        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            r = 0f;
            g = 0.5f;
            b = 0.76f;
        }

        public override bool CanPlace(int i, int j)
        {
            if (Main.tile[i, j + 1].Slope == 0 && !Main.tile[i, j + 1].IsHalfBlock)
                return true;
            if (Main.tile[i, j - 1].Slope == 0 && !Main.tile[i, j - 1].IsHalfBlock)
                return true;
            if (Main.tile[i + 1, j].Slope == 0 && !Main.tile[i + 1, j].IsHalfBlock)
                return true;
            if (Main.tile[i - 1, j].Slope == 0 && !Main.tile[i - 1, j].IsHalfBlock)
                return true;
            return false;
        }

        public override void PlaceInWorld(int i, int j, Item item) => DecideFrame(i, j);

        public static void DecideFrame(int i, int j)
        {
            Tile t = Framing.GetTileSafely(i, j);
            Tile left = Framing.GetTileSafely(i - 1, j);
            Tile right = Framing.GetTileSafely(i + 1, j);
            Tile top = Framing.GetTileSafely(i, j - 1);
            Tile bottom = Framing.GetTileSafely(i, j + 1);

            t.TileFrameX = (short)(WorldGen.genRand.Next(18) * 18);
            if (bottom.HasTile && Main.tileSolid[bottom.TileType] && bottom.Slope == 0 && !bottom.IsHalfBlock)
                t.TileFrameY = 0;
            else if (top.HasTile && Main.tileSolid[top.TileType] && top.Slope == 0 && !top.IsHalfBlock)
                t.TileFrameY = 18;
            else if (right.HasTile && Main.tileSolid[right.TileType] && right.Slope == 0 && !right.IsHalfBlock)
                t.TileFrameY = 36;
            else if (left.HasTile && Main.tileSolid[left.TileType] && left.Slope == 0 && !left.IsHalfBlock)
                t.TileFrameY = 54;
        }

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch) => false;

        public static void DefineCrystalDrawers()
        {
            int crystalID = ModContent.TileType<LargeLumenylCrystal>();

            // Cached for performance reasons. Profiling revealed that all of the LocalPlayer/Center getters were causing slowdowns.
            Vector2 playerCenter = Main.LocalPlayer.Center;

            int width = (int)Ceiling(Main.screenWidth / 18) + 6;
            int height = (int)Ceiling(Main.screenHeight / 18) + 6;
            for (int k = 0; k < width * height; k++)
            {
                int dx = k % width - width / 2;
                int dy = k / width - height / 2;
                int i = (int)(playerCenter.X / 16f + dx);
                int j = (int)(playerCenter.Y / 16f + dy);
                Point p = new(i, j);
                if (!WorldGen.InWorld(i, j, 1))
                    continue;

                Tile t = Main.tile[p];
                if (t.TileType != crystalID)
                {
                    if (CrystalCache.ContainsKey(p))
                        CrystalCache.Remove(p);
                    continue;
                }

                // Create a crystal drawer if one does not exist yet. This will also create the crystal's mesh.
                if (!CrystalCache.ContainsKey(p))
                {
                    float offsetDirection = 0f;
                    Tile top = Framing.GetTileSafely(p.X, p.Y - 1);
                    Tile left = Framing.GetTileSafely(p.X - 1, p.Y);
                    Tile right = Framing.GetTileSafely(p.X + 1, p.Y);
                    if (top.HasTile && top.Slope == SlopeType.Solid && !top.IsHalfBlock && WorldGen.SolidTile(top))
                        offsetDirection = Pi;
                    else if (left.HasTile && left.Slope == SlopeType.Solid && !left.IsHalfBlock && WorldGen.SolidTile(left))
                        offsetDirection = PiOver2;
                    else if (right.HasTile && right.Slope == SlopeType.Solid && !right.IsHalfBlock && WorldGen.SolidTile(right))
                        offsetDirection = -PiOver2;

                    float baseDistance = Lerp(40f, 64f, (i * 0.13f + j * 3.84f) % 1f);
                    if ((i * 2 + j * 3) % 7 == 0)
                        baseDistance *= 1.6f;

                    CrystalCache[p] = new()
                    {
                        Seed = p.X + p.Y * 3111,
                        MaxDistanceBeforeCutoff = baseDistance,
                        DistanceUsedForBase = baseDistance,
                        BranchMaxBendFactor = 0.076f,
                        BranchTurnAngleVariance = 0.11f,
                        MinBranchLength = 85f,
                        BaseWidth = 10f,
                        ChanceToCreateNewBranches = 0f,
                        VerticalStretchFactor = 1f,
                        BranchGrowthWidthDecay = 0.6f,
                        MaxCutoffBranchesPerBranch = 2,
                        BaseDirection = offsetDirection + Lerp(-0.41f, 0.41f, (p.X + p.Y) * 0.1854f % 1f),
                        IcicleColor = Color.LightCyan * 0.3f
                    };
                }
            }
        }
    }
}
