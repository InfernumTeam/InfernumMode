using Terraria.ModLoader;
using CalamityMod.UI.DraedonSummoning;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class DraetingSimSystem : ModSystem
    {
        public static bool ShouldEnableDraedonDialog => true;

        public override void OnModLoad()
        {
            if (!ShouldEnableDraedonDialog)
                return;
        }
    }
}