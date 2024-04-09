using System.Collections.Generic;
using InfernumMode.Content.Rarities.Sparkles;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace InfernumMode.Content.Rarities.InfernumRarities
{
    public class InfernumRedSparkRarity : ModRarity
    {
        public override Color RarityColor => new(250, 95, 105);

        internal static List<RaritySparkle> LightningSparkleList = [];

        public static void DrawCustomTooltipLine(DrawableTooltipLine tooltipLine)
        {
            // Draw the base tooltip text and glow.
            InfernumRarityHelper.DrawBaseTooltipTextAndGlow(tooltipLine, Color.DarkRed, Color.Pink);

            // Draw base sparkles.
            InfernumRarityHelper.SpawnAndUpdateTooltipParticles(tooltipLine, ref LightningSparkleList, 3, SparkleType.RedLightningSparkle);
        }
    }
}
