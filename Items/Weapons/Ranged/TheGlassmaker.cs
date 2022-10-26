using CalamityMod.Items;
using InfernumMode.Projectiles.Ranged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Items.Weapons.Ranged
{
    public class TheGlassmaker : ModItem
    {
        internal static bool TransformsSandIntoGlass = false;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("The Glassmaker");
            Tooltip.SetDefault("70% chance to not consume gel");
            SacrificeTotal = 1;
        }

        public override void SetDefaults()
        {
            Item.damage = 49;
            Item.knockBack = 1f;
            Item.DamageType = DamageClass.Ranged;
            Item.autoReuse = true;
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.useAmmo = AmmoID.Gel;
            Item.shootSpeed = 5.6f;
            Item.shoot = ModContent.ProjectileType<GlassmakerFire>();

            Item.width = 64;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.UseSound = SoundID.Item34;
            Item.value = CalamityGlobalItem.Rarity9BuyPrice;
            Item.rare = ItemRarityID.Cyan;
        }

        public override Vector2? HoldoutOffset() => new Vector2(-4, 0);

        public override bool CanConsumeAmmo(Item ammo, Player player) => Main.rand.Next(100) >= 70;

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            position += velocity.SafeNormalize(Vector2.UnitX * player.direction) * Item.width * 0.8f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (Main.rand.NextBool(3))
                Projectile.NewProjectile(source, position, velocity.RotatedByRandom(0.43f) * 2.4f, ModContent.ProjectileType<GlassPiece>(), damage, knockback, player.whoAmI);

            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe().
                AddIngredient(ItemID.FlareGun).
                AddIngredient(ItemID.Ruby).
                AddIngredient(ItemID.Gel, 12).
                AddTile(TileID.Anvils).
                Register();
        }
    }
}
