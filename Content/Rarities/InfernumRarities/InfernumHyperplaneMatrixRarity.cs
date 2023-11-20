using CalamityMod;
using InfernumMode.Content.Rarities.Sparkles;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace InfernumMode.Content.Rarities.InfernumRarities
{
    public class InfernumHyperplaneMatrixRarity : ModRarity
    {
        public override Color RarityColor => new(17, 17, 23);

        internal static List<RaritySparkle> CodeSymbols = new();

        public static void DrawCustomTooltipLine(DrawableTooltipLine tooltipLine)
        {
            // Draw the base tooltip text and glow.
            Color outerColor = CalamityUtils.ColorSwap(new(117, 226, 211), new(155, 185, 205), 3f);
            outerColor = Color.Lerp(outerColor, Color.DarkCyan, 0.6f);
            InfernumRarityHelper.DrawBaseTooltipTextAndGlow(tooltipLine, glowColor: Color.LightSlateGray, textOuterColor: outerColor, Color.Lerp(Color.Black, outerColor, 0.2f));

            // Draw base sparkles.
            InfernumRarityHelper.SpawnAndUpdateTooltipParticles(tooltipLine, ref CodeSymbols, 2, SparkleType.CodeSymbols);
        }
    }
}
