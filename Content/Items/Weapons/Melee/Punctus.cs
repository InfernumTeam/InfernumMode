using CalamityMod;
using CalamityMod.Items;
using InfernumMode.Content.Projectiles.Melee;
using InfernumMode.Content.Rarities.InfernumRarities;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace InfernumMode.Content.Items.Weapons.Melee
{
    // Dedicated to: Toasty
    public class Punctus : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
            ItemID.Sets.BonusAttackSpeedMultiplier[Item.type] = 0.33f;
            ItemID.Sets.Spears[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.damage = 950;
            Item.knockBack = 4.5f;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = Item.useTime = 32;
            Item.autoReuse = true;
            Item.channel = true;
            Item.shoot = ModContent.ProjectileType<PunctusProjectile>();
            Item.shootSpeed = 45f;

            Item.value = CalamityGlobalItem.Rarity9BuyPrice;
            Item.rare = ModContent.RarityType<InfernumProfanedRarity>();
            Item.Infernum_Tooltips().DeveloperItem = true;

            Item.width = Item.height = 90;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.UseSound = SoundID.Item1;
            Item.noMelee = true;
            Item.useTurn = true;
            Item.noUseGraphic = true;
        }

        public override void HoldItem(Player player)
        {
            Item.channel = true;
            player.Calamity().rightClickListener = true;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            // Don't create more spears if one is being aimed.
            foreach (Projectile spear in Utilities.AllProjectilesByID(Item.shoot))
            {
                if (spear.owner != player.whoAmI || spear.ModProjectile<PunctusProjectile>().CurrentState != PunctusProjectile.UseState.Aiming)
                    continue;

                return false;
            }

            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int useType = 0;
            if (player.altFunctionUse == 2)
                useType = 1;

            Projectile.NewProjectile(source, position, velocity.SafeNormalize(Vector2.UnitY), type, damage, knockback, player.whoAmI, useType);
            return false;
        }

        public override bool? CanHitNPC(Player player, NPC target) => false;

        public override bool CanHitPvp(Player player, Player target) => false;
    }
}
