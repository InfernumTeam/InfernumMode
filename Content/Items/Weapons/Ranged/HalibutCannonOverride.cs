using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Rarities;
using InfernumMode.Content.Projectiles.Ranged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Items.Weapons.Ranged
{
    public static class HalibutCannonOverride
    {
        public static void SetDefaults(Item item)
        {
			item.damage = 50;
			item.DamageType = DamageClass.Ranged;
			item.width = 118;
			item.height = 56;
			item.useTime = 10;
			item.useAnimation = 20;
			item.useStyle = ItemUseStyleID.Shoot;
			item.rare = ModContent.RarityType<HotPink>();
			item.noMelee = true;
			item.knockBack = 1f;
			item.value = CalamityGlobalItem.Rarity16BuyPrice;
			item.UseSound = null;
			item.autoReuse = true;
			item.channel = true;
			item.shoot = ModContent.ProjectileType<HalibutCannonHoldout>();
			item.shootSpeed = 12f;
			item.useAmmo = AmmoID.Bullet;
			item.Calamity().canFirePointBlankShots = true;
		}

		public static bool Shoot(Item item, Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
			if (Main.myPlayer == player.whoAmI)
				Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<HalibutCannonHoldout>(), damage, knockback, player.whoAmI);
			return false;
        }
    }
}
