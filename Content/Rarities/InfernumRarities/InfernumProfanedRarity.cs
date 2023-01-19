using CalamityMod;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Rarities.InfernumRarities
{
    public class InfernumProfanedRarity : ModRarity
    {
        public override Color RarityColor => Color.Red;

        internal static List<RaritySparkle> ProfanedRaritySparkleList = new();

        public static void DrawCustomTooltipLine(DrawableTooltipLine tooltipLine)
        {
            // Draw the base tooltip text and glow.
            InfernumRarityHelper.DrawBaseTooltipTextAndGlow(tooltipLine, new Color(255, 191, 73), CalamityUtils.ColorSwap(Color.Goldenrod, Color.Gold, 2));

            // Draw base sparkles.
            InfernumRarityHelper.SpawnAndUpdateTooltipParticles(tooltipLine, ref ProfanedRaritySparkleList, 14, SparkleType.ProfanedSparkle);
        }
    }
}
