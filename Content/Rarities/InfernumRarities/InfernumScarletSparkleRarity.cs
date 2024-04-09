using System.Collections.Generic;
using CalamityMod;
using InfernumMode.Content.Rarities.Sparkles;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace InfernumMode.Content.Rarities.InfernumRarities
{
    public class InfernumScarletSparkleRarity : ModRarity
    {
        public override Color RarityColor => new(255, 42, 0);

        internal static List<RaritySparkle> SparkleList = [];

        public static void DrawCustomTooltipLine(DrawableTooltipLine tooltipLine)
        {
            // Draw the base tooltip text and glow.
            InfernumRarityHelper.DrawBaseTooltipTextAndGlow(tooltipLine, new Color(255, 72, 40), CalamityUtils.ColorSwap(Color.Orange, Color.DarkRed, 2f), new(56, 14, 13));

            // Draw base sparkles.
            InfernumRarityHelper.SpawnAndUpdateTooltipParticles(tooltipLine, ref SparkleList, 8, SparkleType.ProfanedSparkle);
        }
    }
}
