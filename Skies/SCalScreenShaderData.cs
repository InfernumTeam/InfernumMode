using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;

namespace InfernumMode.Skies
{
    public class SCalScreenShaderData : ScreenShaderData
    {
        public SCalScreenShaderData(Ref<Effect> shader, string passName) : base(shader, passName) { }

        public override void Apply()
        {
            // If scal is not set, or the reduced graphics config is enabled, do not draw.
            if (CalamityGlobalNPC.SCal < 0 || InfernumConfig.Instance.ReducedGraphicsConfig)
                return;

            UseTargetPosition(Main.LocalPlayer.Center);
            UseColor(new Color(100, 150, 255));

            // Perform various matrix calculations to transform SCal's arena to UV coordinate space.
            NPC scal = Main.npc[CalamityGlobalNPC.SCal];
            Rectangle arena = scal.Infernum().Arena;
            Vector4 uvScaledArena = new(arena.X, arena.Y - 6f, arena.Width + 8f, arena.Height + 14f);
            uvScaledArena.X -= Main.screenPosition.X;
            uvScaledArena.Y -= Main.screenPosition.Y;
            Vector2 downscaleFactor = new(Main.screenWidth, Main.screenHeight);
            Matrix toScreenCoordsTransformation = Main.GameViewMatrix.TransformationMatrix;
            Vector2 coordinatePart = Vector2.Transform(new Vector2(uvScaledArena.X, uvScaledArena.Y), toScreenCoordsTransformation) / downscaleFactor;
            Vector2 areaPart = Vector2.Transform(new Vector2(uvScaledArena.Z, uvScaledArena.W), toScreenCoordsTransformation with { M41 = 0f, M42 = 0f }) / downscaleFactor;
            uvScaledArena = new(coordinatePart.X, coordinatePart.Y, areaPart.X, areaPart.Y);

            Shader.Parameters["uvArenaArea"].SetValue(uvScaledArena);
            UseImage(InfernumTextureRegistry.CultistRayMap.Value, 0, SamplerState.AnisotropicWrap);

            UseOpacity(0.36f);
            base.Apply();
        }
    }
}
