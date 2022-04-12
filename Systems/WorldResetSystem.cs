using Terraria.ModLoader;

namespace InfernumMode.Systems
{
    public class WorldResetSystem : ModSystem
    {
        public override void OnWorldLoad() => PoDWorld.InfernumMode = false;

        public override void OnWorldUnload() => PoDWorld.InfernumMode = false;
    }
}