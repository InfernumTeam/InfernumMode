using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.BaseEntities;
using InfernumMode.Content.Projectiles.Cutscene;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Providence
{
    public class ProviBurnPulseRing : BasePulseRingProjectile
    {
        public override int Lifetime => 120;

        public override Color DeterminePulseColor(float lifetimeCompletionRatio) => DoGProviCutsceneProjectile.TimeColor;

        public override float DetermineScreenShakePower(float lifetimeCompletionRatio, float distanceFromPlayer) => 15;

        public override Texture2D BorderNoiseTexture => InfernumTextureRegistry.HarshNoise.Value;

        public override Texture2D InnerNoiseTexure => InfernumTextureRegistry.HarshNoise.Value;

        public override PulseParameterData SetParameters(float lifetimeCompletionRatio) => PulseParameterData.Default;
    }
}
