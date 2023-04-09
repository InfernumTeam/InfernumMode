using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class DraetingSimSystem : ModSystem
    {
        public static bool ShouldEnableDraedonDialog => false;

        public override void OnModLoad()
        {
            if (!ShouldEnableDraedonDialog)
                return;
        }
    }
}