using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;

namespace InfernumMode.Skies
{
    public class MadnessScreenShaderData : ScreenShaderData
    {
        public MadnessScreenShaderData(Ref<Effect> shader, string passName) : base(shader, passName) { }

        public override void Apply()
        {
            float interpolant = Main.LocalPlayer.Infernum().MadnessInterpolant;
            UseTargetPosition(Main.LocalPlayer.Center);
            base.UseIntensity(interpolant);
            base.Apply();
        }
    }
}
