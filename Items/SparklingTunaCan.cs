using CalamityMod;
using CalamityMod.Items.Placeables;
using CalamityMod.NPCs.SunkenSea;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Items
{
    public class SparklingTunaCan : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sparkling Mollusk Can");
            Tooltip.SetDefault("Summons the Giant Clam\n" +
                "Can only be used in the sunken sea\n" +
                "Not consumable");
        }

        public override void SetDefaults()
        {
            item.width = 28;
            item.height = 34;
            item.rare = ItemRarityID.Green;
            item.useAnimation = 45;
            item.useTime = 45;
            item.useStyle = ItemUseStyleID.EatingUsing;
            item.consumable = false;
            item.maxStack = 999;
        }

        public override bool CanUseItem(Player player) => !NPC.AnyNPCs(ModContent.NPCType<GiantClam>()) && player.Calamity().ZoneSunkenSea;

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
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spawnPosition = player.Center + Vector2.UnitX * player.direction * 300f;
                NPC.NewNPC((int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<GiantClam>());
            }
            return true;
        }
    }
}
