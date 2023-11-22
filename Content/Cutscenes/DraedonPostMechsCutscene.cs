using CalamityMod;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Cutscenes
{
    // After beating all 3 mechanical bosses, draedons ambience theme will start playing. Then, the screen will slowly zoom out,
    // revealing that it is on a large monitor in a lab with draedon watching. Then, with a quick black flash, it will go back to normal.
    public class DraedonPostMechsCutscene : Cutscene
    {
        #region Instance Fields/Properties
        public ManagedRenderTarget ScreenTarget
        {
            get;
            private set;
        }

        public Texture2D LabTexture
        {
            get;
            private set;
        }

        private int FrameCounter;

        private Rectangle Frame = new(0, 0, 100, 120);
        #endregion

        #region Static Properties
        public static int InitialWait => 150;

        public static int CRTFadeInLength => 120;

        public static int ZoomOutLength => 380;

        public static int ZoomHoldLength => 240;

        public static int ScreenFlashLength => 20;

        public static int ScreenFlashHoldLength => ScreenFlashLength / 3;
        #endregion

        #region Overrides
        public override int CutsceneLength => InitialWait + ZoomOutLength + ZoomHoldLength;

        public override BlockerSystem.BlockCondition? GetBlockCondition => new(false, true, () => IsActive && Timer < CutsceneLength - ScreenFlashLength / 2);

        public override void Load()
        {
            ScreenTarget = new(true, RenderTargetManager.CreateScreenSizedTarget);
            LabTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Cutscenes/Textures/DraedonLab", AssetRequestMode.ImmediateLoad).Value;
        }

        public override void Unload()
        {
            LabTexture = null;
        }

        public override void OnEnd() => WorldSaveSystem.HasSeenPostMechsCutscene = true;

        public override void Update()
        {
            // Framing for him that matches his actual npc framing.
            Frame.Width = 100;
            int frameHeight = 120;
            int frameCount = 12;
            int xFrame = Frame.X / Frame.Width;
            int yFrame = Frame.Y / frameHeight;
            int frame = xFrame * frameCount + yFrame;

            int frameChangeDelay = 7;

            FrameCounter++;
            if (FrameCounter >= frameChangeDelay)
            {
                frame++;

                if (frame < 23 || frame > 47)
                    frame = 23;

                FrameCounter = 0;
            }

            Frame.X = frame / frameCount * Frame.Width;
            Frame.Y = frame % frameCount * frameHeight;
        }

        public override RenderTarget2D DrawWorld(SpriteBatch spriteBatch, RenderTarget2D screen)
        {
            // Store the screen before swapping targets.
            ScreenTarget.SwapToRenderTarget();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.Draw(screen, Vector2.Zero, Color.White);
            spriteBatch.End();

            Texture2D draedon = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/Draedon").Value;
            Texture2D draedonGlowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ExoMechs/DraedonGlowmask").Value;

            Vector2 screenSize = new(Main.screenWidth, Main.screenHeight);
            Vector2 labSize = LabTexture.Size();
            Vector2 labSizeCorrection = screenSize / labSize;

            Vector2 labScreenSize = new(582f, 277f);

            // The middle of the screen on the lab texture. The lab needs to shove this into the center of the screen, and zoom into it.
            Vector2 originScalar = new Vector2(1274f, 585f) / new Vector2(2559f, 1374f);

            // The ratio for the zooms. Eased out for smoothness later on.
            float zoomoutRatio = Utils.GetLerpValue(InitialWait, ZoomOutLength + InitialWait, Timer, true);

            // The scale of the screen.
            float screenScale = Lerp(1f, 0.75f, CalamityUtils.SineOutEasing(zoomoutRatio, 1));

            // The scale of the lab.
            Vector2 initialLabScale = labSizeCorrection * (labSize / labScreenSize);
            Vector2 labScale = initialLabScale * Lerp(1f, 0.745f, CalamityUtils.SineOutEasing(zoomoutRatio, 1));

            // Bit hacky, but restore the screen scale, make the lab really zoomed out so the window is more than visible and stop drawing
            // draedon if after the flash.
            bool drawDraedon = true;
            if (Timer >= CutsceneLength - ScreenFlashLength / 2)
            {
                drawDraedon = false;
                screenScale = 1f;
                labScale = Vector2.One * 100f;
            }

            // Swap to the sscreen target and begin drawing.
            screen.SwapToRenderTarget();

            // Get and prepare a CRT effect shader, to make the screen appear like its on a monitor.
            var shader = InfernumEffectsRegistry.CRTShader.GetShader().Shader;
            float intensity = Utils.GetLerpValue(InitialWait, CRTFadeInLength + InitialWait, Timer, true) * Utils.GetLerpValue(CutsceneLength, CutsceneLength - ScreenFlashLength / 2, Timer, true);
            shader.Parameters["globalIntensity"]?.SetValue(intensity);
            shader.Parameters["curvature"]?.SetValue(8f);
            shader.Parameters["scanlineOpacity"]?.SetValue(0.2f);
            shader.Parameters["vignetteRoundness"]?.SetValue(2f);
            shader.Parameters["vignetteOpacity"]?.SetValue(2f);
            shader.Parameters["brightnessMultiplier"]?.SetValue(3.5f);
            shader.Parameters["chromaticStrength"]?.SetValue(0.0005f);
            shader.Parameters["screenSize"]?.SetValue(screen.Size() * screenScale);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, shader);
            spriteBatch.Draw(ScreenTarget, screenSize * 0.5f, null, Color.White, 0f, screen.Size() * 0.5f, screenScale, SpriteEffects.None, 0f);
            spriteBatch.End();

            // Restart the spritebatch and draw the lab.
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.DepthRead, null);
            spriteBatch.Draw(LabTexture, screenSize * 0.5f, null, Color.Lerp(Color.White, Color.Black, 0.75f), 0f, originScalar * LabTexture.Size(), labScale, SpriteEffects.None, 0f);

            // Draw Draedon if he should be.
            if (drawDraedon)
            {
                // Make him slide into view at around the same time as the zoom starts happening, to give a weird 3D depth illusion thing that looks like hes been there the entire
                // time, just closer to the screen than his monitor.
                Vector2 draedonPosition = new(Main.screenWidth * 1.2f, Main.screenHeight * 0.875f);
                draedonPosition += -Vector2.UnitX * Main.screenWidth * 0.3f * CalamityUtils.SineOutEasing(Utils.GetLerpValue(InitialWait, ZoomOutLength + 110, Timer, true), 1);

                // Draw a dropshadow because I think it looks pretty.
                Vector2 dropshadowPosition = draedonPosition + Vector2.One * 20f;
                spriteBatch.Draw(draedon, dropshadowPosition, Frame, Color.Black * 0.7f, 0f, Frame.Size() * 0.5f, 5.5f, SpriteEffects.None, 0f);

                // Draw him and his glowmask.
                spriteBatch.Draw(draedon, draedonPosition, Frame, Color.Lerp(Color.White, Color.Black, 0.85f), 0f, Frame.Size() * 0.5f, 5.5f, SpriteEffects.None, 0f);
                spriteBatch.Draw(draedonGlowmask, draedonPosition, Frame, Color.White, 0f, Frame.Size() * 0.5f, 5.5f, SpriteEffects.None, 0f);
            }
            spriteBatch.End();

            return screen;
        }

        public override void PostDraw(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            // Draw a black flash if the time is correct over everything.
            float flashOpacity = Utils.GetLerpValue(CutsceneLength - ScreenFlashLength, CutsceneLength - ScreenFlashLength + ScreenFlashHoldLength, Timer, true) *
                     Utils.GetLerpValue(CutsceneLength, CutsceneLength - ScreenFlashLength + ScreenFlashHoldLength * 2, Timer, true);

            if (flashOpacity > 0f)
                spriteBatch.Draw(InfernumTextureRegistry.Pixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Black * flashOpacity);

            spriteBatch.End();
        }
        #endregion
    }
}
