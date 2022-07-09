using InfernumMode;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;

namespace InfernumMode.Skies
{
    public class MadnessScreenShaderData : ScreenShaderData
    {
        public MadnessScreenShaderData(Ref<Effect> shader, string passName) : base(shader, passName) { }

        public override void Apply()
        {
            UseTargetPosition(Main.LocalPlayer.Center);
            base.UseOpacity(Main.LocalPlayer.Infernum().MadnessInterpolant);
            base.Apply();
        }
    }
}
