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
            Color outerColor = Color.CadetBlue;
            InfernumRarityHelper.DrawBaseTooltipTextAndGlow(tooltipLine, glowColor: Color.OrangeRed, textOuterColor: outerColor, Color.Lerp(Color.Black, outerColor, 0.1f));

            // Draw base sparkles.
            InfernumRarityHelper.SpawnAndUpdateTooltipParticles(tooltipLine, ref CreditSparkles, 16, SparkleType.CreditSparkle);
        }
    }
}
