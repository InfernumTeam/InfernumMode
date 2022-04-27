using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace InfernumMode.Systems
{
	public class WorldgenSystem : ModSystem
    {
        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref float totalWeight)
        {
            int floatingIslandIndex = tasks.FindIndex(g => g.Name == "Floating Islands");
            if (floatingIslandIndex != -1)
                tasks.Insert(floatingIslandIndex, new PassLegacy("Desert Digout Area", GenerateUndergroundDesertArea));
        }

        public static void GenerateUndergroundDesertArea(GenerationProgress progress, GameConfiguration config)
        {
            Vector2 cutoutAreaCenter = WorldGen.UndergroundDesertLocation.Center.ToVector2();

            for (int i = 0; i < 4; i++)
            {
                cutoutAreaCenter += WorldGen.genRand.NextVector2Circular(15f, 15f);
                WorldUtils.Gen(cutoutAreaCenter.ToPoint(), new Shapes.Mound(75, 48), Actions.Chain(
                    new Modifiers.Blotches(12),
                    new Actions.ClearTile(),
                    new Actions.PlaceWall(WallID.Sandstone)
                    ));
            }
        }
    }
}