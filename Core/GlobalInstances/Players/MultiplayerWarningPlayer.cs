using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class MultiplayerWarningPlayer : ModPlayer
    {
        public static LocalizedText Warning => Utilities.GetLocalization("Status.MultiplayerWarning");

        // Other mods (looking at you OE) LOVE to spam the chat with utterly useless join messages lasting several lines. Ensure that this message waits before sending to avoid getting buried by them.
        public const int DisplayMessageTimerLength = 180;

        public static int DisplayMessageTimer
        {
            get;
            private set;
        }

        public override void OnEnterWorld()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                DisplayMessageTimer = DisplayMessageTimerLength;
        }

        public override void PreUpdate()
        {
            if (DisplayMessageTimer > 0)
            {
                DisplayMessageTimer--;

                if (DisplayMessageTimer == 0)
                    Main.NewText(Warning.Value, Color.Red);
            }
        }
    }
}
