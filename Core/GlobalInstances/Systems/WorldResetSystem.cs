using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class WorldResetSystem : ModSystem
    {
        public override void OnWorldLoad() => WorldSaveSystem.InfernumMode = false;

        public override void OnWorldUnload() => WorldSaveSystem.InfernumMode = false;
    }
}