using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.SummonItems
{
    public class RedBait : ModItem
    {
        public override void SetStaticDefaults()
        {
            SacrificeTotal = 1;
            DisplayName.SetDefault("Red Bait");
            Tooltip.SetDefault("Summons the Dreadnautilus\n" +
                "Can only be used at night\n" +
                "Not consumable");
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.rare = ItemRarityID.Pink;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = false;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Worm);
            recipe.AddIngredient(ModContent.ItemType<EssenceofChaos>(), 5);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }

        public override bool CanUseItem(Player player) => !NPC.AnyNPCs(NPCID.BloodNautilus) && !Main.dayTime;

        public override bool? UseItem(Player player)/* tModPorter Suggestion: Return null instead of false */
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spawnPosition = player.Center - Vector2.UnitY * 400f;
                NPC.SpawnBoss((int)spawnPosition.X, (int)spawnPosition.Y, NPCID.BloodNautilus, player.whoAmI);
            }
            return true;
        }
    }
}
