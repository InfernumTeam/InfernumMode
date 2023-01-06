using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Shaders;

namespace InfernumMode.Content.Skies
{
    public class NightProvidenceShaderData : ScreenShaderData
    {
        public NightProvidenceShaderData(string passName) : base(passName) { }

        public override void Apply()
        {
            UseTargetPosition(Main.LocalPlayer.Center);
            UseColor(new Color(100, 150, 255));
            UseOpacity(0.36f);
            base.Apply();
        }
    }
}
