using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Common.Graphics.Fluids
{
    public class FluidSimulatorManagementSystem : ModSystem
    {
        // For the sake of absolute management all fields must be kept track of to ensure no loose resources are hanging around (Especially on mod reloads, since GPU memory cannot be easily cleared automatically).
        // Furthermore, the updating must be performed in such a way that it happens at a specialized point in the draw loop, to prevent screwing up the vanilla game's backbuffer contents.
        internal static readonly List<FluidFieldInfernum> CreatedFields = new();

        public override void OnModLoad()
        {
            Main.OnPreDraw += UpdateFields;
        }

        internal static void UpdateFields(GameTime obj)
        {
            if (Main.gameMenu)
                return;

            /*
            TestField.ShouldUpdate = true;

            int instanceCount = 8;
            for (int i = 0; i < instanceCount; i++)
            {
                var color = CalamityUtils.MulticolorLerp(MathF.Sin(Main.GlobalTimeWrappedHourly * 2f + i * 1.2f) * 0.5f + 0.5f, Color.Red, Color.Orange, Color.Yellow, Color.Wheat, Color.Orange);
                float angularOffset = MathF.Cos(Main.GlobalTimeWrappedHourly * 2.2f + i) * 0.25f;
                float speedFactor = MathHelper.Lerp(2f, 4f, MathF.Cos(Main.GlobalTimeWrappedHourly * 2f + i * 3.3f) * 0.5f + 0.5f);
                Vector2 angularSpawnOffset = (MathHelper.TwoPi * i / instanceCount + Main.GlobalTimeWrappedHourly * 0.45f).ToRotationVector2() * 104f;

                angularSpawnOffset.X += MathF.Sin(i + Main.GlobalTimeWrappedHourly * 0.8f) * 45f;
                angularSpawnOffset.Y += MathF.Sin(i * 1.9f + Main.GlobalTimeWrappedHourly * 0.8f + 1.09f) * 25f;

                TestField.CreateSource(new Vector2(325 + angularSpawnOffset.X, 245 + angularSpawnOffset.Y).ToPoint(), Vector2.One * 11f, -angularSpawnOffset.SafeNormalize(Vector2.UnitY).RotatedBy(angularOffset) * speedFactor, color, 0.5f);
            }
            */

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
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