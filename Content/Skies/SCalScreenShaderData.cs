using CalamityMod.NPCs;
using InfernumMode.Assets.ExtraTextures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas.SupremeCalamitasBehaviorOverride;

namespace InfernumMode.Content.Skies
{
    public class SCalScreenShaderData : ScreenShaderData
    {
        public static Color BackgroundColor
        {
            get;
            set;
        }

        public static Color GriefColor => new(238, 58, 58);

        public static Color LamentColor => new(33, 158, 248);

        public static Color EpiphanyColor => Color.Lerp(Color.Yellow, Color.Red, 0.56f);

        public static Color AcceptanceColor => new(78, 78, 78);

        public SCalScreenShaderData(Ref<Effect> shader, string passName) : base(shader, passName) { }

        public override void Apply()
        {
            // If scal is not present do not draw.
            if (CalamityGlobalNPC.SCal < 0)
            {
                BackgroundColor = GriefColor;
                return;
            }

            if (BackgroundColor == Color.Transparent)
                BackgroundColor = GriefColor;

            UseTargetPosition(Main.LocalPlayer.Center);
            UseColor(BackgroundColor);

            NPC scal = Main.npc[CalamityGlobalNPC.SCal];
            float lifeRatio = scal.life / (float)scal.lifeMax;
            float brightness = 1f;
            bool acceptancePhase = scal.Infernum().ExtraAI[4] == 4f && scal.ai[0] == (int)SCalAttackType.DesperationPhase;

            // Make the backgrounds change based on SCal's HP thresholds in accordance with the Stained Brutal Calamity track.
            if (acceptancePhase)
                BackgroundColor = Color.Lerp(BackgroundColor, AcceptanceColor, 0.1f);
            else if (lifeRatio <= Phase4LifeRatio)
            {
                BackgroundColor = Color.Lerp(BackgroundColor, EpiphanyColor, 0.1f);
                brightness = 2f;
            }
            else if (lifeRatio <= Phase3LifeRatio)
                BackgroundColor = Color.Lerp(BackgroundColor, LamentColor, 0.1f);

            // Perform various matrix calculations to transform SCal's arena to UV coordinate space.
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
            UseImage(InfernumTextureRegistry.GrayscaleWater.Value, 0, SamplerState.AnisotropicWrap);

            UseOpacity(0.36f);
            UseIntensity(brightness);
            base.Apply();
        }
    }
}
