using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace InfernumMode.Common.Graphics.Drawers.SceneDrawers
{
    public class WaterScene : BaseSceneDrawSystem
    {
        public override void DrawToMainTarget(SpriteBatch spriteBatch)
        {
            var shader = InfernumEffectsRegistry.WaterOverlayShader.GetShader().Shader;
            shader.Parameters["time"]?.SetValue(Main.GlobalTimeWrappedHourly);
            shader.Parameters["screenPosition"]?.SetValue(Main.screenPosition);
            shader.Parameters["screenSize"]?.SetValue(Main.ScreenSize.ToVector2());

            shader.Parameters["colors"]?.SetValue(new Vector3[3] { new Color(36, 94, 187).ToVector3(), new Color(28, 175, 189).ToVector3(), new Color(19, 255, 203).ToVector3() });
            Main.instance.GraphicsDevice.Textures[1] = InfernumTextureRegistry.VoronoiLoop.Value;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, shader);
            //shader.CurrentTechnique.Passes[0].Apply();

            spriteBatch.Draw(InfernumTextureRegistry.Pixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White);

        }
    }
}
