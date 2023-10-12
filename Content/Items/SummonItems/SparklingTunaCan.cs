using CalamityMod;
using CalamityMod.Items.Placeables;
using CalamityMod.NPCs.SunkenSea;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.SummonItems
{
    public class SparklingTunaCan : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            ItemID.Sets.SortingPriorityBossSpawns[Type] = 2; // King Slime
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 34;
            Item.rare = ItemRarityID.Green;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.consumable = false;
            Item.maxStack = 999;
        }

        public override bool CanUseItem(Player player) => !NPC.AnyNPCs(ModContent.NPCType<GiantClam>()) && player.Calamity().ZoneSunkenSea;

        public override void AddRecipes()
        {
            #region fuck fish
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<PrismShard>(), 3);
            recipe.AddIngredient(ModContent.ItemType<Navystone>(), 3);
            recipe.Register();

            Recipe recipe1 = Recipe.Create(Item.type);
            recipe1.AddIngredient(ItemID.TinCan, 1);
            recipe1.AddIngredient(ItemID.Bass, 1);
            recipe1.AddTile(TileID.CookingPots);
            recipe1.Register();

            recipe1 = Recipe.Create(Item.type);
            recipe1.AddIngredient(ItemID.TinCan, 1);
            recipe1.AddIngredient(ItemID.RedSnapper, 1);
            recipe1.AddTile(TileID.CookingPots);
            recipe1.Register();

            recipe1 = Recipe.Create(Item.type);
            recipe1.AddIngredient(ItemID.TinCan, 1);
            recipe1.AddIngredient(ItemID.Tuna, 1);
            recipe1.AddTile(TileID.CookingPots);
            recipe1.Register();

            recipe1 = Recipe.Create(Item.type);
            recipe1.AddIngredient(ItemID.TinCan, 1);
            recipe1.AddIngredient(ItemID.Trout, 1);
            recipe1.AddTile(TileID.CookingPots);
            recipe1.Register();
            #endregion
        }

        public override bool? UseItem(Player player)/* tModPorter Suggestion: Return null instead of false */
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spawnPosition = player.Center + Vector2.UnitX * player.direction * 300f;
                NPC.NewNPC(player.GetSource_ItemUse(Item), (int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<GiantClam>());
            }
            return true;
        }
    }
}
