using CalamityMod.Items.Materials;
using InfernumMode.Content.Buffs;
using InfernumMode.Content.Projectiles;
using InfernumMode.Content.Rarities.InfernumRarities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Pets
{
    public class BronzePetItem : ModItem
    {
        public override void SetStaticDefaults()
        {
            SacrificeTotal = 1;
            DisplayName.SetDefault("lol");
            Tooltip.SetDefault("lol 2");
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

            Item.value = Item.sellPrice(0, 5, 0, 0);
            Item.rare = ModContent.RarityType<InfernumHatgirlRarity>();

            Item.shoot = ModContent.ProjectileType<BronzePetProj>();
            Item.buffType = ModContent.BuffType<BronzePetBuff>();
            //Item.UseSound = SoundID.Meowmere;
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
            recipe.AddIngredient(ModContent.ItemType<DesertFeather>(), 3);
            recipe.AddIngredient(ItemID.ChickenNugget, 2);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
