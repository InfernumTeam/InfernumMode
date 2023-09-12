using InfernumMode.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

        public override void OnModLoad()
        {
            CreatedFields = new();
            Main.OnPreDraw += UpdateFields;
        }

        internal static void UpdateFields(GameTime obj)
        {
            if (Main.gameMenu || (InfernumConfig.Instance?.ReducedGraphicsConfig ?? true))
                return;

            CreatedFields.RemoveAll(f => f is null);

            if (!CreatedFields.Any())
                return;

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
