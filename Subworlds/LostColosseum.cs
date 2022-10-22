using Microsoft.Xna.Framework;
using SubworldLibrary;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace InfernumMode.Subworlds
{
    public class LostColosseum : Subworld
    {
        public static bool HasBereftVassalAppeared
        {
            get;
            set;
        } = false;

        public class LostColosseumGenPass : GenPass
        {
            public LostColosseumGenPass() : base("Terrain", 1f) { }
            
            protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
            {
                progress.Message = "Generating a Sunken Colosseum";
                Main.worldSurface = 1;
                Main.rockLayer = 3;
                for (int i = 0; i < Main.maxTilesX; i++)
                {
                    for (int j = 0; j < Main.maxTilesY; j++)
                    {
                        progress.Set((j + i * Main.maxTilesY) / (float)(Main.maxTilesX * Main.maxTilesY));
                        Tile tile = Main.tile[i, j];
                        tile.HasTile = j >= Main.maxTilesY / 2 + 1;
                        tile.TileType = TileID.Sand;
                    }
                }
            }
        }

        public override int Width => 510;

        public override int Height => 200;

        public override bool ShouldSave => true;

        public override List<GenPass> Tasks => new()
        {
            new LostColosseumGenPass()
        };

        public override bool GetLight(Tile tile, int x, int y, ref FastRandom rand, ref Vector3 color)
        {
            Vector3 lightMin = Vector3.Zero;
            bool notSolid = tile.Slope != SlopeType.Solid || tile.IsHalfBlock;
            if (!tile.HasTile || !Main.tileNoSunLight[tile.TileType] || (notSolid && Main.wallLight[tile.WallType] && tile.LiquidAmount < 200))
                lightMin = Vector3.One * 0.8f;

            color = Vector3.Max(color, lightMin);
            return false;
        }
    }
}