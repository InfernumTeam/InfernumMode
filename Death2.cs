using CalamityMod.Events;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode
{
    public class Death2 : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Infernal Chalice");
            Tooltip.SetDefault("Makes bosses absurd unless Boss Rush is active\n" +
                               "Revengeance Mode must be active to use this item\n" +
                               "Malice Mode is disabled while this is active\n" +
                               "Infernum");
            Main.RegisterItemAnimation(item.type, new DrawAnimationVertical(6, 8));
        }

        public override void SetDefaults()
        {
            item.rare = ItemRarityID.Red;
            item.width = 50;
            item.height = 96;
            item.useAnimation = 45;
            item.useTime = 45;
            item.channel = true;
            item.noUseGraphic = true;
            item.shoot = ModContent.ProjectileType<InfernalChaliceHoldout>();
            item.useStyle = ItemUseStyleID.HoldingUp;
            item.consumable = false;
        }

        public override bool CanUseItem(Player player) => CalamityWorld.revenge && !BossRushEvent.BossRushActive;

        public override void ModifyTooltips(List<TooltipLine> tooltips) => tooltips.FirstOrDefault(x => x.Name == "Tooltip3" && x.mod == "Terraria").overrideColor = Color.DarkRed;

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
