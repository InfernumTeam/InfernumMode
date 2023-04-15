using CalamityMod.MainMenu;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.MainMenu
{
    internal class InfernumMainMenu : ModMenu
    {
        public override string DisplayName => "Infernum Style";

        public override ModSurfaceBackgroundStyle MenuBackgroundStyle => ModContent.GetInstance<NullSurfaceBackground>();

        public override Asset<Texture2D> Logo => ModContent.Request<Texture2D>("InfernumMode/Content/MainMenu/Logo", AssetRequestMode.ImmediateLoad);

        public override Asset<Texture2D> MoonTexture => InfernumTextureRegistry.Invisible;

        public override Asset<Texture2D> SunTexture => InfernumTextureRegistry.Invisible;

        public override int Music => SetMusic();

        private static int SetMusic()
        {
            if (InfernumMode.MusicModIsActive)
                return MusicLoader.GetMusicSlot(InfernumMode.InfernumMusicMod, "Sounds/Music/TitleScreen");
            return MusicID.MenuMusic;
        }

        public override bool PreDrawLogo(SpriteBatch spriteBatch, ref Vector2 logoDrawCenter, ref float logoRotation, ref float logoScale, ref Color drawColor)
        {

            Texture2D backgroundTexture = ModContent.Request<Texture2D>("CalamityMod/MainMenu/MenuBackground").Value;
            Vector2 drawOffset = Vector2.Zero;
            float xScale = (float)Main.screenWidth / backgroundTexture.Width;
            float yScale = (float)Main.screenHeight / backgroundTexture.Height;
            float scale = xScale;
            if (xScale != yScale)
            {
                if (yScale > xScale)
                {
                    scale = yScale;
                    drawOffset.X -= (backgroundTexture.Width * scale - Main.screenWidth) * 0.5f;
                }
                else
                {
                    drawOffset.Y -= (backgroundTexture.Height * scale - Main.screenHeight) * 0.5f;
                }
            }
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
            
            // Apply a raindrop effect to the texture.
            Effect raindrop = InfernumEffectsRegistry.RaindropShader.GetShader().Shader;
            raindrop.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            raindrop.Parameters["cellResolution"].SetValue(20f);
            raindrop.Parameters["intensity"].SetValue(2f);
            raindrop.CurrentTechnique.Passes["RainPass"].Apply();
            spriteBatch.Draw(backgroundTexture, drawOffset, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
            spriteBatch.Draw(Logo.Value, logoDrawCenter, null, drawColor, logoRotation, Logo.Value.Size() * 0.5f, logoScale, SpriteEffects.None, 0f);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
            return false;
        }
    }
}