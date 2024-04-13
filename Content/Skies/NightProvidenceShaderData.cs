using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Shaders;

namespace InfernumMode.Content.Skies
{
    public class NightProvidenceShaderData(string passName) : ScreenShaderData(passName)
    {
        public override void Apply()
        {
            UseTargetPosition(Main.LocalPlayer.Center);
            UseColor(new Color(100, 150, 255));
            UseOpacity(0.2f);
            base.Apply();
        }
    }
}
