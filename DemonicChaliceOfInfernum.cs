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
    [LegacyName("Death2")]
    public class DemonicChaliceOfInfernum : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Demonic Chalice of Infernum");
            Tooltip.SetDefault("Makes bosses absurd unless Boss Rush is active\n" +
                               "Revengeance Mode must be active to use this item\n" +
                               "Malice Mode is disabled while this is active\n" +
                               "This item cannot be used in Master Mode or For The Worthy seed worlds\n" +
                               "Infernum");
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(6, 8));
        }

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Red;
            Item.width = 50;
            Item.height = 96;
            Item.useAnimation = 46;
            Item.useTime = 46;
            Item.channel = true;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<InfernalChaliceHoldout>();
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = false;
        }

        public override bool CanUseItem(Player player)
        {
            if (!CalamityWorld.revenge || BossRushEvent.BossRushActive)
                return false;

            // Go fuck yourself.
            if (Main.masterMode || Main.getGoodWorld || CalamityWorld.malice)
                return false;

            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) => tooltips.FirstOrDefault(x => x.Name == "Tooltip4" && x.Mod == "Terraria").OverrideColor = Color.DarkRed;

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
