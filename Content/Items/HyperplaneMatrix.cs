using CalamityMod;
using CalamityMod.Items;
using CalamityMod.NPCs.ExoMechs;
using InfernumMode.Content.Projectiles;
using InfernumMode.Content.Rarities.InfernumRarities;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items
{
    public class HyperplaneMatrix : ModItem
    {
        public static bool CanBeUsed => (DownedBossSystem.downedSCal && DownedBossSystem.downedExoMechs) || Main.LocalPlayer.name == "Dominic";

        // How much the player gets hurt for if the matrix explodes due to being able to be used.
        public const int UnableToBeUsedHurtDamage = 500;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Hyperplane Matrix");
            Tooltip.SetDefault("An incalculably complex apparatus containing infinite power\n" +
                "Using it opens a panel that can grants a variety of reality-warping abilities\n" +
                "Upon a distant celestial body, a being named Draedon is born\n" +
                "Made to construct and to evolve\n" +
                "One purpose, one cycle\n" +
                "High, the machine works\n" +
                "Low, it rests\n" +
                "All ordered\n" +
                "All same");
            SacrificeTotal = 1;
        }
        
        public override void SetDefaults()
        {
            Item.width = 56;
            Item.height = 56;
            Item.useTime = Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 0f;

            Item.value = CalamityGlobalItem.RarityHotPinkBuyPrice;
            Item.rare = ModContent.RarityType<InfernumHyperplaneMatrixRarity>();
            Item.Infernum_Tooltips().DeveloperItem = true;

            Item.autoReuse = true;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<HyperplaneMatrixProjectile>();
            Item.channel = true;
            Item.shootSpeed = 0f;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var poemTooltips = tooltips.Where(x => x.Name.Contains("Tooltip") && x.Mod == "Terraria");
            foreach (var tooltip in poemTooltips)
            {
                int tooltipLineIndex = (int)char.GetNumericValue(tooltip.Name.Last());
                if (tooltipLineIndex >= 2)
                    tooltip.OverrideColor = Draedon.TextColor;
            }
        }

        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
    }
}
