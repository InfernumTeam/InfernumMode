using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.WorldBuilding;

namespace InfernumMode.Content.WorldGeneration
{
    public static class DungeonArena
    {
        public static void Generate(GenerationProgress _, GameConfiguration _2)
        {
            int boxArea = 95;
            ushort dungeonWallID = 0;
            ushort dungeonTileID = 0;
            switch (WorldGen.crackedType)
            {
                case TileID.CrackedBlueDungeonBrick:
                    dungeonWallID = WallID.BlueDungeonSlabUnsafe;
                    dungeonTileID = TileID.BlueDungeonBrick;
                    break;
                case TileID.CrackedGreenDungeonBrick:
                    dungeonWallID = WallID.GreenDungeonSlabUnsafe;
                    dungeonTileID = TileID.GreenDungeonBrick;
                    break;
                case TileID.CrackedPinkDungeonBrick:
                    dungeonWallID = WallID.PinkDungeonSlabUnsafe;
                    dungeonTileID = TileID.PinkDungeonBrick;
                    break;
            }

            int index = WorldGen.numDungeonPlatforms / 2 + 1;
            Point dungeonCenter = new(WorldGen.dungeonPlatformX[index], WorldGen.dungeonPlatformY[index]);
            WorldUtils.Gen(dungeonCenter, new Shapes.Rectangle(boxArea, boxArea), Actions.Chain(
                new Actions.SetTile(dungeonTileID, true)));
            WorldUtils.Gen(new(dungeonCenter.X + 2, dungeonCenter.Y + 2), new Shapes.Rectangle(boxArea - 2, boxArea - 2), Actions.Chain(
                new Actions.ClearTile(),
                new Actions.PlaceWall(dungeonWallID)));
        }
}