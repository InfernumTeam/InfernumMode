using InfernumMode.Content.Buffs;
using InfernumMode.Content.Projectiles;
using InfernumMode.Content.Projectiles.Pets;
using InfernumMode.Content.Rarities.InfernumRarities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Pets
{
    public class BlastedTophat : ModItem
    {
        public override void SetStaticDefaults()
        {
            SacrificeTotal = 1;
            DisplayName.SetDefault("Blasted Tophat");
            Tooltip.SetDefault("Summons a small hat girl that gives you advice about bosses that you fight");
        }
        public override void SetDefaults()
        {
            Item.damage = 0;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.noMelee = true;
            Item.width = 30;
            Item.height = 30;

            Item.value = Item.sellPrice(copper: 69);
            Item.rare = ModContent.RarityType<InfernumHatgirlRarity>();

            Item.shoot = ModContent.ProjectileType<HatGirl>();
            Item.buffType = ModContent.BuffType<HatGirlBuff>();
            Item.UseSound = SoundID.Meowmere;
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
            recipe.AddIngredient(ItemID.TopHat);
            recipe.AddIngredient(ItemID.Cobweb, 15);
            recipe.AddIngredient(ItemID.Torch);
            recipe.AddIngredient(ItemID.Dynamite);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
