using CalamityMod;
using InfernumMode.Content.Rarities.Sparkles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace InfernumMode.Content.Rarities.InfernumRarities
{
    public class InfernumVassalRarity : ModRarity
    {
        public override Color RarityColor => Color.Cyan;

        internal static List<RaritySparkle> VassalRaritySparkleList = new();

        public static Texture2D DropletTexture => ModContent.Request<Texture2D>("InfernumMode/Content/Rarities/Textures/DropletTexture").Value;

        public static void DrawCustomTooltipLine(DrawableTooltipLine tooltipLine)
        {
            // Draw the base tooltip text and glow.
            InfernumRarityHelper.DrawBaseTooltipTextAndGlow(tooltipLine, Color.Cyan, CalamityUtils.ColorSwap(Color.CornflowerBlue, Color.LightBlue, 2), new(12, 26, 47));

            // Draw base sparkles.
            InfernumRarityHelper.SpawnAndUpdateTooltipParticles(tooltipLine, ref VassalRaritySparkleList, 7, SparkleType.VassalSparkle);
        }
    }
}
