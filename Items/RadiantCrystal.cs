using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Items
{
    public class RadiantCrystal : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Radiant Crystal");
            Tooltip.SetDefault("Summons the Empress of Light\n" +
                "Does not need to be used in the Hallow\n" +
                "Not consumable");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.rare = ItemRarityID.Yellow;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = false;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.UnicornHorn);
            recipe.AddIngredient(ItemID.CrystalShard, 10);
            recipe.AddIngredient(ItemID.PixieDust, 10);
            recipe.Register();
        }

        public override bool CanUseItem(Player player) => !NPC.AnyNPCs(NPCID.HallowBoss);

        public override bool? UseItem(Player player)/* tModPorter Suggestion: Return null instead of false */
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spawnPosition = player.Center - Vector2.UnitY * 200f;
                NPC.SpawnBoss((int)spawnPosition.X, (int)spawnPosition.Y, NPCID.HallowBoss, player.whoAmI);
            }
            return true;
        }
    }
}
