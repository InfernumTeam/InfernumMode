using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;

namespace InfernumMode.Content.Skies
{
    public class MLScreenShaderData : ScreenShaderData
    {
        public MLScreenShaderData(Ref<Effect> shader, string passName) : base(shader, passName) { }

        public override void Apply()
        {
            // If the moon lord is not available do not draw.
            int moonLordIndex = NPC.FindFirstNPC(NPCID.MoonLordCore);
            if (moonLordIndex < 0)
                return;

            UseTargetPosition(Main.LocalPlayer.Center);
            UseColor(new Color(0, 247, 255));

            // Perform various matrix calculations to transform Moon Lord's arena to UV coordinate space.
            NPC ml = Main.npc[moonLordIndex];
            Rectangle arena = ml.Infernum().Arena;
            Vector4 uvScaledArena = new(arena.X, arena.Y - 6f, arena.Width + 8f, arena.Height + 14f);
            uvScaledArena.X -= Main.screenPosition.X;
            uvScaledArena.Y -= Main.screenPosition.Y;
            uvScaledArena.X *= 0.8f;
            Vector2 downscaleFactor = new(Main.screenWidth, Main.screenHeight);
            Matrix toScreenCoordsTransformation = Main.GameViewMatrix.TransformationMatrix;
            Vector2 coordinatePart = Vector2.Transform(new Vector2(uvScaledArena.X, uvScaledArena.Y), toScreenCoordsTransformation) / downscaleFactor;
            Vector2 areaPart = Vector2.Transform(new Vector2(uvScaledArena.Z, uvScaledArena.W), toScreenCoordsTransformation with { M41 = 0f, M42 = 0f }) / downscaleFactor;
            uvScaledArena = new(coordinatePart.X, coordinatePart.Y, areaPart.X, areaPart.Y);

            Shader.Parameters["uvArenaArea"].SetValue(uvScaledArena);
            UseImage(InfernumTextureRegistry.MoonLordBackground.Value, 0, SamplerState.AnisotropicWrap);

            UseOpacity(1f);
            UseIntensity(3f);
            base.Apply();
        }
    }
}
