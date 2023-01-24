using InfernumMode.Content.Rarities.Sparkles;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace InfernumMode.Content.Rarities.InfernumRarities
{
    public class InfernumHatgirlRarity : ModRarity
    {
        public override Color RarityColor => Color.Red;

        internal static List<RaritySparkle> HourglassSparkleList = new();

        public static void DrawCustomTooltipLine(DrawableTooltipLine tooltipLine)
        {
            // Draw the base tooltip text and glow. new Color(154, 140, 226)
            InfernumRarityHelper.DrawBaseTooltipTextAndGlow(tooltipLine, Color.Lerp(new Color(255, 266, 108), new Color(154, 140, 226), 0.5f), new Color(50, 42, 97), new Color(246, 199, 97));

            // Draw base sparkles.
            InfernumRarityHelper.SpawnAndUpdateTooltipParticles(tooltipLine, ref HourglassSparkleList, 25, SparkleType.HourglassSparkle);
        }
    }
}
