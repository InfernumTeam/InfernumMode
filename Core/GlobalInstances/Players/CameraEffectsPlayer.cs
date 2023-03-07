using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class CameraEffectsPlayer : ModPlayer
    {
        public float CurrentScreenShakePower
        {
            get;
            set;
        }

        public Vector2 ScreenFocusPosition
        {
            get;
            set;
        }

        public float ScreenFocusInterpolant
        {
            get;
            set;
        }

        public int ScreenFocusHoldInPlaceTime
        {
            get;
            set;
        }

        public override void ResetEffects()
        {
            // Decrement the countdown if the camera should be held in place.
            if (ScreenFocusHoldInPlaceTime > 0)
            {
                ScreenFocusHoldInPlaceTime--;
                return;
            }

            // Naturally pan back to the player if the camera should not be held in place.
            ScreenFocusInterpolant = MathHelper.Clamp(ScreenFocusInterpolant - 0.1f, 0f, 1f);
        }

        public override void ModifyScreenPosition()
        {
            if (Player.dead)
                return;

            // Handle camera focus effects.
            if (ScreenFocusInterpolant > 0f)
            {
                Vector2 idealScreenPosition = ScreenFocusPosition - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
                Main.screenPosition = Vector2.Lerp(Main.screenPosition, idealScreenPosition, ScreenFocusInterpolant);
            }

            // Handle screen-shake effects. This can be disabled with one of Calamity's configuration options.
            if (CurrentScreenShakePower > 0f)
                CurrentScreenShakePower = Utils.Clamp(CurrentScreenShakePower - 0.2f, 0f, 15f);
            else
                return;

            if (!CalamityConfig.Instance.Screenshake)
            {
                CurrentScreenShakePower = 0f;
                return;
            }

            Main.screenPosition += Main.rand.NextVector2CircularEdge(CurrentScreenShakePower, CurrentScreenShakePower);
        }
    }
}