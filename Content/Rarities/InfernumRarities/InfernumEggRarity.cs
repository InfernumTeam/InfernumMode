using CalamityMod;
using InfernumMode.Content.Rarities.Sparkles;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace InfernumMode.Content.Rarities.InfernumRarities
{
    public class InfernumEggRarity : ModRarity
    {
        public override Color RarityColor => Color.Gold;

        internal static List<RaritySparkle> EggSparkleList = [];

        public static void DrawCustomTooltipLine(DrawableTooltipLine tooltipLine)
        {
            // Draw the base tooltip text and glow.
            Color outerColor = CalamityUtils.ColorSwap(new(255, 223, 192), new(238, 195, 154), 2f);
            InfernumRarityHelper.DrawBaseTooltipTextAndGlow(tooltipLine, glowColor: Color.Gold, textOuterColor: outerColor, Color.Lerp(Color.Black, outerColor, 0.15f));

            // Draw base sparkles.
            InfernumRarityHelper.SpawnAndUpdateTooltipParticles(tooltipLine, ref EggSparkleList, 10, SparkleType.EggSparkle);
        }
    }
}
