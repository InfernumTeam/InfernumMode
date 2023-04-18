using InfernumMode.Content.Rarities.Sparkles;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace InfernumMode.Content.Rarities.InfernumRarities
{
    public class InfernumSakuraRarity : ModRarity
    {
        public override Color RarityColor => Color.HotPink;

        internal static List<RaritySparkle> SakuraSparkleList = new();

        public static void DrawCustomTooltipLine(DrawableTooltipLine tooltipLine)
        {
            // Draw the base tooltip text and glow.
            InfernumRarityHelper.DrawBaseTooltipTextAndGlow(tooltipLine, Color.Magenta, Color.HotPink);

            // Draw base sparkles.
            InfernumRarityHelper.SpawnAndUpdateTooltipParticles(tooltipLine, ref SakuraSparkleList, 25, SparkleType.SakuraSparkle);
        }
    }
}
