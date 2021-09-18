using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;
using CalamityMod.NPCs.SunkenSea;
using CalamityMod.Items.Placeables;

namespace InfernumMode.Items
{
    public class SparklingTunaCan : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sparkling Tuna Can");
            Tooltip.SetDefault("Summons the Giant Clam\n" +
                "Can only be used in the sunken sea\n" +
                "Infernum");
        }

        public override void SetDefaults()
        {
            item.width = 28;
            item.height = 34;
            item.rare = ItemRarityID.Cyan;
            item.useAnimation = 45;
            item.useTime = 45;
            item.useStyle = ItemUseStyleID.EatingUsing;
            item.consumable = true;
        }

        public override bool CanUseItem(Player player) => !NPC.AnyNPCs(ModContent.NPCType<GiantClam>()) && player.Calamity().ZoneSunkenSea;
        public override void ModifyTooltips(List<TooltipLine> tooltips) => tooltips.FirstOrDefault(x => x.Name == "Tooltip2" && x.mod == "Terraria").overrideColor = Color.DarkRed;

        public override void AddRecipes()
        {
            #region fuck fish
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<PrismShard>(), 3);
            recipe.AddIngredient(ModContent.ItemType<Navystone>(), 3);
            recipe.needWater = true;
            recipe.SetResult(this);
            recipe.AddRecipe();

            ModRecipe recipe1 = new ModRecipe(mod);
            recipe1.AddIngredient(ItemID.TinCan, 1);
            recipe1.AddIngredient(ItemID.Bass, 1);
            recipe1.AddTile(TileID.CookingPots);
            recipe1.SetResult(item.type, 3);
            recipe1.AddRecipe();

            recipe1 = new ModRecipe(mod);
            recipe1.AddIngredient(ItemID.TinCan, 1);
            recipe1.AddIngredient(ItemID.RedSnapper, 1);
            recipe1.AddTile(TileID.CookingPots);
            recipe1.SetResult(item.type, 3);
            recipe1.AddRecipe();

            recipe1 = new ModRecipe(mod);
            recipe1.AddIngredient(ItemID.TinCan, 1);
            recipe1.AddIngredient(ItemID.Tuna, 1);
            recipe1.AddTile(TileID.CookingPots);
            recipe1.SetResult(item.type, 3);
            recipe1.AddRecipe();

            recipe1 = new ModRecipe(mod);
            recipe1.AddIngredient(ItemID.TinCan, 1);
            recipe1.AddIngredient(ItemID.Trout, 1);
            recipe1.AddTile(TileID.CookingPots);
            recipe1.SetResult(item.type, 3);
            recipe1.AddRecipe();
            #endregion
        }

        public override bool UseItem(Player player)
        {
            Vector2 spawnPosition = new Vector2(player.direction == 1 ? player.Center.X + 300 : player.Center.X - 300, player.Center.Y);
            NPC.NewNPC((int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<GiantClam>());
            return true;
        }
    }
}
