using CalamityMod;
using InfernumMode.Content.Rarities.Sparkles;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace InfernumMode.Content.Rarities.InfernumRarities
{
    public class InfernumPurityRarity : ModRarity
    {
        public override Color RarityColor => Color.Red;

        internal static List<RaritySparkle> PuritySparkleList = new();

        public static void DrawCustomTooltipLine(DrawableTooltipLine tooltipLine)
        {
            Color outerColor = CalamityUtils.ColorSwap(Color.Cyan, Color.LightBlue, 2);
            // Draw the base tooltip text and glow.
            InfernumRarityHelper.DrawBaseTooltipTextAndGlow(tooltipLine, glowColor: Color.DeepSkyBlue, textOuterColor: outerColor);

            // Draw base sparkles.
            InfernumRarityHelper.SpawnAndUpdateTooltipParticles(tooltipLine, ref PuritySparkleList, 15, SparkleType.PuritySparkle);
        }
    }
}
