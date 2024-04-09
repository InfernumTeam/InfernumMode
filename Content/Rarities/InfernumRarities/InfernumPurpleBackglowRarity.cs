using System.Collections.Generic;
using CalamityMod;
using InfernumMode.Content.Rarities.Sparkles;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace InfernumMode.Content.Rarities.InfernumRarities
{
    public class InfernumPurpleBackglowRarity : ModRarity
    {
        public override Color RarityColor => Color.White;

        internal static List<RaritySparkle> PuritySparkleList = [];

        public static void DrawCustomTooltipLine(DrawableTooltipLine tooltipLine)
        {
            Color outerColor = CalamityUtils.ColorSwap(Color.Purple, Color.Fuchsia, 2f);

            // Draw the base tooltip text and glow.
            InfernumRarityHelper.DrawBaseTooltipTextAndGlow(tooltipLine, glowColor: Color.Violet, textOuterColor: outerColor);
        }
    }
}
