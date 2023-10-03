using Terraria;
using Terraria.Graphics;
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
