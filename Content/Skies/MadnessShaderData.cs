using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;

namespace InfernumMode.Content.Skies
{
    public class MadnessScreenShaderData : ScreenShaderData
    {
        public MadnessScreenShaderData(Ref<Effect> shader, string passName) : base(shader, passName) { }

        public override void Apply()
        {
            UseTargetPosition(Main.LocalPlayer.Center);
            float interlopant = Clamp(Main.LocalPlayer.Infernum().GetValue<int>("MadnessTime") / 600f, 0f, 1f);
            UseIntensity(interlopant);
            base.Apply();
        }
    }
}
