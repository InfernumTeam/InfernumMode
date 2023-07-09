using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace InfernumMode.Content.Skies
{
    public class HyperplaneMatrixTimeChangeSkyScene : ModSceneEffect
    {
        public override bool IsSceneEffectActive(Player player) => HyperplaneMatrixTimeChangeSystem.BackgroundChangeInterpolant > 0f;

        public override SceneEffectPriority Priority => (SceneEffectPriority)20;

        public override void SpecialVisuals(Player player, bool isActive)
        {
            player.ManageSpecialBiomeVisuals("InfernumMode:HyperplaneMatrixTimeChange", isActive);
        }
    }

    public class HyperplaneMatrixTimeChangeSky : CustomSky
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

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            float opacity = HyperplaneMatrixTimeChangeSystem.BackgroundChangeInterpolant;
            float codeTextureScale = 1.2f;
            Texture2D techyNoise = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/TechyNoise").Value;
            Texture2D codeTexture = InfernumTextureRegistry.HyperplaneMatrixCode.Value;
            Color techyNoiseColor = Color.Cyan * opacity * 0.05f;
            Color codeColor = Color.LightCyan * opacity * 0.02f;

            for (int i = -2500; i < Main.screenWidth + 2500; i += (int)(codeTexture.Width * codeTextureScale))
            {
                for (int j = -2500; j < Main.screenWidth + 2500; j += (int)(codeTexture.Height * codeTextureScale))
                {
                    Vector2 drawOffset = Vector2.UnitY * Main.GlobalTimeWrappedHourly * -500f;
                    drawOffset.Y %= (int)(codeTexture.Height * codeTextureScale);
                    Vector2 drawPosition = new Vector2(i, j) + drawOffset;
                    Main.spriteBatch.Draw(codeTexture, drawPosition, null, codeColor, 0f, Vector2.Zero, codeTextureScale, 0, 0f);
                }
            }
            for (int i = -600; i < Main.screenWidth + 600; i += techyNoise.Width)
            {
                for (int j = -600; j < Main.screenWidth + 600; j += techyNoise.Height)
                {
                    Vector2 drawOffset = Main.GlobalTimeWrappedHourly * new Vector2(55f, 34f);
                    drawOffset.X %= techyNoise.Width;
                    drawOffset.Y %= techyNoise.Height;
                    Vector2 drawPosition = new Vector2(i, j) + drawOffset;
                    Main.spriteBatch.Draw(techyNoise, drawPosition, null, techyNoiseColor, 0f, Vector2.Zero, 1f, 0, 0f);

                    drawOffset = Main.GlobalTimeWrappedHourly * new Vector2(-45f, -19f);
                    drawOffset.X %= techyNoise.Width;
                    drawOffset.Y %= techyNoise.Height;
                    drawPosition = new Vector2(i, j) + drawOffset;
                    Main.spriteBatch.Draw(techyNoise, drawPosition, null, techyNoiseColor, 0f, Vector2.Zero, 1f, 0, 0f);
                }
            }
        }

        public override float GetCloudAlpha()
        {
            return 0f;
        }

        public override void Activate(Vector2 position, params object[] args)
        {
            isActive = true;
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
