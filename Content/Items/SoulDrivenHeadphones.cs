using CalamityMod.Items;
using InfernumMode.Content.Projectiles;
using InfernumMode.Content.Rarities.InfernumRarities;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items
{
    public class SoulDrivenHeadphones : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Soul-Driven Headphones");
            Tooltip.SetDefault("Allows you to pick and play custom Infernum music of defeated bosses, and to toggle a special boss battle theme\n" +
                "As you hold onto them, you vaguely feel a mystical presence\n" +
                "You can also hear the sounds of instruments passionately playing");
            SacrificeTotal = 1;
        }
        
        public override void SetDefaults()
        {
            Item.width = 50;
            Item.height = 64;
            Item.useTime = Item.useAnimation = 4;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 0f;

            Item.value = CalamityGlobalItem.RarityHotPinkBuyPrice;
            Item.rare = ModContent.RarityType<InfernumSoulDrivenHeadphonesRarity>();
            Item.Infernum_Tooltips().DeveloperItem = true;

            Item.autoReuse = false;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<SoulDrivenHeadphonesProj>();
            Item.channel = true;
            Item.shootSpeed = 0f;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            TooltipLine line = tooltips.FirstOrDefault(x => x.Mod == "Terraria" && x.Name == "Tooltip1");
            TooltipLine line2 = tooltips.FirstOrDefault(x => x.Mod == "Terraria" && x.Name == "Tooltip2");

            if (line is not null && line2 is not null)
                line.OverrideColor = line2.OverrideColor = new((int)MathHelper.Lerp(156f, 255f, Main.DiscoR / 256f), 108, 251);
        }

        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Glass, 10);
            recipe.AddIngredient(ItemID.Silk, 10);
            recipe.AddIngredient(ItemID.Stinger);
            recipe.AddTile(TileID.WorkBenches);
            recipe.Register();
        }
    }
}
