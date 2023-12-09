using InfernumMode.Content.Buffs;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Content.Rarities.InfernumRarities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Pets
{
    // Dedicated to: BronzeCkn
    public class SuspiciouslyRoundFeather : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 0;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.noMelee = true;
            Item.width = 30;
            Item.height = 30;

            Item.value = Item.sellPrice(0, 5, 0, 0);
            Item.rare = ModContent.RarityType<InfernumRedSparkRarity>();

            Item.shoot = ModContent.ProjectileType<BronzePetProj>();
            Item.buffType = ModContent.BuffType<BronzePetBuff>();
            Item.Infernum_Tooltips().DeveloperItem = true;
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (player.whoAmI == Main.myPlayer && player.itemTime == 0)
                player.AddBuff(Item.buffType, 15, true);
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Feather, 2);
            recipe.AddIngredient(ItemID.ChickenNugget);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
