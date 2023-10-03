using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class ZoomSystem : ModSystem
    {
        public static float Zoom
        {
            get;
            private set;
        }

        public static int ZoomHoldTime
        {
            get;
            private set;
        }

        /// <summary>
        /// Call to create a zoom effect for the local client.
        /// </summary>
        /// <param name="zoom">The amount of zoom to apply.</param>
        /// <param name="holdTime">The length of time to hold the zoom. Defaults to one frame.</param>
        public static void SetZoomEffect(float zoom, int holdTime = 1)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            Zoom = zoom;
            ZoomHoldTime = holdTime;
        }

        public override void ModifyScreenPosition()
        {
            if (Main.LocalPlayer.dead || !Main.LocalPlayer.active)
            {
                Zoom = 0f;
                ZoomHoldTime = 0;
                return;
            }
            if (ZoomHoldTime > 0)
                ZoomHoldTime--;
            else if (!Main.gamePaused)
                Zoom = Lerp(Zoom, 0f, 0.09f);

            if (Zoom < 0.01f)
                Zoom = 0f;
        }

        public override void ModifyTransformMatrix(ref SpriteViewMatrix Transform) => Transform.Zoom *= 1f + Zoom;
    }
}
