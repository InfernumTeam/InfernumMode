using Terraria.ModLoader;
using CalamityMod.UI.DraedonSummoning;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class DraetingSimSystem : ModSystem
    {
        public static bool ShouldEnableDraedonDialog => false;

        public override void OnModLoad()
        {
            if (!ShouldEnableDraedonDialog)
                return;

            DraedonDialogRegistry.DialogOptions[0] = new("What is this?", "Hahahah you FOOL! Welcome to my ZOOM CALL!!!");
        }
    }
}