using System.Collections.Generic;
using InfernumMode.Content.WorldGeneration;
using Terraria.GameContent.Generation;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class WorldgenSystem : ModSystem
    {
        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {
            int finalCleanupIndex = tasks.FindIndex(g => g.Name == "Final Cleanup");
            if (finalCleanupIndex != -1)
            {
                tasks.Insert(++finalCleanupIndex, new PassLegacy("Prov Arena", ProfanedGarden.Generate));
                tasks.Insert(++finalCleanupIndex, new PassLegacy("Desert Digout Area", LostColosseumEntrance.Generate));
                tasks.Insert(++finalCleanupIndex, new PassLegacy("Hiding eggs", EggShrineGeneration.Generate));
                tasks.Insert(++finalCleanupIndex, new PassLegacy("Growing garden", BlossomGarden.Generate));
            }
        }
    }
}
