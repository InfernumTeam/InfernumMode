using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Items
{
    public class SuspiciousSkull : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Suspicious Skull");
            Tooltip.SetDefault("Summons Skeletron\n" +
                "Can only be used at night\n" +
                "Not consumable");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.rare = ItemRarityID.Green;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.consumable = false;
        }

        public override bool CanUseItem(Player player) => !NPC.AnyNPCs(NPCID.SkeletronHead) && !Main.dayTime;

        public override void AddRecipes()
        {
            CreateRecipe(1).AddRecipeGroup("Wood", 25).AddIngredient(ItemID.RottenChunk, 5).AddIngredient(ItemID.ShadowScale, 20).Register();
            CreateRecipe(1).AddRecipeGroup("Wood", 25).AddIngredient(ItemID.Vertebrae, 5).AddIngredient(ItemID.TissueSample, 20).Register();
        }

        public override bool? UseItem(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spawnPosition = player.Center - Vector2.UnitY * 800f;
                NPC.NewNPC(new InfernumSource(), (int)spawnPosition.X, (int)spawnPosition.Y, NPCID.SkeletronHead);
            }
            return true;
        }
    }
}
