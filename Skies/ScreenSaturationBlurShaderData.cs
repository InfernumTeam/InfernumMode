using InfernumMode.Systems;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;

namespace InfernumMode.Skies
{
    public class ScreenSaturationBlurShaderData : ScreenShaderData
    {
        public ScreenSaturationBlurShaderData(Ref<Effect> shader, string passName) : base(shader, passName) { }

        public override void Apply()
        {
            float effectiveIntensity = InfernumConfig.Instance.SaturationBloomIntensity * ScreenSaturationBlurSystem.Intensity;
            Main.instance.GraphicsDevice.Textures[1] = ScreenSaturationBlurSystem.BloomTarget;
            Shader.Parameters["blurAdditiveBrightness"].SetValue(effectiveIntensity * ScreenSaturationBlurSystem.BlurBrightnessFactor);
            Shader.Parameters["maxSaturationAdditive"].SetValue(effectiveIntensity);
            Shader.Parameters["blurExponent"].SetValue(ScreenSaturationBlurSystem.BlurBrightnessExponent);
            Shader.Parameters["blurSaturationBiasInterpolant"].SetValue(effectiveIntensity * ScreenSaturationBlurSystem.BlurSaturationBiasInterpolant);
            base.Apply();
        }
    }
}
