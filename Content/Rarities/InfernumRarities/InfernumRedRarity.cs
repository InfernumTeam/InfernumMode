using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
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
            InfernumRarityHelper.DrawBaseTooltipTextAndGlow(tooltipLine, Color.OrangeRed, CalamityUtils.ColorSwap(new Color(200, 0, 0), Color.OrangeRed, 2));

            // Draw base sparkles.
            InfernumRarityHelper.SpawnAndUpdateTooltipParticles(tooltipLine, ref RedRaritySparkleList, 8, SparkleType.RelicSparkle);
        }
    }
}
