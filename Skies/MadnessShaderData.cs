using InfernumMode.GlobalInstances.Players;
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
            UseTargetPosition(Main.LocalPlayer.Center);
            UseIntensity(Main.LocalPlayer.GetModPlayer<DebuffEffectsPlayer>().MadnessInterpolant);
            base.Apply();
        }
    }
}
