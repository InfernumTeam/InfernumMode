using CalamityMod;
using InfernumMode.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.Fluids
{
    public class FluidSimulatorManagementSystem : ModSystem
    {
        // For the sake of absolute management all fields must be kept track of to ensure no loose resources are hanging around (Especially on mod reloads, since GPU memory cannot be easily cleared automatically).
        // Furthermore, the updating must be performed in such a way that it happens at a specialized point in the draw loop, to prevent screwing up the vanilla game's backbuffer contents.
        internal static List<FluidFieldInfernum> CreatedFields = new();

        //internal static FluidFieldInfernum TestField = new(650, 650, new(0.0051f, 0.8f, 0.99975f, 0.3f, 0.08f, 0.77f, 0.99f));

        public override void OnModLoad()
        {
            CreatedFields = new();
            Main.OnPreDraw += UpdateFields;
            //On.Terraria.Main.DrawInfernoRings += DrawTestField;
        }

        private void DrawTestField(On.Terraria.Main.orig_DrawInfernoRings orig, Main self)
        {
            /*
            TestField.Draw(new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f - Vector2.UnitY * 36f, 1f, 85f, new Vector4[]
            {
                Color.Transparent.ToVector4() with { W = 0f },
                Color.SaddleBrown.ToVector4()
            });
            */
        }

        public override void OnModUnload()
        {
            // Clear all GPU memory that was used by the created fields when the mod is unloaded.
            Main.RunOnMainThread(() =>
            {
                for (int i = 0; i < CreatedFields.Count; i++)
                {
                    CreatedFields[i]?.Dispose();
                    i = 0;
                }
            });
        }

        internal static void UpdateFields(GameTime obj)
        {
            if (Main.gameMenu || (InfernumConfig.Instance?.ReducedGraphicsConfig ?? true))
                return;

            bool doLava = false;
            int instanceCount = doLava ? 8 : 56;

            //TestField.ShouldUpdate = true;

            // Lava convergence.
            /*
            if (doLava)
            {
                TestField.Properties = new(0.0021f, 0.8f, 0.99975f, 0.3f, 0.12f, 0.77f, 0.99f);

                for (int i = 0; i < instanceCount; i++)
                {
                    var color = CalamityUtils.MulticolorLerp(MathF.Sin(Main.GlobalTimeWrappedHourly * 2f + i * 1.2f) * 0.5f + 0.5f, Color.OrangeRed, Color.Orange, Color.Wheat, Color.Orange);
                    float angularOffset = MathF.Cos(Main.GlobalTimeWrappedHourly * 2.2f + i) * 0.25f;
                    float speedFactor = MathHelper.Lerp(9f, 13f, MathF.Cos(Main.GlobalTimeWrappedHourly * 2f + i * 3.3f) * 0.5f + 0.5f);
                    Vector2 angularSpawnOffset = (MathHelper.TwoPi * i / instanceCount + Main.GlobalTimeWrappedHourly * 0.017f).ToRotationVector2() * 194f;

                    angularSpawnOffset.X += MathF.Sin(i + Main.GlobalTimeWrappedHourly * 0.8f) * 45f;
                    angularSpawnOffset.Y += MathF.Sin(i * 1.9f + Main.GlobalTimeWrappedHourly * 0.8f + 1.09f) * 25f;

                    TestField.CreateSource(new Vector2(325 + angularSpawnOffset.X, 245 + angularSpawnOffset.Y).ToPoint(), Vector2.One * 11f, -angularSpawnOffset.SafeNormalize(Vector2.UnitY).RotatedBy(angularOffset) * speedFactor, color, 1f);
                }
            }

            // Flame jet.
            else
            {
                TestField.Properties = new(0.01f, 2.9f, 0.986f, 1.19f, 0.5f, 0.77f, 0.98f);
                TestField.MovementUpdateSteps = 5;

                float interpolant = Main.GlobalTimeWrappedHourly % 1f;
                for (int i = 0; i < instanceCount; i++)
                {
                    var color = CalamityUtils.MulticolorLerp(MathF.Pow(interpolant, 0.2f), Color.Wheat, Color.Orange, Color.Red, Color.Black);
                    float speedFactor = Main.rand.NextFloat(9f, 10f) * TestField.Width * 1.04f / (i * 0.04f + 1f) * 9f;
                    Vector2 angularSpawnOffset = (MathHelper.TwoPi * i / instanceCount).ToRotationVector2() * 6f;
                    Vector2 velocity = -Vector2.UnitY.RotatedBy(MathF.Sin(Main.GlobalTimeWrappedHourly * 4f) * 0.2f) * speedFactor;

                    TestField.CreateSource(new Vector2(TestField.Width / 2 + angularSpawnOffset.X, TestField.Height / 2 + angularSpawnOffset.Y).ToPoint(), Vector2.One, velocity, color, 1f);
                }
            }
            */

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            CreatedFields.RemoveAll(f => f is null);
            foreach (FluidFieldInfernum field in CreatedFields)
            {
                if (!field.ShouldUpdate)
                    continue;

                field.PerformUpdateStep();
            }

            Main.spriteBatch.End();
        }
    }
}