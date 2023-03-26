using CalamityMod.NPCs;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics
{
    public class CeaselessVoidWhiteningEffect : ModSystem
    {
        public static float WhiteningInterpolant
        {
            get;
            set;
        }

        public override void OnModLoad()
        {
            On.Terraria.GameContent.Events.MoonlordDeathDrama.DrawWhite += DrawWhiteningHook;
        }

        private void DrawWhiteningHook(On.Terraria.GameContent.Events.MoonlordDeathDrama.orig_DrawWhite orig, SpriteBatch spriteBatch)
        {
            DrawWhitening();
            orig(spriteBatch);
        }

        public static void DrawWhitening()
        {
            // Don't draw anything if the whitening effect is inactive.
            if (WhiteningInterpolant <= 0f)
                return;

            // Draw the whitening effect.
            Texture2D pixel = InfernumTextureRegistry.Pixel.Value;
            Vector2 screenArea = new(Main.screenWidth, Main.screenHeight);
            Vector2 scale = screenArea / pixel.Size() * 2f;
            Main.spriteBatch.Draw(pixel, screenArea * 0.5f, null, Color.White * WhiteningInterpolant, 0f, pixel.Size() * 0.5f, scale, 0, 0f);

            // Draw the Ceaseless Void's mask above the effect.
            if (CalamityGlobalNPC.voidBoss != -1 && Main.npc[CalamityGlobalNPC.voidBoss].ai[0] != 0f)
            {
                Main.spriteBatch.EnterShaderRegion();

                NPC ceaselessVoid = Main.npc[CalamityGlobalNPC.voidBoss];
                bool voidIsCracked = ceaselessVoid.localAI[0] == 1f;
                Texture2D voidMaskTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/CeaselessVoid/CeaselessMetalShellMask").Value;
                Vector2 voidDrawPosition = ceaselessVoid.Center - Main.screenPosition;
                Color voidColor = Color.White * MathF.Pow(WhiteningInterpolant, 0.66f) * ceaselessVoid.Opacity;

                // Apply the crack effect if necessary.
                if (voidIsCracked)
                {
                    Rectangle frame = voidMaskTexture.Frame();
                    InfernumEffectsRegistry.CeaselessVoidCrackShader.UseShaderSpecificData(new(frame.X, frame.Y, frame.Width, frame.Height));
                    InfernumEffectsRegistry.CeaselessVoidCrackShader.UseImage1("Images/Misc/Perlin");
                    InfernumEffectsRegistry.CeaselessVoidCrackShader.Shader.Parameters["sheetSize"].SetValue(voidMaskTexture.Size());
                    InfernumEffectsRegistry.CeaselessVoidCrackShader.Apply();
                }

                Main.spriteBatch.Draw(voidMaskTexture, voidDrawPosition, null, voidColor, ceaselessVoid.rotation, voidMaskTexture.Size() * 0.5f, ceaselessVoid.scale, 0, 0f);

                Main.spriteBatch.ExitShaderRegion();
            }
            else
                WhiteningInterpolant = MathHelper.Clamp(WhiteningInterpolant - 0.1f, 0f, 1f);
        }
    }
}