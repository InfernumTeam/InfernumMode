using InfernumMode.WorldGeneration;
using System.Collections.Generic;
using Terraria.GameContent.Generation;
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
                tasks.Insert(++floatingIslandIndex, new PassLegacy("Lost Colosseum Entrance", LostColosseumEntrance.Generate));

            int finalCleanupIndex = tasks.FindIndex(g => g.Name == "Final Cleanup");
            if (finalCleanupIndex != -1)
            {
                tasks.Insert(++finalCleanupIndex, new PassLegacy("Jungle Digout Area", JungleArena.Generate));
                tasks.Insert(++finalCleanupIndex, new PassLegacy("Dungeon Digout Area", DungeonArena.Generate));
                tasks.Insert(++finalCleanupIndex, new PassLegacy("Prov Arena", (progress, config) =>
                {
                    progress.Message = "Constructing a temple for an ancient goddess";
                    ProfanedGarden.Generate(progress, config);
                }));
            }
        }
    }
}