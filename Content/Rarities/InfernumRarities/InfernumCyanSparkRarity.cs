using System.Collections.Generic;
using InfernumMode.Content.Rarities.Sparkles;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace InfernumMode.Content.Rarities.InfernumRarities
{
    public class InfernumCyanSparkRarity : ModRarity
    {
        public override Color RarityColor => new(102, 255, 248);

        internal static List<RaritySparkle> LightningSparkleList = [];

        public static void DrawCustomTooltipLine(DrawableTooltipLine tooltipLine)
        {
            // Draw the base tooltip text and glow.
            InfernumRarityHelper.DrawBaseTooltipTextAndGlow(tooltipLine, Color.SkyBlue, Color.Cyan);

            // Draw base sparkles.
            InfernumRarityHelper.SpawnAndUpdateTooltipParticles(tooltipLine, ref LightningSparkleList, 3, SparkleType.CyanLightningSparkle);
        }
    }
}
