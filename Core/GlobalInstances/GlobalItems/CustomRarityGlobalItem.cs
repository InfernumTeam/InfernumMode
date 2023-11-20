using InfernumMode.Content.Rarities.InfernumRarities;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.GlobalItems
{
    // TODO - Automate this properly.
    public class CustomRarityGlobalItem : GlobalItem
    {
        public override bool PreDrawTooltipLine(Item item, DrawableTooltipLine line, ref int yOffset)
        {
            // If the item is of the rarity, and the line is the item name.
            if (line.Mod == "Terraria" && line.Name == "ItemName")
            {
                if (item.rare == ModContent.RarityType<InfernumRedRarity>())
                {
                    // Draw the custom tooltip line.
                    InfernumRedRarity.DrawCustomTooltipLine(line);
                    return false;
                }
                else if (item.rare == ModContent.RarityType<InfernumVassalRarity>())
                {
                    // Draw the custom tooltip line.
                    InfernumVassalRarity.DrawCustomTooltipLine(line);
                    return false;
                }
                else if (item.rare == ModContent.RarityType<InfernumProfanedRarity>())
                {
                    // Draw the custom tooltip line.
                    InfernumProfanedRarity.DrawCustomTooltipLine(line);
                    return false;
                }
                else if (item.rare == ModContent.RarityType<InfernumHatgirlRarity>())
                {
                    InfernumHatgirlRarity.DrawCustomTooltipLine(line);
                    return false;
                }
                else if (item.rare == ModContent.RarityType<InfernumEggRarity>())
                {
                    InfernumEggRarity.DrawCustomTooltipLine(line);
                    return false;
                }
                else if (item.rare == ModContent.RarityType<InfernumHyperplaneMatrixRarity>())
                {
                    InfernumHyperplaneMatrixRarity.DrawCustomTooltipLine(line);
                    return false;
                }
                else if (item.rare == ModContent.RarityType<InfernumRedSparkRarity>())
                {
                    InfernumRedSparkRarity.DrawCustomTooltipLine(line);
                    return false;
                }
                else if (item.rare == ModContent.RarityType<InfernumPurityRarity>())
                {
                    InfernumPurityRarity.DrawCustomTooltipLine(line);
                    return false;
                }
                else if (item.rare == ModContent.RarityType<InfernumSoulDrivenHeadphonesRarity>())
                {
                    InfernumSoulDrivenHeadphonesRarity.DrawCustomTooltipLine(line);
                    return false;
                }
                else if (item.rare == ModContent.RarityType<InfernumOceanFlowerRarity>())
                {
                    InfernumOceanFlowerRarity.DrawCustomTooltipLine(line);
                    return false;
                }
                else if (item.rare == ModContent.RarityType<InfernumCyanSparkRarity>())
                {
                    InfernumCyanSparkRarity.DrawCustomTooltipLine(line);
                    return false;
                }
                else if (item.rare == ModContent.RarityType<InfernumScarletSparkleRarity>())
                {
                    InfernumScarletSparkleRarity.DrawCustomTooltipLine(line);
                    return false;
                }
                else if (item.rare == ModContent.RarityType<InfernumPurpleBackglowRarity>())
                {
                    InfernumPurpleBackglowRarity.DrawCustomTooltipLine(line);
                    return false;
                }
                else if (item.rare == ModContent.RarityType<InfernumSakuraRarity>())
                {
                    InfernumSakuraRarity.DrawCustomTooltipLine(line);
                    return false;
                }
                else if (item.rare == ModContent.RarityType<InfernumDreamtasticRarity>())
                {
                    InfernumDreamtasticRarity.DrawCustomTooltipLine(line);
                    return false;
                }
                else if (item.rare == ModContent.RarityType<InfernumTransRarity>())
                {
                    InfernumTransRarity.DrawCustomTooltipLine(line);
                    return false;
                }
                else if (item.rare == ModContent.RarityType<InfernumCreditRarity>())
                {
                    InfernumCreditRarity.DrawCustomTooltipLine(line);
                    return false;
                }
            }
            return true;
        }
    }
}
