using Terraria;
using Terraria.Graphics.Shaders;

namespace InfernumMode.Skies
{
    public class TwinsScreenShaderData : ScreenShaderData
    {
        public TwinsScreenShaderData(string passName)
            : base(passName)
        {
        }

        public override void Apply()
        {
            UseTargetPosition(Main.LocalPlayer.Center);
            UseColor(TwinsSky.ScreenBackgroundColor);
            base.Apply();
        }
    }
}
