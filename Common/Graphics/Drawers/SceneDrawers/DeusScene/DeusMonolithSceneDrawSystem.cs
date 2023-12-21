using CalamityMod;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using System;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using InfernumMode.Common.Graphics.ScreenEffects;

namespace InfernumMode.Common.Graphics.Drawers.SceneDrawers.DeusScene
{
    public class DeusMonolithSceneDrawSystem : BaseSceneDrawSystem
    {
        public override bool ShouldDrawThisFrame => AstralDimensionSystem.MonolithIntensity > 0f;

        public List<BaseSceneObject> StarObjects
        {
            get;
            private set;
        } = new();

        public override void ExtraUpdate()
        {
            foreach (BaseSceneObject obj in StarObjects)
                obj.Update();

            StarObjects.RemoveAll(obj => obj.ShouldKill);

            for (int i = 0; i < 1; i++)
            {
                StarObjects.Add(new DeusStarObject(
                    position: Main.LocalPlayer.Center + new Vector2(Main.rand.NextFloatDirection() * 7500f, Main.rand.NextFloat(-Main.screenHeight * 3f, Main.screenHeight * 3f)),//Main.screenPosition + Main.rand.NextVector2FromRectangle(new Rectangle(-1000, -500, Main.screenWidth + 2000, Main.screenHeight + 1000)),
                    velocity: Vector2.Zero,
                    scale: new Vector2(Main.rand.NextFloat(0.2f, 0.8f)),
                    lifetime: Main.rand.Next(240, 480),
                    depth: Main.rand.NextFloat(3f, 4f),
                    rotation: Main.rand.NextFloat(-0.002f, 0.002f),
                    rotationSpeed: 0f));
            }

            if (Main.rand.NextBool(2))
            {
                Vector2 position = Main.LocalPlayer.Center + new Vector2(Main.rand.NextFloatDirection() * 7500f, Main.rand.NextFloat(-Main.screenHeight * 3f, Main.screenHeight * 3f));//Main.screenPosition + new Vector2(xPos * 3.25f, yPos * 2f);

                Vector2 velocity = position.DirectionTo(Main.LocalPlayer.Center).RotatedBy(Main.rand.NextFloat(-Pi, Pi)) * Main.rand.NextFloat(2f, 3.5f);
                Objects.Add(new DeusRockObject(
                    position: position,
                    velocity: velocity,
                    scale: new(Main.rand.NextFloat(0.55f, 0.8f)),
                    lifetime: Main.rand.Next(340, 550),
                    depth: Main.rand.NextFloat(1.5f, 2.2f),
                    rotation: 0f,
                    rotationSpeed: Main.rand.NextFloat(-0.01f, 0.01f)));
            }
        }

        public override void DrawObjectsToMainTarget(SpriteBatch spriteBatch)
        {
            DrawObjectListToMainTarget(spriteBatch, StarObjects);
            DrawObjectListToMainTarget(spriteBatch, Objects);
        }

        public override void DrawToMainTarget(SpriteBatch spriteBatch)
        {        
            spriteBatch.Draw(InfernumTextureRegistry.Pixel.Value, new Rectangle(0, 0, MainTarget.Width, MainTarget.Height), Color.Black);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
            Effect gasShader = InfernumEffectsRegistry.DeusGasShader.GetShader().Shader;
            gasShader.Parameters["supernovaColor1"]?.SetValue(Color.MediumPurple.ToVector3());
            gasShader.Parameters["supernovaColor2"]?.SetValue(Color.Orchid.ToVector3());
            gasShader.Parameters["bloomColor"]?.SetValue(Color.SlateBlue.ToVector3());

            gasShader.Parameters["generalOpacity"]?.SetValue(1f);
            gasShader.Parameters["scale"]?.SetValue(20f);
            gasShader.Parameters["time"]?.SetValue(Main.GlobalTimeWrappedHourly);

            gasShader.Parameters["brightness"]?.SetValue(1f);
            Utilities.SetTexture1(InfernumTextureRegistry.BlurryPerlinNoise.Value);
            Utilities.SetTexture2(InfernumTextureRegistry.WavyNeuronsNoise.Value);
            Utilities.SetTexture3(InfernumTextureRegistry.SmokyNoise.Value);
            gasShader.CurrentTechnique.Passes[0].Apply();

            Texture2D pixel = InfernumTextureRegistry.Pixel.Value;
            Main.spriteBatch.Draw(pixel, new Rectangle(0, 0, MainTarget.Width, MainTarget.Height), Color.White * 0.42f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
            Texture2D skyTexture = ModContent.Request<Texture2D>("InfernumMode/Common/Graphics/Drawers/SceneDrawers/DeusScene/Textures/DeusSky").Value;

            spriteBatch.Draw(skyTexture, new Rectangle(0, 0, MainTarget.Width, MainTarget.Height), Color.White * 0.25f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
            for (int i = 0; i < 40; i++)
            {
                Texture2D gasTexture = i % 2 == 0 ? InfernumTextureRegistry.Cloud.Value : InfernumTextureRegistry.Cloud2.Value;
                Vector2 drawPosition = new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
                float drawOutwardness = Utils.GetLerpValue(0.45f, 1.1f, i % 14f / 14f);
                drawPosition += (TwoPi * 7f * i / 75f).ToRotationVector2() * MathF.Max(Main.screenWidth, Main.screenHeight) * drawOutwardness;
                float rotation = TwoPi * (drawOutwardness + i % 12f / 12f);
                float scale = Utils.GetLerpValue(0.8f, 1.15f, i % 16f / 16f);
                Color drawColor = CalamityUtils.MulticolorLerp(i / 29f % 0.999f + Main.GlobalTimeWrappedHourly * 0.05f, new Color(109, 242, 196), new Color(234, 119, 93), Color.MediumPurple) * 0.44f;
                spriteBatch.Draw(gasTexture, drawPosition, null, drawColor, rotation, gasTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
        }
    }
}
