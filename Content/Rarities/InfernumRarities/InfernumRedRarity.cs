using CalamityMod;
using InfernumMode.Content.Rarities.Sparkles;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace InfernumMode.Content.Rarities.InfernumRarities
{
    public class InfernumRedRarity : ModRarity
    {
        public override Color RarityColor => Color.Red;

        internal static List<RaritySparkle> RedRaritySparkleList = new();

        public static void DrawCustomTooltipLine(DrawableTooltipLine tooltipLine)
        {
            // Draw the base tooltip text and glow.
            InfernumRarityHelper.DrawBaseTooltipTextAndGlow(tooltipLine, Color.OrangeRed, CalamityUtils.ColorSwap(new Color(200, 0, 0), Color.Lerp(Color.OrangeRed, Color.Red, 0.34f), 2));

            // Draw base sparkles.
            InfernumRarityHelper.SpawnAndUpdateTooltipParticles(tooltipLine, ref RedRaritySparkleList, 8, SparkleType.RelicSparkle);
        }
    }
}
