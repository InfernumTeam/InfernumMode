using System.Collections.Generic;
using InfernumMode.Content.Rarities.Sparkles;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace InfernumMode.Content.Rarities.InfernumRarities
{
    public class InfernumTransRarity : ModRarity
    {
        public override Color RarityColor => Color.HotPink;

        internal static List<RaritySparkle> TransParticleList = new();

        public static void DrawCustomTooltipLine(DrawableTooltipLine tooltipLine)
        {
            // Draw the base tooltip text and glow.
            Color blue = new(51, 191, 255);
            Color pink = new(255, 159, 198);
            InfernumRarityHelper.DrawBaseTooltipTextAndGlow(tooltipLine, glowColor: blue , textOuterColor: pink, Color.Lerp(Color.Black, blue, 0.15f), glowScaleOffset: new(1.2f, 1f));

            // Draw base sparkles.
            InfernumRarityHelper.SpawnAndUpdateTooltipParticles(tooltipLine, ref TransParticleList, 15, SparkleType.TransSparkle);
        }
    }
}
