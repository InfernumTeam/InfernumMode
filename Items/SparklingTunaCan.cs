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
            CreateRecipe(1).AddIngredient(ModContent.ItemType<PrismShard>(), 3).AddIngredient(ModContent.ItemType<Navystone>(), 3).Register();
            CreateRecipe(3).AddIngredient(ItemID.TinCan, 1).AddIngredient(ItemID.Bass, 1).AddTile(TileID.CookingPots).Register();
            CreateRecipe(3).AddIngredient(ItemID.TinCan, 1).AddIngredient(ItemID.RedSnapper, 1).AddTile(TileID.CookingPots).Register();
            CreateRecipe(3).AddIngredient(ItemID.TinCan, 1).AddIngredient(ItemID.Tuna, 1).AddTile(TileID.CookingPots).Register();
            CreateRecipe(3).AddIngredient(ItemID.TinCan, 1).AddIngredient(ItemID.Trout, 1).AddTile(TileID.CookingPots).Register();
        }

        public override bool? UseItem(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spawnPosition = player.Center + Vector2.UnitX * player.direction * 300f;
                NPC.NewNPC(new InfernumSource(), player.GetItemSource_Misc(0), (int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<GiantClam>());
            }
            return true;
        }
    }
}
