using Terraria;
using Terraria.Graphics.Shaders;

namespace InfernumMode.Content.Skies
{
    public class TwinsScreenShaderData(string passName) : ScreenShaderData(passName)
    {
        public override void Apply()
        {
            UseTargetPosition(Main.LocalPlayer.Center);
            UseColor(TwinsSky.ScreenBackgroundColor);
            base.Apply();
        }
    }
}
