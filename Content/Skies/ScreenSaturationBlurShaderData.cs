using CalamityMod.NPCs.Providence;
using InfernumMode.Common.Graphics.ScreenEffects;
using InfernumMode.Core;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Skies
{
    public class ScreenSaturationBlurShaderData : ScreenShaderData
    {
        public ScreenSaturationBlurShaderData(Ref<Effect> shader, string passName) : base(shader, passName) { }

        public override void Apply()
        {
            if (ScreenSaturationBlurSystem.BloomTarget.IsUninitialized)
                return;

            float effectiveIntensity = InfernumConfig.Instance.SaturationBloomIntensity * ScreenSaturationBlurSystem.Intensity;
            Main.instance.GraphicsDevice.Textures[1] = ScreenSaturationBlurSystem.BloomTarget.Target;

            Shader.Parameters["maxSaturationAdditive"].SetValue(effectiveIntensity);
            Shader.Parameters["blurExponent"].SetValue(ScreenSaturationBlurSystem.BlurBrightnessExponent);

            float saturationBias = effectiveIntensity * ScreenSaturationBlurSystem.BlurSaturationBiasInterpolant;
            float brightness = effectiveIntensity * ScreenSaturationBlurSystem.BlurBrightnessFactor;
            if (NPC.AnyNPCs(ModContent.NPCType<Providence>()))
                saturationBias *= Main.dayTime ? 0.4f : 0.05f;
            if (NPC.AnyNPCs(NPCID.HallowBoss))
                brightness *= 1.5f;

            Shader.Parameters["blurAdditiveBrightness"].SetValue(brightness);
            Shader.Parameters["blurSaturationBiasInterpolant"].SetValue(saturationBias);
            Shader.Parameters["onlyShowBlurMap"].SetValue(ScreenSaturationBlurSystem.DebugDrawBloomMap);
            base.Apply();
        }
    }
}
