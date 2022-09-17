using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Shaders;

namespace InfernumMode.Skies
{
    public class NightProvidenceShaderData : ScreenShaderData
    {
        public NightProvidenceShaderData(string passName) : base(passName) { }

        public override void Apply()
        {
            UseTargetPosition(Main.LocalPlayer.Center);
            UseColor(new Color(100, 150, 255));
            base.UseOpacity(0.36f);
            base.Apply();
        }
    }
}
