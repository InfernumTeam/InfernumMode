using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Items.SummonItems
{
    public class TrufflePlatter : ModItem
    {
        public override void SetStaticDefaults()
        {
            SacrificeTotal = 1;
            DisplayName.SetDefault("Truffle Worm Platter");
            Tooltip.SetDefault("Summons Duke Fishron\n" +
                "Can only be used at the Ocean\n" +
                "Not consumable");
        }

        public override void SetDefaults()
        {
            Item.width = 38;
            Item.height = 54;
            Item.rare = ItemRarityID.Yellow;
            Item.useAnimation = 40;
            Item.useTime = 40;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.consumable = false;
        }

        public override bool CanUseItem(Player player) => !NPC.AnyNPCs(NPCID.DukeFishron) && player.ZoneBeach;

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.TruffleWorm, 3);
            recipe.AddIngredient(ItemID.FoodPlatter);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }

        public override bool? UseItem(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spawnPosition = player.Center - Vector2.UnitY * 800f;
                NPC.NewNPC(player.GetSource_ItemUse(Item), (int)spawnPosition.X, (int)spawnPosition.Y, NPCID.DukeFishron);
            }
            return true;
        }
    }
}
