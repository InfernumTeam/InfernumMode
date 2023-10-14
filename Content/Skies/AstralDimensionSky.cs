using InfernumMode.Common.Graphics.ScreenEffects;
using Terraria.ModLoader;
using Terraria;
using Terraria.Graphics.Effects;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace InfernumMode.Content.Skies
{
    public class AstralDimensionSkyScene : ModSceneEffect
    {
        public override bool IsSceneEffectActive(Player player) => AstralDimensionSystem.EffectIsActive || AstralDimensionSystem.MonolithIntensity > 0f;

        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

        public override float GetWeight(Player player) => 0.9f;

        public override void SpecialVisuals(Player player, bool isActive)
        {
            if (!AstralDimensionSystem.EffectIsActive)
                AstralDimensionSystem.MonolithIntensity = Clamp(AstralDimensionSystem.MonolithIntensity - 0.02f, 0f, 1f);
            player.ManageSpecialBiomeVisuals("InfernumMode:AstralDimension", isActive);
        }
    }

    public class AstralDimensionSky : CustomSky
    {
        public bool isActive;

        public float Intensity;

        public override void Update(GameTime gameTime)
        {
            if (isActive && Intensity < 1f)
                Intensity += 0.01f;
            else if (!isActive && Intensity > 0f)
                Intensity -= 0.01f;
        }


        public override Color OnTileColor(Color inColor)
        {
            return Color.Lerp(new Color(97, 75, 118), inColor, 1f - AstralDimensionSystem.MonolithIntensity);
        }


        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            if (AstralDimensionSystem.EffectIsActive || AstralDimensionSystem.MonolithIntensity > 0f)
            {
                //Main.spriteBatch.End();
                //Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicWrap, DepthStencilState.None, Main.Rasterizer, null, Utilities.GetCustomSkyBackgroundMatrix());// Matrix.CreateScale(Main.GameViewMatrix.Zoom.X, Main.GameViewMatrix.Zoom.Y, 1f));

                AstralDimensionSystem.Draw();

                //Main.spriteBatch.End();
                //Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicWrap, DepthStencilState.None, Main.Rasterizer, null, Utilities.GetCustomSkyBackgroundMatrix());
            }
        }

        public override float GetCloudAlpha()
        {
            return 1f - Intensity;
        }

        public override void Activate(Vector2 position, params object[] args)
        {
            isActive = true;
            if (Main.raining)
                CalamityMod.CalamityMod.StopRain();
        }

        public override void Deactivate(params object[] args)
        {
            isActive = false;
        }

        public override void Reset()
        {
            isActive = false;
        }

        public override bool IsActive()
        {
            return isActive || Intensity > 0f;
        }
    }
}
