using System.Collections.Generic;
using InfernumMode.Content.Rarities.Sparkles;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace InfernumMode.Content.Rarities.InfernumRarities
{
    public class InfernumCreditRarity : ModRarity
    {
        public override Color RarityColor => Color.Black;

        internal static List<RaritySparkle> CreditSparkles = new();

        public static void DrawCustomTooltipLine(DrawableTooltipLine tooltipLine)
        {
            // Draw the base tooltip text and glow.
            Color outerColor = Color.Lerp(Color.Black, Color.Gold, 0.05f);
            InfernumRarityHelper.DrawBaseTooltipTextAndGlow(tooltipLine, glowColor: Color.DarkOrange, textOuterColor: outerColor, Color.Lerp(Color.OrangeRed, Color.Goldenrod, 0.5f));

            // Draw base sparkles.
            InfernumRarityHelper.SpawnAndUpdateTooltipParticles(tooltipLine, ref CreditSparkles, 16, SparkleType.CreditSparkle);
        }
    }
}
