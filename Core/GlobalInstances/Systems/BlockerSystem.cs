using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class BlockerSystem : ModSystem
    {
        public static bool BlockInput
        {
            get;
            private set;
        }

        public static bool HideUI
        {
            get;
            private set;
        }

        public static void SetBlockers(bool? input = null, bool? ui = null)
        {
            BlockInput = input ?? Main.blockInput;
            HideUI = ui ?? Main.hideUI;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            Main.blockInput = BlockInput;
            Main.hideUI = HideUI;

            if (!Main.instance.IsActive)
                return;

            // Reset the flags.
            if (BlockInput)
                BlockInput = false;
            if (HideUI)
                HideUI = false;
        }
    }
}
